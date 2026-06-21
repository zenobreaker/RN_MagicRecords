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

    private RecordUIMode currentMode;
    private RecordCard selectCard;
    private RecordManager rm;
    private List<RecordCard> cards = new();

    protected override void Awake()
    {
        base.Awake();
        if (completeButton != null)
            completeButton.onClick.AddListener(OnCompleteSelectRecord);
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        // 선택한 정보는 데이터가 세팅되면 초기화??
        selectCard = null;
        // 선택한 카드가 없으면 결정 버튼 비활성화 
        if (selectCard == null)
            completeButton.interactable = false;
        else
            completeButton.interactable = true;
    }

    public void SetData(List<RecordData> options, bool canReroll, RecordUIMode mode = RecordUIMode.DRAFT)
    {
        currentMode = mode;
        cards.Clear();

        if (AppManager.Instance != null)
            rm = AppManager.Instance.GetRecordManager();

        // 전달받은 데이터 수만큼 카드 배치 
        InitReplaceContentObject(options.Count);

        int index = 0;
        SetContentChildObjectsCallback<RecordCard>(card =>
        {
            if (index >= options.Count) return;
            cards.Add(card);

            int currentIndex = index;
            var currentData = options[currentIndex];

            card.Setup(currentData,
                onClickAction: () =>
                {
                    OnCardClicked(currentData);
                    Card_Clicked(card); 
                },
                onlockAction: () => OnLockRecord(currentData),
                canReroll);

            card.Refresh(false);
            card.ShowCard();
            index++;

        });

        // 리롤 UI 활성화/비활성화 처리
        if (rerollButton != null) rerollButton.gameObject.SetActive(canReroll);
        if (rerollText != null) rerollText.gameObject.SetActive(canReroll);

        if (canReroll)
        {
            DrawRerollText();
        }

        ShowPopUp();
    }

    private void Card_Clicked(RecordCard card)
    {
        if (card == null ) return;
        
        // 이전에 선택한 카드랑 같으면 선택 해제 
        if(selectCard != null && selectCard.Equals(card))
        {
            selectCard.Refresh(false);
            selectCard = null;

            foreach (var c in cards)
                c.Refresh(false);
        }
        // 이전에 선택한 카드랑 다르거나 선택한게 없고 들어온 정보가 있다면 해당 카드 선택
        else if (selectCard == null || selectCard.Equals(card) == false)
        {
            selectCard = card;
            card.Refresh(true);

            foreach (var c in cards)
            {
                if (c != selectCard)
                    c.Refresh(false);
            }
        }

        // 팝업 다시 그림 
        DrawPopUp(); 
    }

    private void OnCardClicked(RecordData selectedData)
    {
        if (rm == null) return;

        rm.SelectedRecord(selectedData);
    }

    private void OnCompleteSelectRecord()
    {
        if (rm == null) return; 

        bool? result = false;

        if (currentMode == RecordUIMode.DRAFT)
        {
            result = rm.OnCompleteSelctRecords();
        }
        else if (currentMode == RecordUIMode.SELECT_OWNED)
        {
            result = rm.OnCompleteArchiveRecord();
        }
        else if (currentMode == RecordUIMode.SELECT_SAVED)
        {
            result = rm.OnCompleteInheritReward();
        }
        else if (currentMode == RecordUIMode.DELETE)
        {
            result = rm.OnCompleteCostDiscard();
        }
        else if (currentMode == RecordUIMode.VIEW)
            result = true; 

        if (result.HasValue && result.Value == true)
        {
            CloseUI();

            // 💡 2. 창이 닫힌 '이후'에 이벤트를 발생시켜, 새로운 보상창(DRAFT 모드)이 열리도록 합니다!
            if (currentMode == RecordUIMode.DELETE)
            {
                rm.TriggerCostPaidEvent();
            }
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

        rerollText.text = rm.RerollCount.ToString();
    }

    public void Reroll()
    {
        if (rm == null) return;

        rm.RerollAllCurrentRecords(); 
    }

    protected override void DrawPopUp()
    {
        // 선택한 카드가 없으면 결정 버튼 비활성화 
        if (currentMode != RecordUIMode.VIEW && selectCard == null)
            completeButton.interactable = false;
        else 
            completeButton.interactable = true;
    }
}
