using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(MovementComponent))]
public class PlayerInputComponent : MonoBehaviour
{
    private MovementComponent movement;
    private Vector2 inputMove;

    private void Awake()
    {
        movement = GetComponent<MovementComponent>();
        Awake_PlayerBindInput();
    }

    private void Awake_PlayerBindInput()
    {
        PlayerInput input = GetComponent<PlayerInput>();
        InputActionMap actionMap = input.actions.FindActionMap("Player");
        InputAction moveAction = actionMap.FindAction("Move");

        moveAction.performed += ctx =>
        {
            inputMove = ctx.ReadValue<Vector2>();
            movement.SetDirection(inputMove);
        };
        moveAction.canceled += ctx =>
        {
            inputMove = Vector2.zero;
            movement.SetDirection(inputMove);
        };
    }

}