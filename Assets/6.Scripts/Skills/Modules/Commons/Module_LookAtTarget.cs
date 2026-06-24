using System;
using UnityEngine;


[ModuleCategory("Common/Look at Target")]
[Serializable]
public class Module_LookAtTarget : SkillModule
{
    public bool useBlackboardPos = true;    // 세팅된 값을 사용할 지 
    public bool lookAtOwnerTarget = false;  // 실시간 타겟을 볼지 
    public float turnSpeed = 0f;

    private PerceptionComponent perception;
    public override void Init(GameObject owner)
    {
        base.Init(owner);
        perception = owner.GetComponent<PerceptionComponent>();
    }


    public override void OnNotify(GameObject owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        Vector3 targetPos;

        if (useBlackboardPos)
        {
            targetPos = skill.Runtime.TargetPosition;
        }
        else
        {
            var target = perception.SafeInvoke(v=>v.GetTarget());
            if (target == null) return;
            targetPos = target.transform.position;
        }

        Vector3 direction = (targetPos - owner.transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            if (turnSpeed <= 0)
                owner.transform.rotation = Quaternion.LookRotation(direction);
            else
            {
                // 실시간 회전이 필요한 경우 코루틴이나 별도 컴포넌트에게 전달
                owner.transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
}