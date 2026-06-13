using UnityEngine;
using UnityEngine.UI;
public class SkillPanel : MonoBehaviour
{
    [Header("Skill Slots")]
    [SerializeField] private SkillSlotUI[] ui_SkillSlot = new SkillSlotUI[(int)SkillSlot.MAX];

    [Header("Skill Hanlder")]
    [SerializeField] private SO_SkillEventHandler handler;

    private readonly string path = "Skills/SO_SkillEventHandler";

    private void Awake()
    {
        if(handler == null)
        {
            handler = Resources.Load<SO_SkillEventHandler>(path);
        }
    }

    private void Start()
    {
        SetSkillHandlerToSlots();
    }

    private void SetSkillHandlerToSlots()
    {
        foreach(SkillSlotUI slot in ui_SkillSlot)
        {
            if(slot == null) continue;
            slot.SetSkillHandler(handler);
        }
    }
}
