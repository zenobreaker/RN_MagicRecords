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
    private bool bIsUsedSkill = false;

    protected override void Awake()
    {
        base.Awake();

        weaponController = GetComponentInChildren<WeaponController>();

        comboComponent = GetComponent<ComboComponent>();
        weapon = GetComponent<WeaponComponent>();
        Debug.Assert(weapon != null);

        skill = GetComponent<SkillComponent>();
        Debug.Assert(skill != null);
        Awake_SkillEventHandle(skill, weapon);
        
        damageHandle = GetComponent<DamageHandleComponent>();
        launch = GetComponent<LaunchComponent>();

        PlayerInput input = GetComponent<PlayerInput>();
        Debug.Assert(input != null);

        InputActionMap actionMap = input.actions.FindActionMap("Player");
        Debug.Assert(actionMap != null);
        
        actionMap.FindAction("Action").started += (context) =>
        {
            currentAction = weapon;
            comboComponent?.InputQueue(InputCommandType.Action);
        };

        Awake_SkillAcitonInput(actionMap);

        actionMap.FindAction("Dash").started += (context) =>
        {
            //TODO: 공격 중 캔슬 기능하려면 여기를 수정
            if (state.IdleMode == false)
                return;
            comboComponent?.InputQueue(InputCommandType.Dash);
            state?.SetEvadeMode();
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
            skill?.UseSkill(SkillSlot.Slot1);
        };

        actionMap.FindAction("SkillAction2").started += (context) =>
        {
            skill?.UseSkill(SkillSlot.Slot2); 
        };

        actionMap.FindAction("SkillAction3").started += (context) =>
        {
            skill?.UseSkill(SkillSlot.Slot3);
        };

        actionMap.FindAction("SkillAction4").started += (context) =>
        {
            skill?.UseSkill(SkillSlot.Slot4); 
        };
    }

    protected override void Start()
    {
        base.Start();

        BattleManager.Instance.ResistPlayer(this);
    }

    public void OnSkillUse(bool bIsUse)
    {
        bIsUsedSkill = bIsUse;

        if (bIsUsedSkill)
        {
            currentAction = skill;
            comboComponent.InputQueue(InputCommandType.Skill);
        }
    }

    public override void Begin_DoAction()
    {
        base.Begin_DoAction();

        currentAction?.BeginDoAction();
    }

    public override void End_DoAction()
    {
        if (bIsUsedSkill)
            bIsUsedSkill = false;
        currentAction?.EndDoAction(); 
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
        MovableStopper.Instance.Delete(this);
        MovableSlower.Instance.Delete(this);
        Destroy(gameObject, 5);
    }

    public void ApplyLaunch(GameObject attacker, Weapon causer, HitData hitData)
    {
        launch?.ApplyLaunch(attacker, causer, hitData);
    }
}
