using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class MovementComponent : MonoBehaviour
{
    public enum DashDirection { Forward, Backward, Left, Right }

    [Header("Speed Settings")]
    [SerializeField] private SO_Movement SO_Movement;
    private SO_Movement movement;

    private float speed;
    private float originSpeed;
    public float Speed => speed;
    public float DeltaSpeed { get; private set; }
    private bool bRun = false;
    private bool bCanMove = true;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 5.0f;
    [SerializeField] private float dashDistance = 5.0f;

    private Vector2 targetDirection;
    public Vector2 TargetDirection => targetDirection;

    #region COMPONENTS
    private CharacterVisual visual;
    private StatusEffectComponent statusEffect;
    private StateComponent state;
    private Rigidbody rigid;
    #endregion

    private CancellationTokenSource dashCts;

    public event Action OnBeginDash;
    public event Action OnEndDash;

    private void Awake()
    {
        movement = SO_Movement.GetMovement();

        statusEffect = GetComponent<StatusEffectComponent>();
        state = GetComponent<StateComponent>();
        visual = GetComponentInChildren<CharacterVisual>();
        rigid = GetComponent<Rigidbody>();

        Debug.Assert(state != null && rigid != null);
    }

    private void OnEnable()
    {
        originSpeed = speed; 
        if (statusEffect != null) statusEffect.OnStatusEffectChanged += OnStatusEffectChanged;
    }

    private void OnDisable()
    {
        if (statusEffect != null) statusEffect.OnStatusEffectChanged -= OnStatusEffectChanged;
        CancelDashTimer();
    }

    public void SetDirection(Vector2 direction, bool isRunning = false)
    {
        targetDirection = direction;
        bRun = isRunning;
    }

    public void SetMoveSpeed(float speed)
    {
        originSpeed = this.speed;
        this.speed = speed;
    }
    public void RecoverSpeed() => speed = originSpeed; 

    public void Move() { bCanMove = true; }
    public void Stop() { bCanMove = false; }

    // --------------------------------------------------------
    // 💡 1. [로직 분리] Update: 회전과 애니메이션만 담당
    // --------------------------------------------------------
    private void Update()
    {
        // 이동 불가거나 대시 중(EvadeMode)일 때는 일반 애니메이션 속도를 0으로!
        if (!bCanMove || (state != null && state.EvadeMode))
        {
            DeltaSpeed = 0f;
            if (visual != null)
                visual.SetMovementAnimation(DeltaSpeed);
            return;
        }

        speed = bRun ? movement.RunSpeed : movement.WalkSpeed;
        Vector3 moveDir = Vector3.zero;

        if (targetDirection.magnitude > 1e-3f)
        {
            moveDir = (Vector3.right * targetDirection.x) + (Vector3.forward * targetDirection.y);
            moveDir.y = 0;

            // 즉시 해당 방향을 바라보게 회전
            transform.localRotation = Quaternion.LookRotation(moveDir);
            moveDir = moveDir.normalized * speed;
        }

        DeltaSpeed = moveDir.magnitude / movement.WalkSpeed * movement.Ratio;
        if (visual != null)
            visual.SetMovementAnimation(DeltaSpeed);
    }

    // --------------------------------------------------------
    // 💡 2. [물리 통합] FixedUpdate: 오직 일반 걷기/뛰기만 담당
    // --------------------------------------------------------
    private void FixedUpdate()
    {
        if (rigid.isKinematic) return;

        // [핵심 방어막] 대시 중(EvadeMode)이거나 이동 불가면 일반 걷기 물리 연산을 완벽 차단!
        if (!bCanMove || (state != null && state.EvadeMode))
        {
            // 대시 중이 아닐 때만 멈춤 처리 (대시 중에는 DashRoutine이 물리를 통제함)
            if (state == null || !state.EvadeMode)
            {
                rigid.linearVelocity = new Vector3(0, rigid.linearVelocity.y, 0);
            }
            return;
        }

        if (targetDirection.magnitude <= 0.001f)
        {
            rigid.linearVelocity = new Vector3(0, rigid.linearVelocity.y, 0);
            return;
        }

        Vector3 moveDir = (Vector3.right * targetDirection.x) + (Vector3.forward * targetDirection.y);
        moveDir.y = 0;
        moveDir = moveDir.normalized * speed;

        // 벽에 파고들지 않는 안전한 이동(Velocity)
        rigid.linearVelocity = new Vector3(moveDir.x, rigid.linearVelocity.y, moveDir.z);
    }

    // --------------------------------------------------------
    // 💡 3. 대시(Dash) 로직 통합
    // --------------------------------------------------------
    public void TryDash()
    {
        if (state == null || state.EvadeMode || !state.IdleMode) return;

        DashDirection dd = DashDirection.Forward;
        bool isDash;
        if (targetDirection.magnitude == 0.0f)
        {
            dd = DashDirection.Backward;
            isDash = true;

        }
        else
        {
            isDash = false;
        }

        Vector3 localDir =
        dd == DashDirection.Backward
            ? Vector3.back
            : Vector3.forward;

        Vector3 direction =
       transform.TransformDirection(localDir);

        if (visual != null)
            visual.PlayDashAnimation(isDash);

        Dash(direction, dashDistance, dashDistance / dashSpeed, null);
    }

    public void Dash(
    Vector3 direction,
    float distance,
    float duration,
    AnimationCurve speedCurve = null)
    {
        if (state == null || state.EvadeMode)
            return;

        if (direction.sqrMagnitude <= 1e-3f)
            return;

        direction.y = 0f;
        direction.Normalize();

        state.SetEvadeMode();

        CancelDashTimer();

        dashCts = new CancellationTokenSource();

        DashRoutine(
            direction,
            distance,
            duration,
            speedCurve,
            dashCts.Token
        ).Forget();
    }

    private async UniTaskVoid DashRoutine(
    Vector3 direction,
    float distance,
    float duration,
    AnimationCurve speedCurve,
    CancellationToken token)
    {
        try
        {
            OnBeginDash?.Invoke();

            rigid.linearVelocity =
                new Vector3(
                    0f,
                    rigid.linearVelocity.y,
                    0f);

            float elapsed = 0f;

            Vector3 start = transform.position;
            Vector3 end = start + direction * distance;

            while (elapsed < duration)
            {
                token.ThrowIfCancellationRequested();

                elapsed += Time.fixedDeltaTime;

                float t = Mathf.Clamp01(elapsed / duration);

                float curveValue =
                    speedCurve != null
                        ? speedCurve.Evaluate(t)
                        : t;

                Vector3 currentTarget =
                    Vector3.Lerp(
                        start,
                        end,
                        curveValue);

                Vector3 velocity =
                    (currentTarget - transform.position)
                    / Time.fixedDeltaTime;

                rigid.linearVelocity = new Vector3(
                    velocity.x,
                    rigid.linearVelocity.y,
                    velocity.z);

                await UniTask.WaitForFixedUpdate(
                    cancellationToken: token);
            }

            rigid.linearVelocity =
                new Vector3(
                    0f,
                    rigid.linearVelocity.y,
                    0f);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            OnEndDash?.Invoke();

            rigid.linearVelocity =
                new Vector3(
                    0f,
                    rigid.linearVelocity.y,
                    0f);

            if (state != null && state.EvadeMode)
                state.SetIdleMode();
        }
    }

    private void CancelDashTimer()
    {
        if (dashCts != null)
        {
            dashCts.Cancel();
            dashCts.Dispose();
            dashCts = null;
        }
    }

    private void OnStatusEffectChanged(StatusEffectType prevType, StatusEffectType newType)
    {
        bool bNotMovable = (prevType & newType) != 0;
        if (bNotMovable) Stop();
        else Move();
    }
}