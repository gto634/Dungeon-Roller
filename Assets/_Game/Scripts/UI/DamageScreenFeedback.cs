using UnityEngine;

[DisallowMultipleComponent]
public sealed class DamageScreenFeedback : MonoBehaviour
{
    [SerializeField] HealthDisplay healthDisplay;
    [SerializeField] ScreenEdgeVignette vignette;
    [SerializeField, Range(0f, 1f)] float lowHealthThreshold = .2f;
    [SerializeField, Range(0f, 1f)] float damageOpacity = 75f / 255f;
    [SerializeField, Min(.01f)] float damageFadeDuration = .7f;
    [SerializeField, Range(0f, 1f)] float lowHealthRestOpacity = .035f;
    [SerializeField, Range(0f, 1f)] float lowHealthPeakOpacity = .2f;
    [SerializeField, Min(.1f)] float heartbeatPeriod = 1f;

    LifeSystem life;
    float flashOpacity, heartbeatElapsed;
    int previousHealth;
    bool lowHealth;

    void OnEnable()
    {
        life = healthDisplay != null ? healthDisplay.Life : null;
        if (vignette != null) vignette.raycastTarget = false;
        if (life != null)
        {
            previousHealth = life.Health;
            life.OnDamage += OnDamage;
            life.Changed += RefreshHealth;
        }
        RefreshHealth();
    }
    void OnDisable()
    {
        if (life != null)
        {
            life.OnDamage -= OnDamage;
            life.Changed -= RefreshHealth;
        }
        life = null;
        lowHealth = false;
        flashOpacity = heartbeatElapsed = 0f;
        SetOpacity(0f);
    }
    void OnDamage(int before, int after)
    {
        if (after >= before || after <= 0) return;
        flashOpacity = IsLowHealth(after) ? lowHealthPeakOpacity : damageOpacity;
        Render();
    }
    bool IsLowHealth(int health)
    {
        if (life == null || health <= 0 || life.MaxHealth <= 0) return false;
        float ratio = (float)health / life.MaxHealth;
        return ratio < lowHealthThreshold && !Mathf.Approximately(ratio, lowHealthThreshold);
    }

    void RefreshHealth()
    {
        bool nextLowHealth = life != null && IsLowHealth(life.Health);
        if (nextLowHealth != lowHealth) heartbeatElapsed = 0f;
        lowHealth = nextLowHealth;
        if (life == null || life.IsDead || life.Health > previousHealth) flashOpacity = 0f;
        if (life != null) previousHealth = life.Health;
        Render();
    }
    void Update()
    {
        if (life == null || life.IsDead) return;
        flashOpacity = Mathf.MoveTowards(flashOpacity, 0f,
            Time.deltaTime * damageOpacity / Mathf.Max(.01f, damageFadeDuration));
        if (lowHealth)
            heartbeatElapsed = Mathf.Repeat(heartbeatElapsed + Time.deltaTime, Mathf.Max(.1f, heartbeatPeriod));
        Render();
    }
    void Render()
    {
        if (life == null || life.IsDead) { SetOpacity(0f); return; }
        float opacity = flashOpacity;
        if (lowHealth)
        {
            float phase = heartbeatElapsed / Mathf.Max(.1f, heartbeatPeriod);

            float beat = Mathf.Max(Beat(phase, .12f, .12f), .6f * Beat(phase, .34f, .1f));
            float pulse = Mathf.Lerp(lowHealthRestOpacity, lowHealthPeakOpacity, beat);
            opacity = Mathf.Min(lowHealthPeakOpacity, Mathf.Max(opacity, pulse));
        }
        SetOpacity(opacity);
    }
    static float Beat(float phase, float centre, float width) =>
        Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(1f - Mathf.Abs(phase - centre) / width));

    void SetOpacity(float opacity)
    {
        if (vignette != null) vignette.canvasRenderer.SetAlpha(Mathf.Clamp01(opacity));
    }
}
