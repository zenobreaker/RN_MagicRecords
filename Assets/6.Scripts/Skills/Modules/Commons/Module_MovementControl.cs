using System;
using UnityEngine;
using UnityEngine.AI;

public enum MovementControlType
{
    LockMovement,       // 이동 완전 금지 (뿌리내림)
    UnlockMovement,     // 이동 금지 해제
    ModifySpeed         // 이동 속도 변경 (예: 차징 중 느리게 걷기)
}

[ModuleCategory("Utility/Movement Control")]
[Serializable]
public class Module_MovementControl : SkillModule
{
    [Header("Control Settings")]
    public MovementControlType controlType = MovementControlType.LockMovement;

    [Tooltip("ModifySpeed일 때 적용할 배율 (0.5 = 50% 속도로 느려짐)")]
    public float speedMultiplier = 0.5f;

    public override void OnNotify(GameObject owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        NavMeshAgent agent = owner.GetComponent<NavMeshAgent>();
        MovementComponent move = owner.GetComponent<MovementComponent>();

        switch (controlType)
        {
            case MovementControlType.LockMovement:
                if (agent != null)
                {
                    agent.isStopped = true;
                    agent.updateRotation = false;
                    agent.velocity = Vector3.zero;
                }
                // (필요하다면 chara.SetMovable(false) 같은 함수 호출)
                if (move != null)
                {
                    move.Stop();
                }
                break;

            case MovementControlType.UnlockMovement:
                if (agent != null)
                {
                    agent.isStopped = false;
                    agent.updateRotation = true;
                }
                // 속도를 원래대로 복구하는 로직 (chara 컴포넌트에 원래 속도를 저장해뒀다고 가정)
                if (move != null)
                {
                    move.RecoverSpeed();
                    move.Move();
                }
                break;

            case MovementControlType.ModifySpeed:
                if (agent != null)
                {
                    agent.isStopped = false;
                    agent.updateRotation = true;
                }
                // 💡 [핵심] 차징 중 느리게 이동하기!
                if (move != null)
                {
                    move.SetMoveSpeed(move.Speed * speedMultiplier);
                    move.Move();
                }
                break;
        }
    }

    public override SkillModule Clone()
    {
        return (Module_MovementControl)base.Clone();
    }
}