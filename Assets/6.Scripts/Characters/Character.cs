using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 💡 Animator 요구 속성 제거! 오직 물리(Rigidbody)만 가집니다.
[RequireComponent(typeof(Rigidbody))]
public class Character
    : MonoBehaviour
    , IStoppable
    , ISlowable
    , ITeamAgent
{
    protected GenenricTeamId genericTeamId;

    protected new Rigidbody rigidbody;

    // 💡 애니메이터 대신 Visual 매니저만 가집니다.
    protected CharacterVisual visual;
    public CharacterVisual Visual { get => visual; }

    protected StateComponent state;
    protected HealthPointComponent healthPoint;
    protected StatusComponent status;

    protected bool bInAction = false;
    public virtual bool InAction { get { return bInAction; } protected set { bInAction = value; } }

    // 슬로우 관리를 위한 토큰 (코루틴 대체)
    private CancellationTokenSource slowCts;

    #region ACTION
    public Action OnBeginDoAction;
    public Action OnEndDoAction;
    public Action<Character> OnDead;
    #endregion

    private int charID;
    public int CharID
    {
        get { return charID; }
        set { charID = value; }
    }

    protected virtual void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        Debug.Assert(rigidbody != null);

        state = GetComponent<StateComponent>();
        healthPoint = GetComponent<HealthPointComponent>();
        status = GetComponent<StatusComponent>();
        if (status != null && healthPoint != null)
            status.OnSetHealth += healthPoint.SetHealthPoint;

        // 💡 자식(Model)에 있는 Visual 컴포넌트를 가져옵니다.
        visual = GetComponentInChildren<CharacterVisual>();
        Debug.Assert(visual != null, "자식 오브젝트에 CharacterVisual이 없습니다!");
    }

    protected virtual void Start()
    {
        Regist_MovableStopper();
        Regist_MovableSlower();
    }

    protected virtual void OnDisable()
    {
        OnDead = null;

        // 메모리 누수 방지
        if (slowCts != null)
        {
            slowCts.Cancel();
            slowCts.Dispose();
        }
    }

    public virtual void End_Damaged() { bInAction = false; }

    public void SetGenericTeamId(GenenricTeamId id) { genericTeamId = id; }
    public GenenricTeamId GetGeneriTeamId() { return genericTeamId; }

    #region AnimationEvent
    // 🚨 주의: 애니메이터가 자식(Model)으로 이동했으므로, Unity Animation Event는 Visual 스크립트를 때리게 됩니다.
    // Visual 스크립트에서 이 함수들을 호출해주도록 브릿지(Bridge) 연결이 필요합니다!
    public virtual void Start_DoAction() { }
    public virtual void Begin_DoAction() { OnBeginDoAction?.Invoke(); }
    public virtual void End_DoAction() { OnEndDoAction?.Invoke(); }
    public virtual void Begin_JudgeAttack(AnimationEvent e = null) { }
    public virtual void End_JudgeAttack(AnimationEvent e = null) { }
    public virtual void Play_Sound() { }
    public virtual void Play_CameraShake() { }
    #endregion

    #region Slow (UniTask로 업그레이드)
    private void Regist_MovableSlower()
    {
        MovableSlower.Instance.Regist(this);
    }

    public void ApplySlow(float duration, float slowFactor)
    {
        // 💡 시각적인 애니메이션 배속은 Visual에게 위임
        visual?.SetAnimationSpeedMultiplier(slowFactor);

        // 기존 실행되던 슬로우가 있으면 취소하고 새로 시작
        if (slowCts != null)
        {
            slowCts.Cancel();
            slowCts.Dispose();
        }
        slowCts = new CancellationTokenSource();
        ResetSpeedAfterDelay(duration, slowCts.Token).Forget();
    }

    public void ResetSpeed()
    {
        visual?.SetAnimationSpeedMultiplier(1.0f);
    }

    public async UniTask ResetSpeedAfterDelay(float duration, CancellationToken token)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: token);
            ResetSpeed();
        }
        catch (OperationCanceledException) { /* 무시 */ }
    }
    #endregion

    #region Movable (Hit Stop)
    public void Regist_MovableStopper()
    {
        MovableStopper.Instance.Regist(this);
    }

    // 역경직(HitStop) 로직도 UniTask로 깔끔하게 변경
    public async UniTask Start_FrameDelay(int frame, CancellationToken token)
    {
        // 정지
        visual?.SetAnimationSpeedMultiplier(0.0f);

        // 지정된 물리 프레임만큼 대기
        for (int i = 0; i < frame; i++)
        {
            await UniTask.WaitForFixedUpdate();
        }

        // 복구
        ResetSpeed();
    }
    #endregion

    public virtual void SetStatus() { }

    public virtual void PlayerAction(ActionData actionData)
    {
        // 💡 주의: AnimatorLayerCache 로직은 Visual 내부로 이동하거나, Visual을 통해 처리해야 합니다.
        // 현재는 임시로 layer를 0으로 고정하거나 로직을 옮겨주세요.
        PlayAction(actionData, 0);
    }

    public virtual void PlayAction(ActionData actionData, int layer = 0)
    {
        float statSpeed = status != null ? status.GetStatusValue(StatusType.ATTACKSPEED) : 1.0f;

        // 💡 액션 재생을 온전히 Visual에게 위임합니다!
        visual?.PlayActionAnimation(actionData, layer, statSpeed);
    }

    protected virtual void Dead() { }


    public static implicit operator GameObject(Character c)
    {
        return c != null ? c.gameObject : null;
    }
}