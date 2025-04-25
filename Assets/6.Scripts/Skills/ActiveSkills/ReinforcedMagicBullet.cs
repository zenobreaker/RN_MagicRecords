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
        string animName = phaseSkill.skillActionPrefix + "." + phaseSkill.skillActionAnimation;
        animator?.Play(animName);
    }

    public override void Begin_DoAction()
    {
        if (phaseSkill == null) return;

        // ��ź ������Ʈ ���� 

        Vector3 localOffset = phaseSkill.spawnPosition; // ���� ��ġ(���� ����)
        Vector3 position = ownerObject.transform.TransformPoint(localOffset); // ���� -> ���� ��ǥ�� ����
        Quaternion rotation = ownerObject.transform.rotation * phaseSkill.spwanQuaternion;

        GameObject obj = ObjectPooler.SpawnFromPool(phaseSkill.objectName, position, rotation);
        //GameObject.Instantiate<GameObject>(phaseSkill.skillObject, position, rotation);
        if (obj.TryGetComponent<Projectile>(out var projectile))
        {
            projectile.AddIgnore(ownerObject);
            projectile.OnProjectileHit -= OnProjectileHit;
            projectile.OnProjectileHit += OnProjectileHit;
        }
    }


    private void OnProjectileHit(Collider self, Collider other, Vector3 point)
    {
        Debug.Log($"self : {self} other : {other}");

        // hit Sound Play
        //SoundManager.Instance.PlaySFX(doActionDatas[index].hitSoundName);

        // Damage 
        IDamagable damage = other.GetComponent<IDamagable>();
        if (damage != null)
        {
            Vector3 hitPoint = self.ClosestPoint(other.transform.position);
            hitPoint = other.transform.InverseTransformPoint(hitPoint);
         //   damage?.OnDamage(ownerObject, this, hitPoint, doActionDatas[index]);
        }

        //Instantiate<GameObject>(doActionDatas[index].HitParticle, point, rootObject.transform.rotation);
    }

    public override void End_DoAction()
    {
        phaseSkill = null;
    }
}
