using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public abstract class ActionComponent : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("체크 해제 시, 애니메이터가 있어도 가짜 타이머(UniTask)로 스킬을 진행합니다.")]
    public bool useAnimationEvents = true;

    public Action OnDoAction;
    public Action OnBeginDoAction;
    public Action OnEndDoAction;

    protected GameObject rootObject;
    protected bool bInAction;
    public bool InAction { get => bInAction; protected set => bInAction = value; }

    public event Action<GameObject, DamageEvent> OnAttackHit;
    public int Priority { get; set; }

    // 가짜 타이머(애니메이션 대체) 관리용 토큰
    protected CancellationTokenSource actionCts;

    // 💡 공통 실행부: 행동 시작 및 가짜 타이머 자동화
    public virtual void DoAction()
    {
        if (bInAction) return;

        bInAction = true;
        OnDoAction?.Invoke();

        if (useAnimationEvents == false)
        {
            CancelManualAction();
            actionCts = new CancellationTokenSource();
            ManualActionRoutine(actionCts.Token).Forget();
        }
    }

    protected virtual async UniTaskVoid ManualActionRoutine(CancellationToken token)
    {
        try
        {
            BeginDoAction();
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: token);

            BeginJudgeAttack(null);
            EndJudgeAttack(null);

            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: token);
            EndDoAction(); // 여기서 자동으로 InAction = false 가 됩니다.
        }
        catch (OperationCanceledException) { /* 캔슬 시 무시 */ }
    }

    public void CancelManualAction()
    {
        if (actionCts != null)
        {
            actionCts.Cancel();
            actionCts.Dispose();
            actionCts = null;
        }
    }

    public virtual void StartAction() { }
    public virtual void BeginDoAction() { }

    public virtual void EndDoAction()
    {
        bInAction = false;
        CancelManualAction(); // 종료 시 타이머도 확실히 끔
    }

    public virtual void BeginJudgeAttack(AnimationEvent e) { }
    public virtual void EndJudgeAttack(AnimationEvent e) { }
    public virtual void PlaySound() { }
    public virtual void PlayCameraShake() { }

    public void NotifyAttackHit(GameObject target, DamageEvent evt)
    {
        OnAttackHit?.Invoke(target, evt);
    }

    protected virtual void OnDisable()
    {
        CancelManualAction();
    }
}