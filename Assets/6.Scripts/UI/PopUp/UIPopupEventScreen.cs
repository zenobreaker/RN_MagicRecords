using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPopupEventScreen : UIPopUp
{
    [Header("UI References")]
    [SerializeField] private Image illustrationImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("Event Data Assets")]
    [SerializeField] private SO_EventImagePalette eventImagePalette;

    [Header("Choice Area")]
    [SerializeField] private Transform choiceContainer;
    [SerializeField] private UIEventChoiceButton choicePrefab;
    [SerializeField] private CanvasGroup choiceContainerGroup;

    [Header("Typewriter Settings")]
    [SerializeField] private float typingSpeed = 0.05f;

    private EventInfo currentEvent;
    private CancellationTokenSource typingCts;
    private bool isTyping = false;

    private RecordManager recordManager;
    private EventChoice pendingChoice;

    // 연타(더블 클릭)를 막기 위한 방어 변수들
    private bool isProcessingChoice = false;
    private bool isClosing = false;
    private bool isStageCleared = false;

    public void SetData(EventInfo eventInfo)
    {
        currentEvent = eventInfo;
        isProcessingChoice = false; // 팝업이 열릴 때 초기화
        isClosing = false;

        isStageCleared = false; 

        if (AppManager.Instance != null)
        {
            recordManager = AppManager.Instance.GetRecordManager();
        }

        ShowPopUp();
    }

    private void Update()
    {
        if (isTyping && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
        {
            SkipTyping();
        }
    }

    private async UniTaskVoid DrawPopUpAsync()
    {
        if (eventImagePalette != null && !string.IsNullOrEmpty(currentEvent.imageKey))
        {
            Sprite evSprite = eventImagePalette.GetImage(currentEvent.imageKey);
            if (evSprite != null)
            {
                illustrationImage.sprite = evSprite;
                illustrationImage.gameObject.SetActive(true);
            }
            else
            {
                illustrationImage.gameObject.SetActive(false);
            }
        }
        else
        {
            illustrationImage.gameObject.SetActive(false);
        }

        titleText.text = LocalizationManager.Instance.GetText(currentEvent.nameKey);
        string descText = LocalizationManager.Instance.GetText(currentEvent.descriptionKey);

        choiceContainerGroup.alpha = 0f;
        choiceContainer.gameObject.SetActive(false);

        PrepareChoices();

        CancelTypingTask();
        typingCts = new CancellationTokenSource();
        await TypewriterAsync(descText, typingCts.Token);

        ShowChoicesWithAnimation(typingCts.Token).Forget();
    }

    private void PrepareChoices()
    {
        foreach (Transform child in choiceContainer)
            Destroy(child.gameObject);

        foreach (EventChoice choice in currentEvent.eventChoices)
        {
            if (choice.ChoiceIsActive == false)
                continue;

            UIEventChoiceButton btn = Instantiate(choicePrefab, choiceContainer);
            btn.Setup(choice, OnChoiceSelected);
        }
    }

    private async UniTaskVoid ShowChoicesWithAnimation(CancellationToken token)
    {
        choiceContainer.gameObject.SetActive(true);

        float duration = 0.3f;
        float elapsed = 0f;
        choiceContainer.localScale = Vector3.one * 0.9f;

        try
        {
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                choiceContainerGroup.alpha = Mathf.Lerp(0, 1, t);
                choiceContainer.localScale = Vector3.Lerp(Vector3.one * 0.9f, Vector3.one, t);

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
            }

            choiceContainerGroup.alpha = 1f;
            choiceContainer.localScale = Vector3.one;
        }
        catch (OperationCanceledException)
        {
            choiceContainerGroup.alpha = 1f;
            choiceContainer.localScale = Vector3.one;
        }
    }

    private async UniTask TypewriterAsync(string fullText, CancellationToken token)
    {
        isTyping = true;
        descriptionText.text = fullText;
        descriptionText.maxVisibleCharacters = 0;
        descriptionText.ForceMeshUpdate();

        int totalCharacters = descriptionText.textInfo.characterCount;

        try
        {
            for (int i = 0; i <= totalCharacters; i++)
            {
                descriptionText.maxVisibleCharacters = i;
                await UniTask.Delay(TimeSpan.FromSeconds(typingSpeed), cancellationToken: token);
            }
        }
        catch (OperationCanceledException)
        {
            descriptionText.maxVisibleCharacters = totalCharacters;
        }
        finally
        {
            isTyping = false;
        }
    }

    public void SkipTyping()
    {
        if (isTyping && typingCts != null)
        {
            typingCts.Cancel();
        }
    }

    private void CancelTypingTask()
    {
        if (typingCts != null)
        {
            typingCts.Cancel();
            typingCts.Dispose();
            typingCts = null;
        }
    }

    private void OnChoiceSelected(EventChoice choice)
    {
        // 선택지가 이미 처리 중(isProcessingChoice)이라면 연타 무시!
        if (isTyping || recordManager == null || isProcessingChoice) return;

        if (!TryPayCost(choice))
        {
            return;
        }

        // 지불에 성공했거나 코스트가 없다면 진행 상태로 잠금
        isProcessingChoice = true;
        ProcessRewardAndResult(choice);
    }

    private bool TryPayCost(EventChoice choice)
    {
        if (choice.CostType == EventCostType.NONE) return true;

        if (choice.CostType == EventCostType.RECORD_ANY)
        {
            List<RecordData> myRecords = recordManager.GetPossesRecord();
            if (myRecords == null || myRecords.Count == 0)
            {
                Debug.LogWarning("소모할 레코드가 없습니다!");
                return false;
            }

            pendingChoice = choice;
            recordManager.OnCostPaidSuccess -= ResumePendingChoice;
            recordManager.OnCostPaidSuccess += ResumePendingChoice;

            UIManager.Instance.OpenRecordSelectPopUp(myRecords, false, RecordUIMode.DELETE);
            return false;
        }

        if (choice.CostType == EventCostType.GOLD ||
            choice.CostType == EventCostType.EXPLORE_COIN)
        {
            CurrencyType ctype = CurrencyType.GOLD;
            if (choice.CostType == EventCostType.GOLD)
                ctype = CurrencyType.GOLD;
            else if (choice.CostType == EventCostType.EXPLORE_COIN)
                ctype = CurrencyType.EXPOLORE_GOLD;

            if (CurrencyManager.Instance.SpendCurrency(ctype, choice.CostValue)) return true;
            return false;
        }

        return true;
    }

    private void ResumePendingChoice()
    {
        if (recordManager != null)
        {
            recordManager.OnCostPaidSuccess -= ResumePendingChoice;
        }

        if (pendingChoice != null)
        {
            isProcessingChoice = true; // 💡 여기서도 락을 걸어줍니다.
            ProcessRewardAndResult(pendingChoice);
            pendingChoice = null;
        }
    }

    private void ProcessRewardAndResult(EventChoice choice)
    {
        bool isSuccess = UnityEngine.Random.Range(0, 100) < choice.Probability;

        string resultKey = isSuccess ? choice.ResultTextKey : choice.FailTextKey;
        if (string.IsNullOrEmpty(resultKey)) resultKey = "ui_event_default_result";

        string resultText = LocalizationManager.Instance.GetText(resultKey);

        ShowResultPhaseAsync(resultText).Forget();

        if (choice.RewardType == EventRewardType.RECORD_DRAFT)
        {
            recordManager.GenerateEventRecords(10, false);
        }
        else if (choice.RewardType == EventRewardType.ARCHIVE_SAVE)
        {
            OpenArchiveRecordUI();
        }
        else if (choice.RewardType == EventRewardType.ARCHIVE_LOAD)
            OpenLastSavedRecordUI();
        else
            OpenTargetRarityRecord(choice.RewardType);
    }

    private void OpenTargetRarityRecord(EventRewardType type)
    {
        if (recordManager == null) return;

        List<RecordData> records = new List<RecordData>();

        switch (type)
        {
            case EventRewardType.RECORD_ONE_OF_NORMAL:
                records = recordManager.GetNormalRecordDatas();
                break;
            case EventRewardType.RECORD_ONE_OF_RARE:
                records = recordManager.GetRareRecordDatas();
                break;
            case EventRewardType.RECORD_ONE_OF_UNIQUE:
                records = recordManager.GetUniqueRecordDatas();
                break;
            case EventRewardType.RECORD_ONE_OF_LEGEND:
                records = recordManager.GetLengdaryRecordDatas();
                break;
            case EventRewardType.RECORD_ONE_OF_MYTH:
                records = recordManager.GetMythRecordDatas();
                break;
            default:
                Debug.LogWarning($"[OpenTargetRarityRecord] 처리되지 않은 레코드 보상 타입입니다: {type}");
                return;
        }

        if (records == null || records.Count == 0)
        {
            Debug.LogWarning($"[{type}] 등급의 레코드가 없거나 모두 소진되었습니다.");
            records = new List<RecordData> { recordManager.GetEmptyRecord() };
        }

        UIManager.Instance.OpenRecordSelectPopUp(records, false, RecordUIMode.DRAFT);
    }

    private async UniTaskVoid ShowResultPhaseAsync(string resultText)
    {
        choiceContainerGroup.alpha = 0f;
        choiceContainer.gameObject.SetActive(false);

        foreach (Transform child in choiceContainer)
            Destroy(child.gameObject);

        UIEventChoiceButton closeBtn = Instantiate(choicePrefab, choiceContainer);
        EventChoice closeChoice = new EventChoice() { TextKey = "ui_btn_leave", CostType = EventCostType.NONE };
        closeBtn.Setup(closeChoice, _ => ClosePopup());

        CancelTypingTask();
        typingCts = new CancellationTokenSource();
        await TypewriterAsync(resultText, typingCts.Token);

        ShowChoicesWithAnimation(typingCts.Token).Forget();
    }

    private void ClosePopup()
    {
        // "닫기(떠난다)" 버튼 연타 방어
        if (isClosing) return;

        if (isTyping)
        {
            SkipTyping();
            return;
        }

        isClosing = true; // 이후 로직 진입 잠금
        CancelTypingTask();
        UIManager.Instance.CloseTopUI();
    }

    protected override void OnDisable()
    {
        CancelTypingTask();

        if (!isStageCleared && AppManager.Instance != null)
        {
            isStageCleared = true; // 중복 실행 방지

            var exploreManager = AppManager.Instance.GetExploreManager();
            if (exploreManager != null)
            {
                // 여기서 안전하게 스테이지를 클리어하고 맵 노드를 갱신합니다.
                exploreManager.ClearStage(true);
            }
        }

        base.OnDisable();
    }

    protected override void DrawPopUp()
    {
        DrawPopUpAsync().Forget();
    }

    private void OpenArchiveRecordUI()
    {
        if (recordManager == null) return;

        List<RecordData> myRecords = recordManager.GetPossesRecord();

        if (myRecords != null && myRecords.Count > 0)
        {
            UIManager.Instance.OpenRecordSelectPopUp(myRecords, false, RecordUIMode.SELECT_OWNED);
        }
        else
        {
            Debug.LogWarning("보관할(가진) 레코드가 하나도 없습니다!");
            // ❌ [치명적 버그 수정] 여기서 ClearStage(true)를 지웠습니다! 
            // 유저는 어차피 결과 화면의 "떠난다" 버튼을 눌러야 하므로, 클리어 처리는 ClosePopup()이 담당하게 둡니다.
        }
    }

    private void OpenLastSavedRecordUI()
    {
        if (recordManager == null) return;

        List<RecordData> transferedDatas = recordManager.GetTransferedRecordIDs();

        if (transferedDatas != null && transferedDatas.Count > 0)
        {
            UIManager.Instance.OpenRecordSelectPopUp(transferedDatas, false, RecordUIMode.SELECT_SAVED);
        }
        else
        {
            Debug.LogWarning("보관할(가진) 레코드가 하나도 없습니다!");
            // ❌ [치명적 버그 수정] 여기서 ClearStage(true)를 지웠습니다!
        }
    }
}