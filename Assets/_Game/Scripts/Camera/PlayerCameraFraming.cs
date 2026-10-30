using Unity.Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineCamera), typeof(CinemachinePositionComposer))]
public sealed class PlayerCameraFraming : CinemachineExtension
{
    const float ZoomDistanceLimit = 15f;
    [SerializeField] Movement movement;
    [SerializeField, Range(0f, .45f)] float maximumScreenOffset = 1f / 6f;
    [SerializeField, Min(.01f)] float smoothingTime = .35f;
    [Header("Speed zoom")]
    [SerializeField, Range(.01f, ZoomDistanceLimit)] float maximumCameraDistance = ZoomDistanceLimit;
    [SerializeField, Min(.01f)] float zoomSmoothingTime = .5f;
    CinemachinePositionComposer composer;
    PlayerImpactFeedback impactFeedback;
    Vector2 screenOffset, smoothingVelocity;
    float zoomRatio, zoomVelocity;

    public Movement Movement => movement;

    void Reset()
    {
        Rigidbody player = GetComponentInParent<Rigidbody>();
        if (player != null) movement = player.GetComponentInChildren<Movement>(true);
    }
    protected override void Awake()
    {
        base.Awake();
        composer = GetComponent<CinemachinePositionComposer>();
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        screenOffset = smoothingVelocity = Vector2.zero;
        zoomRatio = zoomVelocity = 0f;
        impactFeedback = movement != null ? movement.GetComponentInParent<PlayerImpactFeedback>() : null;
    }
    void OnValidate()
    {
        maximumScreenOffset = Mathf.Clamp(maximumScreenOffset, 0f, .45f);
        smoothingTime = Mathf.Max(.01f, smoothingTime);
        zoomSmoothingTime = Mathf.Max(.01f, zoomSmoothingTime);
        float restingDistance = TryGetComponent<CinemachinePositionComposer>(out var body) ? body.CameraDistance : .01f;
        maximumCameraDistance = Mathf.Clamp(maximumCameraDistance, Mathf.Min(restingDistance, ZoomDistanceLimit), ZoomDistanceLimit);
    }
    public override float GetMaxDampTime() => Mathf.Max(smoothingTime, zoomSmoothingTime);

    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Finalize || vcam != ComponentOwner || !Application.isPlaying || vcam.Follow == null)
            return;
        if (composer == null && !TryGetComponent(out composer)) return;

        Quaternion orientation = state.GetFinalOrientation();
        float aspect = Mathf.Max(.0001f, state.Lens.Aspect);
        float speedRatio = movement != null && movement.isActiveAndEnabled ? movement.NormalizedSpeed : 0f;
        Vector2 desiredOffset = Vector2.zero;
        if (movement != null && movement.isActiveAndEnabled)
        {
            Vector3 facing = Vector3.ProjectOnPlane(movement.FacingDirection, state.ReferenceUp);
            Vector3 localFacing = Quaternion.Inverse(orientation) * facing;
            Vector2 direction = new Vector2(localFacing.x / aspect, localFacing.y);
            float largestAxis = Mathf.Max(Mathf.Abs(direction.x), Mathf.Abs(direction.y));
            if (largestAxis > .0001f)
                desiredOffset = direction / largestAxis * (maximumScreenOffset * speedRatio);
        }

        if (deltaTime < 0f || !vcam.PreviousStateIsValid)
        {
            screenOffset = desiredOffset;
            smoothingVelocity = Vector2.zero;
            zoomRatio = speedRatio;
            zoomVelocity = 0f;
        }
        else if (deltaTime > 0f)
        {
            screenOffset = Vector2.SmoothDamp(screenOffset, desiredOffset, ref smoothingVelocity,
                smoothingTime, Mathf.Infinity, deltaTime);
            zoomRatio = Mathf.Clamp01(Mathf.SmoothDamp(zoomRatio, speedRatio, ref zoomVelocity,
                zoomSmoothingTime, Mathf.Infinity, deltaTime));
        }

        Vector3 trackedPoint = vcam.Follow.position + vcam.Follow.rotation * composer.TargetOffset;
        float depth = Vector3.Dot(trackedPoint - state.GetFinalPosition(), orientation * Vector3.forward);
        if (depth <= 0f) return;
        depth = ApplySpeedZoom(ref state, orientation, depth);
        float halfHeight = state.Lens.Orthographic ? state.Lens.OrthographicSize
            : Mathf.Tan(state.Lens.FieldOfView * .5f * Mathf.Deg2Rad) * depth;

        state.PositionCorrection += orientation * new Vector3(
            screenOffset.x * halfHeight * aspect * 2f, screenOffset.y * halfHeight * 2f, 0f);
        if (impactFeedback != null)
        {
            Vector2 shake = impactFeedback.ShakeOffset;
            state.PositionCorrection += orientation * new Vector3(shake.x, shake.y, 0f);
        }
    }
    float ApplySpeedZoom(ref CameraState state, Quaternion orientation, float restingDepth)
    {
        float restingDistance = Mathf.Min(restingDepth, ZoomDistanceLimit);
        float maximumDepth = Mathf.Clamp(maximumCameraDistance, restingDistance, ZoomDistanceLimit);
        float distance = Mathf.Lerp(restingDistance, maximumDepth, zoomRatio);
        state.PositionCorrection -= orientation * Vector3.forward * (distance - restingDepth);
        if (state.Lens.Orthographic)
        {
            var lens = state.Lens;
            lens.OrthographicSize *= distance / restingDepth;
            state.Lens = lens;
        }
        return distance;
    }
}
