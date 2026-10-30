using UnityEngine;

[DisallowMultipleComponent]
public sealed class BumperRotationFeedback : MonoBehaviour
{
    [SerializeField] Transform bumpersSides;
    [SerializeField] float degreesPerSecond = 90f;

    void Update()
    {
        if (bumpersSides != null)
            bumpersSides.Rotate(Vector3.up, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
