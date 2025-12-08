using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public class PassiveSystem
{
    // key : Job ID 
    protected Dictionary<int, List<PassiveSkill>> passiveSkillList = new();
    private List<PassiveSkill> updatablePassive = new List<PassiveSkill>();

    public void Add(int jobID, PassiveSkill skill)
    {
        if (passiveSkillList.ContainsKey(jobID))
        {
            passiveSkillList[jobID].Unique(skill);
        }
        else
        {
            passiveSkillList.Add(jobID, new List<PassiveSkill>());
            passiveSkillList[jobID].Unique(skill);
        }
    }

    public void Remove(int jobID, PassiveSkill skill)
    {
        if(passiveSkillList.ContainsKey(jobID))
            passiveSkillList[jobID].Remove(skill);
    }

    public void OnInit()
    {
        passiveSkillList.Clear();

        //TODO : 직업이 추가되면 리스트 추가
        passiveSkillList[1] = new();
    }


    public void OnChangedLevel(int jobID, SkillRuntimeData data)
    {
        if (passiveSkillList.ContainsKey(jobID) == false)
            return;

        int skillID = data.GetSkillID();
        int newLevel = data.currentLevel;

        // 1. list 에서 해당 패시브 객체를 찾는다.
        PassiveSkill passive = passiveSkillList[jobID].Find(s => s.SkillID == skillID);

        // 2. 최소 승급 체크 : 1레벨 이상인데 아직 리스트에 없는 경우
        if (newLevel > 0 && passive == null)
        {
            // a. 템플릿 정보를 이용해 PassiveSkill 객체 생성
            passive = (PassiveSkill)data.template.CreateSkill();

            // b. PassiveSystem에 리스트 추가
            Add(jobID, passive);

            Debug.Log($"[PassiveSystem] ID {skillID} 스킬이 1레벨로 습득");
        }

        // 3. 레벨 동기화 및 정적 효과 재적용
        if (passive != null)
        {
            // 레벨 동기화 
            passive.SetLevel(newLevel);

            //TODO : 어딘가로부터 플레이어 정보를 전달해서 능력치 할당 등의 이벤트 적용 
            //passive.OnApplyStaticEffect()

        }
    }

   private bool IsOverriden(Type type, string methodName)
    {
        System.Reflection.MethodInfo methodInfo = type.GetMethod(methodName); 

        if(methodInfo == null ) return false;   

        System.Reflection.MethodInfo baseDefinition = methodInfo.GetBaseDefinition();

        return methodInfo.DeclaringType != baseDefinition.DeclaringType;
    }

    //private bool IsOverridenAlternative(Type type, string methodName)
    //{
    //    System.Reflection.MethodInfo methodInfo = type.GetMethod(methodName);

    //    if (methodInfo == null) return false;

    //    // IsFinal, IsVirtual, IsAbstract 속성을 함께 확인하면 정확도 상승
    //    // 간단하게 NewSlot이 설정되지 않았는지 확인
    //    return !methodInfo.Attributes.HasFlag(System.Reflection.MethodAttributes.NewSlot);
    //}

    public void OnAcquire(int jobID, GameObject ownerObj)
    {
        if (passiveSkillList.ContainsKey(jobID) == false) return;

        foreach (PassiveSkill skill in passiveSkillList[jobID])
        {
            skill.OnAcquire(ownerObj);

            // 해당 스킬이 OnUpdate 메서드를 오버라이드 한 전적이 있는지 확인
            Type skillType = skill.GetType();
            bool needsUpdate = IsOverriden(skillType, "OnUpdate");

            if (needsUpdate)
            {
                updatablePassive.Unique(skill);
            }
        }
    }

    public void OnLose() 
    {
        updatablePassive.Clear();
    }

    public void OnUpdate(float dt)
    {
        foreach (PassiveSkill skill in updatablePassive)
        {
            skill.OnUpdate(dt);
        }
    }
}
