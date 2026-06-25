using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

public enum PhaseTransitionType
{
    NextPhase,           // 다음 페이즈로 즉시 넘어가기
    JumpToSpecificPhase, // 특정 페이즈 인덱스로 강제 점프
    EndSkill             // 스킬 즉시 종료 (취소)
}

[ModuleCategory("Utility/Phase Transition")]
[Serializable]
public class Module_PhaseTransition : SkillModule
{
    [Header("Transition Settings")]
    public PhaseTransitionType transitionType = PhaseTransitionType.NextPhase;

    [Tooltip("JumpToSpecificPhase 일 때 이동할 페이즈 번호")]
    public int targetPhaseIndex = 0;

    [Tooltip("모듈 실행 후 몇 초 뒤에 페이즈를 넘길 것인가? (0이면 즉시)")]
    public float delayTime = 0f;

    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        if (skill == null) return;

        CancellationToken token = owner.GetCancellationTokenOnDestroy();
        Character character = owner.GetComponent<Character>(); 
        ExecuteTransitionAsync(character, skill, token).Forget();
    }

    private async UniTaskVoid ExecuteTransitionAsync(Character character, ActiveSkill skill, CancellationToken token)
    {
        // 딜레이가 있다면 대기
        if (delayTime > 0f)
        {
            // 💡 지정된 시간만큼 대기하되, 취소 명령이 들어오면 에러 없이 조용히 종료합니다.
            bool isCancelled = await UniTask.Delay(TimeSpan.FromSeconds(delayTime), cancellationToken: token).SuppressCancellationThrow();

            if (isCancelled) return; // 애니메이션이 끝나거나 캔슬당했으면 발동 안 함!
        }

        if (token.IsCancellationRequested) return;

        // 💡 ActiveSkill에게 페이즈 전환을 명령합니다!
        switch (transitionType)
        {
            case PhaseTransitionType.NextPhase:
                skill.EndPhaseAndNext();
                break;
            case PhaseTransitionType.JumpToSpecificPhase:
                skill.JumpToPhase(targetPhaseIndex);
                break;
            case PhaseTransitionType.EndSkill:
                if (character != null)
                    character.End_DoAction(); 
                //skill.End_DoAction(); // 스킬 종료 함수 호출
                break;
        }
    }

    public override SkillModule Clone()
    {
        return (Module_PhaseTransition)base.Clone();
    }
}