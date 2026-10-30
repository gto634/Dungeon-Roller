using UnityEngine;

[DisallowMultipleComponent]
public sealed class CorridorRewardsAction : MonoBehaviour
{
    [SerializeField] CorridorPassageAction passage;
    [SerializeField] ExperienceOrb orbPrefab;
    [SerializeField, Min(0f)] float experienceReward = 100f;
    [SerializeField, Range(1, 32)] int orbCount = 15;
    [SerializeField, Range(0f, 89f)] float burstHalfAngle = 80f;
    [SerializeField, Min(0f)] float exitOffset = .15f;
    [SerializeField, Min(0f)] float launchSpeed = 11f;
    [SerializeField, Range(0f, 1f)] float speedVariation = .25f;
    [SerializeField] Vector2 upwardSpeedRange = new Vector2(1.2f, 3.96f);
    [SerializeField, HideInInspector] bool rewarded;

    public bool IsRewarded => rewarded;

    void OnEnable()
    {
        if (passage == null) return;
        if (passage.HasPassed) rewarded = true;
        passage.OnDoorPassed += Reward;
    }
    void OnDisable()
    {
        if (passage != null) passage.OnDoorPassed -= Reward;
    }
    void Reward(Vector3 direction)
    {
        if (rewarded || !Application.isPlaying) return;
        GameManager game = GameManager.Instance;
        Rigidbody player = passage.Player;
        if (game == null || !game.isActiveAndEnabled || game.IsGameEnded || player == null) return;
        if (game.Rooms == null || game.Rooms.Player != player.transform) return;
        if (game.ExperienceSystem == null || !game.ExperienceSystem.isActiveAndEnabled || orbPrefab == null)
        {
            Debug.LogError("Assign the corridor experience orb prefab and the active LevelManager.", this);
            return;
        }
        if (!passage.ExitTrigger.TryGetExitBurst(exitOffset, out Vector3 origin, out _)) return;
        rewarded = true;
        var velocities = new Vector3[Mathf.Clamp(orbCount, 1, 32)];
        for (int i = 0; i < velocities.Length; i++)
            velocities[i] = ExperienceOrbBurst.ConeVelocity(direction, burstHalfAngle, launchSpeed, speedVariation, upwardSpeedRange);
        ExperienceOrbBurst.Emit(orbPrefab, gameObject.scene, player, game.ExperienceSystem, experienceReward, origin, velocities);
    }
}
