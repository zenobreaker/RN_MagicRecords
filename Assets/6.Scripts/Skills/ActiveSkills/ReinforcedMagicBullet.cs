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

        //TODO: ������Ʈ Ǯ������ �������� 
        // ��ź ������Ʈ ���� 

        Vector3 localOffset = phaseSkill.spawnPosition; // ���� ��ġ(���� ����)
        Vector3 position = ownerObject.transform.TransformPoint(localOffset); // ���� -> ���� ��ǥ�� ����
        Quaternion rotation = ownerObject.transform.rotation * phaseSkill.spwanQuaternion;

        ObjectPooler.SpawnFromPool(phaseSkill.objectName, position, rotation);
        //GameObject.Instantiate<GameObject>(phaseSkill.skillObject, position, rotation);
    }

    public override void End_DoAction()
    {
        phaseSkill = null;
    }
}
