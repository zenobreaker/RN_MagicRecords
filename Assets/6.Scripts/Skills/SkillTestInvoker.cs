using UnityEngine;


public class SkillTestInvoker : MonoBehaviour
{
#if UNITY_EDITOR
    private static SkillTestInvoker instance;
    private Character owner;

    private void Awake()
    {
        instance = this;
        owner = GetComponent<Character>();
    }

    public static void ReceiveSkillForTest(SkillSlot slot, SO_SkillData skill, int skillLevel =1)
    {
        if (instance == null)
        {
            Debug.LogWarning("Scene에 SkillTestInvoker가 없음");
            return;
        }

        instance.ExecuteSkill(slot, skill, skillLevel);
    }

    private void ExecuteSkill(SkillSlot slot, SO_SkillData skill, int skilLevel)
    {
        if (skill == null) return;
        if (owner == null) return; 

        SkillComponent skillComponent = owner.GetComponent<SkillComponent>();
        if (skillComponent == null) return; 

        if(skill == null)
        {
            skillComponent.SetActiveSkill(slot, null);
            return; 
        }

        if(skill is SO_ActiveSkillData active)
        {
            ActiveSkill activeSkill = (ActiveSkill)active.CreateSkill();
            if (activeSkill == null)
            {
                Debug.LogWarning($"Skill이 정상적으로 생성되지 않았습니다.");
                return;
            }

            activeSkill.SetLevel(skilLevel);
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

            passiveSkill.SetLevel(skilLevel);   
            AppManager.Instance.AddPassiveSkill(1, passiveSkill); 
            AppManager.Instance.OnAcquire(1, owner);
            AppManager.Instance.OnApplySttaicEffct(1, owner);
        }
    }


#endif
}

