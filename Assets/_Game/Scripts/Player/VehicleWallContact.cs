using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class VehicleWallContact : MonoBehaviour
{
    [SerializeField] Movement movement;
    Collider selectedWall;
    Vector3 incomingVelocity, selectedNormal;
    float cornerBlockedUntil;

    public event Action<Vector3, Vector3, float> OnWallDeflected;
    public event Action<Vector3, Vector3, float> OnWallCollision;

    void Reset() => movement = GetComponentInChildren<Movement>(true);
    void FixedUpdate() => incomingVelocity = movement != null ? movement.CurrentVelocity : Vector3.zero;
    void OnDisable()
    {
        selectedWall = null; incomingVelocity = Vector3.zero; cornerBlockedUntil = 0f;
        if (movement != null) movement.CancelWallDeflection();
    }
    void OnCollisionEnter(Collision collision) => HandleContact(collision, true);
    void OnCollisionStay(Collision collision) => HandleContact(collision, false);
    void OnCollisionExit(Collision collision)
    {
        if (selectedWall == collision.collider) selectedWall = null;
    }
    void HandleContact(Collision collision, bool entering)
    {
        if (Time.timeScale <= 0f) return;
        Collider wall = collision.collider;
        if (selectedWall != null && (!selectedWall.enabled || !selectedWall.gameObject.activeInHierarchy)) selectedWall = null;
        if (wall.attachedRigidbody != null && !wall.attachedRigidbody.isKinematic) return;
        if (wall.GetComponentInParent<RoomVisual>() == null && wall.GetComponentInParent<CorridorVisual>() == null) return;
        Vector3 velocity = -collision.relativeVelocity;
        if (incomingVelocity.sqrMagnitude > velocity.sqrMagnitude) velocity = incomingVelocity;
        int drive = movement != null && movement.isActiveAndEnabled
            ? (movement.WantToMove ? 1 : 0) - (movement.WantToBack ? 1 : 0) : 0;
        Vector3 travel = drive != 0 ? movement.FacingDirection * drive : velocity;
        Vector3 normal = Vector3.zero, point = Vector3.zero;
        float best = float.NegativeInfinity;
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            Vector3 candidate = contact.normal;
            if (Mathf.Abs(candidate.y) > .5f) continue;
            candidate = Vector3.ProjectOnPlane(candidate, Vector3.up).normalized;
            float blocking = -Vector3.Dot(travel, candidate);
            if (blocking <= best) continue;
            best = blocking; normal = candidate; point = contact.point;
        }
        if (normal.sqrMagnitude < .5f) return;
        if (entering)
            OnWallCollision?.Invoke(point, normal, Mathf.Abs(Vector3.Dot(velocity, normal)));
        if (movement == null || !movement.isActiveAndEnabled) return;
        if (selectedWall != null && selectedWall != wall)
        {
            if (Mathf.Abs(Vector3.Dot(selectedNormal, normal)) < .5f)
            {
                cornerBlockedUntil = Time.time + .1f;
                movement.CancelWallDeflection();
            }
            return;
        }
        if (Time.time < cornerBlockedUntil) { movement.CancelWallDeflection(); return; }
        bool continuing = movement.IsAligningWithWall && selectedWall == wall && Vector3.Dot(selectedNormal, normal) > .99f;
        if (movement.DeflectAlongWall(normal, velocity))
        {
            selectedWall = wall; selectedNormal = normal;
            if (movement.IsAligningWithWall && !continuing)
                OnWallDeflected?.Invoke(point, normal, Mathf.Abs(Vector3.Dot(velocity, normal)));
        }
    }
}
