using System;
using Unity.VisualScripting;
using UnityEngine;


[ModuleCategory("Common/Set Target By Perception")]
[Serializable]
public class Module_SetTargetByPerception : SkillModule
{
    [Tooltip("해당 모듈이 계산 전에 이미 감지 된 적을 사용할 것인지")]
    public bool isAutoTarget = true;
    public float defaultDistance = 5f; // 사거리 변수화

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
        Vector3 finalPos;

        // 다시 한 번 검사 해당 모듈은 퍼셉션을 필요로함 
        if(bCompleteExist == false)
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
                if(owner != null && owner.TryGetComponent<PerceptionComponent>(out var perception))
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

        if (isAutoTarget && perception != null)
        {
            GameObject target = perception.GetTarget();

            // 감지에서 타겟이 없다면 이전에 타겟을 기록했는지 확인해서 그것으로 대체 
            if (target == null)
            {
                target = owner.GetComponent<AIBehaviourComponent>().SafeInvoke(v => v.GetTarget());
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

        skill.Runtime.Spawn.TargetPosition =  finalPos;
    }
}