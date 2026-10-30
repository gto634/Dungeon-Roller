using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(SphereCollider))]
public sealed class SpringExitTrigger : MonoBehaviour
{
    [SerializeField] DungeonExit exit;
    public DungeonExit Exit => exit;

    void OnTriggerEnter(Collider other)
    {
        if (exit != null) exit.TryComplete(other);
    }
}
