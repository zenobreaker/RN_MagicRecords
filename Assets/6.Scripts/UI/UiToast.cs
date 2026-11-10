using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
public class UIToast : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private float showTime = 1.5f; // 떠있는 시간

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.3f; // 나타나고 사라지는 속도
    [SerializeField] private float moveDistance = 50f;  // 위로 뿅! 하고 올라갈 거리

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    // 원래 위치를 기억하기 위한 변수
    private Vector2 originalPosition;

    private CancellationTokenSource cts;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();

        // 💡 프리팹이 처음 생성되었을 때의 원래 Y 위치를 기억해둡니다.
        originalPosition = rectTransform.anchoredPosition;
    }

    public void ShowMessage(string messageKey)
    {
        Debug.Assert(LocalizationManager.Instance != null);

        // 새 토큰을 먼저 등록한 뒤 이전 연출을 취소합니다.
        // 취소 콜백 중 OnDisable이 실행돼도 새 토큰을 건드리지 않습니다.
        CancellationTokenSource previousCts = cts;
        CancellationTokenSource currentCts = new CancellationTokenSource();
        cts = currentCts;

        CancelAndDispose(previousCts);

        gameObject.SetActive(true);

        // 최상단으로 끌어올리기
        transform.SetAsLastSibling();

        messageText.text = LocalizationManager.Instance.GetText(messageKey);

        HideRoutine(currentCts).Forget();
    }

    private async UniTaskVoid HideRoutine(CancellationTokenSource routineCts)
    {
        try
        {
            canvasGroup.alpha = 0f;
            rectTransform.anchoredPosition =
                originalPosition - new Vector2(0, moveDistance);

            // Fade In
            await DOTween.Sequence()
                .Append(canvasGroup.DOFade(1f, fadeDuration))
                .Join(rectTransform.DOAnchorPos(
                    originalPosition,
                    fadeDuration)
                    .SetEase(Ease.OutBack))
                .SetUpdate(true)
                .ToUniTask(cancellationToken: routineCts.Token);

            // 대기
            await UniTask.Delay(
                TimeSpan.FromSeconds(showTime),
                ignoreTimeScale: true,
                cancellationToken: routineCts.Token);

            // Fade Out
            await DOTween.Sequence()
                .Append(canvasGroup.DOFade(0f, fadeDuration))
                .Join(rectTransform.DOAnchorPos(
                    originalPosition + new Vector2(0, moveDistance),
                    fadeDuration)
                    .SetEase(Ease.InBack))
                .SetUpdate(true)
                .ToUniTask(cancellationToken: routineCts.Token);

            // 이전 연출이 새 토스트를 비활성화하지 않도록 현재 토큰인지 확인합니다.
            if (cts == routineCts)
            {
                gameObject.SetActive(false);
            }
        }
        catch (OperationCanceledException)
        {
            // 새로운 메시지가 들어와서(광클) 기존 토큰이 취소된 경우
            // ToUniTask가 DOTween을 알아서 Kill 해줬으므로 여기선 조용히 넘어가기만 하면 됩니다.
            // 다음 ShowMessage 호출 시 HideRoutine 맨 위에서 초기화가 진행되므로 꼬이지 않습니다!
        }
    }

    private void CancelAndDispose(CancellationTokenSource source)
    {
        if (source == null)
        {
            return;
        }

        try
        {
            if (source.IsCancellationRequested == false)
            {
                source.Cancel();
            }

            source.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // 이미 종료 처리된 토큰은 다시 해제하지 않습니다.
        }
    }

    private void OnDisable()
    {
        // 오브젝트가 꺼지거나 씬이 넘어갈 때 찌꺼기 방지
        CancellationTokenSource currentCts = cts;
        cts = null;
        CancelAndDispose(currentCts);
    }
}
