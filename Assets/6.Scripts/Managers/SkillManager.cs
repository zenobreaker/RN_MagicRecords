using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkillManager : Singleton<SkillManager>
{
    [Header("Skill Event Handler")]
    [SerializeField]
    private SO_SkillEventHandler skillEventHandler;

    private const int SKILL_SLOT_MAX_COUNT = 4;

    // 캐릭터별 장착 스킬
    // Key   : Character ID
    // Value : Runtime Data
    private readonly Dictionary<int, List<SkillRuntimeData>> equippedActiveSkills = new();

    public event Action OnDataChanged;

    protected override void Awake()
    {
        base.Awake();

        ResetRunTimeData();
    }


    ///////////////////////////////////////////////////////////////////////////
    #region SKILL DATA

    /// <summary>
    /// 특정 캐릭터의 Active Skill을 특정 슬롯에 장착합니다.
    /// </summary>
    public void EquipActiveSkill(
        int charId,
        int slot,
        SkillRuntimeData skill)
    {
        if (!equippedActiveSkills.TryGetValue(
                charId,
                out var equippedSkills))
        {
            Debug.LogWarning(
                $"[SkillManager] 존재하지 않는 Character ID입니다. ID : {charId}");
            return;
        }

        if (slot < 0 || slot >= SKILL_SLOT_MAX_COUNT)
        {
            Debug.LogWarning(
                $"[SkillManager] 잘못된 Skill Slot입니다. Slot : {slot}");
            return;
        }

        // 이미 장착된 스킬이라면 기존 슬롯에서 제거
        int prevSlot = equippedSkills.FindIndex(x => x == skill);

        if (prevSlot != -1)
        {
            equippedSkills[prevSlot] = null;
        }

        equippedSkills[slot] = skill;

        OnDataChanged?.Invoke();
    }

    /// <summary>
    /// 특정 캐릭터가 장착한 Active Skill 목록을 반환합니다.
    /// </summary>
    public List<SkillRuntimeData> GetActiveSkillList(int charId)
    {
        if (!equippedActiveSkills.TryGetValue(
                charId,
                out var equippedSkills))
        {
            return null;
        }

        return equippedSkills;
    }

    /// <summary>
    /// 특정 캐릭터의 장착 스킬 ID 목록을 반환합니다.
    /// </summary>
    public List<int> GetActiveSkillIDList(int charId)
    {
        if (!equippedActiveSkills.TryGetValue(
                charId,
                out var equippedSkills))
        {
            return null;
        }

        return equippedSkills
            .Select(skill =>
                skill != null
                    ? skill.GetSkillID()
                    : 0)
            .ToList();
    }

    /// <summary>
    /// 특정 캐릭터의 특정 슬롯에 장착된 스킬 데이터를 반환합니다.
    /// </summary>
    public SkillRuntimeData GetActiveSkillData(
        int charId,
        int slot)
    {
        if (!equippedActiveSkills.TryGetValue(
                charId,
                out var equippedSkills))
        {
            return null;
        }

        if (slot < 0 || slot >= equippedSkills.Count)
        {
            return null;
        }

        return equippedSkills[slot];
    }

    /// <summary>
    /// 현재 장착된 스킬을 SkillComponent에 적용합니다.
    /// </summary>
    public void SetActiveSkills(
        int charId,
        SkillComponent skillComp)
    {
        if (skillComp == null)
            return;

        if (!equippedActiveSkills.TryGetValue(
                charId,
                out var equippedSkills))
        {
            return;
        }

        for (int i = 0; i < SKILL_SLOT_MAX_COUNT; i++)
        {
            SkillRuntimeData skillData =
                i < equippedSkills.Count
                    ? equippedSkills[i]
                    : null;

            ActiveSkill activeSkill = null;

            if (skillData?.template is SO_ActiveSkillData skillDataAsset)
            {
                Skill skill = skillDataAsset.CreateSkill();
                activeSkill = skill as ActiveSkill;
            }

            SkillSlot skillSlot =
                (SkillSlot)((int)SkillSlot.SLOT1 + i);

            skillComp.SetActiveSkill(
                skillSlot,
                activeSkill);
        }
    }

    /// <summary>
    /// 런타임 스킬 데이터를 초기화합니다.
    /// </summary>
    public void ResetRunTimeData()
    {
        equippedActiveSkills.Clear();

        var slots =
            new List<SkillRuntimeData>(SKILL_SLOT_MAX_COUNT);

        for (int i = 0; i < SKILL_SLOT_MAX_COUNT; i++)
        {
            slots.Add(null);
        }

        // 현재 1번 캐릭터의 스킬 슬롯 처리
        equippedActiveSkills.Add(1, slots);

        OnDataChanged?.Invoke();
    }

    #endregion


    ///////////////////////////////////////////////////////////////////////////
    #region EVENT NOTIFY

    /*
     * SkillComponent가 직접 SO_SkillEventHandler를 호출하지 않도록 합니다.
     *
     * 외부에서는 SkillManager.NotifyXXX()만 호출합니다.
     * 실제 Handler 호출 여부와 구현은 SkillManager가 책임집니다.
     */

    public void NotifyActiveSkillChanged(
        SkillSlot slot,
        ActiveSkill skill)
    {
        skillEventHandler?.OnSetting_ActiveSkill(slot, skill);
    }

    public void NotifySkillCooldownState(
        SkillSlot slot,
        bool isCooldown)
    {
        skillEventHandler?.OnInCoolDown(slot, isCooldown);
    }

    public void NotifySkillCooldown(
        SkillSlot slot,
        float currentCooldown,
        float maxCooldown)
    {
        skillEventHandler?.OnCooldown(
            slot,
            currentCooldown,
            maxCooldown);
    }

    public void NotifySkillUseBegin()
    {
        skillEventHandler?.OnBegin_UseSkill();
    }

    public void NotifySkillUseEnd()
    {
        skillEventHandler?.OnEnd_UseSkill();
    }

    public void NotifyMagicBulletLoad(
        int bulletCount)
    {
        skillEventHandler?.OnUpdateMagciBulletLoad(bulletCount);
    }

    public void NotifyMagicBulletChanged(
        Queue<BulletData> bullets)
    {
        skillEventHandler?.OnChangedBullets(bullets);
    }

    #endregion
}