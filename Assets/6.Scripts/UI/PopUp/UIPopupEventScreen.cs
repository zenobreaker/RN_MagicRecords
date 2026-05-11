using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPopupEventScreen : UIPopUpBase
{
    [Header("UI References")]
    [SerializeField] private Image illustrationImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

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
    private bool isResultPhase = false;

    public void SetData(EventInfo eventInfo)
    {
        currentEvent = eventInfo;
        isResultPhase = false;
        DrawPopUp();
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
        ShowChoicesWithAnimation(typingCts.Token).Forget();
    }

    private void PrepareChoices()
    {
        foreach (Transform child in choiceContainer)
            Destroy(child.gameObject);

        foreach (EventChoice choice in currentEvent.eventChoices)
        {
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

                await UniTask.Yield(); // 다음 프레임까지 대기
            }

            choiceContainerGroup.alpha = 1f;
            choiceContainer.localScale = Vector3.one;
        }
        catch (OperationCanceledException)
        {

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
        // 이제 타이핑 중에 버튼을 누를 일은 없지만, 혹시 모를 방어코드
        if (isTyping) return;

        bool isSuccess = UnityEngine.Random.Range(0, 100) < choice.Probability;

        string resultKey = isSuccess ? choice.ResultTextKey : choice.FailTextKey;
        if (string.IsNullOrEmpty(resultKey)) resultKey = "ui_event_default_result";

        string resultText = LocalizationManager.Instance.GetText(resultKey);

        ShowResultPhaseAsync(resultText).Forget();


        if (choice.RewardType == EventRewardType.RECORD_DRAFT)
        {
            // 이벤트 팝업 숨기기
            //gameObject.SetActive(false);

            // 💡 마법의 호출! (레코드 10개 뽑기, 리롤 불가능 설정)
            AppManager.Instance.GenerateRecord(10, false);
        }
    }

    private async UniTaskVoid ShowResultPhaseAsync(string resultText)
    {
        isResultPhase = true;

        // 1. 기존 버튼들 스르륵 지우기 (옵션)
        choiceContainerGroup.alpha = 0f;
        choiceContainer.gameObject.SetActive(false);

        foreach (Transform child in choiceContainer)
            Destroy(child.gameObject);

        // 2. 닫기(떠난다) 버튼 미리 생성
        UIEventChoiceButton closeBtn = Instantiate(choicePrefab, choiceContainer);
        EventChoice closeChoice = new EventChoice() { TextKey = "ui_button_leave", CostType = EventCostType.NONE };
        closeBtn.Setup(closeChoice, _ => ClosePopup());

        // 3. 결과 텍스트 타이핑 시작 & 끝날때까지 대기
        CancelTypingTask();
        typingCts = new CancellationTokenSource();
        await TypewriterAsync(resultText, typingCts.Token);

        // 4. 타이핑 완료 후 닫기 버튼 등장!
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
        AppManager.Instance.GetExploreManager().ClearStage(true);
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
}