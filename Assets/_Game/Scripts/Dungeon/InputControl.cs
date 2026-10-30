using UnityEngine;
using UnityEngine.InputSystem;

public class InputControl : MonoBehaviour
{
    [Header("Input Action")]
    [SerializeField] InputActionReference _inputForward;
    [SerializeField] InputActionReference _inputBack;
    [SerializeField] InputActionReference _inputLeft;
    [SerializeField] InputActionReference _inputRight;
    [SerializeField] InputActionReference _testJoystick;
    [SerializeField] InputActionReference _drift;
    [Header("Component")]
    [SerializeField] Movement _movement;

    void Update()
    {
        if (_movement == null) return;
        if (!_movement.isActiveAndEnabled || Time.timeScale <= 0f) { ClearInputs(); return; }
        _movement.WantToMove = Pressed(_inputForward);
        _movement.WantToBack = Pressed(_inputBack);
        _movement.WantToDrift = Pressed(_drift);
        Vector2 direction = _testJoystick != null && _testJoystick.action != null
            ? _testJoystick.action.ReadValue<Vector2>() : Vector2.zero;
        _movement.Direction = new Vector3(direction.x, 0f, direction.y);
    }
    static bool Pressed(InputActionReference reference)
        => reference != null && reference.action != null && reference.action.IsPressed();
    void OnDisable() => ClearInputs();
    void ClearInputs()
    {
        if (_movement == null) return;
        _movement.WantToMove = _movement.WantToBack = _movement.WantToDrift = false;
        _movement.Direction = Vector3.zero;
    }
}
