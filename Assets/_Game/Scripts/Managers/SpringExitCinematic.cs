using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SpringExitCinematic : MonoBehaviour
{
    [SerializeField] DungeonExit exit;
    [SerializeField] CinemachineCamera exitCamera;
    [SerializeField] AnimationClip flightClip;
    [SerializeField] Transform animationPose, compressionPose, springMotion;
    [SerializeField] Transform coil, plate, platePose, shakePose, cameraShakePose;
    [SerializeField] FollowTransformPosition platePosition;
    [SerializeField, Min(0f)] float launchTime = 3.2f;
    [SerializeField] Vector3 cameraOffset = new Vector3(2f, 4.6f, -6.5f);
    readonly List<Behaviour> suspended = new List<Behaviour>();
    readonly List<BrainState> brains = new List<BrainState>();
    Renderer[] plateRenderers;
    Rigidbody player;
    Action completed, cancelled;
    Vector3 savedPosition, modelPosition, coilScale, plateScale, launchPoint, playerCenter, cameraPosition;
    Quaternion heading, savedRotation, modelRotation, cameraYaw;
    RigidbodyConstraints constraints;
    RigidbodyInterpolation interpolation;
    CollisionDetectionMode collisionMode;
    bool wasKinematic, detectedCollisions, ownsPause, rigCaptured;
    float elapsed, previousTimeScale, feetOffset;

    struct BrainState
    {
        public CinemachineBrain Brain;
        public CinemachineBlendDefinition Blend;
        public CinemachineBlenderSettings CustomBlends;
        public CinemachineBrain.LensModeOverrideSettings LensMode;
        public CinemachineBrain.UpdateMethods UpdateMethod;
        public CinemachineBrain.BrainUpdateMethods BlendUpdateMethod;
        public bool IgnoreTimeScale;
    }
    DungeonExit Exit => exit != null ? exit : exit = GetComponentInParent<DungeonExit>(true);
    public bool IsPlaying { get; private set; }
    public CinemachineCamera ExitCamera => exitCamera;
    public AnimationClip FlightClip => flightClip;
    public Transform Coil => coil;
    public Transform Plate => plate;
    public FollowTransformPosition PlatePosition => platePosition;
    public float LaunchTime => launchTime;
    public float Elapsed => elapsed;

    public void PlaceCamera(Room room)
    {
        if (exitCamera == null || Exit == null || room == null || !room.IsInitialized) return;
        Vector3 center = room.transform.TransformPoint(new Vector3(room.Data.Bounds.width * .5f, 0f, room.Data.Bounds.height * .5f));
        Vector3 inward = Vector3.ProjectOnPlane(center - Exit.transform.position, Vector3.up);
        if (inward.sqrMagnitude < .01f) inward = room.transform.forward;
        heading = Quaternion.LookRotation(inward.normalized, Vector3.up);
        cameraPosition = Exit.transform.position + heading * cameraOffset;
        Vector3 forward = Vector3.ProjectOnPlane(Exit.transform.position - cameraPosition, Vector3.up).normalized;
        cameraYaw = Quaternion.LookRotation(forward, Vector3.up);
        Vector3 target = Exit.transform.position + Vector3.up * 1.4f;
        exitCamera.transform.SetPositionAndRotation(cameraPosition, Quaternion.LookRotation(target - cameraPosition, Vector3.up));
    }

    internal bool Play(Rigidbody actor, Action onCompleted, Action onCancelled)
    {
        if (!isActiveAndEnabled || IsPlaying || actor == null || exitCamera == null || flightClip == null
            || animationPose == null || compressionPose == null || springMotion == null || Exit == null || Exit.Volume == null
            || coil == null || plate == null || platePose == null || shakePose == null || cameraShakePose == null || platePosition == null) return false;
        actor.GetComponent<PlayerImpactFeedback>()?.CancelFreeze();
        if (Time.timeScale <= 0f) return false;
        player = actor; completed = onCompleted; cancelled = onCancelled;
        savedPosition = actor.position; savedRotation = actor.rotation;
        constraints = actor.constraints; interpolation = actor.interpolation;
        collisionMode = actor.collisionDetectionMode;
        wasKinematic = actor.isKinematic; detectedCollisions = actor.detectCollisions;
        modelPosition = springMotion.localPosition; modelRotation = springMotion.localRotation;
        coilScale = coil.localScale; plateScale = plate.localScale;
        rigCaptured = true;
        plateRenderers = plate.GetComponentsInChildren<Renderer>(true);
        Bounds shape = DungeonSpawnPlacement.PlayerBounds(actor);
        playerCenter = shape.center;
        feetOffset = Mathf.Max(0f, -shape.min.y * Mathf.Abs(actor.transform.lossyScale.y)) + .06f;
        ApplyRig(Mathf.Clamp(launchTime, 0f, flightClip.length));
        launchPoint = PlateSurface() + Vector3.up * feetOffset;
        foreach (Movement movement in actor.GetComponentsInChildren<Movement>(true)) { movement.Stop(); Suspend(movement); }
        foreach (InputControl input in actor.GetComponentsInChildren<InputControl>(true)) Suspend(input);
        Suspend(actor.GetComponent<VehicleTireTracks>());
        Suspend(actor.GetComponent<PlayerImpactFeedback>());
        if (!wasKinematic) actor.linearVelocity = actor.angularVelocity = Vector3.zero;
        actor.collisionDetectionMode = CollisionDetectionMode.Discrete;
        actor.isKinematic = true; actor.detectCollisions = false;
        actor.constraints = RigidbodyConstraints.None; actor.interpolation = RigidbodyInterpolation.None;
        previousTimeScale = Time.timeScale; ownsPause = true; Time.timeScale = 0f;
        IsPlaying = true; elapsed = 0f;
        Sample(0f);
        CutToCamera();
        return true;
    }

    void Suspend(Behaviour behaviour)
    {
        if (behaviour == null || !behaviour.enabled) return;
        suspended.Add(behaviour); behaviour.enabled = false;
    }
    void CutToCamera()
    {
        foreach (var brain in FindObjectsByType<CinemachineBrain>(FindObjectsSortMode.None))
        {
            if (((uint)brain.ChannelMask & (uint)exitCamera.OutputChannel) == 0) continue;
            brains.Add(new BrainState { Brain = brain, Blend = brain.DefaultBlend, CustomBlends = brain.CustomBlends,
                IgnoreTimeScale = brain.IgnoreTimeScale, LensMode = brain.LensModeOverride,
                UpdateMethod = brain.UpdateMethod, BlendUpdateMethod = brain.BlendUpdateMethod });
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
            brain.CustomBlends = null; brain.IgnoreTimeScale = true;
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            brain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;
            var mode = brain.LensModeOverride; mode.Enabled = true; brain.LensModeOverride = mode;
        }
        exitCamera.Priority = int.MaxValue;
        exitCamera.gameObject.SetActive(true);
        exitCamera.PreviousStateIsValid = false;
    }
    void Update()
    {
        if (!IsPlaying) return;
        if (player == null) { Cancel(); return; }
        elapsed = Mathf.Min(flightClip.length, elapsed + Time.unscaledDeltaTime);
        Sample(elapsed);
        if (elapsed < flightClip.length) return;
        IsPlaying = false;
        RestorePlayer(false);
        RestorePause();
        Action callback = completed; completed = cancelled = null;
        callback?.Invoke();
    }
    void LateUpdate()
    {
        if (!IsPlaying) return;
        if (ownsPause) Time.timeScale = 0f;
        AimCamera();
    }
    void ApplyRig(float time)
    {
        flightClip.SampleAnimation(animationPose.gameObject, time);
        springMotion.SetLocalPositionAndRotation(modelPosition + shakePose.localPosition, modelRotation * shakePose.localRotation);
        coil.localScale = Vector3.Scale(coilScale, new Vector3(1f, compressionPose.localScale.y, 1f));
        plate.localScale = Vector3.Scale(plateScale, platePose.localScale);
        platePosition.Apply();
    }
    Vector3 PlateSurface()
    {
        Vector3 point = plate.position;
        float top = point.y;
        foreach (var renderer in plateRenderers)
            if (renderer != null) top = Mathf.Max(top, renderer.bounds.max.y);
        point.y = top;
        return point;
    }
    void Sample(float time)
    {
        ApplyRig(time);
        Vector3 origin = time < launchTime ? PlateSurface() + Vector3.up * feetOffset : launchPoint;
        Vector3 position = origin + heading * animationPose.localPosition;
        Quaternion rotation = heading * animationPose.localRotation;
        player.position = position; player.rotation = rotation;
        player.transform.SetPositionAndRotation(position, rotation);
        AimCamera();
    }
    void AimCamera()
    {
        if (player == null || exitCamera == null) return;
        Vector3 position = cameraPosition + heading * cameraShakePose.localPosition;
        Vector3 target = player.transform.TransformPoint(playerCenter);
        Vector3 offset = target - position;
        float forward = Mathf.Max(.05f, Vector3.Dot(offset, cameraYaw * Vector3.forward));
        float pitch = Mathf.Clamp(-Mathf.Atan2(offset.y, forward) * Mathf.Rad2Deg, -82f, 68f);
        Quaternion rotation = cameraYaw * Quaternion.Euler(pitch, 0f, 0f) * cameraShakePose.localRotation;
        exitCamera.transform.SetPositionAndRotation(position, rotation);
    }
    void RestoreRig()
    {
        if (!rigCaptured) return;
        if (springMotion != null) springMotion.SetLocalPositionAndRotation(modelPosition, modelRotation);
        if (coil != null) coil.localScale = coilScale;
        if (plate != null) plate.localScale = plateScale;
        if (platePosition != null) platePosition.Apply();
        rigCaptured = false;
    }
    void RestorePlayer(bool restorePose)
    {
        if (player != null)
        {
            if (restorePose) { player.position = savedPosition; player.rotation = savedRotation; player.transform.SetPositionAndRotation(savedPosition, savedRotation); }
            player.constraints = constraints; player.interpolation = interpolation;
            player.isKinematic = wasKinematic; player.detectCollisions = detectedCollisions;
            player.collisionDetectionMode = collisionMode;
            if (!wasKinematic) player.linearVelocity = player.angularVelocity = Vector3.zero;
            player.GetComponent<VehicleTireTracks>()?.ResetSampling();
        }
        foreach (Behaviour behaviour in suspended) if (behaviour != null) behaviour.enabled = true;
        suspended.Clear();
        RestoreRig();
        player = null;
    }
    void RestorePause()
    {
        if (!ownsPause) return;
        if (Time.timeScale == 0f) Time.timeScale = previousTimeScale;
        ownsPause = false;
    }
    internal void Cancel()
    {
        bool wasPlaying = IsPlaying; IsPlaying = false;
        if (wasPlaying) { RestorePlayer(true); RestorePause(); }
        if (exitCamera != null) exitCamera.gameObject.SetActive(false);
        foreach (var state in brains)
        {
            if (state.Brain == null) continue;
            state.Brain.DefaultBlend = state.Blend; state.Brain.CustomBlends = state.CustomBlends;
            state.Brain.IgnoreTimeScale = state.IgnoreTimeScale; state.Brain.LensModeOverride = state.LensMode;
            state.Brain.UpdateMethod = state.UpdateMethod; state.Brain.BlendUpdateMethod = state.BlendUpdateMethod;
        }
        brains.Clear();
        Action callback = cancelled; completed = cancelled = null;
        if (wasPlaying) callback?.Invoke();
    }
    void OnDisable() => Cancel();
}
