using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : Character
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
}
