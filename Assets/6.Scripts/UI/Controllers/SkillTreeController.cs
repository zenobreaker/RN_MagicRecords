using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;



public class SkillTreeController : UiBase
{
    [SerializeField] private UISkillTree uiSkillTree;
    [SerializeField] private UISkillDetail uiSkillDetail;
    [SerializeField] private UISkillReplaceDetail uiSkillReplace;
    private SkillTreeManager skillTreeManager;

    public enum SkillUIEnterType
    {
        MainRoom,
        SelectPopup,
    }

    [SerializeField] private SkillUIEnterType skillUIEnterType;

    public void SetMainRoomType() => skillUIEnterType = SkillUIEnterType.MainRoom;
    public void SetSelectPopupType() => skillUIEnterType = SkillUIEnterType.SelectPopup;

    public enum Skill_Category
    {
        CLASS_ACTIVE,
        CLASS_PASSIVE,
        COMMON_ACTIVE,
        COMMON_PASSIVE,
    };
    private Skill_Category category;
    private int jobID = 1;

    protected override void Awake()
    {
        base.Awake(); 

        Debug.Assert(SkillTreeManager.Instance != null);
        skillTreeManager = SkillTreeManager.Instance;

        if (uiSkillDetail != null)
        {
            uiSkillDetail.OnSelectedSkillRunTimeData += OnSelctedSkillData;
            uiSkillDetail.HideDetail();
        }

        if (uiSkillTree != null)
        {
            if (uiSkillDetail != null)
            {
                uiSkillTree.OnDrawedDetail += uiSkillDetail.OnDrawSkillDetail;
                //uiSkillDetail.HideDetail();
            }
        }

        if (uiSkillReplace != null)
        {
            uiSkillReplace.HideUI();
            uiSkillReplace.SetSkillTreeManager(skillTreeManager);
            uiSkillDetail.OnDrawEquipUI += uiSkillReplace.ShowUI;
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        category = Skill_Category.CLASS_ACTIVE;

        if (skillUIEnterType == SkillUIEnterType.MainRoom)
            uiSkillReplace.SafeInvoke(v => v.HideUI());
        else 
            uiSkillReplace.SafeInvoke(v => v.ShowUI());

        RefreshUI();
    }

    public void SetJobID(int jobID)
    {
        this.jobID = jobID;
    }

    public override void RefreshUI()
    {
        DrawSkillTree();
    }

    public void OnSelctedSkillData(SkillRuntimeData data)
    {
        if (skillTreeManager == null) return;
        skillTreeManager.SelectedSkillData = data;
    }

    public void HideUI()
    {
        gameObject.SetActive(false);
    }

    public void OnDrawSkillTree(int category)
    {
        this.category = (Skill_Category)category;

        RefreshUI(); 
    }

    private void DrawSkillTree()
    {
        if (skillTreeManager == null || uiSkillTree == null) return;

        SkillTreeManager.SkillTreeCategory treeCategroy = SkillTreeManager.SkillTreeCategory.Theme;
        
        if (category == Skill_Category.CLASS_PASSIVE ||
            category == Skill_Category.CLASS_ACTIVE)
            treeCategroy = SkillTreeManager.SkillTreeCategory.Theme;
        else if (category == Skill_Category.COMMON_ACTIVE ||
            category == Skill_Category.COMMON_PASSIVE)
            treeCategroy = SkillTreeManager.SkillTreeCategory.Common;

        // 해당 직업의 전용 스킬트리를 가져옴 
        SkillTree skilltree = skillTreeManager.GetSkillTree(treeCategroy, jobID);

        uiSkillTree.DrawSkillTree(skilltree, category);
    }
}
