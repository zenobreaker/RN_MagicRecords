using UnityEngine;

public class MovementComponent : MonoBehaviour
{
    [Header("Speed Settings")]
    [SerializeField] private SO_Movement SO_Movement;
    private SO_Movement movement;

    private float speed;
    public float Speed => speed; 
    public float DeltaSpeed { get; private set; }
    private bool bRun = false;
    private bool bCanMove = true;

    private Vector2 targetDirection;
    public Vector2 TargetDirection => targetDirection;

    private Animator animator;
    private StatusEffectComponent statusEffect;
    private static readonly int SPEED = Animator.StringToHash("SpeedY");

    private void Awake()
    {
        movement = SO_Movement.GetMovement();
        animator = GetComponent<Animator>();
        statusEffect = GetComponent<StatusEffectComponent>();
    }

    private void OnEnable()
    {
        if (statusEffect != null) statusEffect.OnStatusEffectChanged += OnStatusEffectChanged;
    }

    private void OnDisable()
    {
        if (statusEffect != null) statusEffect.OnStatusEffectChanged -= OnStatusEffectChanged;
    }

    public void SetDirection(Vector2 direction, bool isRunning = false)
    {
        targetDirection = direction;
        bRun = isRunning;
    }

    public void Move() { bCanMove = true; }
    public void Stop() { bCanMove = false; }

    private void Update()
    {
        // 1. 이동 불가 상태면 즉시 속도를 0으로 만들고 종료
        if (!bCanMove)
        {
            DeltaSpeed = 0f;
            animator?.SetFloat(SPEED, DeltaSpeed);
            return;
        }

        speed = bRun ? movement.RunSpeed : movement.WalkSpeed;
        Vector3 moveDir = Vector3.zero;

        // 2. 입력이 데드존 이상일 때만 (버튼을 누르고 있을 때만) 방향과 속도를 계산!
        if (targetDirection.magnitude > 0.001f)
        {
            moveDir = (Vector3.right * targetDirection.x) + (Vector3.forward * targetDirection.y);
            moveDir.y = 0;

            // 즉시 해당 방향을 바라보게 회전
            transform.localRotation = Quaternion.LookRotation(moveDir);

            // 누른 방향으로 속도 적용
            moveDir = moveDir.normalized * speed;
        }

        // 3. 버튼을 떼면 moveDir.magnitude가 0이 되므로 DeltaSpeed도 즉시 0이 됨
        DeltaSpeed = moveDir.magnitude / movement.WalkSpeed * movement.Ratio;
        // 4. 이동 및 애니메이터 동기화
        transform.Translate(moveDir * Time.deltaTime, Space.World);
        animator?.SetFloat(SPEED, DeltaSpeed);
    }

    private void OnStatusEffectChanged(StatusEffectType prevType, StatusEffectType newType)
    {
        bool bNotMovable = (prevType & newType) != 0;
        if (bNotMovable) Stop();
        else Move();
    }
}