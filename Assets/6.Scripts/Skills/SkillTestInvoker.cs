using System.Collections.Generic;
using UnityEngine;


public class SkillTestInvoker : MonoBehaviour
{
#if UNITY_EDITOR
    private Dictionary<SkillSlot, ActiveSkill> testActiveSkills = new();
    private List<PassiveSkill> testPassiveSkills = new();

    private static SkillTestInvoker instance;
    private Character owner;

    private void Awake()
    {
        instance = this;
        owner = GetComponent<Character>();
    }

    public static void ReceiveSkillForTest(SkillSlot slot, SO_SkillData skill, int skillLevel = 1)
    {
        if (instance == null)
        {
            Debug.LogWarning("Scene에 SkillTestInvoker가 없음");
            return;
        }

        instance.ExecuteSkill(slot, skill, skillLevel);
    }

    private void ExecuteSkill(SkillSlot slot, SO_SkillData skill, int skillLevel)
    {
        if (skill == null) return;
        if (owner == null) return;

        SkillComponent skillComponent = owner.GetComponent<SkillComponent>();
        if (skillComponent == null) return;

        if (skill == null)
        {
            skillComponent.SetActiveSkill(slot, null);
            return;
        }

        if (skill is SO_ActiveSkillData active)
        {
            ActiveSkill activeSkill = (ActiveSkill)active.CreateSkill();
            if (activeSkill == null)
            {
                Debug.LogWarning($"Skill이 정상적으로 생성되지 않았습니다.");
                return;
            }

            if (testActiveSkills.ContainsKey(slot))
                testActiveSkills.Remove(slot);

            testActiveSkills.Add(slot, activeSkill);

            activeSkill.SetLevel(skillLevel);
            skillComponent.SetActiveSkill(slot, activeSkill);
        }
        else if (skill is SO_PassiveSkillData passive)
        {
            PassiveSkill passiveSkill = (PassiveSkill)passive.CreateSkill();
            if (passiveSkill == null)
            {
                Debug.LogWarning($"Skill이 정상적으로 생성되지 않았습니다.");
                return;
            }

            testPassiveSkills.Remove(passiveSkill);
            testPassiveSkills.Unique(passiveSkill);

            passiveSkill.SetLevel(skillLevel);
            AppManager.Instance.AddPassiveSkill(1, passiveSkill);
            AppManager.Instance.OnLose(1, owner);
            AppManager.Instance.OnAcquire(1, owner);
            AppManager.Instance.OnApplyStaticEffct(1, owner);
        }
    }

    public static void UndoSkill(SkillSlot slot, SO_SkillData skill)
    {
        if (instance == null)
            return;

        instance.ExecuteUndo(slot, skill);
    }

    private void ExecuteUndo(SkillSlot slot, SO_SkillData skill)
    {
        SkillComponent skillComp = owner.GetComponent<SkillComponent>();
        if (skillComp == null) return;

        // 액티브 스킬 해제
        if (skill is SO_ActiveSkillData active)
        {
            if (testActiveSkills.ContainsKey(slot))
            {
                skillComp.SetActiveSkill(slot, null);
                testActiveSkills.Remove(slot);
            }
        }
        // 패시브 스킬 해제
        else if (skill is SO_PassiveSkillData passiveSkill)
        {
            PassiveSkill target = testPassiveSkills.Find(p => p.SkillID == skill.id);

            if (target != null)
            {
                testPassiveSkills.Remove(target);
                AppManager.Instance.OnLose(1, owner);
                AppManager.Instance.RemovePassiveSkill(1, target);
            }
        }
    }

#endif
}

