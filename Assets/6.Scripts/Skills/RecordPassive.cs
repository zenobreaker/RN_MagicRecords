using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RecordPassive : PassiveSkill
{
    protected RecordData recordData;
    private readonly Dictionary<StatusComponent, List<StatModifier>> appliedModifiers = new();
    private readonly List<CommonRecordModule> modules = new();
    private BattleManager battle;
    public RecordPassive(SO_RecordData data) : base()
    {
        if (data == null) return;
        recordData = data.GetRecordData(); 
        foreach (var template in data.Modules ?? new List<CommonRecordModule>())
        {
            if (template == null) continue;
            var module = (CommonRecordModule)template.Clone();
            module.SetRecordId(data.id);
            modules.Add(module);
        }
    }

    public override void OnAcquire(GameObject skillOwner)
    {
        OnLose();
        owner = skillOwner;
        if (owner == null) return;
        foreach (var module in modules) module.OnAcquire(owner, 1);
        battle = modules.Count > 0 ? BattleManager.Instance : null;
        if (battle != null && modules.Count > 0)
        {
            battle.OnBattleStarted += OnBattleStarted;
            if (battle.IsInBattle) OnBattleStarted();
        }
    }
    private void OnBattleStarted()
    {
        if (owner == null || battle == null) return;
        foreach (var module in modules) module.OnBattleStarted(battle.CurrentEnemies);
    }
    public override void OnSkillCast(SkillUseEvent evt, SkillRuntimeContext context)
    {
        foreach (var module in modules) module.OnSkillCast(evt, context);
    }
    public override void OnUpdate(float deltaTime)
    {
        foreach (var module in modules) module.OnUpdate(deltaTime);
    }


    public override void OnApplyStaticEffect(StatusComponent status)
    {
        if (recordData == null || status == null) return;
        foreach (var module in modules) module.OnApplyStaticEffect(status);
        if (modules.Count > 0) return;
        RemoveModifiers(status);
        if (!status.IsSameJob(recordData.targetFilter) || recordData.Stats == null) return;

        var modifiers = new List<StatModifier>();
        appliedModifiers[status] = modifiers;
        foreach (var modifier in recordData.Stats)
        {
            if (modifier == null) continue;
            var statMod = ModifierFactory.CreateStatModifier(
                modifier.Status, modifier.Value, modifier.ValueType);
            status.ApplyBuff(statMod);
            modifiers.Add(statMod);
        }
    }

    public override void OnLose()
    {
        if (battle != null) battle.OnBattleStarted -= OnBattleStarted;
        battle = null;
        foreach (var module in modules) module.OnLose();
        owner = null;
        foreach (var target in new List<StatusComponent>(appliedModifiers.Keys))
            RemoveModifiers(target);
    }

    private void RemoveModifiers(StatusComponent target)
    {
        if (!appliedModifiers.TryGetValue(target, out var modifiers)) return;
        foreach (var modifier in modifiers)
            target.SafeInvoke(value => value.RemoveBuff(modifier));
        appliedModifiers.Remove(target);
    }
}
