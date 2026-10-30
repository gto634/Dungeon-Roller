using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LevelDisplay : MonoBehaviour
{
    [SerializeField] LevelManager levelManager;
    [SerializeField] UnityEngine.UI.Slider slider;
    [SerializeField] TextMeshProUGUI label;

    void OnEnable()
    {
        if (levelManager == null) return;
        levelManager.Changed += Refresh;
        levelManager.Initialize();
        Refresh();
    }
    void OnDisable()
    {
        if (levelManager != null) levelManager.Changed -= Refresh;
    }
    void Refresh()
    {
        if (slider != null)
        {
            slider.minValue = 0; slider.maxValue = levelManager.ExperienceToReach;
            slider.value = levelManager.Experience;
        }
        if (label != null) label.text = $"{(int)levelManager.Experience} / {(int)levelManager.ExperienceToReach} XP";
    }
}
