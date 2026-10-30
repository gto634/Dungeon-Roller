using System;
using UnityEngine;
using UnityEngine.AI;

public class KnockbackSystem : MonoBehaviour
{
    [SerializeField] Rigidbody _rb;
    [SerializeField] NavMeshAgent _navAgent;
    [SerializeField, Min(0f)] float _navigationPauseDuration = 0.2f;

    bool _navigationPaused;
    bool _wasStopped;
    bool _wasUpdatingPosition;
    bool _wasUpdatingRotation;
    float _navigationPauseTimer;

    public Action<int, Vector2> OnKnockback;

    void Awake()
    {
        if (_rb == null) _rb = GetComponentInParent<Rigidbody>();
        if (_navAgent == null && _rb != null) _navAgent = _rb.GetComponent<NavMeshAgent>();
    }

    public void Knockback(int Amount, Vector3 direction)
    {
        if (_navAgent != null && _navAgent.isActiveAndEnabled && _navAgent.isOnNavMesh)
        {
            if (!_navigationPaused)
            {
                _wasStopped = _navAgent.isStopped;
                _wasUpdatingPosition = _navAgent.updatePosition;
                _wasUpdatingRotation = _navAgent.updateRotation;
            }

            _navigationPaused = true;
            _navigationPauseTimer = _navigationPauseDuration;
            _navAgent.isStopped = true;
            _navAgent.updatePosition = false;
            _navAgent.updateRotation = false;
        }

        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        _rb.AddForce(direction * Amount, ForceMode.Impulse);

        OnKnockback?.Invoke(Amount, direction);
    }

    void LateUpdate()
    {
        if (!_navigationPaused) return;
        if (_navAgent != null && _navAgent.isActiveAndEnabled && _navAgent.isOnNavMesh)
            _navAgent.nextPosition = _rb.position;

        _navigationPauseTimer -= Time.deltaTime;
        if (_navigationPauseTimer <= 0f) ResumeNavigation();
    }

    void ResumeNavigation()
    {
        if (!_navigationPaused) return;
        _navigationPaused = false;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        if (_navAgent == null) return;
        if (_navAgent.isActiveAndEnabled && _navAgent.isOnNavMesh)
        {
            _navAgent.nextPosition = _rb.position;
            _rb.position = _navAgent.nextPosition;
            _navAgent.isStopped = _wasStopped;
        }
        _navAgent.updatePosition = _wasUpdatingPosition;
        _navAgent.updateRotation = _wasUpdatingRotation;
    }

    void OnDisable()
    {
        ResumeNavigation();
    }
}
