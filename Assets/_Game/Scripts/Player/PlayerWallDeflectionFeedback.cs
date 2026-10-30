using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class PlayerWallDeflectionFeedback : MonoBehaviour
{
    [SerializeField] VehicleWallContact wallContact;
    [SerializeField] ParticleSystem sparks;
    [SerializeField, Min(0f)] float surfaceOffset = .12f;
    [SerializeField, Min(0f)] float verticalLift = .6f;
    [InspectorName("On Wall Collision")]
    [SerializeField] UnityEvent<float> onWallDeflected = new UnityEvent<float>();

    public VehicleWallContact WallContact => wallContact;
    public ParticleSystem Sparks => sparks;
    public UnityEvent<float> OnWallDeflected => onWallDeflected;

    void OnEnable()
    {
        if (wallContact != null) wallContact.OnWallCollision += Invoke;
    }
    void OnDisable()
    {
        if (wallContact != null) wallContact.OnWallCollision -= Invoke;
        if (sparks != null) sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
    void Invoke(Vector3 point, Vector3 normal, float impactSpeed)
    {
        Vector3 emissionDirection = (normal + Vector3.up * .55f).normalized;
        transform.SetPositionAndRotation(point + normal * surfaceOffset + Vector3.up * verticalLift,
            Quaternion.LookRotation(emissionDirection, Vector3.up));

        if (sparks != null) sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        onWallDeflected.Invoke(impactSpeed);
    }
}
