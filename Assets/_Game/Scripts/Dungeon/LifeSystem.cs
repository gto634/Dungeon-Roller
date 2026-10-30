using System;
using Unity.Collections;
using UnityEngine;


public class LifeSystem : MonoBehaviour
{
    [SerializeField] Stats stats;
    [SerializeField, Min(0)] int _maxHealth;
    [SerializeField, ReadOnly] int _health;

    public event Action Changed;

    public int Health
    {
        get => _health;
        private set
        {
            if (value > _maxHealth)
            {
                value = _maxHealth;
            }
            if (value < 0)
            {
                value = 0;
            }
            _health = value;
            Changed?.Invoke();

            if (_health == 0)
            {
                OnDeath?.Invoke();
            }
        }
    }



    int _def;
    public int MaxHealth => _maxHealth;
    public int Defense => _def;
    public void SetMaxHealth(int value) { _maxHealth = Mathf.Max(1, value); Health = Mathf.Min(Health, _maxHealth); }
    public void AddMaxHealth(int value) => SetMaxHealth(_maxHealth + value);
    public void SetDefense(int value) => _def = Mathf.Max(0, value);
    public void AddDefense(int value) => SetDefense(_def + value);
    public bool IsDead => _health == 0;


    public Action<int, int> OnHeal;
    public Action<int, int> OnDamage;
    public Action OnDeath;

    private void Reset()
    {
        _maxHealth = stats.Health;
        _health = _maxHealth;
        _def = stats.Def;
    }

    private void Start()
    {
        _maxHealth = stats.Health;
        _health = _maxHealth;
        _def = stats.Def;
        Changed?.Invoke();
    }

    public void Heal(int amount) {
        if (IsDead) return;

        int newHealth = Mathf.Min(Health + amount, _maxHealth);

        OnHeal?.Invoke(Health, newHealth);

        Health = newHealth;
    }

    public void TakeDamage(int amount) {
        if (IsDead) return;

        Debug.Log($"{gameObject.name} took {amount} damage.");

        int newHealth = Mathf.Max(Health - Mathf.Clamp(amount - _def, 1, int.MaxValue), 0);

        OnDamage?.Invoke(Health, newHealth);

        Health = newHealth;

        Debug.Log($"{gameObject.name} health is now {Health}.");
    }
    public void Revive()
    {
        OnHeal?.Invoke(_health, _maxHealth);
        Health = _maxHealth;
    }
}
