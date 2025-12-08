using System;
using System.Collections;
using System.Collections.Generic;
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
    private ActionComponent currentAction;
    private List<ActionComponent> actionComponents = new();

    private bool bIsUsedSkill = false;

    private Action<InputAction.CallbackContext> onAction;
    private Action<InputAction.CallbackContext> onDash;
    private Action<InputAction.CallbackContext>[] onSkillActions;

    protected override void Awake()
    {
        base.Awake();

        weaponController = GetComponentInChildren<WeaponController>();
        comboComponent = GetComponent<ComboComponent>();
        weapon = GetComponent<WeaponComponent>();
        Debug.Assert(weapon != null);
        weapon.OnDoAction += OnDoAction;
        actionComponents.Add(weapon);

        skill = GetComponent<SkillComponent>();
        Debug.Assert(skill != null);
        Awake_SkillEventHandle(skill, weapon);
        skill.OnDoAction += OnDoAction;
        actionComponents.Add(skill);

        damageHandle = GetComponent<DamageHandleComponent>();
        launch = GetComponent<LaunchComponent>();

        if (state != null)
            state.OnStateTypeChanged += ChangeType;

        equipment = GetComponent<EquipmentComponent>();

        PlayerInput input = GetComponent<PlayerInput>();
        Debug.Assert(input != null);

        InputActionMap actionMap = input.actions.FindActionMap("Player");
        Debug.Assert(actionMap != null);

        onAction = (context) =>
        {
            if (bIsUsedSkill) return;

            currentAction = weapon;
            comboComponent?.InputQueue(InputCommandType.ACTION);
        };

        onDash = (context) =>
        {
            comboComponent?.InputQueue(InputCommandType.DASH);
        };


        Awake_SkillAcitonInput(actionMap);

        actionMap.FindAction("Action").started += onAction;
        actionMap.FindAction("Dash").started += onDash;
    }

    private void Awake_SkillEventHandle(SkillComponent skill, WeaponComponent weapon)
    {
        if (skill == null || weapon == null) return;

        skill.OnSkillUse += OnSkillUse;
        skill.skillEventHandler.OnBeginUseSkill += weapon.OnBeginSkillAction;
        skill.skillEventHandler.OnEndUseSkill += weapon.OnEndSkillAction;
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
        BattleManager.Instance?.RegistPlayer(this);
    }

    protected void OnDisable()
    {
        if(weapon != null)
        weapon.OnDoAction -= OnDoAction;

        if (skill != null)
        {
            skill.OnDoAction -= OnDoAction;
            skill.OnSkillUse -= OnSkillUse;
            skill.skillEventHandler.OnBeginUseSkill -= weapon.OnBeginSkillAction;
            skill.skillEventHandler.OnEndUseSkill -= weapon.OnEndSkillAction;
        }

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

    public void OnDoAction() => bInAction = true;

    public void OnSkillUse(bool bIsUse)
    {
        bIsUsedSkill = bIsUse;

        if (bIsUsedSkill)
        {
            currentAction = skill;
        }
    }

    public override void Begin_DoAction()
    {
        base.Begin_DoAction();

        OnBeginDoAction?.Invoke();
        currentAction?.BeginDoAction();
    }

    public override void End_DoAction()
    {
        if (bIsUsedSkill)
            bIsUsedSkill = false;

        bInAction = false;
        Debug.Log("Player End DoAction");
        OnEndDoAction?.Invoke();
        currentAction?.EndDoAction();
        foreach(var  ac in actionComponents)
        {
            if (ac != currentAction)
                ac.EndDoAction(); 
        }
    }

    public override void Begin_JudgeAttack(AnimationEvent e)
    {
        base.Begin_JudgeAttack(e);
        currentAction?.BeginJudgeAttack(e);
    }

    public override void End_JudgeAttack(AnimationEvent e)
    {
        base.End_JudgeAttack(e);
        currentAction?.EndJudgeAttack(e);
    }

    public override void Play_Sound()
    {
        base.Play_Sound();
        currentAction?.PlaySound();
    }

    public override void Play_CameraShake()
    {
        base.Play_CameraShake();
        currentAction?.PlayCameraShake();
    }

    public WeaponController GetWeaponController() => weaponController;

    public void OnDamage(GameObject attacker, Weapon causer, Vector3 hitPoint, DamageEvent damageEvent)
    {
        //if (isInvicible)
        //    return;

        if (state.Type == StateType.Evade)
        {
            //OnEvadeState?.Invoke();
            MovableSlower.Instance.Start_Slow(this);
            return;
        }

        ApplyLaunch(attacker, causer, damageEvent.hitData);
        damageHandle?.OnDamage(damageEvent);

        if (healthPoint.Dead == false)
        {
            state.SetDamagedMode();

            return;
        }

        // Dead
        state.SetDeadMode();

        Collider collider = GetComponent<Collider>();
        collider.enabled = false;

        animator.SetTrigger("Dead");
        //MovableStopper.Instance.Delete(this);
        //MovableSlower.Instance.Delete(this);
        //Destroy(gameObject, 5);
        StartCoroutine(HandleDeath());
    }

    private IEnumerator HandleDeath()
    {
        yield return new WaitForSeconds(1.0f);
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
    }

    protected override void End_Damaged()
    {
        base.End_Damaged();
        
        state?.SetIdleMode();
        foreach(var action in actionComponents)
            action.EndDoAction();
    }

    public void ApplyLaunch(GameObject attacker, Weapon causer, HitData hitData)
    {
        launch?.ApplyLaunch(attacker, causer, hitData);
    }

    public void SetActiveSkills()
    {
        AppManager.Instance.SetActiveSkills(1, skill);
    }

    public void SetStatus()
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
