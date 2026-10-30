using UnityEngine;

[DisallowMultipleComponent]
public sealed class CorridorParticlesAction : MonoBehaviour
{
    [SerializeField] CorridorPassageAction passage;
    [SerializeField] ParticleSystem[] particles;
    [SerializeField] Transform doorModel;
    [SerializeField, Min(0f)] float heightAboveDoor = .25f;

    public int PlayCount { get; private set; }


    public void Play(Vector3 direction)
    {
        if (!isActiveAndEnabled || passage == null || passage.ExitTrigger == null || particles == null) return;
        if (passage.ExitTrigger.Owner == null || !passage.ExitTrigger.Owner.IsInitialized) return;
        Vector3 origin = passage.ExitTrigger.Owner.transform.position;
        Vector3 forward = direction.sqrMagnitude > .0001f ? direction.normalized : passage.LastDirection;
        if (forward.sqrMagnitude < .0001f) return;
        float top = passage.ExitTrigger.Volume.bounds.max.y;
        if (doorModel != null)
            foreach (var renderer in doorModel.GetComponentsInChildren<MeshRenderer>(true))
                top = Mathf.Max(top, renderer.bounds.max.y);
        origin.y = top + heightAboveDoor;
        bool played = false;
        foreach (ParticleSystem effect in particles)
        {
            if (effect == null) continue;
            effect.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(Vector3.up, forward));
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.Play(true);
            played = true;
        }
        if (played) PlayCount++;
    }
    void OnDisable()
    {
        if (particles == null) return;
        foreach (ParticleSystem effect in particles)
            if (effect != null) effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
