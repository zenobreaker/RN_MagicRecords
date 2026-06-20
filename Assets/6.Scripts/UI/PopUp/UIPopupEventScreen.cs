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

    private EventExecutionResult currentResult;

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
        currentResult =
            EventChoiceExecutor.Execute(choice);

        if (currentResult == null)
            return;

        ProcessRewardAndResult(currentResult);
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
            ProcessRewardAndResult(currentResult);
            pendingChoice = null;
        }
    }

    private void ProcessRewardAndResult(
        EventExecutionResult result)
    {
        string resultKey =
            result.IsSuccess
            ? result.Choice.ResultTextKey
            : result.Choice.FailTextKey;

        string resultText =
            LocalizationManager.Instance.GetText(resultKey);

        ShowResultPhaseAsync(resultText, 
            result.Choice.ResultButtonTextKey).Forget();

        if (!result.NeedCombat)
        {
            if (result.IsSuccess)
            {
                EventActionProcessor.Execute(result.Choice);
                EventRewardProcessor.GiveReward(result.Choice);
            }
        }
    }


    private void OpenTargetRarityRecord(string rarityParam)
    {
        if (recordManager == null) return;

        List<RecordData> records = new List<RecordData>();

        // 문자열 파라미터를 RecordRarity Enum으로 변환하여 해당 등급의 리스트를 요청합니다.
        if (Enum.TryParse(rarityParam, out RecordRarity targetRarity))
        {
            // 💡 RecordManager에 GetUnpossessedRecordDatas(RecordRarity) 같은 함수가 
            // public으로 열려있다고 가정하고 사용합니다.
            // 만약 없다면, switch문으로 GetRareRecordDatas() 등을 직접 연결하셔도 됩니다.

            switch (targetRarity)
            {
                case RecordRarity.NORMAL: records = recordManager.GetNormalRecordDatas(); break;
                case RecordRarity.RARE: records = recordManager.GetRareRecordDatas(); break;
                case RecordRarity.UNIQUE: records = recordManager.GetUniqueRecordDatas(); break;
                case RecordRarity.LEGENDARY: records = recordManager.GetLengdaryRecordDatas(); break;
                case RecordRarity.MYTH: records = recordManager.GetMythRecordDatas(); break;
            }
        }
        else
        {
            Debug.LogWarning($"[OpenTargetRarityRecord] 알 수 없는 레코드 등급 파라미터입니다: {rarityParam}");
            return;
        }

        if (records == null || records.Count == 0)
        {
            Debug.LogWarning($"[{rarityParam}] 등급의 레코드가 없거나 모두 소진되었습니다.");
            records = new List<RecordData> { recordManager.GetEmptyRecord() };
        }

        // 특정 등급의 레코드 창을 띄울 때 몇 개를 띄울지는 기획에 따라 조절하세요 (현재는 리스트 전체를 넘김)
        UIManager.Instance.OpenRecordSelectPopUp(records, false, RecordUIMode.DRAFT);
    }

    private async UniTaskVoid ShowResultPhaseAsync(string resultText,
          string buttonTextKey = "ui_btn_leave")
    {
        choiceContainerGroup.alpha = 0f;
        choiceContainer.gameObject.SetActive(false);

        foreach (Transform child in choiceContainer)
            Destroy(child.gameObject);

        UIEventChoiceButton closeBtn = Instantiate(choicePrefab, choiceContainer);
        EventChoice closeChoice = new EventChoice() { TextKey = buttonTextKey, CostType = EventCostType.NONE };
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
        // 💡 팝업이 완전히 닫힐 때, 보류해둔 전투가 있다면 ExploreManager에게 지시합니다!
        var exploreManager = AppManager.Instance.GetExploreManager();
        if (exploreManager != null)
        {
            if (currentResult != null &&
                currentResult.NeedCombat)
            {
                EventActionProcessor.Execute(currentResult.Choice);
            }
            else
            {
                exploreManager.ClearStage(true);
            }
        }

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