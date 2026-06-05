using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEditor;

public class UIStageInfo
    : UiBase
{
    private MapNode node;
    private RectTransform rect;
    private MapNodeInfo mapNodeInfo;

    [SerializeField] private TextMeshProUGUI titleText;

    protected override  void Awake()
    {
        base.Awake(); 

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

    public void SetStageData(MapNode node, MapNodeInfo mapNodeInfo)
    {
        if (node == null || mapNodeInfo == null) return;

        this.node = node;
        this.mapNodeInfo = mapNodeInfo;


        DrawStageTitle();

    }

    private void DrawStageTitle()
    {
        if (mapNodeInfo == null ||  titleText == null) return;

        Debug.Assert(LocalizationManager.Instance != null); 

        string title = "";

        switch (mapNodeInfo.type)
        {
            case StageType.Shop:
                title = LocalizationManager.Instance.GetText("ui_stage_info_title_shop");
                break;
            case StageType.None:
                title = LocalizationManager.Instance.GetText("ui_stage_info_title_none");
                break;
            case StageType.Combat:
                title = LocalizationManager.Instance.GetText("ui_stage_info_title_combat");
                break;
            case StageType.Event:
                title = LocalizationManager.Instance.GetText("ui_stage_info_title_event");
                break;
            case StageType.Boss_Combat:
                // 보스전은 보스의 이름이나 전용 타이틀로 작성
                //TODO : 임시적으로 할당 
                title = "BOSS"; 
                break;
        }

        titleText.text = title; 
    }

    public void EnterStage()
    {
        if (node == null || mapNodeInfo.isCleared
            || AppManager.Instance.EnableNode(node) == false)
            return;

        UIManager.Instance.CloseTopUI();
        AppManager.Instance.EnterStageByNode(node);
    }
}
