using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class MainMenuHoodCredits : MonoBehaviour
{
    [SerializeField] MainMenuController menu;
    [SerializeField] EventSystem eventSystem;
    [SerializeField] Transform hood;
    [SerializeField] Collider restingHitArea, movingHitArea;
    [SerializeField] GameObject credits;
    [SerializeField] Vector3 hingeAxis = Vector3.right;
    [SerializeField, Range(0f, 15f)] float hoverAngle = 7f;
    [SerializeField, Range(30f, 100f)] float openAngle = 65f;
    [SerializeField, Min(1f)] float angularSpeed = 180f;
    [SerializeField, Min(.01f)] float motionDuration = .25f;
    readonly List<RaycastResult> uiHits = new List<RaycastResult>();
    PointerEventData pointer;
    Quaternion closedRotation;
    bool initialized;
    float angularVelocity;

    public Transform Hood => hood;
    public GameObject Credits => credits;
    public bool IsHovered { get; private set; }
    public bool IsOpen { get; private set; }
    public float CurrentAngle { get; private set; }

    void Awake()
    {
        if (hood != null) closedRotation = hood.localRotation;
        if (eventSystem != null) pointer = new PointerEventData(eventSystem);
        initialized = true;
    }
    void OnEnable() => ResetPose();
    void OnDisable() => ResetPose();
    void ResetPose()
    {
        if (!initialized) return;
        IsHovered = IsOpen = false;
        CurrentAngle = 0f;
        angularVelocity = 0f;
        if (hood != null) hood.localRotation = closedRotation;
        if (credits != null) credits.SetActive(false);
    }
    void Update()
    {
        if (hood == null) return;
        Mouse mouse = Mouse.current;
        IsHovered = mouse != null && CanInteract && HitsHood(mouse.position.ReadValue());
        if (IsHovered && mouse.leftButton.wasPressedThisFrame) IsOpen = !IsOpen;
        float target = IsOpen ? openAngle : IsHovered ? hoverAngle : 0f;
        CurrentAngle = Mathf.SmoothDamp(CurrentAngle, target, ref angularVelocity,
            motionDuration, angularSpeed, Time.unscaledDeltaTime);
        if (Mathf.Abs(CurrentAngle - target) < .01f) { CurrentAngle = target; angularVelocity = 0f; }
        hood.localRotation = closedRotation * Quaternion.AngleAxis(CurrentAngle, hingeAxis.normalized);
        if (credits != null) credits.SetActive(IsOpen && CurrentAngle >= openAngle * .65f);
    }
    bool CanInteract => isActiveAndEnabled && menu != null && menu.isActiveAndEnabled && !menu.IsTransitioning;
    public bool TryToggleAt(Vector2 screenPosition)
    {
        if (!Application.isPlaying || !CanInteract || !HitsHood(screenPosition)) return false;
        IsOpen = !IsOpen;
        return true;
    }
    public bool HitsHood(Vector2 screenPosition)
    {
        if (menu == null || menu.ViewCamera == null) return false;
        if (eventSystem != null && pointer != null)
        {
            pointer.position = screenPosition; uiHits.Clear();
            eventSystem.RaycastAll(pointer, uiHits);
            if (uiHits.Count > 0) return false;
        }
        return Physics.Raycast(menu.ViewCamera.ScreenPointToRay(screenPosition), out var hit,
            menu.ViewCamera.farClipPlane, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide)
            && (hit.collider == restingHitArea || hit.collider == movingHitArea);
    }
}
