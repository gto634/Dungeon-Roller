using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _transform;
    [SerializeField] Stats _stats;
    [SerializeField] Rigidbody _character;

    [Header("Configuration")]
    [SerializeField] private float _rotationSpeed;

    [SerializeField, Min(0f)] float accelerationPerPoint = 10f;

    [Header("Friction")]
    [SerializeField, Min(0f)] float rollingFriction = .5f;
    [SerializeField, Min(0f)] float lateralFriction = 18f;

    [Header("Steering and drift")]
    [SerializeField, Min(.1f)] float steeringSpeedReference = 20f;
    [SerializeField, Min(.1f)] float fullSteeringSpeed = 3f;
    [SerializeField, Min(0f)] float handlingBonusPerPoint = .2f;
    [SerializeField, Min(0f)] float driftLateralFriction = .7f;
    [SerializeField, Range(1f, 2f)] float driftSteeringMultiplier = 1.25f;
    [SerializeField, Min(0f)] float minimumDriftSpeed = 2f;

    [Header("Wall collisions")]
    [SerializeField, Min(0f)] float wallAlignmentSpeed = 180f;
    Vector3 wallForward, wallNormal;
    float wallTravelSign;
    bool aligningWithWall;

    [Header("Nitro")]
    [SerializeField, Min(1f)] float nitroSpeedMultiplier = 2f;
    [SerializeField, Min(1f)] float nitroAccelerationMultiplier = 2f;
    [SerializeField, Min(.01f)] float nitroDrainPerSecond = 10f;
    [SerializeField, Min(.01f)] float nitroRechargePerSecond = 5f;

    [Header("Inputs")]
    [SerializeField] InputActionReference _nitroBoost;

    Vector3 _direction;
    float _speed;
    float _maxSpeed;
    float _maxNitro;
    float _nitro;
    float _maniability = 1f;
    bool _wantToDrift;
    bool _nitroBoosting;
    bool _nitroWait = false;

    public float Speed => _speed;
    public float CurrentSpeed => CurrentVelocity.magnitude;
    public float Acceleration => CurrentSpeed;
    public float NormalizedSpeed => _maxSpeed > 0f ? Mathf.Clamp01(CurrentSpeed / _maxSpeed) : 0f;
    public Vector3 FacingDirection => _transform != null ? _transform.forward : transform.forward;
    public Vector3 CurrentVelocity
    {
        get
        {
            if (_character == null) return Vector3.zero;
            return _character.linearVelocity;
        }
    }
    public float MaximumSpeed => _maxSpeed;
    public event System.Action OnNitroActivated;
    public event System.Action OnNitroDeactivated;
    public event System.Action NitroChanged;
    public bool IsNitroBoosting => _nitroBoosting;
    public bool CanActivateNitro => _maxNitro > 0f && _nitro > 0f && !_nitroWait;
    public float MaximumNitro => _maxNitro;
    public float CurrentNitro => _nitro;
    public float Maniability => _maniability;
    public bool WantToDrift { get => _wantToDrift; set => _wantToDrift = value; }
    public bool IsDrifting => _wantToDrift && isActiveAndEnabled && Time.timeScale > 0f
        && _character != null && !_character.isKinematic
        && Vector3.ProjectOnPlane(CurrentVelocity, Vector3.up).sqrMagnitude >= minimumDriftSpeed * minimumDriftSpeed;

    void Awake()
    {
        if (_character == null) _character = GetComponentInParent<Rigidbody>();
        if (_character != null)
        {
            _character.useGravity = false;
            _character.linearDamping = 0f;
            _character.constraints |= RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationY;
        }
        if (_transform != null) return;
        _transform = transform;
        if (_character != null) _transform = _character.transform;
    }

    void OnDisable() { aligningWithWall = false; _wantToDrift = false; SetNitroBoosting(false); }

    public void Stop()
    {
        aligningWithWall = false;
        _direction = Vector3.zero;
        _wantToMove = _wantToBack = _wantToDrift = false;
        SetNitroBoosting(false);
        if (_character == null || _character.isKinematic) return;
        _character.linearVelocity = Vector3.zero;
        _character.angularVelocity = Vector3.zero;
    }

    private void Start()
    {
        _speed = _stats.Speed;
        _maxSpeed = _stats.MaxSpeed;
        _maxNitro = _stats.MaxNitro;
        _nitro = Mathf.Clamp(_stats.Nitro, 0f, _maxNitro);
        _maniability = Mathf.Max(0f, _stats.Maniability);
        NitroChanged?.Invoke();
    }

    public Vector3 Direction
    {
        get => _direction;
        set => _direction = value;
    }

    bool _wantToMove;
    public bool WantToMove
    {
        get => _wantToMove;
        set => _wantToMove = value;
    }
    bool _wantToBack;
    public bool WantToBack
    {
        get => _wantToBack;
        set => _wantToBack = value;
    }

    void Update()
    {
        if (Time.timeScale <= 0f)
        {
            SetNitroBoosting(false);
            return;
        }
        bool requested = _nitroBoost != null && _nitroBoost.action != null && _nitroBoost.action.ReadValue<float>() > .5f;
        UpdateNitro(Time.deltaTime, requested);
    }

    void SetNitroBoosting(bool boosting)
    {
        if (_nitroBoosting == boosting) return;
        _nitroBoosting = boosting;
        if (boosting) OnNitroActivated?.Invoke();
        else OnNitroDeactivated?.Invoke();
    }

    void UpdateNitro(float seconds, bool requested)
    {
        float previous = _nitro;
        if (_maxNitro <= 0f)
        {
            _nitro = 0f;
            _nitroWait = false;
            SetNitroBoosting(false);
            if (_nitro != previous) NitroChanged?.Invoke();
            return;
        }
        if (!requested || _nitroWait)
            _nitro = Mathf.Min(_maxNitro, _nitro + Stats.FromPoints(nitroRechargePerSecond) * seconds);
        if (_nitroWait && _nitro >= _maxNitro) _nitroWait = false;
        bool boosting = requested && CanActivateNitro;
        if (boosting)
        {
            _nitro = Mathf.Max(0f, _nitro - Stats.FromPoints(nitroDrainPerSecond) * seconds);
            if (_nitro <= 0f)
            {
                _nitroWait = true;
                boosting = false;
            }
        }
        SetNitroBoosting(boosting);
        if (_nitro != previous) NitroChanged?.Invoke();
    }

    void ApplySteering(float seconds)
    {
        float planarSpeed = Vector3.ProjectOnPlane(_character.linearVelocity, Vector3.up).magnitude;
        float reference = Mathf.Max(.1f, steeringSpeedReference * (1f + _maniability * handlingBonusPerPoint));
        float ratio = planarSpeed / reference;
        float factor = Mathf.Clamp01(planarSpeed / Mathf.Max(.1f, fullSteeringSpeed)) / (1f + ratio * ratio);
        if (IsDrifting) factor *= driftSteeringMultiplier;
        float toRotate = _direction.x * _rotationSpeed * seconds * factor;
        int drive = (_wantToMove ? 1 : 0) - (_wantToBack ? 1 : 0);
        float rollingSpeed = Vector3.Dot(_character.linearVelocity, FacingDirection);
        int steeringDirection = drive != 0 ? drive : rollingSpeed < 0f ? -1 : 1;
        Vector3 steeredForward = Quaternion.AngleAxis(toRotate * steeringDirection, Vector3.up) * FacingDirection;
        if (aligningWithWall && (wallAlignmentSpeed <= 0f
            || (drive != 0 && Vector3.Dot(FacingDirection * drive, wallNormal) > .05f)
            || (Mathf.Abs(_direction.x) > .001f
                && Vector3.Dot(steeredForward * wallTravelSign, wallNormal)
                > Vector3.Dot(FacingDirection * wallTravelSign, wallNormal) + .00001f)))
            aligningWithWall = false;
        if (aligningWithWall)
        {
            Quaternion target = Quaternion.LookRotation(wallForward, Vector3.up);
            _transform.rotation = Quaternion.RotateTowards(_transform.rotation, target, wallAlignmentSpeed * seconds);
            if (Quaternion.Angle(_transform.rotation, target) <= .1f)
            {
                _transform.rotation = target;
                aligningWithWall = false;
            }
        }
        else if (drive != 0 || planarSpeed > .1f)
        {
            Quaternion turn = Quaternion.AngleAxis(toRotate * steeringDirection, Vector3.up);
            Vector3 nextForward = Vector3.ProjectOnPlane(turn * FacingDirection, Vector3.up).normalized;
            Quaternion facing = Quaternion.LookRotation(nextForward, Vector3.up);
            _transform.rotation = facing;
            if (_transform == _character.transform) _character.rotation = facing;
            if (!IsDrifting)
            {
                Vector3 vertical = Vector3.Project(_character.linearVelocity, Vector3.up);
                Vector3 planar = Vector3.ProjectOnPlane(_character.linearVelocity, Vector3.up);
                _character.linearVelocity = turn * planar + vertical;
            }
        }
    }

    internal bool IsAligningWithWall => aligningWithWall;
    internal void CancelWallDeflection() => aligningWithWall = false;

    internal bool DeflectAlongWall(Vector3 normal, Vector3 incomingVelocity)
    {
        if (!isActiveAndEnabled || wallAlignmentSpeed <= 0f) return false;
        int drive = (_wantToMove ? 1 : 0) - (_wantToBack ? 1 : 0);
        float travelSign = drive != 0 ? drive : Vector3.Dot(incomingVelocity, FacingDirection) < 0f ? -1f : 1f;
        if (Vector3.Dot(FacingDirection * travelSign, normal) >= -.001f) return false;
        if (drive == 0 && Mathf.Max(incomingVelocity.sqrMagnitude, CurrentVelocity.sqrMagnitude) < .01f) return false;
        if (aligningWithWall && Vector3.Dot(wallNormal, normal) > .99f) return true;
        Vector3 tangent = Vector3.Cross(Vector3.up, normal).normalized;
        float along = Vector3.Dot(FacingDirection, tangent);
        if (Mathf.Abs(along) < .001f) along = Vector3.Dot(incomingVelocity * travelSign, tangent);
        wallForward = along >= 0f ? tangent : -tangent;
        wallNormal = normal;
        wallTravelSign = travelSign;
        aligningWithWall = Vector3.Angle(FacingDirection, wallForward) > .1f;
        return true;
    }

    void ApplyDrive(float seconds, Vector3 forward, Vector3 rollingVelocity, float rollingLoss)
    {
        int drive = (_wantToMove ? 1 : 0) - (_wantToBack ? 1 : 0);
        if (drive == 0 || _speed <= 0f || _maxSpeed <= 0f || accelerationPerPoint <= 0f) return;
        float limit = _maxSpeed * (_nitroBoosting ? nitroSpeedMultiplier : 1f);
        float signedSpeed = Vector3.Dot(_character.linearVelocity, forward) * drive;
        if (signedSpeed > limit + .001f) return;
        float acceleration = _speed * accelerationPerPoint * (_nitroBoosting ? nitroAccelerationMultiplier : 1f);
        float change = Mathf.Clamp(limit - signedSpeed, 0f, acceleration * seconds);
        Vector3 engine = forward * (drive * change);
        if (signedSpeed >= 0f) engine += rollingVelocity * rollingLoss;
        _character.AddForce(engine, ForceMode.VelocityChange);
    }

    void FixedUpdate()
    {
        if (_character == null || _character.isKinematic || Time.timeScale <= 0f) return;
        ApplySteering(Time.fixedDeltaTime);
        Vector3 forward = Vector3.ProjectOnPlane(FacingDirection, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 velocity = _character.linearVelocity;
        Vector3 rollingVelocity = forward * Vector3.Dot(velocity, forward);
        Vector3 lateralVelocity = right * Vector3.Dot(velocity, right);
        float rollingLoss = 1f - Mathf.Exp(-Mathf.Max(0f, rollingFriction) * Time.fixedDeltaTime);
        float lateralLoss = 1f - Mathf.Exp(-Mathf.Max(0f, IsDrifting ? driftLateralFriction : lateralFriction) * Time.fixedDeltaTime);
        _character.AddForce(-rollingVelocity * rollingLoss - lateralVelocity * lateralLoss, ForceMode.VelocityChange);
        ApplyDrive(Time.fixedDeltaTime, forward, rollingVelocity, rollingLoss);
    }

    public void AddManiability(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f) throw new System.ArgumentOutOfRangeException(nameof(value));
        _maniability += value;
    }

    public void AddSpeed(int v) => AddSpeed((float)v);
    public void AddSpeed(float v)
    {
        _speed += v;
    }

    public void AddMaxSpeed(int v) => AddMaxSpeed((float)v);
    public void AddMaxSpeed(float v)
    {
        _maxSpeed += v;
    }

    public void AddMaxNitro(int v) => AddMaxNitro((float)v);
    public void AddMaxNitro(float v)
    {
        if (float.IsNaN(v) || float.IsInfinity(v) || v < 0f) throw new System.ArgumentOutOfRangeException(nameof(v));
        _maxNitro += v;
        _nitro = Mathf.Min(_maxNitro, _nitro + v);
        if (_nitro > 0f) _nitroWait = false;
        if (v > 0f) NitroChanged?.Invoke();
    }
}
