using UnityEngine;

[DisallowMultipleComponent]
public sealed class NitroDisplay : MonoBehaviour
{
    [SerializeField] Movement movement;
    [SerializeField] HealthDisplay playerHealth;
    [SerializeField] CanvasGroup content;
    [SerializeField] UnityEngine.UI.Slider slider;
    [SerializeField, Range(0f, 1f)] float unavailableBrightness = .7f;

    UnityEngine.UI.Graphic[] barGraphics;
    Color[] barColors;

    public Movement Movement => movement;
    public UnityEngine.UI.Slider Slider => slider;
    public bool IsVisible => content != null && content.gameObject.activeInHierarchy && content.alpha > 0f;

    void Awake()
    {
        if (slider == null) return;
        barGraphics = slider.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
        barColors = new Color[barGraphics.Length];
        for (int i = 0; i < barGraphics.Length; i++) barColors[i] = barGraphics[i].color;
    }

    void OnEnable()
    {
        if (movement == null && playerHealth != null && playerHealth.Life != null)
        {
            var player = playerHealth.Life.GetComponentInParent<Rigidbody>();
            if (player != null) movement = player.GetComponentInChildren<Movement>(true);
        }
        if (movement != null) movement.NitroChanged += Refresh;
        Refresh();
    }
    void OnDisable()
    {
        if (movement != null) movement.NitroChanged -= Refresh;
        SetAvailability(true);
        SetVisible(false);
    }
    void Refresh()
    {
        bool unlocked = movement != null && movement.MaximumNitro > 0f;
        SetVisible(unlocked);
        SetAvailability(unlocked && movement.CanActivateNitro);
        if (slider == null) return;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(unlocked ? Mathf.Clamp01(movement.CurrentNitro / movement.MaximumNitro) : 0f);
    }
    void SetAvailability(bool available)
    {
        if (barGraphics == null) return;
        float brightness = available ? 1f : unavailableBrightness;
        for (int i = 0; i < barGraphics.Length; i++)
        {
            if (barGraphics[i] == null) continue;
            Color color = barColors[i];
            color.r *= brightness;
            color.g *= brightness;
            color.b *= brightness;
            barGraphics[i].color = color;
        }
    }
    void SetVisible(bool visible)
    {
        if (content == null) return;
        content.alpha = visible ? 1f : 0f;
        content.interactable = false;
        content.blocksRaycasts = false;
    }
}
