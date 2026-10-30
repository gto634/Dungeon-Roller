using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HealthDisplay : MonoBehaviour
{
    [SerializeField] LifeSystem life;
    [SerializeField] UnityEngine.UI.Slider slider;
    [SerializeField] TextMeshProUGUI label;
    [SerializeField, Min(0f)] float damageShakeAmplitude = 3f;
    [SerializeField, Min(.01f)] float damageShakeDuration = .2f;
    [SerializeField, Min(.01f)] float damageShakeFrequency = 28f;

    RectTransform[] shakeTargets;
    Vector2[] restPositions;
    float shakeElapsed = -1f;

    public LifeSystem Life => life;

    void OnEnable()
    {
        CacheShakeTargets();
        if (life == null) return;
        life.Changed += Refresh;
        life.OnDamage += Shake;
        Refresh();
    }
    void OnDisable()
    {
        if (life != null)
        {
            life.Changed -= Refresh;
            life.OnDamage -= Shake;
        }
        StopShake();
    }
    void CacheShakeTargets()
    {
        StopShake();
        if (slider == null) return;
        var bar = slider.transform;
        shakeTargets = new RectTransform[bar.childCount];
        restPositions = new Vector2[bar.childCount];
        for (int index = 0; index < bar.childCount; index++)
        {
            var target = bar.GetChild(index) as RectTransform;
            shakeTargets[index] = target;
            if (target != null) restPositions[index] = target.anchoredPosition;
        }
    }
    void Shake(int before, int after)
    {
        if (after >= before || damageShakeAmplitude <= 0f || shakeTargets == null) return;
        shakeElapsed = 0f;
        ApplyShake();
    }
    void LateUpdate()
    {
        if (shakeElapsed < 0f) return;
        shakeElapsed += Time.unscaledDeltaTime;
        if (shakeElapsed >= damageShakeDuration)
        {
            StopShake();
            return;
        }
        ApplyShake();
    }
    void ApplyShake()
    {
        float strength = damageShakeAmplitude * (1f - Mathf.Clamp01(shakeElapsed / Mathf.Max(.01f, damageShakeDuration)));
        float phase = shakeElapsed * damageShakeFrequency * Mathf.PI * 2f;
        Vector2 offset = new Vector2(Mathf.Sin(phase), Mathf.Sin(phase * 1.31f + 1.1f) * .5f) * strength;
        for (int index = 0; index < shakeTargets.Length; index++)
            if (shakeTargets[index] != null) shakeTargets[index].anchoredPosition = restPositions[index] + offset;
    }
    void StopShake()
    {
        shakeElapsed = -1f;
        if (shakeTargets == null) return;
        for (int index = 0; index < shakeTargets.Length; index++)
            if (shakeTargets[index] != null) shakeTargets[index].anchoredPosition = restPositions[index];
    }
    void Refresh()
    {
        if (life == null) return;
        if (slider != null)
        {
            slider.minValue = 0; slider.maxValue = Mathf.Max(1, life.MaxHealth);
            slider.value = life.Health;
        }
        if (label != null) label.text = $"{life.Health} / {life.MaxHealth} PV";
    }
}
