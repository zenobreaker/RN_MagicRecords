using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SkillTreeController;

public class UISkillTree : UiBase
{
    //    private int maxLineCount = 5; // MaxLevel / 5 
    public event Action<SkillRuntimeData> OnDrawedDetail;

    public void DrawSkillTree(SkillTree skillTree, SkillTreeController.Skill_Category category)
    {
        if(skillTree == null) return;

        bool filterActive = category == Skill_Category.CLASS_ACTIVE ||
                            category == Skill_Category.COMMON_ACTIVE;

        bool filterPassive = category == Skill_Category.CLASS_PASSIVE||
                            category == Skill_Category.COMMON_PASSIVE;

        DrawFilterTree(skillTree, filterActive, filterPassive);
    }

    private void DrawFilterTree(SkillTree skillTree, bool active, bool passive)
    {
        if (skillTree == null) return;

        List<List<SkillRuntimeData>> skillLines = new();

        int lineCount = 0;
        for (int i = 0; i <= 50; i++)
        {
            var runtimeList = skillTree.GetSkillRuntimeDatasByLevel(
                i,
                onlyActive: active,
                onlyPassive: passive
                );

            if (runtimeList != null && runtimeList.Count > 0)
            {
                skillLines.Add(runtimeList);
                lineCount++;
            }
        }

        DrawSkillTreeLines(lineCount, skillLines);
    }
    private void DrawSkillTreeLines(int lineCount, List<List<SkillRuntimeData>> skillLines)
    {
        // 라인 오브젝트 배치 
        InitReplaceContentObject(lineCount);

        // 생성된 라인 순회하면서 DrawLine
        int index = 0;
        SetContentChildObjectsCallback<UISkillTreeLine>(line =>
        {
            if (index < skillLines.Count)
            {
                line.DrawTreeLine(skillLines[index]);
                line.OnClickedSkillSlot += OnShowSkillDetail;
                index++;
            }
            else
            {
                line.OnClickedSkillSlot -= OnShowSkillDetail;
                line.gameObject.SetActive(false);
            }
        });
    }

    public void OnShowSkillDetail(SkillRuntimeData data)
    {
        if (data == null) return;

        Debug.Log($"skill data : {data?.template?.id}");
        OnDrawedDetail?.Invoke(data);
    }
}
