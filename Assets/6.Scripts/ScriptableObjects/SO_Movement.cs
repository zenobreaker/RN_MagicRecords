using UnityEngine;

[CreateAssetMenu(fileName = "SO_Movement", menuName = "Scriptable Objects/SO_Movement")]
public class SO_Movement : ScriptableObject
{
    public enum SpeedType
    {
        Walk = 1,
        Run = 2,
        Sprint = 3,
    };


    [Header("Speed")]
    [SerializeField] private float walkSpeed = 2.0f;
    [SerializeField] private float runSpeed = 4.0f;
    [SerializeField] private float sprintSpeed = 6.0f;
    [SerializeField] private float walkToRunRatio = 2.0f;
    public float WalkSpeed { get => walkSpeed; }
    public float RunSpeed { get => runSpeed; }
    public float SprintSpeed { get => SprintSpeed; }

    public float Ratio { get => walkToRunRatio; }

    public SO_Movement GetMovement()
    {
        SO_Movement movement = SO_Movement.CreateInstance<SO_Movement>();
        movement.walkSpeed = walkSpeed;
        movement.runSpeed = runSpeed;
        movement.sprintSpeed = sprintSpeed;
        return movement;
    }
}
