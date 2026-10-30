using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public sealed class ExperienceOrb : MonoBehaviour
{
    [SerializeField, Min(0f)] float pickupDelay = .4f;
    [SerializeField, Min(.1f)] float attractionRadius = 15f;
    [SerializeField, Min(.1f)] float attractionSpeed = 18f;
    [SerializeField, Min(.1f)] float maximumAttractionSpeed = 50f;
    [SerializeField, Min(.1f)] float minimumAttractionSpeed = 4f;
    [SerializeField, Min(.1f)] float attractionAcceleration = 55f;
    [SerializeField, Min(1f)] float playerSpeedMultiplier = 1.2f;
    [SerializeField, Min(.05f)] float pickupDistance = .35f;
    [Header("Physical projection")]
    [SerializeField, Min(1f)] float gravityMultiplier = 1.6f;
    [SerializeField, Range(0f, .9f)] float bounceRestitution = .55f;
    [SerializeField, Range(0f, 1f)] float bounceHorizontalRetention = .72f;
    [SerializeField, Min(0f)] float minimumBounceSpeed = .7f;
    [SerializeField, Min(0f)] float bouncePresentationTime = .35f;
    [SerializeField, Min(0f)] float maximumScatterDuration = 1.1f;
    [Header("Squash and stretch")]
    [SerializeField] Transform visuals;
    [SerializeField, Min(.01f)] float visualBounceDuration = .22f;
    [SerializeField, Range(0f, .6f)] float impactSquash = .45f;
    [SerializeField, Range(0f, .5f)] float reboundStretch = .25f;
    [SerializeField, Range(0f, .4f)] float maximumFlightStretch = .2f;
    Rigidbody body, player;
    SphereCollider sphere;
    Movement movement;
    LevelManager experience;
    GameManager run;
    float reward, age, currentAttractionSpeed;
    float firstBounceAge = -1f, visualBounceStarted = -1f, bounceStrength;
    float lastBounceAge = -1f, worldRadius;
    Vector3 baseVisualScale = Vector3.one, baseVisualPosition;
    bool initialized, attracting, collected;

    public float ExperienceReward => reward;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        sphere = GetComponent<SphereCollider>();
        Vector3 scale = transform.lossyScale;
        worldRadius = sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        if (visuals != null) { baseVisualScale = visuals.localScale; baseVisualPosition = visuals.localPosition; }
    }

    public void Initialize(Rigidbody target, LevelManager system, float amount, Vector3 launchVelocity)
    {
        if (initialized) return;
        player = target;
        experience = system;
        reward = amount;
        GameManager game = GameManager.Instance;
        if (game != null && game.Rooms != null && target != null
            && game.Rooms.Player == target.transform && game.ExperienceSystem == system)
            run = game;
        movement = player != null ? player.GetComponentInChildren<Movement>() : null;
        initialized = true;
        body.linearVelocity = launchVelocity;
        if (player != null)
            foreach (Collider collider in player.GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(sphere, collider);
    }

    void FixedUpdate()
    {
        if (!initialized || collected) return;
        if (player == null || experience == null) { Destroy(gameObject); return; }
        if (run != null && (!run.isActiveAndEnabled || run.IsGameEnded
            || run.PointManager == null || !run.PointManager.isActiveAndEnabled)) return;
        float playerSpeed = Mathf.Max(player.linearVelocity.magnitude,
            movement != null && movement.isActiveAndEnabled ? movement.CurrentSpeed : 0f);
        age += Time.fixedDeltaTime;
        if (!attracting && body.useGravity)
            body.AddForce(Physics.gravity * (Mathf.Max(1f, gravityMultiplier) - 1f), ForceMode.Acceleration);
        if (!attracting) PredictGroundContact();
        if (age < pickupDelay || !experience.isActiveAndEnabled || !player.gameObject.activeInHierarchy) return;
        if (!attracting && age < maximumScatterDuration
            && (firstBounceAge < 0f || age - firstBounceAge < bouncePresentationTime)) return;

        Vector3 target = player.worldCenterOfMass;
        float distanceSquared = (target - body.position).sqrMagnitude;
        float effectiveRadius = attractionRadius + playerSpeed * Mathf.Max(pickupDelay, maximumScatterDuration);
        if (!attracting && distanceSquared > effectiveRadius * effectiveRadius) return;
        if (!attracting)
        {
            attracting = true;
            sphere.isTrigger = true;
            body.isKinematic = true;
        }

        float distance = Mathf.Sqrt(distanceSquared);
        float proximity = 1f - Mathf.InverseLerp(pickupDistance, Mathf.Max(pickupDistance + .01f, effectiveRadius), distance);
        float magneticSpeed = Mathf.Lerp(Mathf.Min(minimumAttractionSpeed, attractionSpeed), attractionSpeed, proximity * proximity);
        float speedLimit = Mathf.Max(.1f, maximumAttractionSpeed);
        float desiredSpeed = Mathf.Min(speedLimit, magneticSpeed + playerSpeed * Mathf.Max(1f, playerSpeedMultiplier));
        float acceleration = attractionAcceleration + playerSpeed * 2f;
        currentAttractionSpeed = Mathf.Min(speedLimit,
            Mathf.MoveTowards(currentAttractionSpeed, desiredSpeed, acceleration * Time.fixedDeltaTime));
        Vector3 next = Vector3.MoveTowards(body.position, target, currentAttractionSpeed * Time.fixedDeltaTime);
        body.MovePosition(next);
        if ((target - next).sqrMagnitude > pickupDistance * pickupDistance) return;

        collected = true;
        if (run != null) run.PointManager.IncrementPoints(reward);
        experience.GetExp(reward);
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!initialized || attracting || collected) return;
        if (lastBounceAge >= 0f && age - lastBounceAge < Time.fixedDeltaTime * 1.5f) return;
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            if (Vector3.Dot(contact.normal, Vector3.up) < .5f) continue;
            if (contact.separation < 0f) body.position -= contact.normal * contact.separation;
            float impactSpeed = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, contact.normal));
            Vector3 surfaceVelocity = collision.rigidbody != null ? collision.rigidbody.GetPointVelocity(contact.point) : Vector3.zero;
            Bounce(contact.normal, surfaceVelocity, impactSpeed);
            return;
        }
    }

    void PredictGroundContact()
    {
        if (body.linearVelocity.y > 0f) return;
        Vector3 gravity = body.useGravity ? Physics.gravity * Mathf.Max(1f, gravityMultiplier) : Vector3.zero;
        Vector3 nextVelocity = body.linearVelocity + gravity * Time.fixedDeltaTime;
        Vector3 step = nextVelocity * Time.fixedDeltaTime;
        float distance = step.magnitude;
        if (distance < .0001f) return;
        Vector3 origin = body.position + transform.TransformVector(sphere.center);
        if (!Physics.SphereCast(origin, worldRadius, step / distance, out RaycastHit hit,
            distance + sphere.contactOffset, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            || Vector3.Dot(hit.normal, Vector3.up) < .5f || hit.rigidbody == player) return;
        body.position += step / distance * hit.distance + hit.normal * .002f;
        Vector3 surfaceVelocity = hit.rigidbody != null ? hit.rigidbody.GetPointVelocity(hit.point) : Vector3.zero;
        Bounce(hit.normal, surfaceVelocity, Mathf.Abs(Vector3.Dot(nextVelocity - surfaceVelocity, hit.normal)));
    }

    void Bounce(Vector3 normal, Vector3 surfaceVelocity, float impactSpeed)
    {
        if (firstBounceAge < 0f) firstBounceAge = age;
        if (impactSpeed < minimumBounceSpeed) return;
        Vector3 tangent = Vector3.ProjectOnPlane(body.linearVelocity - surfaceVelocity, normal);
        body.linearVelocity = surfaceVelocity + tangent * bounceHorizontalRetention
            + normal * (impactSpeed * bounceRestitution);
        lastBounceAge = age;
        visualBounceStarted = Time.time;
        bounceStrength = Mathf.Clamp01(impactSpeed / 5f);
    }

    void LateUpdate()
    {
        if (visuals == null || !initialized) return;
        float verticalScale = 1f + maximumFlightStretch * Mathf.Clamp01(Mathf.Abs(body.linearVelocity.y) / 6f);
        float elapsed = Time.time - visualBounceStarted;
        if (visualBounceStarted >= 0f && elapsed < visualBounceDuration)
        {
            float phase = Mathf.Clamp01(elapsed / Mathf.Max(.01f, visualBounceDuration));
            float deformation = phase < .3f
                ? Mathf.Lerp(-impactSquash, reboundStretch, Mathf.SmoothStep(0f, 1f, phase / .3f))
                : Mathf.Lerp(reboundStretch, 0f, Mathf.SmoothStep(0f, 1f, (phase - .3f) / .7f));
            verticalScale += deformation * bounceStrength;
        }
        float width = 1f / Mathf.Sqrt(Mathf.Max(.1f, verticalScale));
        visuals.localScale = Vector3.Scale(baseVisualScale, new Vector3(width, verticalScale, width));
        visuals.localPosition = baseVisualPosition + Vector3.up * (sphere.radius * Mathf.Min(0f, verticalScale - 1f));
    }

    void OnDisable()
    {
        if (visuals != null) { visuals.localScale = baseVisualScale; visuals.localPosition = baseVisualPosition; }
    }
}
