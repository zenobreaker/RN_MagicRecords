using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class RecordUI : UIPopUp
{
    [Header("Reroll UI")]
    [SerializeField] private Button rerollButton;
    [SerializeField] private TextMeshProUGUI rerollText;

    [Header("Confirm UI")]
    [SerializeField] private Button completeButton;

    private List<RecordCard> cardPool = new();
    private RecordUIMode currentMode;

    private void Awake()
    {
        if (completeButton != null)
            completeButton.onClick.AddListener(OnCompleteSelectRecord);
    }

    protected override void DrawPopUp()
    {

    }

    public void ShowUI(List<RecordData> options, bool canReroll, RecordUIMode mode = RecordUIMode.DRAFT)
    {
        currentMode = mode;

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

        // 💡 3. 리롤 UI 활성화/비활성화 처리
        if (rerollButton != null) rerollButton.gameObject.SetActive(canReroll);
        if (rerollText != null) rerollText.gameObject.SetActive(canReroll);

        //3. 리롤 카운트 텍스트 그리기
        if (canReroll)
        {
            DrawRerollText();
        }
    }


    private void OnCardClicked(RecordData selectedData)
    {
        AppManager.Instance?.OnRecordSelected(selectedData);
    }

    private void OnCompleteSelectRecord()
    {
        bool? result = false;

        if (currentMode == RecordUIMode.DRAFT || currentMode == RecordUIMode.SELECT_SAVED)
        {
            result = AppManager.Instance?.OnCompleteSelctRecords();
        }
        else if (currentMode == RecordUIMode.SELECT_OWNED)
        {
            result = AppManager.Instance?.OnCompleteArchiveRecord();
        }

        if (result.HasValue && result.Value == true)
        {
            CloseUI();

            // 아카이브 모드였다면, 레코드 선택이 곧 이벤트의 종료이므로 스테이지를 클리어 처리합니다.
            //if (currentMode == RecordUIMode.SelectOwned)
            //{
            //    AppManager.Instance.GetExploreManager().ClearStage(true);
            //}
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
