using UnityEngine;
using UnityEngine.UI;

public class SkillTreeController : MonoBehaviour
{
    private UISkillTree uiSkillTree;
    private UISkillDetail uiSkillDetail;
    private UISkillReplaceDetail uiSkillReplace;
    private SkillTreeManager skillTreeManager;

    private void Awake()
    {
        uiSkillTree = FindAnyObjectByType<UISkillTree>();
        uiSkillDetail = FindAnyObjectByType<UISkillDetail>();
        uiSkillReplace = FindAnyObjectByType<UISkillReplaceDetail>();

        skillTreeManager = FindAnyObjectByType<SkillTreeManager>();

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
                uiSkillDetail.HideDetail();
            }
        }

        if (uiSkillReplace != null)
        {
            uiSkillReplace.HideUI();
            uiSkillReplace.SetSkillTreeManager(skillTreeManager);
            uiSkillDetail.OnDrawEquipUI += uiSkillReplace.ShowUI;
        }
    }

    private void Start()
    {
        if (uiSkillTree == null || skillTreeManager == null) return;

        uiSkillTree.DrawSkillTree(skillTreeManager.GetSkillTableByClassId(1));
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
}
