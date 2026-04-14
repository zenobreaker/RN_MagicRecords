using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEditor;

public class UIStageInfo 
    : UiBase
{
    private MapNode node; 
    private RectTransform rect;
    private StageInfo stageInfo;

    [SerializeField] private TextMeshProUGUI titleText;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }


    protected override void OnEnable()
    {
        base.OnEnable();

        transform.DOKill();

        rect.anchoredPosition = new Vector2(Screen.width + 300f, rect.anchoredPosition.y);


        rect.DOAnchorPosX(0, 0.25f)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        transform.DOKill();
    }

    public void SetStageData(MapNode node, StageInfo stageInfo)
    {
        if (node == null|| stageInfo == null) return;

        this.node = node;
        this.stageInfo = stageInfo;

        if (titleText != null)
        {
            titleText.text = stageInfo.ToString();
        }
    }

    public void EnterStage()
    {
        if (node == null || stageInfo.bIsCleared 
            || AppManager.Instance.EnableNode(node) == false)
            return;

        AppManager.Instance.EnterStageByNode(node);
    }
}
