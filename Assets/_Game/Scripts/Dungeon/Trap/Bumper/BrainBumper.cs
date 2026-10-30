using System;
using System.Collections.Generic;
using UnityEngine;

public class BrainBumper : MonoBehaviour
{
    [Header("Dependences")]
    [SerializeField] List<AttackHitbox> _hitboxes = new List<AttackHitbox>();
    [SerializeField] private float bounceForce;
    [SerializeField] private bool resetVelocityBeforeBounce;

    [SerializeField] List<string> _damageableTags = new List<string>();

    [Header("Optionnal")]
    [SerializeField] private AudioSource hitSound;
    [SerializeField] private ParticleSystem hitParticles;

    readonly List<Collider> bodyColliders = new List<Collider>();

    public event Action OnBump;

    void OnEnable()
    {
        foreach (AttackHitbox settings in _hitboxes)
        {
            if (settings != null)
            {
                settings.ContactDetected += Trigger;
            }
        }
    }
    void OnDisable()
    {
        foreach (AttackHitbox settings in _hitboxes)
        {
            if (settings != null)
            {
                settings.ContactDetected -= Trigger;
            }
        }
    }

    void Trigger(Collider source, Collider target)
    {
        Rigidbody playerRb = target.attachedRigidbody;
        if (playerRb == null || playerRb.isKinematic || !CanBump(target, playerRb)) return;
        if (source.TryGetComponent(out AttackHitbox hitbox) && !hitbox.TryRegisterHit(target)) return;

        Vector3 bounceDirection = Vector3.ProjectOnPlane(playerRb.worldCenterOfMass - source.bounds.center, Vector3.up).normalized;

        if (resetVelocityBeforeBounce)
        {
            playerRb.linearVelocity = Vector3.zero;
        }
        playerRb.AddForce(bounceDirection * bounceForce, ForceMode.Impulse);
        OnBump?.Invoke();

        if (hitSound != null) hitSound.Play();
        if (hitParticles != null) hitParticles.Play();
    }

    bool CanBump(Collider target, Rigidbody targetBody)
    {
        if (_damageableTags.Contains(target.tag)) return true;

        targetBody.GetComponentsInChildren(false, bodyColliders);
        foreach (Collider collider in bodyColliders)
        {
            if (collider.enabled && collider.attachedRigidbody == targetBody && _damageableTags.Contains(collider.tag)) return true;
        }
        return false;
    }
}
