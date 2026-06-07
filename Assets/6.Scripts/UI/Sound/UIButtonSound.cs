using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    private Button button;

    [SerializeField] private string hoverSoundName = "";
    [SerializeField] private string clickSoundName = "UI_Click";

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    // 마우스가 버튼 위에 올라갔을 때 (Hover)
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button.interactable)
        {
            SoundManager.Instance.PlaySFX(hoverSoundName); // 가벼운 전자음 
        }
    }

    // 버튼을 클릭했을 때 (Click)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (button.interactable)
        {
            SoundManager.Instance.PlaySFX(clickSoundName); // 확실하고 묵직한 클릭음
        }
    }
}