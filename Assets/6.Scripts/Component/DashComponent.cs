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

    // �ִϸ��̼��� ������ �ƴ� ĳ���� ��ü�� ���� �ð� ��ŭ �����̰� �Ѵ�. 
    private IEnumerator Dash_Coroutine(DashDirection dir)
    {

        // 1. ���� ���� ��� 
        Vector3 dd = GetDirection(dir);
        Vector3 dashDirection = transform.TransformDirection(dd);

        // 2. ������ ���� �� ������ �ϴ� ���� 

        // 3. ��ǥ ��ġ 
        Vector3 targetPos = transform.position + (dashDirection.normalized * dashDistance);
        Vector3 finalDir = (targetPos - transform.position).normalized;


        float remainDistance = dashDistance;
        float startTime = Time.time;
        float resultTime = dashDistance / (float)dashSpeed;

        Begin_Dash();

        // 4. �̵� ���� 
        moving.Stop();

        while (remainDistance > 0)
        {
            // ���� ��ǥ�� �̵� -> �̵� �� ���� ��ǥ�踦 �����Ͽ� ���ϴ� ������ �뽬
            transform.Translate(finalDir.normalized * dashSpeed * Time.deltaTime, Space.World);

            // ���� �Ÿ� 
            remainDistance = Vector3.Distance(targetPos, transform.position);

            yield return new WaitForFixedUpdate();

            // �ð� �ʰ� üũ
            float remainTime = Time.time - startTime;
            if (remainTime >= resultTime)
                break;
        }


        End_Dash();

        // 4. �̵� ����
        moving.Move();
        state.SetIdleMode();
    }


    public void OnStateTypeChanged(StateType prevType, StateType newType)
    {
        if (newType == StateType.Evade)
        {
            Vector2 value = moving.InputMove;

            // �⺻������ ĳ���Ͱ� ���� ���� �̵�
            DashDirection dd = DashDirection.Forward;

            // �ƹ��͵� ������ ���� ���� 
            if (value.magnitude == 0.0f)
            {
                // ĳ���Ͱ� �齺�ܿ� ����� ������ ���δ�.
                dd = DashDirection.Backward;
                animator?.SetTrigger(EVADE);
            }
            // ����Ű�� ���� ���� 
            else
            {
                // �� �������� �뽬�Ѵ�. 
                // ���⺰�� �뽬 �ִϸ��̼��� �ٸ��ٸ� ������ ���� �����ؾ� �Ѵ�.
                //animator.SetInteger("Direction", (int)direction);
                animator?.SetTrigger(DASH);
            }

            DoAction_Dash(dd);
        }
    }

    // �뽬 ���� ��, ��Ÿ ��� ĵ�� 
    private void Begin_Dash()
    {
        OnBeginDash?.Invoke();
    }

    private void End_Dash()
    {
        OnEndDash?.Invoke();
    }

}
