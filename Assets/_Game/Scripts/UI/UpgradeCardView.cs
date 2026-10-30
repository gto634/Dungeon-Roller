using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Image = UnityEngine.UI.Image;

[ExecuteAlways, DisallowMultipleComponent]
public sealed class UpgradeCardView : MonoBehaviour, UnityEngine.UI.IMaterialModifier, ISelectHandler, IDeselectHandler, IPointerEnterHandler
{
    [SerializeField] Image artwork;
    [SerializeField] TextMeshProUGUI label;
    [SerializeField] TextMeshProUGUI rarityLabel;
    [SerializeField] StatDefinition stat;
    [SerializeField] RarityDefinition rarity;
    [SerializeField] GameObject selectionArrow;
    Material instance;
    static readonly int StatColor = Shader.PropertyToID("_StatColor");
    static readonly int RarityColor = Shader.PropertyToID("_RarityColor");

    public StatDefinition Stat => stat;
    public RarityDefinition Rarity => rarity;
    public Image Artwork => artwork;

    void OnEnable()
    {
        RefreshAppearance();
        SetSelected(EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject);
    }
    void OnDisable() { SetSelected(false); ReleaseMaterial(); }
    void OnDestroy() => ReleaseMaterial();

    public void OnSelect(BaseEventData eventData) => SetSelected(true);
    public void OnDeselect(BaseEventData eventData) => SetSelected(false);
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (EventSystem.current == null || !TryGetComponent(out UnityEngine.UI.Button button) || !button.IsInteractable()) return;
        EventSystem.current.SetSelectedGameObject(gameObject);
    }
    void SetSelected(bool selected)
    {
        if (selectionArrow != null) selectionArrow.SetActive(selected);
    }

    public void Present(StatDefinition definition, RarityDefinition quality, string text)
    {
        stat = definition; rarity = quality;
        if (label != null) label.text = text;
        if (rarityLabel != null) rarityLabel.text = quality != null ? quality.DisplayName : string.Empty;
        RefreshAppearance();
    }

    void RefreshAppearance()
    {
        if (artwork != null) artwork.SetMaterialDirty();
    }

    public Material GetModifiedMaterial(Material baseMaterial)
    {
        if (!isActiveAndEnabled || baseMaterial == null || stat == null || rarity == null) return baseMaterial;
        if (!baseMaterial.HasProperty(StatColor)) return baseMaterial;
        if (instance == null)
            instance = new Material(baseMaterial) { name = "Upgrade card (instance)", hideFlags = HideFlags.HideAndDontSave };
        instance.CopyPropertiesFromMaterial(baseMaterial);
        instance.SetColor(StatColor, stat.Color);
        instance.SetColor(RarityColor, rarity.Color);
        return instance;
    }

    void ReleaseMaterial()
    {
        if (instance == null) return;
        if (Application.isPlaying) Destroy(instance); else DestroyImmediate(instance);
        instance = null;
        if (artwork != null) artwork.SetMaterialDirty();
    }
}
