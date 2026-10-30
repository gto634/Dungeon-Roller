using UnityEngine;

[DisallowMultipleComponent]
public sealed class DungeonExit : MonoBehaviour
{
    [SerializeField] BoxCollider volume;
    [SerializeField] SpringExitCinematic cinematic;
    [SerializeField, HideInInspector] GameManager owner;
    [SerializeField, HideInInspector] Rigidbody player;

    public Room Room => GetComponentInParent<Room>(true);
    public BoxCollider Volume => volume != null ? volume : volume = GetComponentInChildren<BoxCollider>(true);
    public SpringExitCinematic Cinematic => cinematic != null ? cinematic : cinematic = GetComponentInChildren<SpringExitCinematic>(true);

    public void Bind(GameManager game, Rigidbody trackedPlayer)
    {
        owner = game;
        player = trackedPlayer;
        Volume.isTrigger = true;
        Cinematic.PlaceCamera(Room);
    }
    internal bool PlaySequence(Rigidbody actor, System.Action completed, System.Action cancelled)
        => Cinematic.Play(actor, completed, cancelled);
    internal void CancelSequence() => Cinematic.Cancel();

    public bool TryComplete(Collider other)
    {
        if (!Application.isPlaying || !isActiveAndEnabled || owner == null || player == null || other == null
            || other.attachedRigidbody != player) return false;
        return owner.TryFinishRun(this, player);
    }
}
