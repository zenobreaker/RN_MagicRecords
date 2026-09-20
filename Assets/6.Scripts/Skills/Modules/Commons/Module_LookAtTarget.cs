using System;
using Unity.VisualScripting;
using UnityEngine;


[ModuleCategory("Common/Look at Target")]
[Serializable]
public class Module_LookAtTarget : SkillModule
{
    public bool useBlackboardPos = true;    // 세팅된 값을 사용할 지 
    public bool lookAtOwnerTarget = false;  // 실시간 타겟을 볼지 
    public float turnSpeed = 0f;

    private PerceptionComponent perception;
    private bool bCompleteExist = false;
    public override void Init(Character owner)
    {
        base.Init(owner);
        if (owner == null) return;

        perception = owner.GetComponent<PerceptionComponent>();
        if (perception != null)
            bCompleteExist = true;
    }


    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        Vector3 targetPos;

        // 다시 한 번 검사 해당 모듈은 퍼셉션을 필요로함 
        if (bCompleteExist == false)
        {
            perception = owner.GetComponent<PerceptionComponent>();
            if (perception != null)
                bCompleteExist = true;
        }
        else
        {
            // 생성한 전적이 없다면 한 번 더 검사를 진행하며 존재한다면 넘기고 존재하지 않으면
            // 새로 붙여서 처리한다. 
            void AddPerception()
            {
                if (owner != null && owner.TryGetComponent<PerceptionComponent>(out var perception))
                {
                    this.perception = perception;
                    bCompleteExist = true;
                }

                if (bCompleteExist == false)
                {
                    this.perception = owner.AddComponent<PerceptionComponent>();
                    bCompleteExist = true;
                }
            }

            AddPerception();
        }


        if (useBlackboardPos)
        {
            targetPos = skill.Runtime.Spawn.TargetPosition;
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