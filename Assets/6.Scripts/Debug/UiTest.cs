using UnityEngine;
using UnityEngine.EventSystems;

public class UITest : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("CLICK!!!");
    }
}