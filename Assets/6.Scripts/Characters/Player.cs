using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : Character
{

    protected override void Awake()
    {
        base.Awake();

        PlayerInput input = GetComponent<PlayerInput>();
        Debug.Assert(input != null);

        InputActionMap actionMap = input.actions.FindActionMap("Player");
        Debug.Assert(actionMap != null);

        actionMap.FindAction("Action").started += (context) =>
        {
            Debug.Log("Actin !");
        };

    }
}
