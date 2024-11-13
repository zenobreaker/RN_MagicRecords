using UnityEngine;

/// <summary>
/// ��ȭ ��ź - �������� ���� ȿ���� ����� ��ȭ�� ��ź�� �߻��Ѵ�.
/// </summary>
public class ReinforcedMagicBullet : ActiveSkill
{
    protected override void ApplyEffects()
    {
        
    }

    protected override void StartPhase(int phaseIndex)
    {
        if (phaseIndex < 0 || phaseIndex >= skillData.phaseList.Count)
            return;

        //TODO: ������Ʈ Ǯ������ �������� 
        // ��ź ������Ʈ ���� 
        PhaseSkill phaseSkill = skillData.phaseList[phaseIndex];

        Vector3 position = ownerObject.transform.position + phaseSkill.spawnPosition;
        Quaternion rotation = ownerObject.transform.rotation * phaseSkill.spwanQuaternion;

        GameObject.Instantiate<GameObject>(skillData.phaseList[phaseIndex].skillObject, position, rotation);
    }

}
