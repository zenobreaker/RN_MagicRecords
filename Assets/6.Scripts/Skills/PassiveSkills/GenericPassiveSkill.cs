using System.Collections.Generic;
using UnityEngine;

// 패시브가 발동될 타이밍 정의
public enum PassiveTriggerTime
{
    OnAcquire,
    OnSkillCast,       // 스킬 시전 시 (컨텍스트 조작)
    OnSpawnObject,     // 투사체 생성 시 (투사체 조작)
    OnHit,             // 적중 시
    OnDamaged
}

// 모든 패시브 모듈의 조상
[System.Serializable]
public abstract class PassiveModule
{
    // 이 모듈이 언제 실행될 것인가?
    public PassiveTriggerTime triggerTime;

    // 💡 각 타이밍에 맞춰 오버라이드할 수 있는 가상 함수들!
    public virtual void OnSkillCast(SkillUseEvent evt, SkillRuntimeContext context) { }
    public virtual void OnSpawnObject(ISkillEffect spawnedObject) { }
    public virtual void OnHit(GameObject target, DamageData damageData) { }
}

public class GenericPassiveSkill : PassiveSkill
{
    // [TriggerTime] -> List<PassiveModule>
    private Dictionary<PassiveTriggerTime, List<PassiveModule>> moduleCache = new();

    public GenericPassiveSkill(SO_PassiveSkillData data) : base(data)
    {
        // 💡 SO에서 조립된 모듈들을 트리거 타이밍별로 분류해서 캐싱!
        foreach (var module in data.Modules)
        {
            if (!moduleCache.ContainsKey(module.triggerTime))
                moduleCache[module.triggerTime] = new List<PassiveModule>();

            moduleCache[module.triggerTime].Add(module);
        }
    }

    // 💡 특정 이벤트가 들어오면, 캐싱된 모듈들만 골라서 실행!
    public override void OnSkillCast(SkillUseEvent evt, SkillRuntimeContext context)
    {
        if (moduleCache.TryGetValue(PassiveTriggerTime.OnSkillCast, out var modules))
        {
            foreach (var mod in modules) mod.OnSkillCast(evt, context);
        }
    }

    // (새로 추가할) 투사체 생성 이벤트
    public void OnSpawnObject(ISkillEffect spawnedObject)
    {
        if (moduleCache.TryGetValue(PassiveTriggerTime.OnSpawnObject, out var modules))
        {
            foreach (var mod in modules) mod.OnSpawnObject(spawnedObject);
        }
    }
}