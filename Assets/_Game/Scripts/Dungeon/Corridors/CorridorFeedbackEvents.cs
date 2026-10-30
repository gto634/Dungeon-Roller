using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class CorridorFeedbackEvents : MonoBehaviour
{
    [SerializeField] CorridorPassageAction passage;
    [SerializeField] UnityEvent<Vector3> onDoorPassed = new UnityEvent<Vector3>();

    public CorridorPassageAction Passage => passage;
    public UnityEvent<Vector3> OnDoorPassed => onDoorPassed;

    void OnEnable()
    {
        if (passage != null) passage.OnDoorPassed += Invoke;
    }
    void OnDisable()
    {
        if (passage != null) passage.OnDoorPassed -= Invoke;
    }
    void Invoke(Vector3 direction) => onDoorPassed.Invoke(direction);
}
