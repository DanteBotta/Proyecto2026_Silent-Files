using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI Texto;

    public void OnPointerEnter(PointerEventData eventData)
    {
        Texto.fontStyle |= FontStyles.Underline;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Texto.fontStyle &= ~FontStyles.Underline;
    }
}