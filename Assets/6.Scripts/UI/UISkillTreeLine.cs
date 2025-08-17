using System;
using System.Collections.Generic;
using UnityEngine;

public class UISkillTreeLine : UiBase
{
    public event Action<SkillRuntimeData> OnClickedSkillSlot;

    public void DrawTreeLine(List<SkillRuntimeData> datas)
    {
        this.gameObject.SetActive(true);
        InitReplaceContentObject(datas.Count);


        int index = 0;
        SetContentChildObjectsCallback<UISkillTreeSlot>(slot =>
        {
            if (index < datas.Count)
            {
                slot.SetSkillData(datas[index]);
                slot.gameObject.SetActive(true);
                slot.OnClickedSlot += OnClickSlot;
                index++;
            }
            else
            {
                slot.OnClickedSlot -= OnClickSlot;
                slot.gameObject.SetActive(false);
            }
        });
    }

    public void OnClickSlot(SkillRuntimeData data)
    {
        if (data == null) return;

        OnClickedSkillSlot?.Invoke(data); 
    }
}
