using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using static StateComponent;
using UnityEngine.WSA;
using UnityEditor.Experimental.GraphView;

public class Player
    : Character
    , IDamagable
    , IWeaponUser
{

    private ComboComponent comboComponent;
    private WeaponComponent weapon;
    private SkillComponent skill;

    private WeaponController weaponController;

    private DamageHandleComponent damageHandle; 


    private bool bIsSkillInput = false;

    protected override void Awake()
    {
        base.Awake();

        weaponController = GetComponentInChildren<WeaponController>();

        comboComponent = GetComponent<ComboComponent>();
        weapon = GetComponent<WeaponComponent>();
        Debug.Assert(weapon != null);

        skill = GetComponent<SkillComponent>();
        Debug.Assert(skill != null);
        skill.OnSkillUse += OnSkillUse;
        skill.skillEventHandler.OnBeginUseSkill += weapon.OnBeginSkillAction;
        skill.skillEventHandler.OnEndUseSkill += weapon.OnEndSkillAction;

        damageHandle = GetComponent<DamageHandleComponent>();

        PlayerInput input = GetComponent<PlayerInput>();
        Debug.Assert(input != null);

        InputActionMap actionMap = input.actions.FindActionMap("Player");
        Debug.Assert(actionMap != null);
        //TODO: 게임시작하고 바로 안나가는 버그 있음
        actionMap.FindAction("Action").started += (context) =>
        {
            comboComponent.InputQueue(InputCommandType.Action);
        };

        Awake_SkillAcitonInput(actionMap);

        actionMap.FindAction("Dash").started += (context) =>
        {
            //TODO: 공격 중 캔슬 기능하려면 여기를 수정
            if (state.IdleMode == false)
                return;

            state.SetEvadeMode();
        };
    }


    private void Awake_SkillAcitonInput(InputActionMap actionMap)
    {
        if (actionMap == null || skill == null)
            return;

        actionMap.FindAction("SkillAction1").started += (context) =>
        {
            skill.UseSkill(SkillSlot.Slot1);
        };

        actionMap.FindAction("SkillAction2").started += (context) =>
        {
            skill.UseSkill(SkillSlot.Slot2);
        };

        actionMap.FindAction("SkillAction3").started += (context) =>
        {
            skill.UseSkill(SkillSlot.Slot3);
        };

        actionMap.FindAction("SkillAction4").started += (context) =>
        {
            skill.UseSkill(SkillSlot.Slot4);
        };
    }

    public override void Begin_Action()
    {
        if (bIsSkillInput)
        {
            skill?.Begin_SkillAction();
            return; 
        }

        weapon?.Begin_DoAction();
    }

    public override void End_Action()
    {
        weapon?.End_DoAction();
        skill?.End_SkillAction();
        bIsSkillInput = false;
    }

    public void OnSkillUse(bool bIsUse)
    {
        bIsSkillInput = bIsUse;
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

        if (damageHandle != null)
            damageHandle.OnDamage(damageEvent);

        // 스킬 액션 중이라면 데미지만 닳도록
        if (weapon != null /*&& weapon.InSkillAction*/)
            return;

        //if (data != null && data.damageInfo != null)
        //{
        //    //TODO: 옵젝풀러한테 불러오게할까
        //    GameObject obj = Instantiate<GameObject>(data.damageInfo.HitParticle, transform, false);
        //    obj.transform.localPosition = hitPoint + data.damageInfo.HitParticlePositionOffset;
        //    obj.transform.localScale = data.damageInfo.HitParticleSacleOffset;
        //}

        if (healthPoint.Dead == false)
        {
            state.SetDamagedMode();
            // launch.DoHit(attacker, causer, data, false);

            // if (data.bDownable == false)
            // {
            //     DownDamaged();

            //     animator.SetInteger(HitIndex, data.HitImpactIndex);
            //     animator.SetTrigger(HitImapact);
            // }
            // else
            //     Begin_DownImpact();

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
}
