using UnityEngine;

[DisallowMultipleComponent]
public sealed class MeteorTrailRotation : MonoBehaviour
{
    [SerializeField] float degreesPerSecond = 90f;

    void Update() => transform.Rotate(Vector3.up, degreesPerSecond * Time.deltaTime, Space.Self);
}
