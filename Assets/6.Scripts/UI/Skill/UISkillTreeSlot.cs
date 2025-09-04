using System;
using UnityEngine;
using UnityEngine.UI;

public class UISkillTreeSlot : MonoBehaviour
{
    private SkillRuntimeData skillData;
    private Image skillIcon; 

    public event Action<SkillRuntimeData> OnClickedSlot;

    private void Awake()
    {
        skillIcon = GetComponent<Image>();
    }

    public void SetSkillData(SkillRuntimeData skillData)
    {
        this.skillData = skillData;

        DrawSkillSlot(); 
    }

    private void DrawSkillSlot()
    {
        if (skillData == null || skillIcon == null)
            return;

        skillIcon.sprite = skillData?.template?.skillImage;
    }

    public void OnClick()
    {
        OnClickedSlot?.Invoke(skillData); 
    }
}
