using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

[MovedFrom(true, null, "Dungeon.Runtime", "MeteoriteBrain")]
public class MeteorBrain : MonoBehaviour
{
    [SerializeField] Transform _modelTransform;
    [SerializeField] Transform _shadowTransform;
    [SerializeField] AttackSystem _attackSystem;

    public float DistToGround;
    public float Speed;

    public event Action Crashed;
    Vector3 groundPosition;
    bool generated, crashed;
    public bool IsFalling => generated && !crashed && isActiveAndEnabled;

    void OnEnable()
    {
        if (generated) _attackSystem.Desactivated += Destroy;
    }

    public void Generate()
    {
        Vector3 local = _modelTransform.localPosition;
        groundPosition = _modelTransform.parent.TransformPoint(new Vector3(local.x, 0f, local.z));
        _modelTransform.position = groundPosition + Vector3.up * DistToGround;
        generated = true;
        crashed = false;
        _attackSystem.Desactivated -= Destroy;
        _attackSystem.Desactivated += Destroy;
        _shadowTransform.localScale = new Vector3(0.1f, 0f, 0.1f);
        foreach (HitboxSettings settings in _attackSystem.Hitboxes)
            settings.Damage *= Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(_modelTransform.lossyScale.x)));
    }

    void OnDisable()
    {
        if (_attackSystem != null) _attackSystem.Desactivated -= Destroy;
    }

    void Update()
    {
        if (!generated || crashed) return;
        _modelTransform.position = Vector3.MoveTowards(_modelTransform.position, groundPosition,
            Mathf.Max(0f, Speed) * Time.deltaTime);
        float height = Vector3.Distance(_modelTransform.position, groundPosition);
        float progress = Mathf.InverseLerp(DistToGround, 0f, height);
        float scale = Mathf.Lerp(0.1f, 1f, progress);
        _shadowTransform.localScale = new Vector3(scale, _shadowTransform.localScale.y, scale);
        if (height <= 0f) Crash();
    }

    void Crash()
    {
        if (crashed) return;
        crashed = true;
        _attackSystem.Attack();
        Crashed?.Invoke();
    }

    void Destroy()
    {
        Rigidbody body = GetComponentInParent<Rigidbody>();
        if (body != null) Destroy(body.gameObject);
    }
}
