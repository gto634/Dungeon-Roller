using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverOutline : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler
{
    private Outline buttonOutline;
    private Button buttonComponent;

    void Awake()
    {
        buttonOutline = GetComponent<Outline>();
        buttonComponent = GetComponent<Button>();

        if (buttonOutline != null)
        {
            buttonOutline.enabled = false;
        }
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        if (buttonComponent != null && buttonComponent.interactable)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
        ShowContour();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (EventSystem.current.currentSelectedGameObject != gameObject)
        {
            HideContour();
        }
    }


    public void OnSelect(BaseEventData eventData)
    {
        ShowContour();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        HideContour();
    }

    private void ShowContour()
    {
        if (buttonComponent != null && !buttonComponent.interactable)
            return;

        if (buttonOutline != null)
        {
            buttonOutline.enabled = true;
        }
    }

    private void HideContour()
    {
        if (buttonOutline != null)
        {
            buttonOutline.enabled = false;
        }
    }

    void OnDisable()
    {
        HideContour();
    }
}