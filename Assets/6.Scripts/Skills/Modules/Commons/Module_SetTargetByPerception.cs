using System;
using UnityEngine;


[ModuleCategory("Common/Set Target By Perception")]
[Serializable]
public class Module_SetTargetByPerception : SkillModule
{
    public bool isAutoTarget = true;
    public float defaultDistance = 5f; // 사거리 변수화

    private PerceptionComponent perception;

    public override void Init(GameObject owner)
    {
        base.Init(owner);
        perception = owner.GetComponent<PerceptionComponent>();
    }

    public override void OnNotify(GameObject owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        Vector3 finalPos;

        if (isAutoTarget && perception != null)
        {
            GameObject target = perception.GetTarget();

            // 감지에서 타겟이 없다면 이전에 타겟을 기록했는지 확인해서 그것으로 대체 
            if (target == null)
            {
                target = owner.GetComponent<AIBehaviourComponent>()?.GetTarget();
            }
            // 타겟이 있으면 타겟 위치, 없으면 앞방향 기본 거리
            finalPos = (target != null)
                ? target.transform.position
                : owner.transform.position + owner.transform.forward * defaultDistance;
        }
        else
        {
            // 수동 타겟이거나 컴포넌트가 없으면 무조건 앞방향
            finalPos = owner.transform.position + owner.transform.forward * defaultDistance;
        }

        skill.Runtime.TargetPosition =  finalPos;
    }
}