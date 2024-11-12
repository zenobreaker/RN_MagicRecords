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

        // 마탄 오브젝트 생성 
        GameObject.Instantiate<GameObject>(skillData.phaseList[phaseIndex].skillObject);
    }

}
