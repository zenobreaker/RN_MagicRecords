using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class UIPopUp 
    : UiBase
    , IPointerClickHandler
{
    [SerializeField] private RectTransform popupArea; 

    protected abstract void DrawPopUp();

    protected virtual void OpenPopUp()
    {
        UIManager.Instance?.OpenUI(this);
    }

    public override void CloseUI()
    {
        UIManager.Instance?.ClosePopup();
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        if(popupArea != null && RectTransformUtility.RectangleContainsScreenPoint(popupArea, eventData.position) == false)
        {
            CloseUI();
        }
    }
}

public abstract class UIPopUpBase : UIPopUp
{
    private Button panelButton;

    protected virtual void Awake()
    {
        panelButton = GetComponent<Button>();

        if (panelButton != null)
        {
            panelButton.onClick.AddListener(() =>
            {
                UIManager.Instance.ClosePopup();
            });
        }
    }
}