using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class PatrolComponent : MonoBehaviour
{
    [SerializeField] private float radius = 10.0f; // 반경 
    [SerializeField] private float goalDelay = 2.0f; // 도착 주기 
    [SerializeField] private float goalDelayRandom = 0.5f; // goalDelay +( - 최소~ + 최대)
    [SerializeField] //private PatrolPoints patrolPoints;
                     //public bool HasPatrolPoints { get => patrolPoints != null; }

    #region Circular
    private Queue<Vector3> sidePositionQueue = new Queue<Vector3>();
    [SerializeField] private float angleIncrement = 10.0f; // 각도 값
    [SerializeField] float totalAngle = 90.0f; // 최종 인지 각도 
    //bool bCircularMode = false;

    #endregion

    [SerializeField] bool bDebugMode = false;


    private bool bArrived = false;
    public bool Arrived { get => bArrived; set => bArrived = value; }

    private Vector3 initPosition; // 시작 위치
    private Vector3 goalPosition; // 도착 위치

    private Coroutine coroutienPathRoutine;
    private NavMeshAgent navMeshAgent;
    private NavMeshPath navMeshPath;
    public NavMeshPath GetPath()
    {
        return navMeshPath;
    }


    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        initPosition = goalPosition = transform.position;
    }



    public void StartMove()
    {
        if (navMeshPath != null)
        {
            SetPath();
            return;
        }

        StartCoroutine(CreateNavMeshPathRoutine());
    }

    private void Update()
    {
        if (navMeshPath == null)
            return;

        if (bArrived == true)
            return;

        if (bDebugMode)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, goalPosition);

        if (distance >= navMeshAgent.stoppingDistance)
            return;

        bArrived = true;

        //TODO: Wait Mode �� ������
        float waitTime = goalDelay + UnityEngine.Random.Range(-goalDelayRandom, goalDelayRandom);

        IEnumerator waitRoutine = WaitDelay(waitTime);

        StartCoroutine(waitRoutine);
    }

    public void SetPath()
    {
        if (bArrived)
            return;

        coroutienPathRoutine = StartCoroutine(CreateNavMeshPathRoutine());
    }

    private IEnumerator CreateNavMeshPathRoutine()
    {
        NavMeshPath path = null;

        //if (HasPatrolPoints)
        //{
        //    goalPosition = patrolPoints.GetMoveToPosition();

        //    path = new NavMeshPath();

        //    bool bCheck = navMeshAgent.CalculatePath(goalPosition, path);
        //    Debug.Assert(bCheck);

        //    patrolPoints.UpdateNextIndex();

        //    return path;
        //}


        Vector3 prevGoalPosition = goalPosition;

        // �� �� �ִ� ��ġ�� ���� ������ ������.
        while (true)
        {
            while (true)
            {
                float x = UnityEngine.Random.Range(-radius * 0.5f, radius * 0.5f);
                float z = UnityEngine.Random.Range(-radius * 0.5f, radius * 0.5f);

                goalPosition = new Vector3(x, 0, z) + initPosition;

                if (Vector3.Distance(goalPosition, prevGoalPosition) > radius * 0.25f)
                    break;

                yield return null;
            }

            path = new NavMeshPath();


            // �� �� �ִ� ��ΰ� ������ ��ƾ ���� 
            if (navMeshAgent.CalculatePath(goalPosition, path) == true)
            {
                navMeshPath = path;
                navMeshAgent.SetPath(navMeshPath);
                yield break;
            }


            yield return null;  // �������� ���� 
        }
    }


    public void GenerateCircularPath(Vector3 center)
    {
        Vector3 forward = transform.forward;
        float distance = Vector3.Distance(center, this.transform.position);
        sidePositionQueue.Clear();

        for (float angle = -totalAngle; angle <= totalAngle; angle += angleIncrement)
        {
            Vector3 direction = Quaternion.Euler(0, angle, 0) * forward;
            Vector3 point = center + direction * distance;

            sidePositionQueue.Enqueue(point);
        }
    }

    public void StartSideStep(GameObject target)
    {
        if (target == null)
            return;

        GenerateCircularPath(target.transform.position);
    }

    public void MoveTo()
    {
        if (navMeshPath == null)
            return;

        navMeshAgent.SetPath(navMeshPath);
    }

    private IEnumerator WaitDelay(float time)
    {
        yield return new WaitForSeconds(time);

        //navMeshPath = CreateNavMeshPath();
        //navMeshAgent.SetPath(navMeshPath);

        StartCoroutine(CreateNavMeshPathRoutine());

        bArrived = false;
    }


#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying == false)
            return;

        if (Selection.activeGameObject != gameObject)
            return;

        Vector3 form = transform.position + new Vector3(0, 0.1f, 0);
        Vector3 to = goalPosition + new Vector3(0, 0.1f, 0);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(form, to);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(goalPosition, 0.5f);


        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(initPosition, 0.25f);

        Gizmos.color = Color.green;
        if (sidePositionQueue == null || sidePositionQueue.Count <= 0)
            return;
        foreach (Vector3 pos in sidePositionQueue)
        {
            Gizmos.DrawWireSphere(pos, .25f);
        }

    }
#endif
}
