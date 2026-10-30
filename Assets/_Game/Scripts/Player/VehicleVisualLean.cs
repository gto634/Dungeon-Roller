using UnityEngine;

[DefaultExecutionOrder(10), DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody), typeof(VehicleTireTracks))]
public sealed class VehicleVisualLean : MonoBehaviour
{
    [SerializeField] Transform model;
    [SerializeField] Movement movement;
    [SerializeField] VehicleTireTracks tireTracks;
    [SerializeField, Range(0f, 45f)] float maximumRoll = 25f;
    [SerializeField, Min(.1f)] float fullRollLateralSpeed = 8f;
    [SerializeField, Min(0f)] float minimumLateralSpeed = .5f;
    [SerializeField, Min(.01f)] float response = 12f;
    [SerializeField, Min(.001f)] float maximumWheelLift = .04f;
    [SerializeField, Min(.001f)] float groundTolerance = .06f;
    [SerializeField, Min(.1f)] float teleportDistance = 8f;
    readonly RaycastHit[] rayHits = new RaycastHit[16];
    Rigidbody body;
    Vector3 restPosition, restScale, previousPosition;
    Quaternion restRotation;
    Vector3[] restContacts, modelContacts;
    RaycastHit[] groundHits;
    bool[] grounded;
    bool initialized;
    float roll;

    public float RollAngle => roll;
    public int WheelCount => restContacts != null ? restContacts.Length : 0;
    public Transform Model => model;
    void Awake() => Initialize();
    void OnEnable()
    {
        if (!Initialize()) return;
        ResetPose(); previousPosition = transform.position;
        tireTracks.ResetSampling();
    }
    void OnDisable()
    {
        ResetPose();
        if (tireTracks != null) tireTracks.ResetSampling();
    }
    bool Initialize()
    {
        if (initialized) return true;
        if (body == null) body = GetComponent<Rigidbody>();
        if (tireTracks == null) tireTracks = GetComponent<VehicleTireTracks>();
        if (movement == null) movement = GetComponentInChildren<Movement>(true);
        if (model == null || model == transform || body == null || tireTracks == null || tireTracks.WheelContactCount == 0) return false;
        restPosition = model.localPosition; restRotation = model.localRotation; restScale = model.localScale;
        int count = tireTracks.WheelContactCount;
        restContacts = new Vector3[count]; modelContacts = new Vector3[count];
        groundHits = new RaycastHit[count]; grounded = new bool[count];
        for (int i = 0; i < count; i++)
        {
            restContacts[i] = tireTracks.RestWheelContact(i);
            modelContacts[i] = model.InverseTransformPoint(transform.TransformPoint(restContacts[i]));
        }
        previousPosition = transform.position;
        initialized = true; return true;
    }
    void LateUpdate() { if (Application.isPlaying) UpdateVisuals(Time.deltaTime); }

    public void UpdateVisuals(float seconds)
    {
        if (!isActiveAndEnabled || !Initialize()) return;
        if (body.isKinematic || movement == null || !movement.isActiveAndEnabled)
        { ResetPose(); previousPosition = transform.position; return; }
        if (seconds <= 0f || (Application.isPlaying && Time.timeScale <= 0f)) return;
        if ((transform.position - previousPosition).sqrMagnitude > teleportDistance * teleportDistance)
        { ResetPose(); tireTracks.ResetSampling(); }
        previousPosition = transform.position;
        Vector3 forward = Vector3.ProjectOnPlane(movement.FacingDirection, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        float lateral = Vector3.Dot(body.linearVelocity, right);
        float longitudinal = Mathf.Abs(Vector3.Dot(body.linearVelocity, forward));
        float magnitude = Mathf.Abs(lateral);
        float target = magnitude > longitudinal && magnitude > minimumLateralSpeed
            ? -Mathf.Sign(lateral) * maximumRoll * Mathf.Clamp01((magnitude - longitudinal) / fullRollLateralSpeed) : 0f;
        roll = Mathf.Lerp(roll, target, 1f - Mathf.Exp(-response * seconds));
        if (Mathf.Abs(roll) < .001f) roll = 0f;
        ApplyPose(); RefreshGroundContacts();
    }
    void ApplyPose()
    {
        float edge = roll <= 0f ? float.NegativeInfinity : float.PositiveInfinity;
        for (int i = 0; i < restContacts.Length; i++)
            edge = roll <= 0f ? Mathf.Max(edge, restContacts[i].x) : Mathf.Min(edge, restContacts[i].x);
        Vector3 pivot = Vector3.zero; int count = 0;
        for (int i = 0; i < restContacts.Length; i++)
            if (Mathf.Abs(restContacts[i].x - edge) < .01f) { pivot += modelContacts[i]; count++; }
        pivot /= Mathf.Max(1, count);
        Vector3 scaledPivot = Vector3.Scale(restScale, pivot);
        Vector3 fixedPivot = restPosition + restRotation * scaledPivot;
        Quaternion rotation = restRotation * Quaternion.AngleAxis(roll, Vector3.forward);
        model.SetLocalPositionAndRotation(fixedPivot - rotation * scaledPivot, rotation);
    }
    public void ResetPose()
    {
        roll = 0f;
        if (!initialized) return;
        if (model != null) model.SetLocalPositionAndRotation(restPosition, restRotation);
        System.Array.Clear(grounded, 0, grounded.Length);
    }
    public Vector3 WheelContactWorld(int index)
        => initialized && index >= 0 && index < modelContacts.Length && model != null
            ? model.TransformPoint(modelContacts[index]) : transform.position;
    public bool IsWheelGrounded(int index)
        => initialized && index >= 0 && index < grounded.Length && grounded[index];
    public bool TryGetGroundHit(int index, out RaycastHit hit)
    {
        hit = default;
        if (!IsWheelGrounded(index)) return false;
        hit = groundHits[index]; return hit.collider != null;
    }
    void RefreshGroundContacts()
    {
        for (int wheel = 0; wheel < restContacts.Length; wheel++)
        {
            grounded[wheel] = false;
            Vector3 contact = WheelContactWorld(wheel);
            float lift = (transform.InverseTransformPoint(contact).y - restContacts[wheel].y) * transform.TransformVector(Vector3.up).magnitude;
            if (lift > maximumWheelLift) continue;
            const float rayHeight = .25f;
            int count = Physics.RaycastNonAlloc(contact + Vector3.up * rayHeight, Vector3.down, rayHits,
                rayHeight + groundTolerance, tireTracks.GroundLayerMask, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                if (rayHits[i].collider.transform.IsChildOf(transform) || rayHits[i].distance >= nearest) continue;
                groundHits[wheel] = rayHits[i]; nearest = rayHits[i].distance;
            }
            grounded[wheel] = nearest < float.PositiveInfinity && Vector3.Dot(groundHits[wheel].normal, Vector3.up) >= .5f;
        }
    }
}
