using System;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.UI.GridLayoutGroup;


[Serializable]
public class Module_PlayAnimation : SkillModule
{
    [Header("Skill Action")]
    public ActionData actionData;

    private Character ownerCharacter;
    private WeaponController weaponController;

    public override void Init(GameObject owner)
    {
        actionData.Initialize();
        ownerCharacter = owner.GetComponent<Character>();

        weaponController = owner.GetComponent<IWeaponUser>()?.GetWeaponController();
    }

    public override void OnNotify(GameObject owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        ownerCharacter?.PlayAction(actionData);
        weaponController?.DoAction(actionData);
    }
}

[Serializable]
public class Module_CameraShake : SkillModule
{
    [Header("Camera Shake")]
    public Vector3 impulseDirection;

    [Tooltip("Cinemachine NoiseSettings asset")]
    public Unity.Cinemachine.NoiseSettings settings;

    public override void OnNotify(GameObject owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        if (MovableCameraShaker.Instance != null)
            MovableCameraShaker.Instance.Play_Impulse(settings);
    }
}

[Serializable]
public class Module_Sound : SkillModule
{
    [Header("Skill Sounds")]
    public string soundName;

    public override void OnNotify(GameObject owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        phaseSkill?.actionData?.Play_Sound();
    }
}


[Serializable]
public class Module_SetTargetByPerception : SkillModule
{
    public  bool isAutoTarget = true;
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
            if(target == null)
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

        skill.Blackboard.SetValue("Target_Pos", finalPos);
    }
}

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

        if(useBlackboardPos && skill.Blackboard.ContainsKey(Constants.TargetPos))
        {
            targetPos = skill.Blackboard.GetValue<Vector3>(Constants.TargetPos);
        }
        else
        {
            var target = perception?.GetTarget();
            if (target == null) return; 
            targetPos = target.transform.position;
        }

        Vector3 direction = (targetPos - owner.transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            if(turnSpeed <= 0)
                owner.transform.rotation = Quaternion.LookRotation(direction);
            else
            {
                // 실시간 회전이 필요한 경우 코루틴이나 별도 컴포넌트에게 전달
                owner.transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
}