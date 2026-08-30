using System;
using UnityEngine;

[ModuleCategory("Movement/Dash")]
[Serializable]
public class Module_Dash : SkillModule
{
    [Header("Dash Settings")]
    public float distance = 5f;
    public float duration = 0.3f;
    public bool useTargetPosition = true;
    public AnimationCurve speedCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("MoveOverTime Settings")]
    public bool bIsMoveOverTime = false;
    [Tooltip("등속 운동 중 특정 대상 무시하기")]
    public bool bIsGhostMode = false; 



    public override void OnNotify(
        Character owner,
        ActiveSkill skill,
        PhaseSkill phaseSkill)
    {
        if (owner == null)
            return;

        MovementComponent movement =
            owner.GetComponent<MovementComponent>();

        if (movement == null)
            return;

        Vector3 direction;

        if (useTargetPosition)
        {
            direction =
                skill.Runtime.Spawn.TargetPosition
                - owner.transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
                direction = owner.transform.forward;
        }
        else
        {
            direction = owner.transform.forward;
        }
        
        movement.SetDirection(new Vector2(direction.x, direction.z));

        if (!bIsMoveOverTime)
            movement.Dash(
            direction,
            distance,
            duration,
            speedCurve
        );
        else
            movement.MoveOverTime(direction, distance, duration, bIsGhostMode);
    }

    public override SkillModule Clone()
    {
        return (Module_Dash)base.Clone();
    }
}