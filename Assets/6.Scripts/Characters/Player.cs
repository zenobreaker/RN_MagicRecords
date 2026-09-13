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
    [SerializeField] private SO_ActiveSkillData subActionSkill;
    private DamageHandleComponent damageHandle;
    private LaunchComponent launch;
    private EquipmentComponent equipment;

    private WeaponController weaponController;

    private InputActionMap playerActionMap;
    private Action<InputAction.CallbackContext> onAction;
    private Action<InputAction.CallbackContext> onMove;
    private Action<InputAction.CallbackContext> onDash;
    private Action<InputAction.CallbackContext>[] onSkillActions;
    private Action<InputAction.CallbackContext>[] onSkillCancels;


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

        skill = GetComponent<SkillComponent>();
        Debug.Assert(skill != null);

        damageHandle = GetComponent<DamageHandleComponent>();
        launch = GetComponent<LaunchComponent>();

        equipment = GetComponent<EquipmentComponent>();

        PlayerInput input = GetComponent<PlayerInput>();
        Debug.Assert(input != null);

        InputActionMap actionMap = input.actions.FindActionMap("Player");
        Debug.Assert(actionMap != null);
        playerActionMap = actionMap;

        onAction = (context) =>
        {
            if (comboComponent != null)
                comboComponent.InputQueue(InputCommandType.ACTION);
        };

        onDash = (context) =>
        {
            if (comboComponent != null)
                comboComponent.InputQueue(InputCommandType.DASH);
        };


        onMove = (context) =>
        {
            comboComponent.SafeInvoke(v => v.BreakCombo());
        };

        Awake_SkillAcitonInput(actionMap);


    }


    private void Awake_SkillAcitonInput(InputActionMap actionMap)
    {
        if (actionMap == null || skill == null)
            return;
        onSkillActions = new Action<InputAction.CallbackContext>[4];
        onSkillCancels = new Action<InputAction.CallbackContext>[4];

        for (int i = 0; i < 4; i++)
        {
            SkillSlot slot = SkillSlot.SLOT1 + i;
            int index = i;
            onSkillActions[i] = (context) =>
            {
                comboComponent.InputQueue(InputCommandType.SKILL, index);
            };

            onSkillCancels[i] = (context) =>
            {
                skill.ReleaseSkill(slot.ToString());
            };


        }
    }

    protected override void Start()
    {
        base.Start();

        SetGenericTeamId(1);
        if (skill != null && subActionSkill != null)
            skill.SetActiveSkill(SkillSlot.SubAction, subActionSkill.CreateSkill() as ActiveSkill);
    }
    protected void OnEnable()
    {
        SetInputSubscriptions(true);
        if (state != null)
            state.OnStateTypeChanged += ChangeType;

        if (skill != null)
            skill.OnDoAction += DoAction;

        Debug.Log($"Battle Manager {BattleManager.Instance}");
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (state != null)
            state.OnStateTypeChanged -= ChangeType;

        if (skill != null)
            skill.OnDoAction -= DoAction;

        SetInputSubscriptions(false);

        BattleManager.Instance.SafeInvoke(v => v.UnreistPlayer(this));
    }

    private void SetInputSubscriptions(bool subscribe)
    {
        if (playerActionMap == null) return;
        void Bind(string name, Action<InputAction.CallbackContext> started,
            Action<InputAction.CallbackContext> canceled = null)
        {
            var action = playerActionMap.FindAction(name, false);
            if (action == null) return;
            if (started != null) action.started -= started;
            if (canceled != null) action.canceled -= canceled;
            if (subscribe)
            {
                if (started != null) action.started += started;
                if (canceled != null) action.canceled += canceled;
            }
        }
        Bind("Action", onAction);
        Bind("Dash", onDash);
        Bind("Move", onMove);
        if (onSkillActions == null || onSkillCancels == null) return;
        for (int i = 0; i < onSkillActions.Length; i++)
            Bind($"SkillAction{i + 1}", onSkillActions[i], onSkillCancels[i]);
    }

    private void DoAction()
    {
        state.SafeInvoke(v => v.SetActionMode());
    }

    public override void Start_DoAction()
    {
        base.Start_DoAction();
        if (skill.SafeInvoke(v => v.InAction))
            skill.StartAction();
    }

    public override void Begin_DoAction()
    {
        base.Begin_DoAction();

        OnBeginDoAction?.Invoke();
        if (skill.SafeInvoke(v => v.InAction))
            skill.BeginDoAction();
    }

    public override void End_DoAction()
    {
        if (endingAction) return;
        endingAction = true;
        try
        {
            if (skill.SafeInvoke(v => v.InAction))
            {
                skill.EndDoAction();
                if (skill.InAction) return;
            }
            bInAction = false;
            if (state != null && !state.DamagedMode && !state.DeadMode && !state.StopMode) state.SetIdleMode();
            OnEndDoAction?.Invoke();
        }
        finally { endingAction = false; }
    }
    public override void Begin_JudgeAttack(AnimationEvent e)
    {
        base.Begin_JudgeAttack(e);
        if (skill.SafeInvoke(v => v.InAction))
            skill.BeginJudgeAttack(e);
    }

    public override void End_JudgeAttack(AnimationEvent e)
    {
        base.End_JudgeAttack(e);
        if (skill.SafeInvoke(v => v.InAction))
            skill.EndJudgeAttack(e);
    }

    public override void Play_Sound()
    {
        base.Play_Sound();
        if (skill.SafeInvoke(v => v.InAction))
            skill.PlaySound();
    }

    public override void Play_CameraShake()
    {
        base.Play_CameraShake();
        if (skill.SafeInvoke(v => v.InAction))
            skill.PlayCameraShake();
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
        damageHandle.SafeInvoke(v => v.OnDamage(attacker, damageEvent));

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
        visual.SafeInvoke(v => v.PlayDeadAnimation());
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
            if (skill.SafeInvoke(v => v.InAction))
                skill.CancelCurrentSkill();
        }
    }

    public override void End_Damaged()
    {
        base.End_Damaged();

        state.SafeInvoke(v => v.SetIdleMode());
        if (skill.SafeInvoke(v => v.InAction))
            skill.CancelCurrentSkill();
    }

    public void ApplyLaunch(GameObject attacker, Weapon causer, DamageEvent devt)
    {
        ApplyLaunch(attacker, causer, devt?.hitData);
    }

    public void ApplyLaunch(GameObject attacker, Weapon causer, HitData hitData)
    {
        launch.SafeInvoke(v => v.ApplyLaunch(attacker, causer, hitData));
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
            status.SafeInvoke(v => v.SetStatusData(data));
        }
    }

    public void SetEquipments()
    {
        if (PlayerManager.Instance != null)
        {
            CharEquipmentData data = PlayerManager.Instance.GetCharEquipmentData(1);
            equipment.SafeInvoke(v => v.SertEquipmentData(data));
        }
    }


}
