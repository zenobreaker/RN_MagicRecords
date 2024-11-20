using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using static StateComponent;
using UnityEngine.WSA;

public class Player 
    : Character
    , IDamagable
{

    private ComboComponent comboComponent;
    private WeaponComponent weapon;



    protected override void Awake()
    {
        base.Awake();

        comboComponent = GetComponent<ComboComponent>();
        weapon = GetComponent<WeaponComponent>();
        Debug.Assert(weapon != null);

        PlayerInput input = GetComponent<PlayerInput>();
        Debug.Assert(input != null);

        InputActionMap actionMap = input.actions.FindActionMap("Player");
        Debug.Assert(actionMap != null);

        actionMap.FindAction("Action").started += (context) =>
        {
            Debug.Log("Action!");
            comboComponent.InputCombo(KeyCode.X);
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
        if (actionMap == null || weapon == null)
            return; 

        actionMap.FindAction("SkillAction1").started += (context) =>
        {
            weapon.DoSkillAction(SkillSlot.Slot1);
        };

        actionMap.FindAction("SkillAction2").started += (context) =>
        {
            weapon.DoSkillAction(SkillSlot.Slot2);
        };

        actionMap.FindAction("SkillAction3").started += (context) =>
        {
            weapon.DoSkillAction(SkillSlot.Slot3);
        };

        actionMap.FindAction("SkillAction4").started += (context) =>
        {
            weapon.DoSkillAction(SkillSlot.Slot4);
        };
    }

    public void OnDamage(GameObject attacker, Weapon causer, Vector3 hitPoint, DoActionData data)
    {
        //if (isInvicible)
        //    return;

        if (state.Type == StateType.Evade)
        {
            //OnEvadeState?.Invoke();
            MovableSlower.Instance.Start_Slow(this);
            return;
        }


        //OnDamaged?.Invoke();

        healthPoint.Damage(data.Power);

        // 스킬 액션 중이라면 데미지만 닳도록
        if (weapon != null /*&& weapon.InSkillAction*/)
            return;

        if (data.HitParticle != null)
        {
            //TODO: 옵젝풀러한테 불러오게할까
            GameObject obj = Instantiate<GameObject>(data.HitParticle, transform, false);
            obj.transform.localPosition = hitPoint + data.HitParticlePositionOffset;
            obj.transform.localScale = data.HitParticleSacleOffset;
        }

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
