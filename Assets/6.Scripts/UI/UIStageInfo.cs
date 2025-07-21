using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class UIStageInfo 
    : UiBase
{
    RectTransform rect;

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

    public void SetStageData(Stage stage)
    {
        //TODO : 스테이지 아이디를 이용해서 데이터베이스에서 데이터 참조 후 UI 세팅
    }

    public void EnterStage()
    {
        SceneManager.LoadScene(2); 
    }
}
