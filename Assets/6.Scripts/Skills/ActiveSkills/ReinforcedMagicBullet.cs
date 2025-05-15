using System;
using UnityEngine;

/// <summary>
/// ��ȭ ��ź - �������� ���� ȿ���� ����� ��ȭ�� ��ź�� �߻��Ѵ�.
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

        phaseSkill?.actionData?.Play_CameraShake();
    }

    public override void Begin_DoAction()
    {
        if (phaseSkill == null) return;

        // ��ź ������Ʈ ���� 

        Vector3 localOffset = phaseSkill.spawnPosition; // ���� ��ġ(���� ����)
        Vector3 position = ownerObject.transform.TransformPoint(localOffset); // ���� -> ���� ��ǥ�� ����
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


    private void OnProjectileHit(Collider self, Collider other, Vector3 point)
    {
        Debug.Log($"self : {self} other : {other}");

        // hit Sound Play
        //SoundManager.Instance.PlaySFX(doactionData[index].hitSoundName);

        //Instantiate<GameObject>(doactionData[index].HitParticle, point, rootObject.transform.rotation);
    }

    public override void End_DoAction()
    {
         phaseSkill = null;
    }
}
