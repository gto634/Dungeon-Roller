using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent, RequireComponent(typeof(CorridorPassage))]
public sealed class CorridorDoorReward : MonoBehaviour, ICorridorTriggerState
{
    [SerializeField] CorridorPassageAction passageAction;
    [FormerlySerializedAs("consumed")]
    [SerializeField, HideInInspector] bool legacyConsumed;
    CorridorPassage passage;

    public bool IsConsumed
    {
        get
        {
            if (passageAction == null) return legacyConsumed;
            return passageAction.IsConsumed(passage);
        }
    }

    void OnEnable()
    {
        passage = GetComponent<CorridorPassage>();
        passage.Rebuilt += Refresh;
        Refresh();
    }
    void OnDisable()
    {
        if (passage != null) passage.Rebuilt -= Refresh;
    }
    public void Bind(CorridorPassage owner)
    {
        if (passage != owner)
        {
            if (passage != null) passage.Rebuilt -= Refresh;
            passage = owner;
            if (isActiveAndEnabled && passage != null) passage.Rebuilt += Refresh;
        }
        Refresh();
    }
    void Refresh()
    {
        if (passage == null) return;
        if (passage.Owner != null) passageAction = passage.Owner.GetComponentInChildren<CorridorPassageAction>(true);
        if (TryGetComponent<Barrel>(out var barrel)) barrel.enabled = false;
    }
    public bool TryBreak(Rigidbody player)
    {
        if (!Application.isPlaying || !isActiveAndEnabled || player == null || passageAction == null) return false;
        GameManager game = GameManager.Instance;
        if (game == null || !game.isActiveAndEnabled || game.IsGameEnded) return false;
        if (game.Rooms == null || game.Rooms.Player != player.transform) return false;
        return passageAction.TryConsume(passage, player);
    }
}
