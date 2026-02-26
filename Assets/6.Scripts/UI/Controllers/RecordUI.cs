using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class RecordUI : UiBase
{
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private TextMeshProUGUI rerollText;
    [SerializeField] private Button completeButton;


    private List<RecordCard> cardPool = new();

    //TODO : 레코드 선택 UI는 탐사 진입할 때 호출되는 타이밍이 있으므로 다른 경로로 이벤트를 
    // 연결해야한다.

    private void Awake()
    {
        if (AppManager.Instance != null)
        {
            AppManager.Instance.OnShowRecordUI += ShowUI;
        }

        visualRoot.SetActive(false);
        if (completeButton != null)
            completeButton.onClick.AddListener(OnCompleteSelectRecord);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (AppManager.Instance != null)
            AppManager.Instance.OnShowRecordUI -= ShowUI;
    }

    private void ShowUI(List<RecordData> options)
    {
        visualRoot.SetActive(true);

        // 1. 모든 카드를 일단 비활성화 
        foreach (var card in cardPool)
            card.gameObject.SetActive(false);


        //2. 전달받은 데이터 수만큼 카드 배치 
        InitReplaceContentObject(options.Count);

        int index = 0;
        SetContentChildObjectsCallback<RecordCard>(card =>
        {
            if (index >= options.Count) return;

            int currentIndex = index;
            var currentData = options[currentIndex];

            card.Setup(currentData,
                () => OnCardClicked(currentData),
                () => OnLockRecord(currentData));

            card.Refresh();
            card.ShowCard();
            index++;

        });

        //3. 리롤 카운트 텍스트 그리기
        DrawRerollText();

    }


    private void OnCardClicked(RecordData selectedData)
    {
        AppManager.Instance?.OnRecordSelected(selectedData);
    }

    private void OnCompleteSelectRecord()
    {
        bool? result = AppManager.Instance?.OnCompleteSelctRecords();
        if (result.HasValue && result.Value == true)
        {
            visualRoot.SetActive(false);
        }
        else
        {
            //TODO : 레코드를 선택하라는 알림 띄우기 
        }
    }

    private void OnLockRecord(RecordData recordData)
    {
        recordData.isLocked = !recordData.isLocked;
    }

    private void DrawRerollText()
    {
        if (rerollText == null) return;

        rerollText.text = AppManager.Instance.GetRerollCount().ToString();
    }

    public void Reroll()
    {
        AppManager.Instance?.RerollAllRecords();
    }
}
