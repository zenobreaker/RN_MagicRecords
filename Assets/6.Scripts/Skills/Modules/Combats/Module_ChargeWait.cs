using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;


[ModuleCategory("Utility/Charge Wait")]
public class Module_ChargeWait : SkillModule
{
    [Tooltip("최대 충전 시간")]
    public float maxChargeTime = 3.0f;

    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        if (skill == null) return;

        skill.isWaitingForRelease = true;
        WaitForMaxChargeAsync(skill, skill.PhaseToken).Forget();
    }
    private async UniTaskVoid WaitForMaxChargeAsync(ActiveSkill skill, CancellationToken token)
    {
        if (maxChargeTime > 0f)
        {
            // 최대 시간 대기
            bool isCancelled = await UniTask.Delay(TimeSpan.FromSeconds(maxChargeTime), cancellationToken: token).SuppressCancellationThrow();

            // 중간에 유저가 키를 떼서(OnReleaseKey) 캔슬되었다면 조용히 종료!
            if (isCancelled) return;
        }
    }
}
