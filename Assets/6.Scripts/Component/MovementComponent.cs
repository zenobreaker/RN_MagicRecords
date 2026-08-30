using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.InputSystem;

public class MovementComponent : MonoBehaviour
{
    public enum DashDirection { Forward, Backward, Left, Right }

    [Header("Speed Settings")]
    [SerializeField] private SO_Movement SO_Movement;
    private SO_Movement movement;

    [Header("Layer Settings")]
    [SerializeField] private float dashCollisionSearchPadding = 0.5f;
    [SerializeField] private LayerMask characterLayer;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 5.0f;
    [SerializeField] private float dashDistance = 5.0f;

    private float speed;
    private float originSpeed;
    public float Speed => speed;
    public float DeltaSpeed { get; private set; }
    private bool bRun = false;
    private bool bCanMove = true;

    private Vector2 targetDirection;
    public Vector2 TargetDirection => targetDirection;

    #region COMPONENTS
    private CharacterVisual visual;
    private StatusEffectComponent statusEffect;
    private StateComponent state;
    private Rigidbody rigid;
    #endregion


    private bool bIsExternalMoving = false;
    private Collider[] ownerColliders;

    /// <summary>
    /// 대시 중 충돌을 무시한 Collider 쌍.
    /// 대시 종료 시 반드시 원복한다.
    /// </summary>
    private readonly List<(Collider owner, Collider target)> ignoredCharacterCollisions = new();

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

        ownerColliders = GetComponentsInChildren<Collider>(); 

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
        if (!bCanMove || 
            bIsExternalMoving ||
            (state != null && state.EvadeMode))
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
        // 외부 이동 중이면 일반 이동 로직을 실행하지 않는다.
        if (bIsExternalMoving)
            return;

        if (rigid.isKinematic) 
            return;

        // 대시 중(EvadeMode)이거나 이동 불가면 일반 걷기 물리 연산을 완벽 차단!
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
            
            // 캐릭터간 무시 시작 
            BeginDashCollisionIgnore(start, end); 

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
            RestoreDashCollision(); 

            OnEndDash?.Invoke();

            rigid.linearVelocity =
                new Vector3(
                    0f,
                    rigid.linearVelocity.y,
                    0f);

            if (state != null && state.EvadeMode)
                state.SetIdleMode();

            bIsExternalMoving = false; 
        }
    }

    public void MoveOverTime(
    Vector3 direction,
    float distance,
    float duration,
    bool ghostMode = false)
    {
        if (state == null || state.EvadeMode)
            return;

        if (direction.sqrMagnitude <= 1e-3f)
            return;

        if (distance <= 0f || duration <= 0f)
            return;

        direction.y = 0f;
        direction.Normalize();

        CancelDashTimer();
        bIsExternalMoving = true;
        dashCts = new CancellationTokenSource();

        MoveOverTimeRoutine(
            direction,
            distance,
            duration,
            dashCts.Token,
            ghostMode
        ).Forget();
    }

    private async UniTaskVoid MoveOverTimeRoutine(
    Vector3 direction,
    float distance,
    float duration,
    CancellationToken token,
    bool ghostMode = false) 
    {
        try
        {
            float elapsed = 0f;

            Vector3 start = transform.position;
            Vector3 end = start + direction * distance;
            if (ghostMode)
            {
                BeginDashCollisionIgnore(start, end);
            }

            while (elapsed < duration)
            {
                token.ThrowIfCancellationRequested();

                elapsed += Time.fixedDeltaTime;

                float t = Mathf.Clamp01(elapsed / duration);

                Vector3 targetPosition =
                    Vector3.Lerp(start, end, t);

                Vector3 velocity =
                    (targetPosition - transform.position)
                    / Time.fixedDeltaTime;

                rigid.linearVelocity = new Vector3(
                    velocity.x,
                    rigid.linearVelocity.y,
                    velocity.z
                );

                await UniTask.WaitForFixedUpdate(
                    cancellationToken: token);
            }

            // 이동 종료
            rigid.linearVelocity = new Vector3(
                0f,
                rigid.linearVelocity.y,
                0f);
        }
        catch (OperationCanceledException)
        {
            RestoreDashCollision();

            bIsExternalMoving = false;
            targetDirection = Vector2.zero;
        }
        finally
        {
            RestoreDashCollision();

            rigid.linearVelocity = new Vector3(
                0f,
                rigid.linearVelocity.y,
                0f
            );
            bIsExternalMoving = false;
            targetDirection = Vector2.zero;
        }
    }
    private void BeginDashCollisionIgnore(Vector3 start, Vector3 end)
    {
        RestoreDashCollision();

        if (ownerColliders == null || ownerColliders.Length == 0)
            return;

        // 현재 주변의 Collider를 찾는다.
        Bounds bounds = ownerColliders[0].bounds;

        for (int i = 1; i < ownerColliders.Length; i++)
            bounds.Encapsulate(ownerColliders[i].bounds);

        // 캐릭터의 크기를 기준으로 탐색 반경 결정
        float radius =
            Mathf.Max(bounds.extents.x, bounds.extents.z)
            + dashCollisionSearchPadding;

        Vector3 castStart = start;
        Vector3 castEnd = end; 

        // 대시 시작 시 주변 캐릭터 탐색
        Collider[] nearbyColliders =
            Physics.OverlapCapsule(
                castStart,
                castEnd, 
                radius, characterLayer, 
                QueryTriggerInteraction.Ignore);

        foreach (Collider targetCollider in nearbyColliders)
        {
            if (targetCollider == null)
                continue;

            // 자기 자신의 Collider
            if (targetCollider.transform.IsChildOf(transform))
                continue;

            Character targetCharacter =
                targetCollider.GetComponentInParent<Character>();

            // Character가 아니면 무시하지 않는다. 
            if (targetCharacter == null)
                continue;

            // 자기 자신이면 무시
            if (targetCharacter == null)
                continue;

            foreach(Collider ownerColiider in ownerColliders)
            {
                if (ownerColiider == null)
                    continue;

                if (IsAlreadyIgnored(
                    ownerColiider,
                    targetCollider))
                    continue;

                Physics.IgnoreCollision(
                    ownerColiider,
                    targetCollider,
                    true);

                ignoredCharacterCollisions.Add((ownerColiider, targetCollider));
            }
        }
        
    }

    private bool IsAlreadyIgnored(
        Collider ownerCollider, 
        Collider targetCollider)
    {
        for(int i = 0; i< ignoredCharacterCollisions.Count; i++)
        {
            var pair = ignoredCharacterCollisions[i];

            if (pair.owner == ownerCollider &&
                pair.target == targetCollider)
                return true; 
        }

        return false; 
    }

    private void RestoreDashCollision()
    {
        for(int i = 0; i< ignoredCharacterCollisions.Count; i++)
        {
            var pair = ignoredCharacterCollisions[i];

            if (pair.owner == null ||
                pair.target == null)
                continue;

            Physics.IgnoreCollision(
                pair.owner, pair.target, false); 
        }

        ignoredCharacterCollisions.Clear(); 
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