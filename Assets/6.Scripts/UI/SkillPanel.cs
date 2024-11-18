using UnityEngine;
using UnityEngine.UI
    ;
public class SkillPanel : MonoBehaviour
{
    [Header("Skill Slots")]
    [SerializeField] private SkillSlotUI[] ui_SkillSlot = new SkillSlotUI[(int)SkillSlot.MAX];

    [Header("Skill Hanlder")]
    [SerializeField] private SO_SkillEventHandler hanlder;

    private string path = "Assets/10.ScriptableObjects/Resources/" +
        "Skills/SO_SkillHander.asset";
    private void Start()
    {
        if(hanlder == null)
        {
            hanlder = Resources.Load<SO_SkillEventHandler>(path);
        }

        SetSkillHandlerToSlots();
    }

    private void SetSkillHandlerToSlots()
    {
        foreach(SkillSlotUI slot in ui_SkillSlot)
        {
            if(slot == null) continue;
            slot.SetSkillHandler(hanlder);
        }
    }


}
