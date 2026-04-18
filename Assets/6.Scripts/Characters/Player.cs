using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player
    : Character
    , IDamagable
    , ILaunchable
    , IWeaponUser
{

    private ComboComponent comboComponent;
    private WeaponComponent weapon;
    private SkillComponent skill;
    private DamageHandleComponent damageHandle;
    private LaunchComponent launch;
    private EquipmentComponent equipment;

    private WeaponController weaponController;
    private List<ActionComponent> actionComponents = new();

    private Action<InputAction.CallbackContext> onAction;
    private Action<InputAction.CallbackContext> onMove;
    private Action<InputAction.CallbackContext> onDash;
    private Action<InputAction.CallbackContext>[] onSkillActions;


    private int jobID; 
    public int JobID
    {
        get { return jobID; }
        set { jobID = value; }
    }

    protected override void Awake()
    {
        base.Awake();

        weaponController = GetComponentInChildren<WeaponController>();
        comboComponent = GetComponent<ComboComponent>();
        weapon = GetComponent<WeaponComponent>();
        Debug.Assert(weapon != null);
        actionComponents.Add(weapon);

        skill = GetComponent<SkillComponent>();
        Debug.Assert(skill != null);
        actionComponents.Add(skill);

        damageHandle = GetComponent<DamageHandleComponent>();
        launch = GetComponent<LaunchComponent>();

        equipment = GetComponent<EquipmentComponent>();

        PlayerInput input = GetComponent<PlayerInput>();
        Debug.Assert(input != null);

        InputActionMap actionMap = input.actions.FindActionMap("Player");
        Debug.Assert(actionMap != null);

        onAction = (context) =>
        {
            comboComponent?.InputQueue(InputCommandType.ACTION);
        };

        onDash = (context) =>
        {
            comboComponent?.InputQueue(InputCommandType.DASH);
        };


        onMove = (context) =>
        {
            comboComponent?.BreakCombo();
        };

        Awake_SkillAcitonInput(actionMap);

        actionMap.FindAction("Action").started += onAction;
        actionMap.FindAction("Dash").started += onDash;
        actionMap.FindAction("Move").started += onMove; 
    }


    private void Awake_SkillAcitonInput(InputActionMap actionMap)
    {
        if (actionMap == null || skill == null)
            return;
        onSkillActions = new Action<InputAction.CallbackContext>[4];

        for (int i = 0; i < 4; i++)
        {
            int slot = i;
            onSkillActions[i] = (context) =>
            {
                comboComponent.InputQueue(InputCommandType.SKILL, slot);
            };

            string actionName = $"SkillAction{slot + 1}";
            actionMap.FindAction(actionName).started += onSkillActions[i];
        }
    }

    protected override void Start()
    {
        base.Start();
        
        SetGenericTeamId(1); 
    }
    protected void OnEnable()
    {
        if (state != null)
            state.OnStateTypeChanged += ChangeType;

        BattleManager.Instance?.RegistPlayer(this);
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (state != null)
            state.OnStateTypeChanged -= ChangeType;

        var input = GetComponent<PlayerInput>();
        if(input != null)
        {
            var actionMap = input.actions.FindActionMap("Player");

            actionMap.FindAction("Action").started -= onAction;
            actionMap.FindAction("Dash").started -= onDash;

            for (int i = 0; i < 4; i++)
            {
                string actionName = $"SkillAction{i + 1}";
                actionMap.FindAction(actionName).started -= onSkillActions[i];
            }
        }

        BattleManager.Instance?.UnreistPlayer(this);
    }

    public override void Begin_DoAction()
    {
        base.Begin_DoAction();

        OnBeginDoAction?.Invoke();
        foreach (var ac in actionComponents)
            if (ac.InAction) ac.BeginDoAction(); 
    }

    public override void End_DoAction()
    {
        bInAction = false;
        Debug.Log("Player End DoAction");
        foreach (var ac in actionComponents)
            if (ac.InAction) ac.EndDoAction();
        
        OnEndDoAction?.Invoke();
    }

    public override void Begin_JudgeAttack(AnimationEvent e)
    {
        base.Begin_JudgeAttack(e);
        foreach (var ac in actionComponents)
            if (ac.InAction) ac.BeginJudgeAttack(e);
    }

    public override void End_JudgeAttack(AnimationEvent e)
    {
        base.End_JudgeAttack(e);
        foreach (var ac in actionComponents)
            if (ac.InAction) ac.EndJudgeAttack(e);
    }

    public override void Play_Sound()
    {
        base.Play_Sound();
        foreach (var ac in actionComponents)
            if (ac.InAction) ac.PlaySound();
    }

    public override void Play_CameraShake()
    {
        base.Play_CameraShake();
        foreach(var ac in actionComponents)
            if (ac.InAction) ac.PlayCameraShake();
    }

    public WeaponController GetWeaponController() => weaponController;

    public void OnDamage(GameObject attacker, Weapon causer, Vector3 hitPoint, DamageEvent damageEvent)
    {
        // 회피 상태일 때의 처리
        if (state.Type == StateType.Evade)
        {
            MovableSlower.Instance.Start_Slow(this);
            return;
        }

        // 1. 에어본/넉백 적용
        ApplyLaunch(attacker, causer, damageEvent);

        // 2. 데미지 계산 및 적용 
        // 💡 주의: 이 함수 내부에서 이미 HP를 깎고 state.SetDamagedMode()를 호출합니다!
        damageHandle?.OnDamage(attacker, damageEvent);

        // 3. 살았는지 죽었는지 판단
        if (healthPoint.Dead == false)
        {
            return; // 💡 이미 DamageHandle에서 상태를 Damaged로 바꿨으므로 여기서 또 할 필요 없음!
        }

        // --- 여기서부터는 죽었을 때의 처리 ---
        state.SetDeadMode();

        Collider collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;

        // 💡 코루틴 대신 UniTask 호출
        HandleDeath().Forget();
        visual?.PlayDeadAnimation();
    }

    // 💡 IEnumerator -> async UniTaskVoid 로 변경
    private async UniTaskVoid HandleDeath()
    {
        // 1초 대기 (토큰이 없으므로 씬 전환 시 에러 안 나게 주의)
        await UniTask.Delay(TimeSpan.FromSeconds(1.0f));
        Dead();
    }

    protected override void Dead()
    {
        base.Dead();
        Destroy(gameObject);
    }

    private void ChangeType(StateType prevType, StateType newType)
    {
        if (newType == StateType.Dead)
        {
            OnDead?.Invoke(this);
        }

        if (newType == StateType.Damaged || newType == StateType.Stop || newType == StateType.Dead)
        {
            // 현재 행동 중(InAction)인 모든 컴포넌트들을 강제로 캔슬시킵니다!
            foreach (var ac in actionComponents)
            {
                if (ac.InAction)
                {
                    ac.EndDoAction(); // (가짜 타이머도 여기서 알아서 다 꺼집니다)
                }
            }
        }
    }

    public override void End_Damaged()
    {
        base.End_Damaged();
        
        state?.SetIdleMode();
        foreach(var action in actionComponents)
            action.EndDoAction();
    }

    public void ApplyLaunch(GameObject attacker, Weapon causer, DamageEvent devt)
    {
        ApplyLaunch(attacker, causer, devt?.hitData);
    }

    public void ApplyLaunch(GameObject attacker, Weapon causer, HitData hitData)
    {
        launch?.ApplyLaunch(attacker, causer, hitData);
    }

    public void SetActiveSkills()
    {
        AppManager.Instance.SetActiveSkills(1, skill);
    }

    public override void SetStatus()
    {
        if (PlayerManager.Instance != null)
        {
            CharStatusData data = PlayerManager.Instance.GetCharacterStatus(1);
            status?.SetStatusData(data);
        }
    }

    public void SetEquipments()
    {
        if(PlayerManager.Instance != null)
        {
            CharEquipmentData data = PlayerManager.Instance.GetCharEquipmentData(1);
            equipment?.SertEquipmentData(data);
        }
    }

  
}
