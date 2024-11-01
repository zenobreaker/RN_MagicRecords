using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovingComponent : MonoBehaviour
{
    /// <summary>
    /// Input Keyboard
    /// </summary>
    [Header("Input Settings")]
    private Vector2 inputMove;
    public Vector2 InputMove { get => inputMove; }

    [SerializeField] private float sensitivity = 10.0f;     // 입력 감도
    [SerializeField] private float deadZone = 0.001f;       // 대시 등의 감소시간

    private Vector2 currentInputMove; // 현재 입력한 이동값
    private Vector2 velocity;
    private Quaternion rotation;


    [Header("Speed Settings")]
    [SerializeField] private SO_Movement SO_Movement;
    private SO_Movement movement;
    private float speed;
    private float deltaSpeed;
    public float Speed { get => speed; }
    public float DeltaSpeed { get => deltaSpeed; }
    /// <summary>
    /// Run & Sprint 
    /// </summary>
    private bool bRun = false;

    private static readonly int SPEED = Animator.StringToHash("SpeedY");

    #region COMPONENT PROPERTY

    private Animator animator;

    #endregion

    private bool bCanMove = true;

    #region PUBLIC MOVE & STOP 
    public void Move()
    {
        bCanMove = true;
    }

    public void Stop()
    {
        bCanMove = false;
    }
    #endregion

    private void Awake()
    {
        movement = SO_Movement.GetMovement();
        animator = GetComponent<Animator>();
        Awake_PlayerBindInput();
    }

    private void Awake_PlayerBindInput()
    {
        PlayerInput input = GetComponent<PlayerInput>();
        Debug.Assert(input != null);
        InputActionMap actionMap = input.actions.FindActionMap("Player");
        Debug.Assert(actionMap != null);

        InputAction moveAction = actionMap.FindAction("Move");
        Debug.Assert(moveAction != null);

        moveAction.performed += Input_Move_Performed;
        moveAction.canceled += Input_Move_Canceled;
    }


    private void Input_Move_Performed(InputAction.CallbackContext context)
    {
        inputMove = context.ReadValue<Vector2>();
    }
    private void Input_Move_Canceled(InputAction.CallbackContext context)
    {
        inputMove = Vector2.zero;
    }

    private void Update()
    {
        //Debug.Log(inputMove);
        currentInputMove = Vector2.SmoothDamp(currentInputMove, inputMove, ref velocity, 1.0f / sensitivity);

        //TODO : Condition에 따라서 이동 불가 

        if (bCanMove == false)
            return;

        speed = bRun ? movement.RunSpeed : movement.WalkSpeed;

        Vector3 direction = Vector3.zero;
        if (currentInputMove.magnitude > deadZone)
        {
            direction = (Vector3.right * currentInputMove.x) + (Vector3.forward * currentInputMove.y);
            direction.y = 0;

            transform.localRotation = Quaternion.LookRotation(direction);
        }

        direction = direction.normalized * speed;
        deltaSpeed = direction.magnitude / movement.WalkSpeed * movement.Ratio;
        transform.Translate(direction * Time.deltaTime, Space.World);
        
        animator?.SetFloat(SPEED, deltaSpeed);
    }
}
