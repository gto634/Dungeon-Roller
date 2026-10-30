using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Scripting.APIUpdating;

[MovedFrom(true, null, "Game.Management", "CorridorPointsAction")]
[DisallowMultipleComponent]
public sealed class CorridorMultiplierAction : MonoBehaviour
{
    [SerializeField] CorridorPassageAction passage;
    [FormerlySerializedAs("awarded")]
    [SerializeField, HideInInspector] bool incremented;

    public CorridorPassageAction Passage => passage;

    void OnEnable()
    {
        if (passage == null) return;
        if (passage.HasPassed) incremented = true;
        passage.OnDoorPassed += Increment;
    }
    void OnDisable()
    {
        if (passage != null) passage.OnDoorPassed -= Increment;
    }
    void Increment(Vector3 direction)
    {
        if (incremented || !Application.isPlaying) return;
        GameManager game = GameManager.Instance;
        Rigidbody player = passage.Player;
        if (game == null || !game.isActiveAndEnabled || !game.IsRunActive || player == null) return;
        if (game.Rooms == null || game.Rooms.Player != player.transform) return;
        PointManager manager = game.PointManager;
        if (manager == null || !manager.isActiveAndEnabled) return;
        incremented = true;
        manager.IncrementMultiplier();
    }
}
