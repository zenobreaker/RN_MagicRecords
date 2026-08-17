using System;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 옵저버 패턴처럼 사용하기 위한 징검다리 클래스
/// </summary>
[CreateAssetMenu(fileName = "SO_SkillEventHandler", menuName = "Scriptable Objects/SO_SkillEventHandler")]
public class SO_SkillEventHandler : ScriptableObject
{
    public event Action<SkillSlot, ActiveSkill> OnSetActiveSkill;
    public event Action<SkillSlot, bool> OnInSkillCooldown;
    public event Action<SkillSlot, float, float> OnSkillCooldown;

    public event Action OnBeginUseSkill;
    public event Action OnEndUseSkill;
    public event Action OnDisableSkill;

    public event Action<int> OnUpdateMagicBulletLoad;
    public event Action<Queue<BulletData>> OnChangeBullets;

    // SkillSlot은 SLOT1(100)부터 시작하므로 enum 값을 인덱스로 사용할 수 있도록
    // 최대 값까지 포함한 크기를 확보합니다. (기존 이벤트 구독자와의 호환용 캐시)
    public ActiveSkill[] CurrentActiveSkills = new ActiveSkill[(int)SkillSlot.MAX];

    public int MagicBulletCount { get; private set; }

    #region EQUIP SKILL
    public void OnSetting_ActiveSkill(SkillSlot slot, ActiveSkill skill)
    {
        int slotIndex = (int)slot;
        if (slotIndex >= 0 && slotIndex < CurrentActiveSkills.Length)
            CurrentActiveSkills[slotIndex] = skill;

        OnSetActiveSkill?.Invoke(slot, skill);
    }

    // 씬 종료나 로비로 나갈 때 캐시를 비워주는 유틸리티 함수
    public void ClearCache()
    {
        Array.Clear(CurrentActiveSkills, 0, CurrentActiveSkills.Length);
    }

    #endregion

    #region COOLDOWN
    // 쿨타임 
    public void OnInCoolDown(SkillSlot slot, bool inCooldown) => OnInSkillCooldown?.Invoke(slot, inCooldown);
    public void OnCooldown(SkillSlot slot, float cooldown, float maxCooldown) => OnSkillCooldown?.Invoke(slot, cooldown, maxCooldown);
    #endregion

#region USE SKILL
    public void OnBegin_UseSkill() => OnBeginUseSkill?.Invoke();

    public void OnEnd_UseSkill() => OnEndUseSkill?.Invoke();
#endregion

    // 장착 해제 
    public void OnUnequipment() => OnDisableSkill?.Invoke();

    public void OnChangedBullets(Queue<BulletData> bullets)
    {
        OnChangeBullets?.Invoke(bullets);
    }

    public void OnUpdateMagciBulletLoad(int maxBulltes)
    {
        OnUpdateMagicBulletLoad?.Invoke(maxBulltes);
    }
}
