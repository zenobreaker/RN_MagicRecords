using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using static StateComponent;

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

    private WeaponController weaponController;
    private ActionComponent currentAction;
    private List<ActionComponent> actionComponents = new();

    private bool bIsUsedSkill = false;

    public event Action<Player> OnDead;

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

        PlayerInput input = GetComponent<PlayerInput>();
        Debug.Assert(input != null);

        InputActionMap actionMap = input.actions.FindActionMap("Player");
        Debug.Assert(actionMap != null);

        actionMap.FindAction("Action").started += (context) =>
        {
            if (bIsUsedSkill) return;
                
            currentAction = weapon;
            comboComponent?.InputQueue(InputCommandType.Action);
        };

        Awake_SkillAcitonInput(actionMap);

        actionMap.FindAction("Dash").started += (context) =>
        {
            comboComponent?.InputQueue(InputCommandType.Dash);
        };
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

        actionMap.FindAction("SkillAction1").started += (context) =>
        {
            comboComponent.InputQueue(InputCommandType.Skill, (int)SkillSlot.Slot1);
        };

        actionMap.FindAction("SkillAction2").started += (context) =>
        {
            comboComponent.InputQueue(InputCommandType.Skill, (int)SkillSlot.Slot2);
        };

        actionMap.FindAction("SkillAction3").started += (context) =>
        {
            comboComponent.InputQueue(InputCommandType.Skill, (int)SkillSlot.Slot3);
        };

        actionMap.FindAction("SkillAction4").started += (context) =>
        {
            comboComponent.InputQueue(InputCommandType.Skill, (int)SkillSlot.Slot4);
        };
    }

    protected override void Start()
    {
        base.Start();
        
        SetGenericTeamId(1); 
    }
    protected void OnEnable()
    {
        BattleManager.Instance.ResistPlayer(this);
    }

    protected void OnDisable()
    {
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

        ApplyLaunch(attacker, causer, damageEvent?.hitData);
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
}
