using UnityEngine;

public class SkillPanel : MonoBehaviour
{
    [Header("Skill Slots")]
    [SerializeField]
    private SkillSlotUI[] ui_SkillSlot = new SkillSlotUI[4];

    private void OnEnable()
    {
        RefreshSkillSlots();
    }

    private void RefreshSkillSlots()
    {
        foreach (SkillSlotUI slot in ui_SkillSlot)
        {
            if (slot == null)
                continue;
        }
    }
}