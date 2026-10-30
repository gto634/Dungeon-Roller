using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class NitroFeedbackEvents : MonoBehaviour
{
    [SerializeField] Movement movement;
    [SerializeField] UnityEvent onNitroActivated = new UnityEvent();
    [SerializeField] UnityEvent onNitroDeactivated = new UnityEvent();

    public Movement Movement => movement;
    public UnityEvent OnNitroActivated => onNitroActivated;
    public UnityEvent OnNitroDeactivated => onNitroDeactivated;

    void OnEnable()
    {
        if (movement == null) return;
        movement.OnNitroActivated += Activate;
        movement.OnNitroDeactivated += Deactivate;
        if (movement.IsNitroBoosting) Activate();
        else Deactivate();
    }
    void OnDisable()
    {
        if (movement != null)
        {
            movement.OnNitroActivated -= Activate;
            movement.OnNitroDeactivated -= Deactivate;
        }
        Deactivate();
    }
    void Activate() => onNitroActivated.Invoke();
    void Deactivate() => onNitroDeactivated.Invoke();
}
