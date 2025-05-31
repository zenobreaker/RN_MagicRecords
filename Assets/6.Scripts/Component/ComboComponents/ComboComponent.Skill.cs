using System;
using UnityEngine;

/// <summary>
/// Process Skill Input
/// </summary>
public partial class ComboComponent: MonoBehaviour
{
    private void TryProcess_Skill(InputCommand newInput)
    {
        if (skill == null || newInput == null) return;

        ResetCombo();
        skill.UseSkill((SkillSlot)newInput.SkillSlotIndex);
    }
}
