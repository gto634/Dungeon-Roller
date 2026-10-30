using System;
using UnityEngine;

public interface ICorridorTriggerState
{
    bool IsConsumed { get; }
}

[DisallowMultipleComponent]
public sealed class CorridorPassageAction : MonoBehaviour
{
    [SerializeField] Corridor corridor;
    [SerializeField] CorridorPassage triggerA;
    [SerializeField] CorridorPassage triggerB;
    [SerializeField, HideInInspector] bool consumedA, consumedB, completed;

    public CorridorPassage TriggerA => triggerA;
    public CorridorPassage TriggerB => triggerB;
    public CorridorPassage ExitTrigger { get; private set; }
    public Rigidbody Player { get; private set; }
    public Vector3 LastDirection { get; private set; }
    public bool HasPassed => completed;
    public event Action<Vector3> OnDoorPassed;

    void OnEnable() => Subscribe();
    void OnDisable() => Unsubscribe();

    public void Bind(Corridor owner, CorridorPassage first, CorridorPassage second)
    {
        Unsubscribe();
        corridor = owner;
        triggerA = first;
        triggerB = second;
        if (isActiveAndEnabled) Subscribe();
    }

    void Subscribe()
    {
        Unsubscribe();
        if (triggerA != null) triggerA.Entered += OnTriggerEntered;
        if (triggerB != null) triggerB.Entered += OnTriggerEntered;
    }

    void Unsubscribe()
    {
        if (triggerA != null) triggerA.Entered -= OnTriggerEntered;
        if (triggerB != null) triggerB.Entered -= OnTriggerEntered;
    }

    void OnTriggerEntered(CorridorPassage trigger, Rigidbody player) => TryConsume(trigger, player);

    public bool IsConsumed(CorridorPassage trigger)
    {
        if (trigger == triggerA) return consumedA;
        if (trigger == triggerB) return consumedB;
        return false;
    }

    public bool TryConsume(CorridorPassage trigger, Rigidbody player)
    {
        if (!Application.isPlaying || !isActiveAndEnabled || completed || player == null) return false;
        if (corridor == null || !corridor.IsInitialized || trigger == null || trigger.Owner != corridor) return false;
        if (trigger.TrackedPlayer != player || !trigger.isActiveAndEnabled) return false;
        if (trigger == triggerA)
        {
            if (consumedA) return false;
            consumedA = true;
        }
        else if (trigger == triggerB)
        {
            if (consumedB) return false;
            consumedB = true;
        }
        else return false;

        if (!consumedA || !consumedB) return true;
        Player = player;
        ExitTrigger = trigger;
        Vector3 direction = trigger.transform.forward;
        if (!trigger.AtEnd) direction = -direction;
        LastDirection = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
        completed = true;
        OnDoorPassed?.Invoke(LastDirection);
        return true;
    }

    public void RestoreConsumption(bool first, bool second)
    {
        consumedA = first;
        consumedB = second;
        completed = first && second;
    }
}
