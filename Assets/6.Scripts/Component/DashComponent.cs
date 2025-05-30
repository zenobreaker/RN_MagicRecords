using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using static StateComponent;

public class DashComponent : MonoBehaviour
{
    public enum DashDirection
    {
        Forward, Backward, Left, Right
    }

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 25.0f;
    [SerializeField] private float dashDistance = 5.0f;

    private readonly int EVADE = Animator.StringToHash("Evade");
    private readonly int DASH = Animator.StringToHash("Dash");
    private Animator animator;

    #region COMPONENTS
    private PlayerMovingComponent moving;
    private StateComponent state;
    #endregion

    public event Action OnBeginDash;
    public event Action OnEndDash;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        PlayerInput input = GetComponent<PlayerInput>();
        Debug.Assert(input != null);

        moving = GetComponent<PlayerMovingComponent>();
        state = GetComponent<StateComponent>();
        Debug.Assert(state != null);
        state.OnStateTypeChanged += OnStateTypeChanged;
    }


    private void DoAction_Dash(DashDirection dir)
    {
        StopAllCoroutines();
        StartCoroutine(Dash_Coroutine(dir));
    }

    private Vector3 GetDirection(DashDirection dir)
    {
        if (dir == DashDirection.Forward)
        {
            return Vector3.forward;
        }
        else if (dir == DashDirection.Backward)
        {
            return Vector3.back;
        }

        return Vector3.forward;
    }

    // 애니메이션의 의함이 아닌 캐릭터 자체를 일정 시간 만큼 움직이게 한다. 
    private IEnumerator Dash_Coroutine(DashDirection dir)
    {

        // 1. 로컬 방향 계산 
        Vector3 dd = GetDirection(dir);
        Vector3 dashDirection = transform.TransformDirection(dd);

        // 2. 경사면은 없는 것 같으니 일단 배제 

        // 3. 목표 위치 
        Vector3 targetPos = transform.position + (dashDirection.normalized * dashDistance);
        Vector3 finalDir = (targetPos - transform.position).normalized;


        float remainDistance = dashDistance;
        float startTime = Time.time;
        float resultTime = dashDistance / (float)dashSpeed;

        Begin_Dash();

        // 4. 이동 제약 
        moving.Stop();

        while (remainDistance > 0)
        {
            // 로컬 좌표계 이동 -> 이동 시 월드 좌표계를 유지하여 원하는 방향을 대쉬
            transform.Translate(finalDir.normalized * dashSpeed * Time.deltaTime, Space.World);

            // 남은 거리 
            remainDistance = Vector3.Distance(targetPos, transform.position);

            yield return new WaitForFixedUpdate();

            // 시간 초과 체크
            float remainTime = Time.time - startTime;
            if (remainTime >= resultTime)
                break;
        }


        End_Dash();

        // 4. 이동 제약
        moving.Move();
        state.SetIdleMode();
    }


    public void OnStateTypeChanged(StateType prevType, StateType newType)
    {
        if (newType == StateType.Evade)
        {
            Vector2 value = moving.InputMove;

            // 기본적으론 캐릭터가 보는 방향 이동
            DashDirection dd = DashDirection.Forward;

            // 아무것도 누르지 않은 상태 
            if (value.magnitude == 0.0f)
            {
                // 캐릭터가 백스텝에 가까운 무빙을 보인다.
                dd = DashDirection.Backward;
                animator?.SetTrigger(EVADE);
            }
            // 방향키를 누른 상태 
            else
            {
                // 그 방향으로 대쉬한다. 
                // 방향별로 대쉬 애니메이션이 다르다면 새로이 값을 전달해야 한다.
                //animator.SetInteger("Direction", (int)direction);
                animator?.SetTrigger(DASH);
            }

            DoAction_Dash(dd);
        }
    }

    // 대쉬 시전 시, 기타 기능 캔슬 
    private void Begin_Dash()
    {
        OnBeginDash?.Invoke();
    }

    private void End_Dash()
    {
        OnEndDash?.Invoke();
    }

}
