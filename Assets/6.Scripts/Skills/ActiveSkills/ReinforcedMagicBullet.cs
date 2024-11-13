using UnityEngine;

/// <summary>
/// 강화 마탄 - 전방으로 여러 효과가 내장된 강화된 마탄을 발사한다.
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

        //TODO: 오브젝트 풀링에서 가져오기 
        // 마탄 오브젝트 생성 
        PhaseSkill phaseSkill = skillData.phaseList[phaseIndex];

        Vector3 position = ownerObject.transform.position + phaseSkill.spawnPosition;
        Quaternion rotation = ownerObject.transform.rotation * phaseSkill.spwanQuaternion;

        GameObject.Instantiate<GameObject>(skillData.phaseList[phaseIndex].skillObject, position, rotation);
    }

}
