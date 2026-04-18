using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class DashComponent : MonoBehaviour
{
    public enum DashDirection
    {
        Forward, Backward, Left, Right
    }

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 25.0f;
    [SerializeField] private float dashDistance = 5.0f;

    #region COMPONENTS
    private MovementComponent moving;
    private StateComponent state;
    private CharacterVisual visual; // 💡 애니메이터 대신 Visual 캐싱
    #endregion

    // 💡 코루틴 대신 사용할 취소 토큰
    private CancellationTokenSource dashCts;

    public event Action OnBeginDash;
    public event Action OnEndDash;

    private void Awake()
    {
        PlayerInput input = GetComponent<PlayerInput>();
        Debug.Assert(input != null);

        moving = GetComponent<MovementComponent>();
        state = GetComponent<StateComponent>();
        Debug.Assert(state != null);

        // 💡 자식 오브젝트에서 Visual 가져오기
        visual = GetComponentInChildren<CharacterVisual>();
    }

    private void OnDisable()
    {
        CancelDashTimer();
    }

    public void TryDash()
    {
        // 이미 회피 상태거나 행동 불능 상태면 시도하지 않음
        if (state == null || state.EvadeMode == true || !state.IdleMode) return;

        // 상태를 회피(Evade) 모드로 변경
        state.SetEvadeMode();

        Vector2 value = moving.TargetDirection;
        DashDirection dd = DashDirection.Forward;

        // 아무것도 누르지 않은 상태 (백스텝)
        if (value.magnitude == 0.0f)
        {
            dd = DashDirection.Backward;
            visual?.PlayDashAnimation(true); // 💡 Visual에게 백스텝 애니메이션 재생 요청
        }
        // 방향키를 누른 상태 (전방 대시)
        else
        {
            // 💡 추후 8방향 대시가 필요하다면 여기서 dd 값을 계산해서 넘겨주면 됩니다.
            visual?.PlayDashAnimation(false); // 💡 Visual에게 대시 애니메이션 재생 요청
        }

        DoAction_Dash(dd);
    }

    private void DoAction_Dash(DashDirection dir)
    {
        CancelDashTimer();
        dashCts = new CancellationTokenSource();
        DashRoutine(dir, dashCts.Token).Forget();
    }

    private Vector3 GetDirection(DashDirection dir)
    {
        if (dir == DashDirection.Forward) return Vector3.forward;
        if (dir == DashDirection.Backward) return Vector3.back;
        return Vector3.forward;
    }

    // 💡 IEnumerator -> UniTask 변경 및 물리 기반 이동 안정화
    private async UniTaskVoid DashRoutine(DashDirection dir, CancellationToken token)
    {
        try
        {
            Begin_Dash();
            moving.Stop(); // 이동 제약

            Vector3 localDir = GetDirection(dir);
            Vector3 dashDirection = transform.TransformDirection(localDir);
            Vector3 finalDir = dashDirection.normalized;

            // 💡 거리 계산 방식 변경: 벽에 막혀도 지정된 시간이 지나면 깔끔하게 종료되도록 수정
            float duration = dashDistance / dashSpeed;
            float passedTime = 0f;

            while (passedTime < duration)
            {
                // 월드 좌표계를 유지하며 원하는 방향으로 대시
                transform.Translate(finalDir * dashSpeed * Time.fixedDeltaTime, Space.World);
                passedTime += Time.fixedDeltaTime;

                // 물리 프레임 대기
                await UniTask.WaitForFixedUpdate(cancellationToken: token);
            }
        }
        catch (OperationCanceledException)
        {
            // 대시 중 피격당하거나 죽어서 캔슬된 경우 조용히 빠져나감
        }
        finally
        {
            // 💡 정상 종료든 캔슬이든 무조건 원래 상태로 복구되도록 finally 사용!
            End_Dash();

            if (moving != null) moving.Move();

            // 현재 상태가 아직 회피 모드라면 Idle로 되돌림 (피격당해서 Damaged로 바뀐 상태라면 건드리지 않음)
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

    private void Begin_Dash()
    {
        OnBeginDash?.Invoke();
    }

    private void End_Dash()
    {
        OnEndDash?.Invoke();
    }
}