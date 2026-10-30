using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider))]
public sealed class Barrel : MonoBehaviour
{
    [SerializeField, Min(0f)] float experienceReward = 10f;
    [SerializeField] ExperienceOrb experienceOrbPrefab;
    [SerializeField, Range(1, 32)] int orbCount = 5;
    [SerializeField, Min(.1f)] float fullImpactSpeed = 10f;
    [SerializeField, Range(0f, 75f)] float spreadAngle = 30f;
    [SerializeField, Min(0f)] float minimumLaunchSpeed = 2.5f, maximumLaunchSpeed = 5.5f;
    [SerializeField, Min(0f)] float upwardLaunchSpeed = 3.3f;
    [SerializeField] bool playImpactFeedback = true;
    [SerializeField] bool destroyOnBreak = true;

    BoxCollider volume;
    bool breakOnTrigger = true;
    bool broken;

    public float ExperienceReward => experienceReward;
    public bool IsBroken => broken;
    public event System.Action<Barrel, Rigidbody> Broken;

    void Awake()
    {
        volume = GetComponent<BoxCollider>();
        volume.isTrigger = true;
    }

    void Reset() => GetComponent<BoxCollider>().isTrigger = true;

    void OnTriggerEnter(Collider other)
    {
        if (!breakOnTrigger || other.isTrigger) return;
        TryBreak(other.attachedRigidbody);
    }

    public void SetExperienceReward(float amount) => experienceReward = Mathf.Max(0f, amount);

    public void ConfigureForTrigger(Barrel template, float amount)
    {
        SetExperienceReward(amount);
        playImpactFeedback = false;
        breakOnTrigger = false;
        destroyOnBreak = false;

        if (template == null || template == this) return;
        experienceOrbPrefab = template.experienceOrbPrefab;
        orbCount = template.orbCount;
        fullImpactSpeed = template.fullImpactSpeed;
        spreadAngle = template.spreadAngle;
        minimumLaunchSpeed = template.minimumLaunchSpeed;
        maximumLaunchSpeed = template.maximumLaunchSpeed;
        upwardLaunchSpeed = template.upwardLaunchSpeed;
    }

    public bool TryBreak(Rigidbody player) => Break(player);

    public bool TryBreak(Rigidbody player, Vector3 burstOrigin, Vector3 burstDirection, float burstHalfAngle,
        float? burstLaunchSpeed = null, float burstSpeedVariation = .15f, Vector2? burstUpwardSpeedRange = null)
    {
        return Break(player, burstOrigin, burstDirection, Mathf.Clamp(burstHalfAngle, 0f, 89f),
            burstLaunchSpeed, Mathf.Clamp01(burstSpeedVariation), burstUpwardSpeedRange);
    }

    bool Break(Rigidbody player, Vector3? burstOrigin = null, Vector3? burstDirection = null,
        float? burstHalfAngle = null, float? burstLaunchSpeed = null,
        float burstSpeedVariation = .15f, Vector2? burstUpwardSpeedRange = null)
    {
        if (!CanBreak(player, out GameManager game)) return false;

        Vector3 velocity = Vector3.ProjectOnPlane(player.linearVelocity, Vector3.up);
        float strength = Mathf.Clamp01(velocity.magnitude / Mathf.Max(.1f, fullImpactSpeed));
        Vector3 origin = burstOrigin.GetValueOrDefault(volume.bounds.center + Vector3.up * .1f);
        Vector3 direction = GetBurstDirection(player, velocity, burstDirection);

        broken = true;
        SpawnExperience(player, game.ExperienceSystem, origin, direction, strength,
            burstHalfAngle, burstLaunchSpeed, burstSpeedVariation, burstUpwardSpeedRange);
        PlayImpact(player, strength);
        FinishBreak(player);
        return true;
    }

    Vector3 GetBurstDirection(Rigidbody player, Vector3 velocity, Vector3? requestedDirection)
    {
        if (requestedDirection.HasValue)
        {
            Vector3 projected = Vector3.ProjectOnPlane(requestedDirection.Value, Vector3.up);
            if (projected.sqrMagnitude > .0001f) return projected.normalized;
        }

        Vector3 awayFromPlayer = Vector3.ProjectOnPlane(volume.bounds.center - player.worldCenterOfMass, Vector3.up);
        if (awayFromPlayer.sqrMagnitude > .0001f) return awayFromPlayer.normalized;
        if (velocity.sqrMagnitude > .0001f) return velocity.normalized;
        return player.transform.forward;
    }

    void SpawnExperience(Rigidbody player, LevelManager experience, Vector3 origin, Vector3 direction,
        float strength, float? halfAngle, float? launchSpeed, float speedVariation, Vector2? upwardSpeedRange)
    {
        if (experienceReward <= 0f) return;

        int count = Mathf.Clamp(orbCount, 1, 32);
        var velocities = new Vector3[count];
        for (int i = 0; i < count; i++)
            velocities[i] = GetLaunchVelocity(direction, strength, halfAngle, launchSpeed, speedVariation, upwardSpeedRange);
        ExperienceOrbBurst.Emit(experienceOrbPrefab, gameObject.scene, player, experience, experienceReward, origin, velocities);
    }

    Vector3 GetLaunchVelocity(Vector3 direction, float strength, float? halfAngle,
        float? requestedSpeed, float speedVariation, Vector2? upwardSpeedRange)
    {
        float speed = GetLaunchSpeed(strength, requestedSpeed, speedVariation);
        float upwardSpeed = GetUpwardSpeed(upwardSpeedRange);
        float yawLimit = spreadAngle;

        if (halfAngle.HasValue)
        {
            float spread = halfAngle.Value * Mathf.Deg2Rad;
            upwardSpeed = Mathf.Min(upwardSpeed, speed * Mathf.Tan(spread));
            float elevation = Mathf.Atan2(upwardSpeed, speed);
            yawLimit = Mathf.Acos(Mathf.Clamp01(Mathf.Cos(spread) / Mathf.Max(.0001f, Mathf.Cos(elevation)))) * Mathf.Rad2Deg;
        }

        float angle = Random.Range(-yawLimit, yawLimit);
        Vector3 launchDirection = Quaternion.AngleAxis(angle, Vector3.up) * direction;
        return launchDirection * speed + Vector3.up * upwardSpeed;
    }

    float GetLaunchSpeed(float strength, float? requestedSpeed, float speedVariation)
    {
        if (requestedSpeed.HasValue)
            return Mathf.Max(0f, requestedSpeed.Value) * Random.Range(1f - speedVariation, 1f + speedVariation);
        return Mathf.Lerp(minimumLaunchSpeed, maximumLaunchSpeed, strength) * Random.Range(.85f, 1.15f);
    }

    float GetUpwardSpeed(Vector2? requestedRange)
    {
        if (!requestedRange.HasValue) return upwardLaunchSpeed * Random.Range(.8f, 1.2f);

        Vector2 range = requestedRange.Value;
        float minimum = Mathf.Max(0f, range.x);
        return Random.Range(minimum, Mathf.Max(minimum, range.y));
    }

    void PlayImpact(Rigidbody player, float strength)
    {
        if (!playImpactFeedback) return;
        if (!player.TryGetComponent<PlayerImpactFeedback>(out var feedback))
            feedback = player.gameObject.AddComponent<PlayerImpactFeedback>();
        feedback.Play(strength);
    }

    public bool TryConsumeWithoutReward(Rigidbody player)
    {
        if (!CanBreak(player, out _)) return false;
        broken = true;
        FinishBreak(player);
        return true;
    }

    bool CanBreak(Rigidbody player, out GameManager game)
    {
        game = GameManager.Instance;
        if (!Application.isPlaying || !isActiveAndEnabled || broken || player == null) return false;
        if (game == null || !game.isActiveAndEnabled || game.IsGameEnded || game.IsExitSequenceActive) return false;
        if (game.Rooms == null || game.Rooms.Player != player.transform) return false;
        if (game.ExperienceSystem == null || !game.ExperienceSystem.isActiveAndEnabled) return false;
        if (experienceReward <= 0f || experienceOrbPrefab != null) return true;

        Debug.LogError("Assign the barrel's Experience Orb Prefab before breaking it.", this);
        return false;
    }

    void FinishBreak(Rigidbody player)
    {

        if (destroyOnBreak) volume.enabled = false;
        Broken?.Invoke(this, player);
        if (!destroyOnBreak) return;

        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
