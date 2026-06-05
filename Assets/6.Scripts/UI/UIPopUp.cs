using UnityEngine;
using UnityEngine.EventSystems;

// 💡 필수로 CanvasGroup을 요구하여 휴먼 에러 방지
[RequireComponent(typeof(CanvasGroup))]
public abstract class UIPopUp : UiBase, IPointerClickHandler
{
    [Header("PopUp Settings")]
    [Tooltip("이 영역 밖(반투명 배경 등)을 클릭하면 팝업이 닫힙니다.")]
    [SerializeField] protected RectTransform popupArea;

    protected CanvasGroup canvasGroup;

    protected override void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    // 💡 1. UiBase의 OnEnable을 오버라이드
    protected override void OnEnable()
    {
        // UiBase에 정의된 UIOpend 이벤트 정상 발생을 위해 base 호출
        base.OnEnable();

        // UIManager에 의해 SetActive(true)가 되는 순간,
        // 일단 투명하게 만들고 터치를 막아서 데이터 세팅을 기다립니다.
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// 파생 클래스에서 데이터(SetData) 세팅이 완료된 후 호출하는 최종 오픈 함수
    /// </summary>
    protected void ShowPopUp()
    {
        // 💡 2. 파생 클래스에서 오버라이드한 UI 그리기 로직 실행
        // (이 안에서 UiBase의 InitReplaceContentObject 등을 사용하면 완벽합니다!)
        DrawPopUp();

        // 💡 3. 모든 그리기가 끝났으므로 화면에 노출하고 터치 허용
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
    }

    // 실제 데이터를 UI에 맵핑하는 로직 (파생 클래스에서 강제 구현)
    protected abstract void DrawPopUp();

    // 💡 4. UiBase의 CloseUI 오버라이드
    public override void CloseUI()
    {
        // 단순 SetActive(false) 대신, UIManager의 스택 관리를 거치도록 위임
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseSpecificUI(this);
        }
        else
        {
            base.CloseUI(); // 예비용 방어 코드
        }
    }

    // 💡 5. 팝업 바깥 영역 터치 시 닫기
    public virtual void OnPointerClick(PointerEventData eventData)
    {
        // popupArea가 존재하고, 클릭한 위치가 popupArea 바깥이라면 팝업 종료
        if (popupArea != null && !RectTransformUtility.RectangleContainsScreenPoint(popupArea, eventData.position))
        {
            CloseUI();
        }
    }
}