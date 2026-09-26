using System.Collections.Generic;
using UnityEngine;

// 패시브가 발동될 타이밍 정의
public enum PassiveTriggerTime
{
    OnApplyStaticEffect,
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
    public virtual int TargetSkillID => 0;
    // 이 모듈이 언제 실행될 것인가?
    public PassiveTriggerTime triggerTime;

    // 💡 각 타이밍에 맞춰 오버라이드할 수 있는 가상 함수들!
    public virtual void OnApplyStaticEffect(StatusComponent status) { }
    public virtual void OnLose() { } // 패시브가 지워질 때 롤백용
    public virtual void OnSkillCast(SkillUseEvent evt, SkillRuntimeContext context) { }
    public virtual void OnSpawnObject(ISkillEffect spawnedObject, ActiveSkill casterSkill) { }
    public virtual void OnAssistDroneNormalProjectile(ISkillEffect spawnedObject, Character owner) { }
    public virtual void OnHit(GameObject target, DamageData damageData) { }

    // SO의 설정만 복사하고 소유자/쿨타임/적용된 버프 등 런타임 상태는 공유하지 않습니다.
    public virtual PassiveModule Clone()
    {
        var clone = (PassiveModule)System.Activator.CreateInstance(GetType());
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(this), clone);
        return clone;
    }
}

public class GenericPassiveSkill : PassiveSkill
{
    // [TriggerTime] -> List<PassiveModule>
    private readonly Dictionary<PassiveTriggerTime, List<PassiveModule>> moduleCache = new();
    private readonly List<PassiveModule> runtimeModules = new();
    private BattleManager subscribedBattleManager;

    public GenericPassiveSkill(SO_PassiveSkillData data) : base(data)
    {
        // 💡 SO에서 조립된 모듈들을 트리거 타이밍별로 분류해서 캐싱!
        if (data.Modules == null) return;
        foreach (var template in data.Modules)
        {
            if (template == null) continue;
            var module = template.Clone();
            runtimeModules.Add(module);
            if (!moduleCache.ContainsKey(module.triggerTime))
                moduleCache[module.triggerTime] = new List<PassiveModule>();

            moduleCache[module.triggerTime].Add(module);
        }
    }

    public override void SetLevel(int level)
    {
        base.SetLevel(level);
        foreach (var module in runtimeModules)
            if (module is PassiveContextModule contextModule) contextModule.OnChangedLevel(level);
    }

    public override void OnChangedLevel(int newLevel) => SetLevel(newLevel);

    public override void OnAcquire(GameObject skillOwner)
    {
        UnsubscribeBattle();
        if (skillOwner == null) { OnLose(); return; }
        owner = skillOwner;
        foreach (var module in runtimeModules)
            if (module is PassiveContextModule contextModule) contextModule.OnAcquire(owner, skillLevel);

        if (moduleCache.ContainsKey(PassiveTriggerTime.OnHit))
        {
            subscribedBattleManager = BattleManager.Instance;
            if (subscribedBattleManager != null) subscribedBattleManager.OnAnyAttackHit += OnAttackHit;
        }
    }

    public override void OnApplyStaticEffect(StatusComponent status)
    {
        if (moduleCache.TryGetValue(PassiveTriggerTime.OnApplyStaticEffect, out var modules))
            foreach (var module in modules) module.OnApplyStaticEffect(status);
    }

    private void OnAttackHit(GameObject attacker, GameObject target, DamageEvent damageEvent)
    {
        if (moduleCache.TryGetValue(PassiveTriggerTime.OnHit, out var modules))
            foreach (var module in modules)
                if (module is PassiveContextModule contextModule)
                    contextModule.OnAttackHit(attacker, target, damageEvent);
    }

    public override void OnLose()
    {
        UnsubscribeBattle();
        foreach (var module in runtimeModules) module.OnLose();
        owner = null;
    }

    private void UnsubscribeBattle()
    {
        if (subscribedBattleManager != null) subscribedBattleManager.OnAnyAttackHit -= OnAttackHit;
        subscribedBattleManager = null;
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
    public void OnSpawnObject(ISkillEffect spawnedObject, ActiveSkill casterSkill)
    {
        if (moduleCache.TryGetValue(PassiveTriggerTime.OnSpawnObject, out var modules))
        {
            foreach (var mod in modules) mod.OnSpawnObject(spawnedObject, casterSkill);
        }
    }

    public void OnAssistDroneNormalProjectile(ISkillEffect spawnedObject, Character owner)
    {
        if (moduleCache.TryGetValue(PassiveTriggerTime.OnSpawnObject, out var modules))
        {
            foreach (var mod in modules)
                mod.OnAssistDroneNormalProjectile(spawnedObject, owner);
        }
    }

}
