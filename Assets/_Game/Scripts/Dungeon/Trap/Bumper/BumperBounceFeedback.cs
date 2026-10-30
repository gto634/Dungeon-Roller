using UnityEngine;

[DisallowMultipleComponent]
public sealed class BumperBounceFeedback : MonoBehaviour
{
    [SerializeField] BrainBumper bump;
    [SerializeField] Transform bumpersSides;
    [SerializeField] Transform bumperCap;
    [SerializeField, Min(1f)] float scaleMultiplier = 1.15f;
    [SerializeField, Min(.001f)] float expandDuration = .06f;
    [SerializeField, Min(.001f)] float returnDuration = .14f;

    Vector3 sidesRestScale, capRestScale, sidesFromScale, capFromScale;
    float elapsed;
    bool playing, captured;

    void OnEnable()
    {
        sidesRestScale = bumpersSides != null ? bumpersSides.localScale : Vector3.one;
        capRestScale = bumperCap != null ? bumperCap.localScale : Vector3.one;
        captured = true;
        if (bump != null) bump.OnBump += Play;
    }

    void OnDisable()
    {
        if (bump != null) bump.OnBump -= Play;
        Restore();
    }

    void Play()
    {
        if (!isActiveAndEnabled) return;
        sidesFromScale = bumpersSides != null ? bumpersSides.localScale : sidesRestScale;
        capFromScale = bumperCap != null ? bumperCap.localScale : capRestScale;
        elapsed = 0f;
        playing = true;
    }

    void Update()
    {
        if (!playing) return;
        elapsed += Time.deltaTime;
        float expand = Mathf.Max(.001f, expandDuration);
        float multiplier = Mathf.Max(1f, scaleMultiplier);
        Vector3 sidesPeak = sidesRestScale * multiplier;
        Vector3 capPeak = capRestScale * multiplier;
        if (elapsed < expand)
        {
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / expand);
            Apply(Vector3.LerpUnclamped(sidesFromScale, sidesPeak, progress), Vector3.LerpUnclamped(capFromScale, capPeak, progress));
            return;
        }
        float returning = Mathf.Clamp01((elapsed - expand) / Mathf.Max(.001f, returnDuration));
        float eased = Mathf.SmoothStep(0f, 1f, returning);
        Apply(Vector3.LerpUnclamped(sidesPeak, sidesRestScale, eased), Vector3.LerpUnclamped(capPeak, capRestScale, eased));
        if (returning >= 1f) Restore();
    }

    void Apply(Vector3 sides, Vector3 cap)
    {
        if (bumpersSides != null) bumpersSides.localScale = sides;
        if (bumperCap != null) bumperCap.localScale = cap;
    }

    void Restore()
    {
        playing = false;
        if (captured) Apply(sidesRestScale, capRestScale);
    }
}
