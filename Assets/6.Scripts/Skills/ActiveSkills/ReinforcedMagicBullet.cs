using System;
using System.Collections.Generic;
using UnityEngine;

// 3. 마탄 전용 로직 모듈 (특수 로직도 모듈화 가능!)
[Serializable]
public class Module_MagicBulletConsum : SkillModule
{
    private IMagicBulletProvider provider; 

    public override void Init(GameObject owner)
    {
        provider = owner.GetComponent<SkillComponent>()?.GetCapability<IMagicBulletProvider>();
    }

    public override void OnNotify(GameObject owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        if (skill == null) return;
     
        bool isCrit = false; 
        provider?.TryConsumBullet(out isCrit);
        skill.Blackboard["isCrit"] = isCrit;
    }
}

/// <summary>
/// 강화 마탄 - 전방으로 여러 효과가 내장된 강화된 마탄을 발사한다.
/// </summary>
public class ReinforcedMagicBullet 
    : ActiveSkill
{
  
    public ReinforcedMagicBullet(SO_SkillData skillData)
        : base(skillData)
    {
    }

    protected override void ApplyEffects()
    {
        
    }

    protected override void ExecutePhase(int phaseIndex)
    {
        SetCurrentPhaseSkill(phaseIndex);
        if (phaseSkill == null || phaseSkill.actionData == null)
            return;

        ownerCharacter?.PlayAction(phaseSkill?.actionData);
        weaponController?.DoAction(phaseSkill?.actionData);
    }


    public override void End_DoAction()
    {
        base.End_DoAction();

        phaseSkill = null;
    }

    public override void Begin_JudgeAttack(AnimationEvent e) 
    {
        if (phaseSkill == null) return;

        base.Begin_JudgeAttack(e);


        // 1. 탄환 소모 시도 및 크리티컬 여부 확인
        bool isCrit = false;
        var provider = skillComponent?.GetCapability<IMagicBulletProvider>();
        if (provider != null)
        {
            // 공급자가 있을 때만 탄환 로직 수행
            provider.TryConsumBullet(out isCrit);
        }

        // 2. 마탄 오브젝트 생성 
        Vector3 localOffset = phaseSkill.spawnPosition; // 스폰 위치(로컬 기준)
        Vector3 position = ownerObject.transform.TransformPoint(localOffset); // 로컬 -> 월드 좌표로 변경
        Quaternion rotation = ownerObject.transform.rotation * phaseSkill.ValidSpawnQuaternion;

        GameObject obj = ObjectPooler.SpawnFromPool(phaseSkill.objectName, position, rotation);
        if (obj.TryGetComponent<Projectile>(out var projectile))
        {
            projectile.SetDamageInfo(ownerObject, phaseSkill.damageData, isCrit);
            projectile.AddIgnore(ownerObject);
        }
    }
}
