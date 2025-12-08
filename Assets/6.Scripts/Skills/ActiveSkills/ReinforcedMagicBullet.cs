using System;
using UnityEngine;

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

        int layer = AnimatorLayerCache.GetLayerIndex(animator, phaseSkill?.actionData?.LayerName);

        animator.SetFloat(phaseSkill.actionData.ActionSpeedHash, phaseSkill.actionData.ActionSpeed);
        animator.Play(phaseSkill?.actionData?.StateName, layer, 0);
        weaponController?.DoAction(phaseSkill?.actionData?.WeaponActionName, layer);
    }

    private void OnProjectileHit(Collider self, Collider other, Vector3 point)
    {
        // hit Sound Play
        //SoundManager.Instance.PlaySFX(doactionData[index].hitSoundName);

        //Instantiate<GameObject>(doactionData[index].HitParticle, point, rootObject.transform.rotation);
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
            projectile.OnProjectileHit -= OnProjectileHit;
            projectile.OnProjectileHit += OnProjectileHit;
        }
    }

    public override void Play_Sound()
    {
        if (phaseSkill == null) return;
        base.Play_Sound();

        phaseSkill.actionData.Play_Sound();
    }

    public override void Play_CameraShake()
    {
        if (phaseSkill == null) return;
        base.Play_CameraShake();

        phaseSkill.actionData.Play_CameraShake();
    }
}
