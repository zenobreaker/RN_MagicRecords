using BT;
using BT.Nodes;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

[BlackboardType]
public enum AIStateType
{
    Wait = 0, Patrol, Approach, Equip, Action, Damaged, Dead, Max,
}

[RequireComponent(typeof(MovementComponent))]
[RequireComponent(typeof(PerceptionComponent))]
[RequireComponent(typeof(BehaviorTreeRunner))]
public abstract class BTAIController : MonoBehaviour
{

    protected AIStateType type;
    public event Action<AIStateType, AIStateType> OnAIStateTypeChanged;
    public bool WaitMode { get => type == AIStateType.Wait; }
    public bool PatrolMode { get => type == AIStateType.Patrol; }
    public bool ApproachMode { get => type == AIStateType.Approach; }
    public bool EquipMode { get => type == AIStateType.Equip; }
    public bool ActionMode { get => type == AIStateType.Action; }
    public bool DamagedMode { get => type == AIStateType.Damaged; }

    public enum WaitCondition
    {
        None = 0, Idle = 1, Strafe, Backward,
    }

    protected WaitCondition waitCondition;
    public bool NoneCondition { get => waitCondition == WaitCondition.None; }
    public bool IdleCondition { get => waitCondition == WaitCondition.Idle; }
    public bool StrafeCondition { get => waitCondition == WaitCondition.Strafe; }
    public bool BackwardCondition { get => waitCondition == WaitCondition.Backward; }
    public WaitCondition MyWaitCondition { get => waitCondition; }


    /// <summary>
    /// 공격 관련 
    /// </summary>
    [Header("About Attack")]
    [SerializeField] protected float attackRange = 1.5f;
    [SerializeField] protected float attackDelay = 2.0f;
    [SerializeField] protected float attackDelayRandom = 0.5f;

    /// <summary>
    /// 데미지 관련 
    /// </summary>
    [Header("About Damage")]
    [SerializeField] protected float damageDelay = 2.0f;
    [SerializeField] protected float damageDelayRandom = 1.0f;

    /// <summary>
    /// 대기 관련
    /// </summary>
    [Header("About Wait")]
    [SerializeField] protected float waitDelay = 1.0f;
    [SerializeField] protected float waitDelayRandom = 0.5f; // goalDelay +( - 랜덤 ~ +랜덤)


    /// <summary>
    ///  이동 관련
    /// </summary>
    [Header("About Move")]
    [SerializeField] protected float moveSpeed = 1.0f;
    public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }


    /// <summary>
    /// 순찰 관련
    /// </summary>
    [Header("About Patrol")]
    [SerializeField] protected float radius;
    [SerializeField] protected PatrolPoints patrolPoints;
    public PatrolPoints PatrolPoints { get => patrolPoints; }


    [Header("UI")]
    [SerializeField] protected bool bDrawDebug = false;
    [SerializeField] protected string uiStateName = "EnemyAIState";

    protected TextMeshProUGUI userInterface;
    protected Canvas uiStateCanvas;

    protected Animator animator;
    protected NavMeshAgent navMeshAgent;
    private float navOriginSpeed;
    private float navOriginAngularSpeed;

    public NavMeshAgent NavMeshAgent { get { return navMeshAgent; } }

    //[SerializeField] bool bBT_DebugMode = false;
    [SerializeField] protected SO_Blackboard so_blackboard;
    protected SO_Blackboard blackboard;


    [SerializeField] protected float tickInterval = 0.1f;
    [SerializeField] protected BehaviorTreeRunner btRunner;


    protected Enemy enemy;
    protected PerceptionComponent perception;
    protected StateComponent state;
    protected ActionComponent action;


    protected bool bCanMove = false; 
    protected Vector3 dest;

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        blackboard = so_blackboard.Clone();

        enemy = GetComponent<Enemy>();
        state = GetComponent<StateComponent>();
        action = GetComponent<ActionComponent>();
        perception = GetComponent<PerceptionComponent>();
        Debug.Assert(perception != null);

        btRunner = GetComponent<BehaviorTreeRunner>();
        if(btRunner == null)
        {
            btRunner = gameObject.AddComponent<BehaviorTreeRunner>();
        }
    }

    protected virtual void Start()
    {
        perception.OnPerceptionUpdated += OnPerceptionUpdated;

        CreateBlackboardKey();

        uiStateCanvas = UIHelpers.CreateBillboardCanvas(uiStateName, transform, Camera.main);

        Transform t = uiStateCanvas.transform.FindChildByName("Txt_AIState");
        userInterface = t.GetComponent<TextMeshProUGUI>();
        userInterface.text = "";
    }

    protected virtual void Update()
    {
        userInterface.gameObject.SetActive(bDrawDebug);

        userInterface.text = type.ToString();
        uiStateCanvas.transform.rotation = Camera.main.transform.rotation;
    }

    protected abstract void LateUpdate();
    

    protected virtual void FixedUpdate()
    {
      
    }

    protected abstract void CreateBlackboardKey();


    protected abstract RootNode CreateBTTree();


    protected void NavMeshUpdateRotationSet()
    {
        if (navMeshAgent.updateRotation == false)
            navMeshAgent.updateRotation = true;
    }

    public virtual void OnEnableAI()
    {
        bCanMove = true; 
    }

    private void OnPerceptionUpdated(List<GameObject> gameObjects)
    {
        if (gameObjects.Count > 0)
        {
            blackboard.SetValue<GameObject>("Target", gameObjects[0]);

            return;
        }
        //Debug.Log($"{this.gameObject.name} Target Loss!  - - 1");
        blackboard.SetValue<GameObject>("Target", null);
    }

    protected virtual bool CheckMode()
    {
        bool check = false;
        check |= DamagedMode;
        
        return check;
    }

    public virtual void SetWaitMode(bool isDamgaed = false)
    {
        if (WaitMode == true)
            return;

        ChangeType(AIStateType.Wait);
    }

    public virtual void SetPatrolMode()
    {
        if (PatrolMode == true)
            return;

        navMeshAgent.stoppingDistance = 0;
        ChangeType(AIStateType.Patrol);
    }

    public virtual void SetApproachMode()
    {
        if (ApproachMode == true)
            return;

        NavMeshUpdateRotationSet();
        navMeshAgent.stoppingDistance = attackRange;
        ChangeType(AIStateType.Approach);
    }

    public virtual void SetActionMode()
    {
        if (ActionMode == true)
            return;

        ChangeType(AIStateType.Action);
    }

    public virtual void SetDamagedMode()
    {
        if (DamagedMode == true)
            return;

        ChangeType(AIStateType.Damaged);
    }


    protected virtual void ChangeType(AIStateType type)
    {
        if (this.type == type)
            return; 

        AIStateType prevType = this.type;
        Debug.Log($"prev Type {prevType} new type : {type}");
        blackboard.SetValue("AIStateType", type);
        this.type = type;
        
        OnAIStateTypeChanged?.Invoke(prevType, type);
    }



    public void SetWaitState_NoneCondition()
    {
        if (NoneCondition)
            return;

        ChangeWaitCondition(WaitCondition.None);
    }


    public virtual void SetWaitState_IdleCondition()
    {
        if (IdleCondition)
            return;

        ChangeWaitCondition(WaitCondition.Idle);
    }

    public virtual void SetWaitState_StrafeCondition()
    {
        if (StrafeCondition)
            return;

        navMeshAgent.stoppingDistance = 0;
        ChangeWaitCondition(WaitCondition.Strafe);
    }

    public virtual void SetWaitState_BackwardCondition()
    {
        if (BackwardCondition)
            return;

        navMeshAgent.stoppingDistance = 0;
        ChangeWaitCondition(WaitCondition.Backward);
    }

    protected virtual void ChangeWaitCondition(WaitCondition condition)
    {
        WaitCondition prevCondition = this.waitCondition;
        waitCondition = condition;
    }



    public void StopMovement()
    {
        //if(condition.NoneCondition)
            navMeshAgent.isStopped = true;
    }
    public void StartMovement()
    {
        //if (condition.NoneCondition)
            navMeshAgent.isStopped = false;
    }

    public void SetSpeed(float speed)
    {
        //Debug.Log($"Set Speed {speed} == ");
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            navMeshAgent.isStopped = true;  // 경로를 일시 중지
            navMeshAgent.speed = speed;  // 속도 변경
            navMeshAgent.isStopped = false; // 다시 이동 시작
        }
    }

    public void Slow_NavMeshSpeed(float slowFactor)
    {
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            navOriginSpeed = navMeshAgent.speed;
            navOriginAngularSpeed = navMeshAgent.angularSpeed;

            navMeshAgent.speed = navMeshAgent.speed * slowFactor;
            navMeshAgent.angularSpeed = navMeshAgent.angularSpeed * slowFactor;
        }
    }

    public void Reset_NavMeshSpeed()
    {
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            navMeshAgent.speed = navOriginSpeed;
            navMeshAgent.angularSpeed = navOriginAngularSpeed;
        }
    }

    public virtual void End_Damage()
    {
        // coroutineEndDamage = StartCoroutine(Wait_End_Damage());
        //SetCoolTime(damageDelay, damageDelayRandom);
        state.SetIdleMode();
        SetWaitMode(true);
    }

    public virtual void ChangeAttackRange(float range)
    {
        attackRange = range; 
    }

    protected virtual void OnBeginDoAction()
    {

    }

    protected virtual void OnEndDoAction()
    {
        //SetWaitMode();
    }
}
