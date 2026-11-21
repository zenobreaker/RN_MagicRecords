using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>정사각형 드론 아이콘에 선택 상태와 클릭 동작을 연결합니다.</summary>
public sealed class UIGuideDroneSelectButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject selectedFrame;

    private SO_GuideDroneData definition;
    private Action<SO_GuideDroneData> onSelected;

    /// <summary>재사용 슬롯의 이미지, 이름, 선택 표시와 클릭 대상을 갱신합니다.</summary>
    public void Setup(SO_GuideDroneData data, bool selected, Action<SO_GuideDroneData> callback)
    {
        definition = data;
        onSelected = callback;
        Sprite sprite = data.SafeInvoke(value => value.icon);
        icon.SafeInvoke(target =>
        {
            target.sprite = sprite;
            target.enabled = sprite != null;
        });

        string label = data.SafeInvoke(value => value.droneName, string.Empty);
        string key = data.SafeInvoke(value => value.displayNameKey);
        if (string.IsNullOrEmpty(key) == false)
        {
            label = LocalizationManager.Instance.SafeInvoke(localization => localization.GetText(key), label);
        }
        nameText.SafeInvoke(text => text.text = label);
        selectedFrame.SafeInvoke(frame => frame.SetActive(selected));
        button.SafeInvoke(target =>
        {
            target.interactable = data != null;
            target.onClick.RemoveListener(OnClick);
            target.onClick.AddListener(OnClick);
        });
    }

    /// <summary>현재 바인딩된 드론을 정보창에 전달합니다.</summary>
    private void OnClick()
    {
        if (definition != null)
        {
            onSelected?.Invoke(definition);
        }
    }
}
