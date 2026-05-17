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

    // 💡 방금 추가한 CanvasGroup을 인스펙터에서 할당해주세요!
    [SerializeField] private CanvasGroup choiceContainerGroup;

    [Header("Typewriter Settings")]
    [SerializeField] private float typingSpeed = 0.05f;

    private EventInfo currentEvent;
    private CancellationTokenSource typingCts;
    private bool isTyping = false;

    private RecordManager recordManager;
    private EventChoice pendingChoice;

    public void SetData(EventInfo eventInfo)
    {
        currentEvent = eventInfo;

        if (AppManager.Instance != null)
        {
            recordManager = AppManager.Instance.GetRecordManager();
        }

        ShowPopUp();
    }

    // 💡 화면 클릭이나 스페이스바 감지 (스킵 기능)
    private void Update()
    {
        if (isTyping && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
        {
            SkipTyping();
        }
    }

    // 💡 비동기(Async) 기반의 팝업 그리기
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
                // 이미지를 찾지 못하면 기본 이미지를 유지하거나 숨깁니다.
                illustrationImage.gameObject.SetActive(false);
            }
        }
        else
        {
            illustrationImage.gameObject.SetActive(false);
        }

        titleText.text = LocalizationManager.Instance.GetText(currentEvent.nameKey);
        string descText = LocalizationManager.Instance.GetText(currentEvent.descriptionKey);

        // 1. 버튼들이 들어갈 공간을 투명하게 만들고 숨겨둡니다.
        choiceContainerGroup.alpha = 0f;
        choiceContainer.gameObject.SetActive(false);

        // 2. 버튼들을 미리 생성해둡니다. (아직 안 보임)
        PrepareChoices();

        // 3. 타이핑 시작! (타이핑이 끝날 때까지 여기서 await로 대기합니다)
        CancelTypingTask();
        typingCts = new CancellationTokenSource();
        await TypewriterAsync(descText, typingCts.Token);

        // 4. 타이핑이 자연스럽게 끝났거나 스킵되었다면, 숨겨둔 버튼들을 연출과 함께 보여줍니다.
        CancellationToken safeToken = typingCts != null ? typingCts.Token : this.GetCancellationTokenOnDestroy();
        ShowChoicesWithAnimation(typingCts.Token).Forget();
    }

    private void PrepareChoices()
    {
        foreach (Transform child in choiceContainer)
            Destroy(child.gameObject);

        foreach (EventChoice choice in currentEvent.eventChoices)
        {
//#if UNITY_EDITOR

//#else
            if(choice.ChoiceIsActive == false)
                continue;
//#endif
            UIEventChoiceButton btn = Instantiate(choicePrefab, choiceContainer);
            btn.Setup(choice, OnChoiceSelected);
        }
    }

    // 💡 버튼들이 나타나는 애니메이션 (UniTask 기반)
    private async UniTaskVoid ShowChoicesWithAnimation(CancellationToken token)
    {
        choiceContainer.gameObject.SetActive(true);

        float duration = 0.3f; // 연출 시간 (0.3초)
        float elapsed = 0f;

        // 살짝 작았다가 커지면서 스르륵 나타나는 연출
        choiceContainer.localScale = Vector3.one * 0.9f;

        try
        {
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                choiceContainerGroup.alpha = Mathf.Lerp(0, 1, t);
                choiceContainer.localScale = Vector3.Lerp(Vector3.one * 0.9f, Vector3.one, t);

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token); // 다음 프레임까지 대기
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

    // 💡 타이프라이터 (기존 UniTaskVoid에서 UniTask로 변경하여 await 가능하게 만듦)
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
            // 스킵(Cancel)이 호출되면 즉시 전체 텍스트 표시
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
            // 토큰을 캔슬하면 TypewriterAsync의 try 블록이 즉시 중단되고 catch로 넘어갑니다!
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
        if (isTyping || recordManager == null) return;

        // 💡 1. 비용(Cost) 먼저 처리 시도
        if (!TryPayCost(choice))
        {
            // TryPayCost가 false를 반환했다면:
            // -> 코스트가 부족해서 실패했거나, 
            // -> UI창을 띄우고 유저의 응답을 기다리는 '대기 상태'로 들어갔음을 의미합니다.
            return;
        }

        // 코스트가 없거나, 즉시 지불 가능한 코스트(예: 골드)라면 바로 보상으로 넘어감
        ProcessRewardAndResult(choice);
    }

    // 비용 지불 검사 및 대기열 처리
    private bool TryPayCost(EventChoice choice)
    {
        // 비용이 없으면 프리패스
        if (choice.CostType == EventCostType.NONE) return true;

        // 💡 만약 비용이 '가진 레코드 소모' 라면?
        if (choice.CostType == EventCostType.RECORD_ANY)
        {
            List<RecordData> myRecords = recordManager.GetPossesRecord();
            if (myRecords == null || myRecords.Count == 0)
            {
                Debug.LogWarning("소모할 레코드가 없습니다!");
                return false; // 진행 불가
            }

            // 나중에 콜백이 왔을 때 보상을 주기 위해 어떤 선택지였는지 기억해둡니다.
            pendingChoice = choice;

            // RecordManager의 "지불 성공" 콜백 이벤트에 귀를 기울입니다 (중복 방지를 위해 뺐다가 넣기)
            recordManager.OnCostPaidSuccess -= ResumePendingChoice;
            recordManager.OnCostPaidSuccess += ResumePendingChoice;

            // 레코드 선택 창을 코스트 지불모드로 띄웁니다!
            UIManager.Instance.OpenRecordSelectPopUp(myRecords, false, RecordUIMode.DELETE);

            // 💡 당장 다음 단계로 넘어가지 않고 여기서 흐름을 '정지'시킵니다.
            return false;
        }

        // (추가 확장) 만약 비용이 골드라면? (예시)
        if (choice.CostType == EventCostType.GOLD ||
            choice.CostType == EventCostType.EXPLORE_COIN)
        {
            CurrencyType ctype = CurrencyType.GOLD;  
            if(choice.CostType == EventCostType.GOLD)
                ctype = CurrencyType.GOLD;
            else if (choice.CostType == EventCostType.EXPLORE_COIN)
                ctype = CurrencyType.EXPOLORE_GOLD;

            if (CurrencyManager.Instance.SpendCurrency(ctype, choice.CostValue)) return true; // 골드 차감 성공 시 패스
                return false; // 돈 부족
        }

        return true;
    }

    // 💡 유저가 RecordUI에서 레코드를 바치고 [완료]를 누르면 이 함수가 자동으로 실행됩니다!
    private void ResumePendingChoice()
    {
        if (recordManager != null)
        {
            // 1회용 콜백이므로 즉시 귀를 닫습니다 (메모리 누수 방지)
            recordManager.OnCostPaidSuccess -= ResumePendingChoice;
        }

        // 기억해두었던 선택지를 꺼내서 드디어 보상 스텝으로 넘어갑니다!
        if (pendingChoice != null)
        {
            ProcessRewardAndResult(pendingChoice);
            pendingChoice = null; // 초기화
        }
    }

    // 💡 기존 OnChoiceSelected에 있던 보상 지급 및 결과창 출력 로직을 그대로 옮겨옴
    private void ProcessRewardAndResult(EventChoice choice)
    {
        bool isSuccess = UnityEngine.Random.Range(0, 100) < choice.Probability;

        string resultKey = isSuccess ? choice.ResultTextKey : choice.FailTextKey;
        if (string.IsNullOrEmpty(resultKey)) resultKey = "ui_event_default_result";

        string resultText = LocalizationManager.Instance.GetText(resultKey);

        ShowResultPhaseAsync(resultText).Forget();

        // 기존 보상(Reward) 분기문들 (RECORD_DRAFT 등등...)
        if (choice.RewardType == EventRewardType.RECORD_DRAFT)
        {
            recordManager.GenerateEventRecords(10, false);
        }
        else if (choice.RewardType == EventRewardType.ARCHIVE_SAVE)
        {
            OpenArchiveRecordUI();
        }
        else if(choice.RewardType == EventRewardType.ARCHIVE_LOAD)
            OpenLastSavedRecordUI();
        else
            OpenTargetRarityRecord(choice.RewardType);  
    }

    private void OpenTargetRarityRecord(EventRewardType type)
    {
        if (recordManager == null) return;

        // 💡 1. 초기화를 해두어야 switch문에 걸리지 않았을 때 발생하는 Null 에러를 막을 수 있습니다.
        List<RecordData> records = new List<RecordData>();

        switch (type)
        {
            case EventRewardType.RECORD_ONE_OF_NORMAL:
                records = recordManager.GetNormalRecordDatas();
                break;

            case EventRewardType.RECORD_ONE_OF_RARE:
                records = recordManager.GetRareRecordDatas();
                break;

            // 💡 2. 나머지 등급 추가 (프로젝트의 Enum 이름에 맞춰 수정하세요!)
            case EventRewardType.RECORD_ONE_OF_UNIQUE:
                records = recordManager.GetUniqueRecordDatas(); // 매니저에 이 함수들도 추가되어야 합니다!
                break;

            case EventRewardType.RECORD_ONE_OF_LEGEND:
                records = recordManager.GetLengdaryRecordDatas();
                break;

            case EventRewardType.RECORD_ONE_OF_MYTH:
                records = recordManager.GetMythRecordDatas();
                break;

            // 💡 3. 정의되지 않은 타입이 들어왔을 때의 안전장치
            default:
                Debug.LogWarning($"[OpenTargetRarityRecord] 처리되지 않은 레코드 보상 타입입니다: {type}");
                return; // 에러를 막기 위해 UI를 열지 않고 함수 종료
        }

        // 💡 4. 해당 등급의 레코드를 모두 획득해서 리스트가 비어있을 경우의 예외 처리
        if (records == null || records.Count == 0)
        {
            Debug.LogWarning($"[{type}] 등급의 레코드가 없거나 모두 소진되었습니다.");

            // 빈 레코드를 하나 쥐어주어 에러를 방지하고 UI를 띄웁니다.
            records = new List<RecordData> { recordManager.GetEmptyRecord() };
        }

        UIManager.Instance.OpenRecordSelectPopUp(records, false, RecordUIMode.DRAFT);
    }

    private async UniTaskVoid ShowResultPhaseAsync(string resultText)
    {

        // 1. 기존 버튼들 스르륵 지우기 (옵션)
        choiceContainerGroup.alpha = 0f;
        choiceContainer.gameObject.SetActive(false);

        foreach (Transform child in choiceContainer)
            Destroy(child.gameObject);

        // 2. 닫기(떠난다) 버튼 미리 생성
        UIEventChoiceButton closeBtn = Instantiate(choicePrefab, choiceContainer);
        EventChoice closeChoice = new EventChoice() { TextKey = "ui_btn_leave", CostType = EventCostType.NONE };
        closeBtn.Setup(closeChoice, _ => ClosePopup());

        // 3. 결과 텍스트 타이핑 시작 & 끝날때까지 대기
        CancelTypingTask();
        typingCts = new CancellationTokenSource();
        await TypewriterAsync(resultText, typingCts.Token);

        // 4. 타이핑 완료 후 닫기 버튼 등장!
        CancellationToken safeToken = typingCts != null ? typingCts.Token : this.GetCancellationTokenOnDestroy();
        ShowChoicesWithAnimation(typingCts.Token).Forget();
    }

    private void ClosePopup()
    {
        if (isTyping)
        {
            SkipTyping();
            return;
        }
        CancelTypingTask();
        UIManager.Instance.CloseTopUI();

        if (AppManager.Instance == null) return; 
        var exploreManager = AppManager.Instance.GetExploreManager();
        if (exploreManager != null)
        {
            exploreManager.ClearStage(true);
        }
    }

    protected override void OnDisable()
    {
        CancelTypingTask();
    }

    protected override void DrawPopUp()
    {
        // UniTaskVoid를 호출할 때는 .Forget()을 붙여 백그라운드 실행을 명시합니다.
        DrawPopUpAsync().Forget();
    }


    private void OpenArchiveRecordUI()
    {
        if (recordManager == null) return; 

        List<RecordData> myRecords = recordManager.GetPossesRecord();

        if (myRecords != null && myRecords.Count > 0)
        {
            // (선택사항) 레코드 UI가 뜰 때 뒤에 이벤트 창이 보이는게 싫다면 이벤트 팝업을 숨깁니다.
            // choiceContainerGroup.alpha = 0f; 

            // 2. AppManager의 래핑 함수를 쓰지 않고, UIManager에게 직접 팝업을 띄우라고 명령합니다!
            // (리롤 불가, SelectOwned 모드로 전달)
            UIManager.Instance.OpenRecordSelectPopUp(myRecords, false, RecordUIMode.SELECT_OWNED);
        }
        else
        {
            Debug.LogWarning("보관할(가진) 레코드가 하나도 없습니다!");

            // 가진 게 없으면 UI를 띄우지 않고 그냥 스테이지를 클리어 처리하고 끝냅니다.
            var exploreManager = AppManager.Instance.GetExploreManager();
            if (exploreManager != null)
                exploreManager.ClearStage(true);
        }
    }

    // 지난 회차에 저장한 레코드 리스트를 띄우는 UI 
    private void OpenLastSavedRecordUI()
    {
        if (recordManager == null) return;

        List<RecordData> transferedDatas = recordManager.GetTransferedRecordIDs();

        if(transferedDatas != null && transferedDatas.Count > 0)
        {
            UIManager.Instance.OpenRecordSelectPopUp(transferedDatas, false, RecordUIMode.SELECT_SAVED);
        }
        else
        {
            Debug.LogWarning("보관할(가진) 레코드가 하나도 없습니다!");

            // 가진 게 없으면 UI를 띄우지 않고 그냥 스테이지를 클리어 처리하고 끝냅니다.
            var exploreManager = AppManager.Instance.GetExploreManager(); 
            if(exploreManager != null)
                exploreManager.ClearStage(true);
        }
    }
}