using UnityEngine;

[DisallowMultipleComponent]
public sealed class NitroFlamesFeedback : MonoBehaviour
{
    [SerializeField] ParticleSystem[] flames;

    public System.Collections.Generic.IReadOnlyList<ParticleSystem> Flames => flames;

    public void Play()
    {
        if (!isActiveAndEnabled || flames == null) return;
        foreach (var flame in flames)
        {
            if (flame == null) continue;
            flame.gameObject.SetActive(true);
            flame.Play(true);
        }
    }
    public void Stop()
    {
        if (flames == null) return;
        foreach (var flame in flames)
        {
            if (flame == null) continue;
            flame.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            flame.gameObject.SetActive(false);
        }
    }
    void OnDisable() => Stop();
}
