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

        // 기존에 실행 중이던 연출이 있다면 취소 
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
        }

        cts = new CancellationTokenSource();

        gameObject.SetActive(true);

        // 최상단으로 끌어올리기
        transform.SetAsLastSibling();

        messageText.text = LocalizationManager.Instance.GetText(messageKey);

        HideRoutine(cts.Token).Forget();
    }

    private async UniTaskVoid HideRoutine(CancellationToken token)
    {
        try
        {
            // 💡 1. 애니메이션 시작 전 상태 초기화 (투명도 0, 원래 위치보다 아래로)
            canvasGroup.alpha = 0f;
            rectTransform.anchoredPosition = originalPosition - new Vector2(0, moveDistance);

            // 💡 2. 나타나는 연출 (Fade In & 위로 올라오기)
            // ToUniTask(cancellationToken: token)을 붙이면 토큰 취소 시 애니메이션도 자동 정지(Kill)됩니다!
            await DOTween.Sequence()
                .Append(canvasGroup.DOFade(1f, fadeDuration))
                .Join(rectTransform.DOAnchorPos(originalPosition, fadeDuration).SetEase(Ease.OutBack))
                .ToUniTask(cancellationToken: token);

            // 💡 3. 떠있는 시간 대기
            await UniTask.Delay(TimeSpan.FromSeconds(showTime), cancellationToken: token);

            // 💡 4. 사라지는 연출 (Fade Out & 위로 더 올라가기)
            await DOTween.Sequence()
                .Append(canvasGroup.DOFade(0f, fadeDuration))
                .Join(rectTransform.DOAnchorPos(originalPosition + new Vector2(0, moveDistance), fadeDuration).SetEase(Ease.InBack))
                .ToUniTask(cancellationToken: token);

            // 모든 연출이 끝나면 비활성화
            gameObject.SetActive(false);
        }
        catch (OperationCanceledException)
        {
            // 새로운 메시지가 들어와서(광클) 기존 토큰이 취소된 경우
            // ToUniTask가 DOTween을 알아서 Kill 해줬으므로 여기선 조용히 넘어가기만 하면 됩니다.
            // 다음 ShowMessage 호출 시 HideRoutine 맨 위에서 초기화가 진행되므로 꼬이지 않습니다!
        }
    }

    private void OnDisable()
    {
        // 오브젝트가 꺼지거나 씬이 넘어갈 때 찌꺼기 방지
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }
    }
}