using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITotalResultPopUp : UIPopUp
{
    [Header("UI Components")]
    [SerializeField] private Image portrait;
    [SerializeField] private TextMeshProUGUI characterNameText; 
    [SerializeField] private Image jobIcon;
    [SerializeField] private TextMeshProUGUI jobNameText; 
    [SerializeField] protected TextMeshProUGUI playTimeText;
    [SerializeField] protected TextMeshProUGUI killCountText;
    [SerializeField] protected TextMeshProUGUI maxExploreText;
    [SerializeField] protected GameObject recordParent;
    [SerializeField] protected GameObject recordCard;
    [SerializeField] protected Button confirmButton; // 확인 버튼 추가

    [Header("Animation Groups (CanvasGroup)")]
    [SerializeField] private CanvasGroup charInfoGroup;
    [SerializeField] private CanvasGroup playTimeGroup;
    [SerializeField] private CanvasGroup killCountGroup;
    [SerializeField] private CanvasGroup exploreNodeGroup;
    [SerializeField] private CanvasGroup recordListGroup;
    [SerializeField] private CanvasGroup buttonGroup;

    [Header("Animation Settings")]
    [SerializeField] private float delayBetweenGroups = 0.3f; // 항목 간 등장 간격
    [SerializeField] private float countDuration = 1.0f;      // 숫자 올라가는 시간

    [Header("Scroll Animation")]
    [SerializeField] private ScrollRect scrollRect; // 인스펙터에서 팝업의 ScrollRect 연결!
    [SerializeField] private RectTransform contentRect; // Content 오브젝트 연결

    // 통계 데이터 (AppManager 등에서 받아올 실제 값)
    private float targetPlayTime = 0f;
    private int targetKillCount = 0;

    // 연출 진행 중인지 체크하는 플래그
    private bool isAnimating = false;

    // UniTask 취소를 위한 토큰 소스 
    private CancellationTokenSource cts; 

    protected override void OnEnable()
    {
        base.OnEnable();
        // 1. 애니메이션 시작 전 모든 UI 투명하게 초기화
        InitAlphaToZero();

        // 2. 데이터 세팅 (그리기는 하지만 alpha가 0이라 안 보임)
        FetchResultData();
        DrawPopUp();

        // 💡 기존 취소 토큰이 있다면 정리하고 새로 생성
        CancelAndDisposeCTS();
        cts = new CancellationTokenSource();

        // 수정 : UniTask 사용, 기존 Coroutine 제거 
        // 💡 UniTask 실행 (Forget을 붙여 Fire and Forget 처리)
        ShowResultSequence(cts.Token).Forget();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        // 💡 UI가 꺼질 때 진행 중인 연출 강제 종료 (메모리 누수 방지)
        CancelAndDisposeCTS();
    }

    private void Update()
    {
        if (isAnimating && (Input.GetMouseButton(0) || Input.touchCount > 0)) 
        {
            SkipAnimation(); 
        }
    }

    private void SkipAnimation()
    {
        isAnimating = false;

        // 1. 현재 진행 중인 모든 연출 코루틴을 즉시 강제 정지!
        StopAllCoroutines();

        // 2. 결과창을 100% 켜진 상태(최종 상태)로 즉시 덮어씌움
        ActiveUIElements();
        SetFinalState();
    }

    private void CancelAndDisposeCTS()
    {
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }
    }


    // ==========================================
    // 최종 상태 강제 세팅 (스킵 시 호출)
    // ==========================================
    private void SetFinalState()
    {
        // 1. 모든 그룹의 투명도를 100% (알파 = 1)로 켜기
        if (charInfoGroup != null) charInfoGroup.alpha = 1f;
        if (playTimeGroup != null) playTimeGroup.alpha = 1f;
        if (killCountGroup != null) killCountGroup.alpha = 1f;
        if (exploreNodeGroup != null) exploreNodeGroup.alpha = 1f;
        if (recordListGroup != null) recordListGroup.alpha = 1f;
        if (buttonGroup != null) buttonGroup.alpha = 1f;

        // 2. 숫자 통계를 최종 목표값으로 즉시 세팅
        int minutes = Mathf.FloorToInt(targetPlayTime / 60F);
        int seconds = Mathf.FloorToInt(targetPlayTime - minutes * 60);
        if (playTimeText != null) playTimeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        if (killCountText != null) killCountText.text = targetKillCount.ToString();

        // 3. 스크롤 위치를 즉시 맨 아래(0)로 강제 이동
        if (contentRect != null && scrollRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            scrollRect.verticalNormalizedPosition = 0f;
        }

        // 4. 확인(로비 귀환) 버튼 클릭 가능하게 활성화
        if (confirmButton != null) confirmButton.interactable = true;
    }

    private void FetchResultData()
    {
        // TODO: AppManager나 ExploreManager에서 실제 통계 데이터를 가져와서 저장합니다.
        // 임시 테스트용 데이터
        targetPlayTime = 145f; // 2분 25초
        targetKillCount = 1240;

        // 초기 텍스트 세팅 (0부터 시작)
        playTimeText.text = "00:00";
        killCountText.text = "0";
    }

    private void InitAlphaToZero()
    {
        // 알파도 0으로 하고, 오브젝트도 아예 꺼서 공간 차지를 없앰
        if (charInfoGroup != null) { charInfoGroup.alpha = 0; charInfoGroup.gameObject.SetActive(false); }
        if (playTimeGroup != null) { playTimeGroup.alpha = 0; playTimeGroup.gameObject.SetActive(false); }
        if (killCountGroup != null) { killCountGroup.alpha = 0; killCountGroup.gameObject.SetActive(false); }
        if (exploreNodeGroup != null) { exploreNodeGroup.alpha = 0; exploreNodeGroup.gameObject.SetActive(false); }
        if (recordListGroup != null) { recordListGroup.alpha = 0; recordListGroup.gameObject.SetActive(false); }
        if (buttonGroup != null) { buttonGroup.alpha = 0; buttonGroup.gameObject.SetActive(false); }

        if (confirmButton != null) confirmButton.interactable = false;
    }

    public override void RefreshUI()
    {
        base.RefreshUI();
        DrawPopUp(); 
    }

    protected override void DrawPopUp()
    {
        // 플레이한 캐릭터 정보 그리기 
        DrawCharInfo();

        // 탐사 정도 정보 그리기 
        DrawExploreInfo(); 

        // 레코드 그리기
        DrawRecords();
    }

    private void DrawExploreInfo()
    {
        playTimeText?.gameObject.SetActive(true);
        killCountText?.gameObject.SetActive(true);
        maxExploreText?.gameObject.SetActive(true);
    }

    private void InactiveUIElements()
    {
        portrait?.gameObject.SetActive(false);
        characterNameText?.gameObject.SetActive(false);
        jobIcon?.gameObject.SetActive(false);
        jobNameText?.gameObject.SetActive(false);
        playTimeText?.gameObject.SetActive(false);
        killCountText?.gameObject.SetActive(false);
        maxExploreText?.gameObject.SetActive(false);
    }

    private void ActiveUIElements()
    {
        charInfoGroup?.gameObject.SetActive(true);
        playTimeGroup?.gameObject.SetActive(true);
        killCountGroup?.gameObject.SetActive(true);
        exploreNodeGroup?.gameObject.SetActive(true);
        recordListGroup?.gameObject.SetActive(true);
        buttonGroup?.gameObject.SetActive(true);

        portrait?.gameObject.SetActive(true);
        characterNameText?.gameObject.SetActive(true);
        jobIcon?.gameObject.SetActive(true);
        jobNameText?.gameObject.SetActive(true);
        playTimeText?.gameObject.SetActive(true);
        killCountText?.gameObject.SetActive(true);
        maxExploreText?.gameObject.SetActive(true);
    }


    private void DrawCharInfo()
    {
        InactiveUIElements();

        if (PlayerManager.Instance == null) return; 

        // 캐릭터 정보 처리 
        var player = PlayerManager.Instance.GetCurrentPlayer();
        if (player == null)
        {
            return;
        }
        else
        {
            var playerInfo = PlayerManager.Instance.GetCharacterInfo(player.CharID); 
            if(playerInfo == null) return;

            if (portrait != null)
            {
                portrait.sprite = playerInfo.charSprite;
                portrait.gameObject.SetActive(true); 
            }

            if (characterNameText != null)
            {
                characterNameText.text = playerInfo.name;
                characterNameText.gameObject.SetActive(true); 
            }
        }

        int jobID = player.JobID;
        // 직업 정보 처리 
        var jobInfo = PlayerManager.Instance.GetJobInfo(jobID);
        if (jobInfo != null)
        {
            if (jobIcon != null)
            {
                jobIcon.sprite = jobInfo.jobSprite;
                jobIcon.gameObject.SetActive(true);
            }

            if (jobNameText != null)
            {
                jobNameText.text = jobInfo.jobName;
                jobNameText.gameObject.SetActive(true);
            }
        }
    }

    private void DrawRecords()
    {
        List<RecordData> records = AppManager.Instance?.GetRecordManager()?.GetPossesRecord();
        if(records == null || recordParent == null || recordCard == null) return;

        UIListDrawer.DrawListToTarget<RecordCard, RecordData>(
            recordParent.transform,
            recordCard,
            records,
            (slot, data, index) =>
            {
                slot.Setup(data);
            });
    }


    // 이 함수는 IEnumerator -> async UniTaskVoid로 변경
    private async UniTaskVoid ShowResultSequence(CancellationToken token)
    {
        try
        {
            isAnimating = true;

            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1f;

            // 1. 캐릭터 정보
            charInfoGroup.gameObject.SetActive(true);
            await FadeInGroup(charInfoGroup, token);
            await ScrollToBottom(token);
            await UniTask.Delay(TimeSpan.FromSeconds(delayBetweenGroups), ignoreTimeScale: true, cancellationToken: token);

            // 2. 플레이 타임
            playTimeGroup.gameObject.SetActive(true);
            await FadeInGroup(playTimeGroup, token);
            await ScrollToBottom(token);
            CountUpPlayTime(targetPlayTime, token).Forget(); // 비동기 병렬 실행
            await UniTask.Delay(TimeSpan.FromSeconds(delayBetweenGroups), ignoreTimeScale: true, cancellationToken: token);

            // 3. 킬 카운트
            killCountGroup.gameObject.SetActive(true);
            await FadeInGroup(killCountGroup, token);
            await ScrollToBottom(token);
            CountUpKillCount(targetKillCount, token).Forget(); // 비동기 병렬 실행
            await UniTask.Delay(TimeSpan.FromSeconds(delayBetweenGroups), ignoreTimeScale: true, cancellationToken: token);

            // 4. 탐사 노드
            exploreNodeGroup.gameObject.SetActive(true);
            await FadeInGroup(exploreNodeGroup, token);
            await ScrollToBottom(token);
            await UniTask.Delay(TimeSpan.FromSeconds(delayBetweenGroups), ignoreTimeScale: true, cancellationToken: token);

            // 5. 획득 레코드 리스트
            recordListGroup.gameObject.SetActive(true);
            await FadeInGroup(recordListGroup, token);
            await ScrollToBottom(token);
            await UniTask.Delay(TimeSpan.FromSeconds(delayBetweenGroups), ignoreTimeScale: true, cancellationToken: token);

            // 6. 확인 버튼
            buttonGroup.gameObject.SetActive(true);
            await FadeInGroup(buttonGroup, token);
            await ScrollToBottom(token);

            if (confirmButton != null) confirmButton.interactable = true;

            // 모든 연출이 자연스럽게 끝났을 경우
            SetFinalState();
            isAnimating = false;
        }
        catch (OperationCanceledException)
        {
            // 💡 스킵(Skip)을 눌러서 강제로 취소되었을 때 발생하는 예외.
            // 이미 SkipAnimation()에서 후처리를 했으므로 여기서는 아무것도 안 해도 됩니다!
        }
    }


    // ==========================================
    // 부드러운 스크롤 이동 코루틴
    // ==========================================
    private async UniTask ScrollToBottom(CancellationToken token)
    {
        if (scrollRect == null || contentRect == null) return;

        // [핵심] 방금 켜진 UI 때문에 늘어난 Content의 세로 길이를 즉시 계산하도록 강제 업데이트
        //LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        // 방금 SetActive(true)된 요소 때문에 바뀐 Content 크기와 ScrollRect 내부 수치를 완벽히 동기화!
        Canvas.ForceUpdateCanvases();

        float time = 0f;
        float startPos = scrollRect.verticalNormalizedPosition;
        float targetPos = 0f; // 0f = 맨 아래, 1f = 맨 위

        // 이미 맨 아래라면 스크롤 생략
        if (Mathf.Approximately(startPos, targetPos)) return;

        // 0.3초 동안 부드럽게 스크롤을 맨 아래로 내림
        while (time < 1f)
        {
            // 프레임 튐 방지를 위해 deltaTime 캡 씌우기
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            time += dt * 3.3f;

            float t = time * (2f - time); // Ease-out
            scrollRect.verticalNormalizedPosition = Mathf.Lerp(startPos, targetPos, t);
            
            // 💡 yield return null -> await UniTask.Yield()
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
        }

        scrollRect.verticalNormalizedPosition = targetPos; // 오차 보정
    }

    // CanvasGroup 페이드 인 유틸 함수
    private async UniTask FadeInGroup(CanvasGroup cg, CancellationToken token)
    {
        if (cg == null) return;

        float t = 0;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 2f;
            cg.alpha = Mathf.Clamp01(t);
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
        }
    }

    // 시간 숫자 롤링 연출
    private async UniTaskVoid CountUpPlayTime(float targetTime, CancellationToken token)
    {
        float current = 0;
        float t = 0;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / countDuration;
            current = Mathf.Lerp(0, targetTime, t);

            int minutes = Mathf.FloorToInt(current / 60f);
            int seconds = Mathf.FloorToInt(current - minutes * 60);
            playTimeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
        }
    }

    // 킬 카운트 숫자 롤링 연출
    private async UniTaskVoid CountUpKillCount(int targetCount, CancellationToken token)
    {
        float current = 0;
        float t = 0;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / countDuration;
            current = Mathf.Lerp(0, targetCount, t);
            killCountText.text = Mathf.FloorToInt(current).ToString();

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
        }
        killCountText.text = targetCount.ToString();
    }

    public void OnClickedConfirmButton()
    {
        AppManager.Instance?.ReturnToLobbyScene();
    }
}
