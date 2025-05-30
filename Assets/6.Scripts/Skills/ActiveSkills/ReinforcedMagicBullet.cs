using System;
using UnityEngine;

/// <summary>
/// 강화 마탄 - 전방으로 여러 효과가 내장된 강화된 마탄을 발사한다.
/// </summary>
public class ReinforcedMagicBullet : ActiveSkill
{
    private PhaseSkill phaseSkill; 
    protected override void ApplyEffects()
    {
        
    }


    protected override void StartPhase(int phaseIndex)
    {
        if (phaseIndex < 0 || phaseIndex >= skillData.phaseList.Count)
            return;


        phaseSkill = skillData.phaseList[phaseIndex];
        if (phaseSkill == null || phaseSkill.actionData == null)
            return; 

        string animName = phaseSkill.skillActionPrefix + "." + phaseSkill.skillActionAnimation;
        //animator?.Play(animName);

        animator.runtimeAnimatorController = phaseSkill?.actionData?.AnimatorOv;
        weaponController?.SetWeaponAnimation(phaseSkill?.actionData?.WeaponAnimOv);
       
        animator.SetFloat(phaseSkill.actionData.ActionSpeedHash, phaseSkill.actionData.ActionSpeed);
        animator.Play(phaseSkill?.actionData?.StateName, 0, 0);
        weaponController?.DoAction(phaseSkill?.actionData?.StateName);
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


        // 마탄 오브젝트 생성 
        Vector3 localOffset = phaseSkill.spawnPosition; // 스폰 위치(로컬 기준)
        Vector3 position = ownerObject.transform.TransformPoint(localOffset); // 로컬 -> 월드 좌표로 변경
        Quaternion rotation = ownerObject.transform.rotation * phaseSkill.spwanQuaternion;

        GameObject obj = ObjectPooler.SpawnFromPool(phaseSkill.objectName, position, rotation);
        if (obj.TryGetComponent<Projectile>(out var projectile))
        {
            projectile.SetDamageInfo(ownerObject, phaseSkill.damageData);
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
