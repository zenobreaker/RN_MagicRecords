using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///  스킬 모듈화 시 최종적으로 만들어지는 클래스 
/// </summary>

public class GenericActiveSkill : ActiveSkill
{
    // [PhaseIndex][TriggerTime] -> List<Module>
    private Dictionary<int, Dictionary<SkillTriggerTime, List<SkillModule>>> phaseModuleCache; 
    public GenericActiveSkill(SO_SkillData skillData) : base(skillData)
    {
        phaseModuleCache = new Dictionary<int, Dictionary<SkillTriggerTime, List<SkillModule>>>();
    }

    public override void SetOwner(GameObject gameObject)
    {
        base.SetOwner(gameObject);
        for(int i =0; i < phaseList.Count; i++)
            CacheModules(i, phaseList[i]); 
    }

    private void CacheModules(int phaseIndex, PhaseSkill phase)
    {
        var timingCache = new Dictionary<SkillTriggerTime, List<SkillModule>>(); 

        //  타이밍 별로 미리 리스트를 만들어 놓는다. 
        foreach (SkillTriggerTime timing in Enum.GetValues(typeof(SkillTriggerTime)))
        {
            timingCache[timing] = new List<SkillModule>(); 
        }

        // 모듈들을 미리 분류해서 담아둔다.
        foreach (var module in phase.modules)
        {
            if (module == null) continue; 

            module.Init(ownerObject); 
            timingCache[module.triggerTime].Add(module);
        }

        // 전체 캐시에 저장 
        phaseModuleCache[phaseIndex] = timingCache;
    }

    protected void NotifyModules(int phaseIndex, SkillTriggerTime timing)
    {
        // 해당 타이밍의 바구니만 꺼내서 돌림 
        if (phaseModuleCache.TryGetValue(phaseIndex, out var timingCache)) 
        {
            if(timingCache.TryGetValue(timing, out var modules))
            {
                foreach(var module in modules)
                    module.OnNotify(ownerObject, this, phaseSkill); 
            }
        }
    }

    protected override void ApplyEffects()
    {
    }

    public override void EndPhaseAndNext()
    {
        ExecutePhase(phaseIndex + 1); 
    }

    public override void Begin_JudgeAttack(AnimationEvent e) => NotifyModules(phaseIndex, SkillTriggerTime.OnJudgeAttack);
    public override void Play_Sound() => NotifyModules(phaseIndex, SkillTriggerTime.OnSoundEvent);
    public override void Play_CameraShake() => NotifyModules(phaseIndex, SkillTriggerTime.OnCameraShake);
    public override void End_DoAction()
    {
        NotifyModules(phaseIndex, SkillTriggerTime.OnEndDoAction);
        base.End_DoAction();
    }
    protected override void ExecutePhase(int phaseIndex)
    {
        SetCurrentPhaseSkill(phaseIndex);

        // 타이밍으로 모든 서절된 모듈 실행 
        NotifyModules(this.phaseIndex, SkillTriggerTime.OnExecute);
    }
}
