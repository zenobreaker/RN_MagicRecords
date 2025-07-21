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
        //TODO : �������� ���̵� �̿��ؼ� �����ͺ��̽����� ������ ���� �� UI ����
    }

    public void EnterStage()
    {
        SceneManager.LoadScene(2); 
    }
}
