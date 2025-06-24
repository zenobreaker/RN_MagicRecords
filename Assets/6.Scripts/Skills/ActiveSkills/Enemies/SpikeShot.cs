using UnityEngine;

public class SpikeShot 
    : ActiveSkill

{
    public SpikeShot(string path) : base(path)
    {
    }

    public SpikeShot(SO_ActiveSkillData skillData) : base(skillData)
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

        animator.SetFloat(phaseSkill.actionData.ActionSpeedHash, phaseSkill.actionData.ActionSpeed);
        animator.Play(phaseSkill?.actionData?.StateName, 0, 0);
        weaponController?.DoAction(phaseSkill?.actionData?.StateName);
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


        // ���� ������Ʈ ���� 
        Vector3 localOffset = phaseSkill.spawnPosition; // ���� ��ġ(���� ����)
        Vector3 position = ownerObject.transform.TransformPoint(localOffset); // ���� -> ���� ��ǥ�� ����
        Quaternion rotation = ownerObject.transform.localRotation * phaseSkill.ValidSpawnQuaternion;

        GameObject obj = ObjectPooler.SpawnFromPool(phaseSkill.objectName, position, rotation);
        if (obj.TryGetComponent<Projectile>(out var projectile))
        {
            projectile.SetDamageInfo(ownerObject, phaseSkill.damageData);
            projectile.AddIgnore(ownerObject);
            //projectile.OnProjectileHit -= OnProjectileHit;
            //projectile.OnProjectileHit += OnProjectileHit;
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
