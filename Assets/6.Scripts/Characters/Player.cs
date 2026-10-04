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
        CancelInvoke(nameof(Dead));

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

    protected override bool CanReceiveDamage(DamageEvent damageEvent)
    {
        // Existing direct-hit evade rule does not suppress ongoing DOT damage.
        if (!damageEvent.IsDOTEffect() && state != null && state.EvadeMode)
        {
            MovableSlower.Instance.SafeInvoke(v => v.Start_Slow(this));
            return false;
        }
        return true;
    }

    protected override void OnDamageDeath()
    {
        Collider collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;

        Invoke(nameof(Dead), 1f);
        visual.SafeInvoke(v => v.PlayDeadAnimation());
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
        AppManager.Instance.SetActiveSkills(CharID, skill);
    }

    public override void SetStatus()
    {
        if (PlayerManager.Instance != null)
        {
            CharStatusData data = gameObject.scene.name == "Stage"
                ? PlayerManager.Instance.GetRunCharacterStatus(CharID)
                : PlayerManager.Instance.GetCharacterStatus(CharID);
            status.SafeInvoke(v => v.SetStatusData(data));
        }
    }

    public void SetEquipments()
    {
        if (PlayerManager.Instance != null)
        {
            CharEquipmentData data = PlayerManager.Instance.GetCharEquipmentData(CharID);
            equipment.SafeInvoke(v => v.SertEquipmentData(data));
        }
    }


}
