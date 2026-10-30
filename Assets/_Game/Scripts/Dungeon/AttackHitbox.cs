using System;
using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    [SerializeField] bool _hitOnceUntilExit;

    private Collider _collider;
    [ReadOnly] HashSet<Collider> _contacts = new HashSet<Collider>();
    [ReadOnly] HashSet<Transform> _hitTargets = new HashSet<Transform>();

    public Transform IgnoredRoot { get; set; }

    public Collider Collider
    {
        get
        {
            if (_collider == null)
            {
                _collider = GetComponent<Collider>();
            }

            return _collider;
        }
    }

    public event Action<Collider, Collider> ContactDetected;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        DetectContact(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (_hitOnceUntilExit) DetectContact(other);
    }

    void DetectContact(Collider other)
    {
        if (!isActiveAndEnabled || !Collider.enabled) return;
        Transform root = other.transform.root;
        if (IgnoredRoot != null && root == IgnoredRoot.root) return;
        ContactDetected?.Invoke(Collider, other);
    }

    public bool TryRegisterHit(Collider target)
    {
        if (!_hitOnceUntilExit) return true;

        _contacts.Add(target);
        return _hitTargets.Add(target.transform.root);
    }

    private void OnTriggerExit(Collider other)
    {
        _contacts.Remove(other);
        Transform root = other.transform.root;
        foreach (Collider contact in _contacts)
        {
            if (contact != null && contact.transform.root == root) return;
        }

        _hitTargets.Remove(root);
    }

    public void ResetHits()
    {
        _hitTargets.Clear();
    }

    private void OnDisable()
    {
        _contacts.Clear();
        ResetHits();
    }
}
