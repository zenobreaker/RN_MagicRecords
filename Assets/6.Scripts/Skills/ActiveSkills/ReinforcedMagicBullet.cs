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

        // ��ź ������Ʈ ���� 
        GameObject.Instantiate<GameObject>(skillData.phaseList[phaseIndex].skillObject);
    }

}
