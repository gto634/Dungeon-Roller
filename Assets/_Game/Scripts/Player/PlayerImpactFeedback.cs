using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerImpactFeedback : MonoBehaviour
{
    [SerializeField, Min(0f)] float maximumShakeAmplitude = .09f;
    [SerializeField, Min(.01f)] float shakeDuration = .18f;
    [SerializeField, Min(0f)] float minimumFreezeDuration = .025f, maximumFreezeDuration = .055f;
    [Header("Nitro")]
    [SerializeField, Min(0f)] float nitroShakeAmplitude = .03f;
    [Header("Wall contacts")]
    [SerializeField] bool automaticWallFeedback = true;
    [SerializeField, Min(0f)] float maximumWallShakeAmplitude = .22f;
    [SerializeField, Min(.01f)] float fullWallImpactSpeed = 10f;
    [SerializeField, Min(.001f)] float fullWallPenetration = .12f;
    [SerializeField, Range(0f, .3f)] float scrapingStrength = .1f;
    [SerializeField, Min(.01f)] float minimumWallMovementSpeed = .1f;
    [SerializeField, Min(.01f)] float wallShakeReleaseDuration = .14f;
    float shakeStarted, shakeStrength, shakePhase, freezeUntil, previousTimeScale;
    Rigidbody body;
    Vector3 previousPhysicsPosition, observedVelocity, incomingBodyVelocity;
    float wallShakeStarted, wallShakeStrength, wallShakePhase;
    bool frozen, nitroShaking;

    void Awake() => body = GetComponent<Rigidbody>();
    void OnEnable()
    {
        previousPhysicsPosition = transform.position;
        observedVelocity = incomingBodyVelocity = Vector3.zero;
        wallShakePhase = (GetInstanceID() & 1023) * .618f;
    }
    void FixedUpdate()
    {
        observedVelocity = (transform.position - previousPhysicsPosition) / Time.fixedDeltaTime;
        previousPhysicsPosition = transform.position;
        incomingBodyVelocity = body != null ? body.linearVelocity : Vector3.zero;
    }

    public Vector2 ShakeOffset
    {
        get
        {
            float age = Time.unscaledTime - shakeStarted;
            float envelope = Mathf.Clamp01(1f - age / Mathf.Max(.01f, shakeDuration));
            float amplitude = maximumShakeAmplitude * shakeStrength * envelope * envelope;
            Vector2 offset = new Vector2(Mathf.Sin(age * 113f + shakePhase), Mathf.Cos(age * 137f + shakePhase)) * amplitude;
            if (Time.timeScale <= 0f) return offset;
            float wallAge = Time.unscaledTime - wallShakeStarted;
            float wallEnvelope = Mathf.Clamp01(1f - wallAge / Mathf.Max(.01f, wallShakeReleaseDuration));
            float wallAmplitude = maximumWallShakeAmplitude * wallShakeStrength * wallEnvelope * wallEnvelope;
            float phase = Time.unscaledTime * 79f + wallShakePhase;
            offset += new Vector2(Mathf.Sin(phase), Mathf.Cos(phase * 1.27f + 1.3f)) * wallAmplitude;
            if (nitroShaking)
            {
                float time = Time.unscaledTime;
                offset += new Vector2(Mathf.Sin(time * 97f + wallShakePhase), Mathf.Cos(time * 127f + wallShakePhase)) * nitroShakeAmplitude;
            }
            return offset;
        }
    }

    public void Play(float normalizedImpactSpeed)
    {
        if (!Application.isPlaying || !isActiveAndEnabled || (Time.timeScale <= 0f && !frozen)) return;
        float strength = Mathf.Lerp(.15f, 1f, Mathf.Clamp01(normalizedImpactSpeed));
        float age = Time.unscaledTime - shakeStarted;
        float remaining = Mathf.Clamp01(1f - age / Mathf.Max(.01f, shakeDuration));
        shakeStrength = Mathf.Max(shakeStrength * remaining * remaining, strength);
        shakeStarted = Time.unscaledTime;
        shakePhase = Random.Range(0f, Mathf.PI * 2f);

        float duration = Mathf.Lerp(minimumFreezeDuration, maximumFreezeDuration, Mathf.Clamp01(normalizedImpactSpeed));
        if (duration <= 0f) return;
        if (!frozen) { previousTimeScale = Time.timeScale; frozen = true; Time.timeScale = 0f; }
        freezeUntil = Mathf.Max(freezeUntil, Time.unscaledTime + duration);
    }

    void Update()
    {
        if (frozen && (Time.unscaledTime >= freezeUntil || Time.timeScale > 0f)) CancelFreeze();
        if (Time.timeScale <= 0f)
        {
            nitroShaking = false;
            wallShakeStrength = 0f;
            previousPhysicsPosition = transform.position;
            observedVelocity = incomingBodyVelocity = Vector3.zero;
        }
    }

    public void PlayWallDeflection(float impactSpeed)
    {
        if (!Application.isPlaying || !isActiveAndEnabled || Time.timeScale <= 0f || impactSpeed < minimumWallMovementSpeed) return;
        StartWallShake(Mathf.Clamp01(impactSpeed / Mathf.Max(.01f, fullWallImpactSpeed)));
    }

    public void StartNitroShake()
    {
        if (Application.isPlaying && isActiveAndEnabled && Time.timeScale > 0f) nitroShaking = true;
    }
    public void StopNitroShake() => nitroShaking = false;

    void OnCollisionEnter(Collision collision) { if (automaticWallFeedback) UpdateWallContact(collision, true); }
    void OnCollisionStay(Collision collision) { if (automaticWallFeedback) UpdateWallContact(collision, false); }

    void UpdateWallContact(Collision collision, bool entering)
    {
        if (!Application.isPlaying || !isActiveAndEnabled || Time.timeScale <= 0f) return;
        Collider other = collision.collider;
        if (other.attachedRigidbody != null && !other.attachedRigidbody.isKinematic) return;
        if (other.GetComponentInParent<RoomVisual>() == null && other.GetComponentInParent<CorridorVisual>() == null) return;
        float strength = 0f;
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            float penetration = Mathf.Max(0f, -contact.separation);
            Vector3 velocity = observedVelocity;
            if (!entering)
            {
                Vector3 wallNormal = Vector3.ProjectOnPlane(contact.normal, Vector3.up).normalized;
                velocity = Vector3.ProjectOnPlane(velocity, wallNormal);
                penetration = 0f;
            }
            strength = Mathf.Max(strength, CalculateWallStrength(velocity, contact.normal, penetration));
            if (entering)
            {
                strength = Mathf.Max(strength, CalculateWallStrength(incomingBodyVelocity, contact.normal, penetration));
                strength = Mathf.Max(strength, CalculateWallStrength(collision.relativeVelocity, contact.normal, penetration));
            }
        }
        if (strength <= 0f) return;
        StartWallShake(strength);
    }

    void StartWallShake(float strength)
    {
        float remaining = Mathf.Clamp01(1f - (Time.unscaledTime - wallShakeStarted) / Mathf.Max(.01f, wallShakeReleaseDuration));
        wallShakeStrength = Mathf.Max(wallShakeStrength * remaining * remaining, strength);
        wallShakeStarted = Time.unscaledTime;
    }

    float CalculateWallStrength(Vector3 velocity, Vector3 normal, float penetration)
    {
        if (Mathf.Abs(normal.y) > .5f) return 0f;
        velocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
        float speed = velocity.magnitude;
        if (speed < minimumWallMovementSpeed) return 0f;
        normal = Vector3.ProjectOnPlane(normal, Vector3.up).normalized;
        float normalSpeed = Vector3.Dot(velocity, normal);
        float inwardSpeed = Mathf.Max(0f, -normalSpeed);
        float tangentSpeed = (velocity - normal * normalSpeed).magnitude;
        float incidence = inwardSpeed / speed;
        float impact = incidence * Mathf.Max(Mathf.Clamp01(inwardSpeed / Mathf.Max(.01f, fullWallImpactSpeed)),
            Mathf.Clamp01(penetration / Mathf.Max(.001f, fullWallPenetration)));
        float scrape = scrapingStrength * Mathf.Clamp01(tangentSpeed / Mathf.Max(.01f, fullWallImpactSpeed * .2f));
        return Mathf.Clamp01(Mathf.Max(impact, scrape));
    }

    public void CancelFreeze()
    {
        if (!frozen) return;
        frozen = false;
        if (Time.timeScale == 0f) Time.timeScale = previousTimeScale;
    }

    void OnDisable()
    {
        CancelFreeze();
        shakeStrength = 0f;
        nitroShaking = false;
        wallShakeStrength = 0f;
        observedVelocity = incomingBodyVelocity = Vector3.zero;
    }
}
