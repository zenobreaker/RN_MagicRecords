using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Edits stay local until the player confirms. Closing/destroying a popup cannot
// spend currency or modify the shared skill tree by accident.
public sealed class SkillEventSession
{
    private readonly Dictionary<int, SkillRuntimeData> originals = new();
    private readonly Dictionary<int, SkillRuntimeData> drafts = new();
    private readonly Dictionary<int, (int level, bool unlocked)> initial = new();
    private readonly HashSet<int> modifiedSkills = new();
    private readonly HashSet<int> loadoutSkills = new();
    private readonly List<SkillRuntimeData> originalSlots;
    private readonly int[] initialSlots;
    private readonly Func<int> balance;
    private readonly Func<int, bool> spend;
    private readonly Action<int, SkillRuntimeData> equip;
    private readonly int baseCost, costPerLevel;
    private bool committed;
    private int replacementCost;
    private bool replacementPayment;
    public int PendingCost { get; private set; }
    public int ReplacementCount { get; private set; }
    public int[] Slots { get; }
    public IEnumerable<SkillRuntimeData> Skills => drafts.Values;
    public int AvailableCurrency => Math.Max(0, balance() - PendingCost);
    public bool HasChanges => PendingCost > 0 || !Slots.SequenceEqual(initialSlots) ||
        modifiedSkills.Any(id => drafts[id].currentLevel != initial[id].level || drafts[id].isUnlocked != initial[id].unlocked);

    public SkillEventSession(IEnumerable<SkillRuntimeData> skills, List<SkillRuntimeData> slots,
        int baseCost, int costPerLevel, Func<int> balance, Func<int, bool> spend,
        Action<int, SkillRuntimeData> equip)
    {
        this.balance = balance; this.spend = spend; this.equip = equip;
        this.baseCost = Math.Max(0, baseCost); this.costPerLevel = Math.Max(0, costPerLevel);
        originalSlots = slots;
        foreach (var data in skills.Concat(slots).Where(s => s?.template != null && !s.IsDevelopmentLocked))
        {
            int id = data.GetSkillID();
            originals[id] = data;
            initial[id] = (data.currentLevel, data.isUnlocked);
            drafts[id] = new SkillRuntimeData { template = data.template,
                currentLevel = data.currentLevel, isUnlocked = data.isUnlocked };
        }
        initialSlots = slots.Select(s => s?.template != null ? s.GetSkillID() : 0).ToArray();
        Slots = (int[])initialSlots.Clone();
        loadoutSkills.UnionWith(initialSlots.Where(id => id != 0));
    }

    public SkillRuntimeData GetSkill(int id) => drafts.TryGetValue(id, out var value) ? value : null;
    // Offer previews belong to this draft. Merely viewing Lv.1 must not unlock a skill.
    public void PrepareCandidate(int id)
    {
        var data = GetSkill(id);
        if (!committed && data?.template is SO_ActiveSkillData)
            data.currentLevel = Math.Max(1, data.currentLevel);
    }
    public void SetReplacementCost(int cost) { replacementCost = Math.Max(0, cost); replacementPayment = true; }
    public int UpgradeCost(int id)
    {
        var data = GetSkill(id);
        return (int)Math.Min(int.MaxValue, (long)baseCost + (long)costPerLevel * Math.Max(0, (data?.currentLevel ?? 1) - 1));
    }
    public bool CanUpgrade(int id)
    {
        var data = GetSkill(id);
        return !committed && data != null && (data.isUnlocked || data.currentLevel > 0) &&
            data.currentLevel < data.GetMaxSkillLevel() && UpgradeCost(id) <= AvailableCurrency;
    }
    public bool TryUpgrade(int id)
    {
        if (!CanUpgrade(id)) return false;
        PendingCost += UpgradeCost(id);
        drafts[id].currentLevel++;
        drafts[id].isUnlocked = true;
        modifiedSkills.Add(id);
        return true;
    }
    public bool TryReplace(int slot, int id, bool allowUnlock = false)
    {
        var data = GetSkill(id);
        if (committed || slot < 0 || slot >= Slots.Length || data == null ||
            !(data.template is SO_ActiveSkillData) || Slots.Contains(id) ||
            (!allowUnlock && (!data.isUnlocked && data.currentLevel <= 0))) return false;
        if (data.currentLevel <= 0 && !allowUnlock) return false;
        if (ReplacementCount == 0 && replacementCost > AvailableCurrency) return false;
        if (ReplacementCount == 0) PendingCost += replacementCost;
        data.isUnlocked = true;
        data.currentLevel = Math.Max(1, data.currentLevel);
        modifiedSkills.Add(id);
        loadoutSkills.Add(id);
        Slots[slot] = id;
        ReplacementCount++;
        return true;
    }
    public bool CanRearrangeSkill(int id) => loadoutSkills.Contains(id) && GetSkill(id) != null;
    public bool TryEquipOwned(int slot, int id)
    {
        if (committed || slot < 0 || slot >= Slots.Length || !CanRearrangeSkill(id) || Slots[slot] == id)
            return false;
        int previous = Array.IndexOf(Slots, id);
        if (previous >= 0) Slots[previous] = 0;
        Slots[slot] = id;
        return true;
    }
    public bool TryUnequip(int slot)
    {
        if (committed || slot < 0 || slot >= Slots.Length || Slots[slot] == 0) return false;
        Slots[slot] = 0;
        return true;
    }
    public bool Commit()
    {
        if (committed || balance() < PendingCost ||
            !originalSlots.Select(s => s?.template != null ? s.GetSkillID() : 0).SequenceEqual(initialSlots) ||
            originals.Any(p => p.Value.currentLevel != initial[p.Key].level || p.Value.isUnlocked != initial[p.Key].unlocked))
            return false;
        // Lock before payment callbacks can re-enter Commit.
        committed = true;
        if ((PendingCost > 0 || (replacementPayment && ReplacementCount > 0)) && !spend(PendingCost))
        { committed = false; return false; }
        foreach (int id in modifiedSkills)
        {
            var source = originals[id]; var draft = drafts[id];
            if (source.currentLevel == draft.currentLevel && source.isUnlocked == draft.isUnlocked) continue;
            source.currentLevel = draft.currentLevel; source.isUnlocked = draft.isUnlocked;
            source.OnDataChanged?.Invoke(source);
        }
        // Clear changed slots first: the existing equip API also removes duplicate skills.
        // This preserves rotations/moves regardless of slot iteration order.
        for (int i = 0; i < Slots.Length; i++)
            if (Slots[i] != initialSlots[i]) equip(i, null);
        for (int i = 0; i < Slots.Length; i++)
            if (Slots[i] != initialSlots[i] && Slots[i] != 0) equip(i, originals[Slots[i]]);
        return true;
    }
}
