using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class LevelManager : MonoBehaviour
{
    [SerializeField] Stats _playerStats;
    [SerializeField] InputActionReference _actionReference;
    [SerializeField, Min(1.01f)] float _experienceGrowth = 1.2f;
    [SerializeField] DifficultyManager clock;
    [SerializeField, Min(.01f)] float luckPeriod = 20f;
    [SerializeField, Min(0)] int passiveLuckGain = 5;
    DifficultyManager boundClock;
    float luck;
    long luckTicks;
    double lastLuckSeconds;
    public event Action LuckChanged;
    public float Luck { get { Initialize(); return luck; } }
    public float LuckPoints => Stats.ToPoints(Luck);

    public UnityEvent LevelUP = new UnityEvent();
    public event Action Changed;
    int _playerLevel, _upgrades;
    float _playerExp, _playerExpToReach;
    bool _initialized;

    public int Upgrades => _upgrades;
    public int PlayerLevel => _playerLevel;
    public float Experience => _playerExp;
    public float ExperienceToReach => _playerExpToReach;
    public Stats PlayerStats => _playerStats;

    void OnEnable() { BindClock(); if (_initialized) RefreshLuck(); }
    void OnDisable()
    {
        if (boundClock != null) boundClock.ClockChanged -= OnClockChanged;
        boundClock = null;
    }
    public void SetClock(DifficultyManager source)
    {
        clock = source; BindClock();
        if (_initialized) RefreshLuck();
    }
    void BindClock()
    {
        if (!isActiveAndEnabled || boundClock == clock) return;
        if (boundClock != null) boundClock.ClockChanged -= OnClockChanged;
        boundClock = clock;
        if (boundClock != null) boundClock.ClockChanged += OnClockChanged;
    }
    void OnClockChanged() { if (!_initialized) Initialize(); else RefreshLuck(); }
    void RefreshLuck()
    {
        if (clock == null || !_initialized) return;
        double now = clock.TotalSeconds;
        if (now < lastLuckSeconds) ResetLuck();
        long ticks = (long)Math.Min(long.MaxValue, Math.Floor(now / Math.Max(.01f, luckPeriod)));
        long gained = Math.Max(0L, ticks - luckTicks);
        luckTicks = ticks; lastLuckSeconds = now;
        if (gained > 0L) AddLuck((float)Math.Min(float.MaxValue, Stats.FromPoints(passiveLuckGain) * (double)gained));
    }
    public void ResetLuck()
    {
        if (!_initialized) { Initialize(); return; }
        luck = _playerStats.Luck;
        lastLuckSeconds = clock != null ? clock.TotalSeconds : 0d;
        luckTicks = (long)Math.Min(long.MaxValue, Math.Floor(lastLuckSeconds / Math.Max(.01f, luckPeriod)));
        LuckChanged?.Invoke();
    }
    public void AddLuck(float value)
    {
        if (!Finite(value) || value < 0f) throw new ArgumentOutOfRangeException(nameof(value));
        Initialize(); luck = (float)Math.Min(float.MaxValue, (double)luck + value); LuckChanged?.Invoke();
    }
    void Start() => Initialize();
    void Update()
    {
        if (_actionReference != null && _actionReference.action != null && _actionReference.action.WasPressedThisFrame()) GetExp(20);
    }
    public void Initialize()
    {
        if (_initialized) return;
        if (_playerStats == null || _playerStats.ExpToReach <= 0f || !Finite(_playerStats.ExpToReach)
            || !Finite(_experienceGrowth) || _experienceGrowth <= 1f)
            throw new InvalidOperationException("Assign valid player stats and an XP threshold growth greater than 1.");
        _playerLevel = _playerStats.Level;
        _playerExp = Mathf.Max(0f, _playerStats.Exp);
        _playerExpToReach = _playerStats.ExpToReach;
        luck = _playerStats.Luck;
        _initialized = true;
        RefreshLuck();
        ApplyEarnedLevels();
        Changed?.Invoke();
    }
    public void GetExp(float exp)
    {
        if (!Finite(exp) || exp < 0f) throw new ArgumentOutOfRangeException(nameof(exp));
        Initialize();
        int previous = _playerLevel;
        _playerExp = (float)Math.Min(float.MaxValue, (double)_playerExp + exp);
        ApplyEarnedLevels();
        Changed?.Invoke();
        if (_playerLevel != previous) LevelUP.Invoke();
    }
    public bool ConsumeUpgrade()
    {
        if (_upgrades == 0) return false;
        _upgrades--;
        Changed?.Invoke();
        return true;
    }
    void ApplyEarnedLevels()
    {
        while (_playerExp >= _playerExpToReach)
        {
            _playerExp -= _playerExpToReach;
            _playerLevel++;
            _playerExpToReach = (float)Math.Min(float.MaxValue, (double)_playerExpToReach * _experienceGrowth);
            _upgrades++;
        }
    }
    static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
