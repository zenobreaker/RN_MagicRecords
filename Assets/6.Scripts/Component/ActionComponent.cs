
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;


public abstract class ActionComponent
    : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("체크 해제 시, 애니메이터가 있어도 가짜 타이머(UniTask)로 스킬을 진행합니다.")]
    public bool useAnimationEvents = true;

    public Action OnDoAction;
    public Action OnBeginDoAction;
    public Action OnEndDoAction;

    protected GameObject rootObject;
    protected bool bInAction;
    public bool InAction { get => bInAction; private set => bInAction = value; }
    public event Action<GameObject, DamageEvent> OnAttackHit;

    public int Priority { get; set; }

    public virtual void DoAction()
    {
        OnDoAction?.Invoke(); 
        InAction = true;
    }

    protected async UniTaskVoid ManualActionRoutine()
    {
        // 스킬 발동 시작
        BeginDoAction();

        // 0.5초 뒤에 타격 판정 발생 (애니메이션의 타격 프레임과 동일한 역할)
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));

        // 🚨 주의: AnimationEvent 매개변수는 null을 넘기거나, 구조를 살짝 바꿔줘야 합니다.
        BeginJudgeAttack(null);
        EndJudgeAttack(null);

        // 다시 0.5초 뒤에 스킬 종료 처리
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        EndDoAction();
    }


    public virtual void StartAction() { }

    public virtual void BeginDoAction() { }
        
    public virtual void EndDoAction()
    {
        InAction = false;
    }

    public virtual void BeginJudgeAttack(AnimationEvent e) { }
    public virtual void EndJudgeAttack(AnimationEvent e) { }

    public virtual void PlaySound() { }
    public virtual void PlayCameraShake() { }

    ///////////////////////////////////////////////////////////////////////////
    public void NotifyAttackHit(GameObject target, DamageEvent evt)
    {
        OnAttackHit?.Invoke(target, evt);
    }
}