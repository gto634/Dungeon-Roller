using System;
using UnityEngine;
using NaughtyAttributes;

public class Clock : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] float _time;
    [SerializeField] bool _autoReset;
    [SerializeField, ReadOnly] float _time_process;

    public event Action OnFinish;

    public float TimeClock
    {
        get
        {
            return _time;
        }

        set
        {
            _time = value;
        }
    }

    void Reset()
    {
        _time = 1;
        _autoReset = true;
    }

    public void Awake()
    {
        _time_process = 0f;
    }
    public void Start()
    {

    }

    public void Update()
    {
        _time_process += Time.deltaTime;
        if (IsFinish())
        {
            OnFinish?.Invoke();
            if (_autoReset)
            {
                _time_process = 0f;
            }
        }
    }

    public bool IsFinish()
    {
        return _time_process > _time;
    }

    public void ResetClock()
    {
        _time_process = 0;
    }

}

