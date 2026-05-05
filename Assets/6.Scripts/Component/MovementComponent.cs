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
    public float Speed => speed;
    public float DeltaSpeed { get; private set; }
    private bool bRun = false;
    private bool bCanMove = true;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 25.0f;
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
            visual?.SetMovementAnimation(DeltaSpeed);
            return;
        }

        speed = bRun ? movement.RunSpeed : movement.WalkSpeed;
        Vector3 moveDir = Vector3.zero;

        if (targetDirection.magnitude > 0.001f)
        {
            moveDir = (Vector3.right * targetDirection.x) + (Vector3.forward * targetDirection.y);
            moveDir.y = 0;

            // 즉시 해당 방향을 바라보게 회전
            transform.localRotation = Quaternion.LookRotation(moveDir);
            moveDir = moveDir.normalized * speed;
        }

        DeltaSpeed = moveDir.magnitude / movement.WalkSpeed * movement.Ratio;
        visual?.SetMovementAnimation(DeltaSpeed);
    }

    // --------------------------------------------------------
    // 💡 2. [물리 통합] FixedUpdate: 오직 일반 걷기/뛰기만 담당
    // --------------------------------------------------------
    private void FixedUpdate()
    {
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

        state.SetEvadeMode();

        DashDirection dd = DashDirection.Forward;
        if (targetDirection.magnitude == 0.0f)
        {
            dd = DashDirection.Backward;
            visual?.PlayDashAnimation(true);
        }
        else
        {
            visual?.PlayDashAnimation(false);
        }

        CancelDashTimer();
        dashCts = new CancellationTokenSource();
        DashRoutine(dd, dashCts.Token).Forget();
    }

    private async UniTaskVoid DashRoutine(DashDirection dir, CancellationToken token)
    {
        try
        {
            OnBeginDash?.Invoke();

            // 💡 대시 시작 시 기존에 걷던 관성을 완벽히 초기화합니다.
            rigid.linearVelocity = new Vector3(0, rigid.linearVelocity.y, 0);

            Vector3 localDir = dir == DashDirection.Backward ? Vector3.back : Vector3.forward;
            Vector3 finalDir = transform.TransformDirection(localDir).normalized;

            float duration = dashDistance / dashSpeed;
            float passedTime = 0f;

            while (passedTime < duration)
            {
                // 💡 [핵심] SweepTest나 MovePosition 없이 오직 linearVelocity로만 쏩니다!
                // 두꺼운 벽이 버티고 있고 Continuous Dynamic이 켜져 있으므로 절대 뚫리지 않고 부드럽게 미끄러집니다.
                rigid.linearVelocity = new Vector3(finalDir.x * dashSpeed, rigid.linearVelocity.y, finalDir.z * dashSpeed);

                passedTime += Time.fixedDeltaTime;

                await UniTask.WaitForFixedUpdate(cancellationToken: token);
            }
        }
        catch (OperationCanceledException)
        {
            // 캔슬(피격 등) 처리
        }
        finally
        {
            OnEndDash?.Invoke();

            // 대시가 끝나면 얼음판처럼 미끄러지지 않게 즉시 속도를 0으로 잡아줍니다.
            rigid.linearVelocity = new Vector3(0, rigid.linearVelocity.y, 0);

            if (state != null && state.EvadeMode)
            {
                state.SetIdleMode();
            }
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