using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public enum SkillEventType
{
    BeginJudgeAttack,
    EndJudgeAttack,
}

[ModuleCategory("Etc/Set DelayEvent")]
[Serializable]
public class Module_DelayEvent : SkillModule
{
    [Header("발동할 이벤트")]
    [Tooltip("모듈이 동작할 이벤트 형태")] 
    public SkillEventType eventType;

    [Tooltip("모듈이 동작할 지연 시간 ")] 
    public float delay = 0f;

    [Header("애니메이션 이벤트 객체 데이터 ")]
    public float floatValue = 0f;
    public int intValue = 0;
    public string stringValue = string.Empty;
    public UnityEngine.Object objectValue = null;

    private CancellationTokenSource delayCts;

    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        CancelDelay();

        delayCts = CancellationTokenSource.CreateLinkedTokenSource(skill.PhaseToken);

        ExecuteAsync(owner, skill).Forget();
    }

   private async UniTaskVoid ExecuteAsync(Character owner, 
       ActiveSkill skill)
    {
        bool canceled = await UniTask.Delay(
            TimeSpan.FromSeconds(delay), 
            cancellationToken : delayCts.Token).SuppressCancellationThrow();

        if (canceled)
            return;

        AnimationEvent eventData = new();

        eventData.objectReferenceParameter = objectValue;
        eventData.floatParameter = floatValue;
        eventData.intParameter = intValue;
        eventData.stringParameter = stringValue; 

        switch(eventType)
        {

            case SkillEventType.BeginJudgeAttack:
                skill.Begin_JudgeAttack(eventData);
                break;

            case SkillEventType.EndJudgeAttack:
                skill.End_JudgeAttack(eventData);
                break;
        }
    }
    private void CancelDelay()
    {
        if (delayCts == null)
            return;

        delayCts.Cancel();
        delayCts.Dispose();
        delayCts = null;
    }
    public override SkillModule Clone()
    {
        return (Module_DelayEvent)base.Clone();
    }
}
