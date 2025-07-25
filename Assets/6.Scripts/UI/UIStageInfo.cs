using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using TMPro;

public class UIStageInfo 
    : UiBase
{
    private StageInfo stageInfo; 
    private RectTransform rect;

    [SerializeField] private TextMeshProUGUI titleText;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }


    protected override void OnEnable()
    {
        base.OnEnable();

        rect.anchoredPosition = new Vector2(Screen.width + 300f, rect.anchoredPosition.y);

        rect.DOAnchorPosX(0, 0.25f);
    }

    public void SetStageData(StageInfo stageInfo)
    {
        if (stageInfo == null) return; 

        //TODO : 스테이지 아이디를 이용해서 데이터베이스에서 데이터 참조 후 UI 세팅
        this.stageInfo = stageInfo;

        this.stageInfo.isOpened = true; 

        if (titleText != null)
        {
            titleText.text = stageInfo.ToString();
        }
    }

    public void EnterStage()
    {
        if (stageInfo == null || stageInfo.isCleared || stageInfo.isOpened == false)
            return;

        GameManager.Instance.EnterStage(stageInfo);

        SceneManager.LoadScene(2); 
    }
}
