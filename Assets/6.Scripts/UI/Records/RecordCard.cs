using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecordCard : MonoBehaviour
{
    public event Action<RecordData> OnRecordData;

    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image selectFrame; 
    [SerializeField] private Button selectButton;
    [SerializeField] private Button lockButton;
    [SerializeField] private TextMeshProUGUI lockText; 
    

    private RecordData myData;

    private void OnEnable()
    {
        if (AppManager.Instance == null) return;
        AppManager.Instance.OnSelectedRecordCard -= Refresh;
        AppManager.Instance.OnSelectedRecordCard += Refresh;
    }

    private void OnDisable()
    {
        if (AppManager.Instance == null) return;
        AppManager.Instance.OnSelectedRecordCard -= Refresh;
    }

    public void Setup(RecordData data,
        System.Action onClickAction = null, 
        System.Action onlockAction  = null)
    {
        myData = data;

        Debug.Assert(LocalizationManager.Instance != null);

        // 1. UI 텍스트 업데이트
        nameText.text = LocalizationManager.Instance.GetText( data.recordName);
        descText.text = LocalizationManager.Instance.GetText(data.description);
        iconImage.sprite = data.icon;

        // 2. 버튼 리스너 초기화 및 재할당
        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => 
        {
            onClickAction?.Invoke();
            OnRecordData?.Invoke(myData);
        }); 

        // 3. 등급(Rarity)에 따른 카드 테두리 색상 변경 등 연출 추가 가능
        // SetRarityColor(data.rarity);

        // 4. 잠금 기능 
        lockButton?.onClick.RemoveAllListeners();
        lockButton?.onClick.AddListener(() =>
        {
            onlockAction?.Invoke();
            DrawLockText();
        });
    }
    
    public void ClearEvent()
    {
        selectButton?.onClick.RemoveAllListeners();
        lockButton?.onClick.RemoveAllListeners();
    }

    public void Refresh()
    {
        OnSelectFrame(myData); 
    }

    public void ShowCard()
    {
        gameObject.SetActive(true); 
    }

    private void OnSelectFrame(RecordData selectedData)
    {
        if (selectedData == null || 
            AppManager.Instance == null) return;
        
        bool isSelected = AppManager.Instance.IsSelectRecordData(selectedData);
        selectFrame.gameObject.SetActive(isSelected);
    }

    private void DrawLockText()
    {
        if (lockText == null) return; 

        if(myData.isLocked)
        {
            lockText.text = "해제";
        }
        else 
        { 
            lockText.text = "잠금";
        }
    }
}
