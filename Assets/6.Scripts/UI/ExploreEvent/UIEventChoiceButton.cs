using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIEventChoiceButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI choiceText;

    // (선택사항) 비활성화 시 자물쇠 아이콘이나 배경색을 바꾸고 싶다면 추가!
    // [SerializeField] private Image background; 
    // [SerializeField] private GameObject lockIcon; 

    private EventChoice choiceData;
    private Action<EventChoice> onClickCallback;
    public void Setup(
        EventChoice data,
        Action<EventChoice> callback)
    {
        choiceData = data;
        onClickCallback = callback;

        choiceText.text =
            LocalizationManager.Instance.GetText(data.TextKey);

        bool isAvailable =
            EventCostChecker.CanAfford(data);

        button.interactable = isAvailable;

        choiceText.color =
            isAvailable
            ? Color.white
            : new Color(0.4f, 0.4f, 0.4f, 1f);

        button.onClick.RemoveAllListeners();

        if (isAvailable)
            button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        onClickCallback?.Invoke(choiceData);
    }
}