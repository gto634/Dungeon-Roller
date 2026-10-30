using System.Collections.Generic;
using UnityEngine;

public enum position { UP, DOWN }

[System.Serializable]
public class Wheel
{
    [SerializeField] internal Transform _transformObject;
    [SerializeField] internal Transform _transformDirection;
    [SerializeField] internal position _position;
    [SerializeField] internal float _radius = .5f;
    [SerializeField] internal int groundContactIndex = -1;
    internal float _currentRollAngle;
    internal float _currentSteerAngle;
}

[DefaultExecutionOrder(20)]
public class WheelRotation : MonoBehaviour
{
    [SerializeField] List<Wheel> _wheels = new List<Wheel>();
    [SerializeField] VehicleVisualLean visualLean;
    [Header("Paramètres de braquage")]
    [SerializeField] float _maxSteerAngle = 30f;
    [SerializeField] float _steerResponse = 10f;
    Movement movement;

    void Awake()
    {
        movement = GetComponent<Movement>();
        if (visualLean == null) visualLean = GetComponentInParent<VehicleVisualLean>();
    }
    void LateUpdate()
    {
        if (movement == null || !movement.isActiveAndEnabled || Time.timeScale <= 0f || Time.deltaTime <= 0f) return;
        float speed = movement.CurrentSpeed;
        float direction = movement.WantToBack ? 1f : -1f;
        foreach (Wheel wheel in _wheels)
        {
            if (wheel._transformObject == null || wheel._radius <= 0f) continue;
            if (visualLean != null && visualLean.isActiveAndEnabled && wheel.groundContactIndex >= 0
                && !visualLean.IsWheelGrounded(wheel.groundContactIndex)) continue;
            float deltaAngle = speed / wheel._radius * Mathf.Rad2Deg * Time.deltaTime * direction;
            wheel._transformObject.Rotate(Vector3.right * deltaAngle, Space.Self);
            if (wheel._transformDirection == null) continue;
            float target = wheel._position == position.UP ? movement.Direction.x * _maxSteerAngle : 0f;
            wheel._currentSteerAngle = Mathf.Lerp(wheel._currentSteerAngle, target, Time.deltaTime * _steerResponse);
            wheel._transformDirection.localRotation = Quaternion.Euler(0f, 0f, -wheel._currentSteerAngle);
        }
    }
}
