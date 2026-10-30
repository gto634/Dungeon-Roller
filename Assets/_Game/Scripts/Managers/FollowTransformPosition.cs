using UnityEngine;

[ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(1000)]
public sealed class FollowTransformPosition : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Transform positionSource;

    public Transform Target => target;
    public Transform PositionSource => positionSource;

    void LateUpdate() => Apply();

    public void Apply()
    {
        if (target == null || positionSource == null || target == positionSource) return;
        Vector3 position = positionSource.position;
        if (target.position != position) target.position = position;
    }
}
