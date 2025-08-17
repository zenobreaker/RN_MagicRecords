using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UISkillTree : UiBase
{
    private int maxLineCount = 5; // MaxLevel / 5 

    private SkillTreeManager.SkillTreeCategory selectedCategory;
    private SkillTree selectedSkillTree;

    public event Action<SkillRuntimeData> OnDrawedDetail;

    public void OnClickCategoryButton(SkillTreeManager.SkillTreeCategory category)
    {
        selectedCategory = category;
        //selectedSkillTree = 
    }

    public void DrawSkillTree(SkillTree skillTree)
    {
        // ��ųƮ���� ���� ������ �׸���. 
        if (skillTree == null) return;

        List<List<SkillRuntimeData>> skillLines = new();
        int lineCount = 0;
        for (int i = 0; i <= 50; i++)
        {
            var skillList = skillTree.GetSkillForLevel(i);
            if (skillList != null && skillList.Count > 0)
            {
                var runtimeList = skillList
                    .Select(sd => new SkillRuntimeData
                    { 
                        template = sd,
                        currentLevel = 0,
                        isUnlocked = false
                    })
                    .ToList();
                skillLines.Add(runtimeList);
                lineCount++;
            }
        }

        // ���� ������Ʈ ��ġ 
        InitReplaceContentObject(lineCount);

        // ������ ���� ��ȸ�ϸ鼭 DrawLine
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
