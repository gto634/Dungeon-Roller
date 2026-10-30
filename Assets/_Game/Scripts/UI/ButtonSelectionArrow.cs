using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent, RequireComponent(typeof(UnityEngine.UI.Button))]
public sealed class ButtonSelectionArrow : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [SerializeField] GameObject selectionArrow;
    UnityEngine.UI.Button button;

    void OnEnable()
    {
        button = GetComponent<UnityEngine.UI.Button>();
        SetSelected(EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject);
    }
    void OnDisable() => SetSelected(false);
    public void OnSelect(BaseEventData eventData) => SetSelected(true);
    public void OnDeselect(BaseEventData eventData) => SetSelected(false);
    void SetSelected(bool selected)
    {
        if (button == null) button = GetComponent<UnityEngine.UI.Button>();
        if (selectionArrow != null) selectionArrow.SetActive(selected && button != null && button.IsInteractable());
    }
}
