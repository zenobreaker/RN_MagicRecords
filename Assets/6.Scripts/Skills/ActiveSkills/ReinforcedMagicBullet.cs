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
        string animName = phaseSkill.skillActionPrefix + "." + phaseSkill.skillActionAnimation;
        animator?.Play(animName);
    }

    public override void Begin_DoAction()
    {
        if (phaseSkill == null) return;

        //TODO: 오브젝트 풀링에서 가져오기 
        // 마탄 오브젝트 생성 

        Vector3 localOffset = phaseSkill.spawnPosition; // 스폰 위치(로컬 기준)
        Vector3 position = ownerObject.transform.TransformPoint(localOffset); // 로컬 -> 월드 좌표로 변경
        Quaternion rotation = ownerObject.transform.rotation * phaseSkill.spwanQuaternion;

        ObjectPooler.SpawnFromPool(phaseSkill.objectName, position, rotation);
        //GameObject.Instantiate<GameObject>(phaseSkill.skillObject, position, rotation);
    }

    public override void End_DoAction()
    {
        phaseSkill = null;
    }
}
