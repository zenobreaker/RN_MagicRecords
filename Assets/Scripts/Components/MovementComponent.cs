using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum SpeedType
{
    Walk = 0, Run, Sprint, Max
}

/// <summary>
///  각 상태별 이동 속도 등을 세팅
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class MovementComponent : MonoBehaviour
{
    [SerializeField]
    private float[] speeds =
        new float[(int)SpeedType.Max] { 2, 4, 6 };

    public float GetWalkSpeed { get => speeds[(int)SpeedType.Walk]; }
    public float GetRunSpeed { get => speeds[(int)SpeedType.Run]; }
    public float GetSprintSpeed { get => speeds[(int)SpeedType.Sprint]; }

    private BTAIController bTAIController;

    private void Awake()
    {
        bTAIController = GetComponent<BTAIController>();
    }


    public void OnSprint()
    {
        SetSpeed(SpeedType.Sprint);
    }

    public void OnRun()
    {
        SetSpeed(SpeedType.Run);
    }

    public void OnWalk()
    {
        SetSpeed(SpeedType.Walk);
    }

    public void SetSpeed(SpeedType speedType)
    {
        if (bTAIController != null)
        {
            bTAIController.SetSpeed(speeds[(int)speedType]);
        }
    }
}
