using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : Character
{

    private ComboComponent comboComponent;
    private DashComponent dashComponent;

    protected override void Awake()
    {
        base.Awake();

        comboComponent = GetComponent<ComboComponent>();
        dashComponent = GetComponent<DashComponent>();

        PlayerInput input = GetComponent<PlayerInput>();
        Debug.Assert(input != null);

        InputActionMap actionMap = input.actions.FindActionMap("Player");
        Debug.Assert(actionMap != null);

        actionMap.FindAction("Action").started += (context) =>
        {
            Debug.Log("Action!");
            comboComponent.InputCombo(KeyCode.X);
        };


        actionMap.FindAction("Dash").started += (context) =>
        {
            //TODO: 공격 중 캔슬 기능하려면 여기를 수정
            if (state.IdleMode == false)
                return;

            state.SetEvadeMode();
        };
    }
}
