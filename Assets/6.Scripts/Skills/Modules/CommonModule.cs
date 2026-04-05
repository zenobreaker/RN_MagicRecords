using System;
using System.Collections.Generic;
using System.Linq;
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

        if (useBlackboardPos && skill.Blackboard.ContainsKey(Constants.TargetPos))
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


[Serializable]
public class Module_CalcTargetPosition : SkillModule
{
    [Header("Targeting Settings")]
    public TargetPositionType targetingType = TargetPositionType.CasterForward;
    public float scanRadius = 10f;       // 적을 찾을 범위
    public LayerMask enemyLayer;         // 적 레이어 설정
    public int maxTargetCount = 1;       // 최대 몇 명까지 타겟팅할지

    [Tooltip("CasterForward일 때 시전자로부터 얼마나 떨어질 것인가?")]
    public float forwardDistance = 5f;

    [Tooltip("블랙보드에 저장할 키 이름")]
    public string blackboardKey = "TargetPosition";

    [Tooltip("지정된 특정 좌표값")]
    public Vector3 fixedLocalOffset;

    public override void OnNotify(GameObject owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        Vector3 finalTargetPos = owner.transform.position; // 기본값은 시전자 위치

        switch (targetingType)
        {
            // 1. 현재 필요한 기능: 시전자 전방 좌표
            case TargetPositionType.CasterForward:
                finalTargetPos = owner.transform.position + (owner.transform.forward * forwardDistance);
                break;

            // 2. 지정된 특정 상대 좌표
            case TargetPositionType.FixedLocalOffset:
                finalTargetPos = owner.transform.TransformPoint(fixedLocalOffset);
                break;

            // 3. 이미 플레이어 컨트롤러가 마우스 커서 위치를 넣어뒀다면 무시!
            case TargetPositionType.ReadFromBlackboard:
                return; // 덮어쓰지 않고 종료

            // 4. 시전자 주변의 무작위 위치 
            case TargetPositionType.RandomAroundCaster:
                Vector2 rand = UnityEngine.Random.insideUnitCircle * forwardDistance;
                finalTargetPos = owner.transform.position + new Vector3(rand.x, 0, rand.y);
                break;

            case TargetPositionType.NearestEnemy:
                finalTargetPos = GetNearestEnemyPosition(owner, owner.transform.position);
                skill.Blackboard.SetValue(blackboardKey, finalTargetPos);
                break;

            case TargetPositionType.MultipleEnemies:
                // 여러 적의 위치 리스트를 가져와서 블랙보드에 저장
                var targetList = GetMultipleEnemyPositions(owner, owner.transform.position);

                if (targetList.Count > 0)
                {
                    skill.Blackboard.SetValue(blackboardKey + "_List", targetList);
                    // 💡 보너스: 찾은 적의 숫자를 패턴 카운트로 자동 설정해줄 수도 있습니다!
                    skill.Blackboard.SetValue(Constants.PatternCount, targetList.Count);
                    // 첫 번째 타겟 위치는 기본값으로 저장
                    finalTargetPos = targetList[0];
                    skill.Blackboard.SetValue(blackboardKey, finalTargetPos);
                }
                break;
        }

        // 💡 계산이 끝난 최종 좌표를 블랙보드에 "TargetPosition"이라는 이름으로 예쁘게 올려둡니다.
        skill.Blackboard.SetValue(blackboardKey, finalTargetPos);
    }

    // 🎯 가장 가까운 적 찾기 로직
    private Vector3 GetNearestEnemyPosition(GameObject owner, Vector3 origin)
    {
        Collider[] cols = Physics.OverlapSphere(origin, scanRadius, enemyLayer);

        float minDst = float.MaxValue;
        Vector3 forward = owner == null ? Vector3.forward : owner.transform.forward;
        Vector3 nearestPos = origin + (forward * forwardDistance); // 적이 없으면 전방 기본값
        ITeamAgent teamAgent = owner.GetComponent<ITeamAgent>();

        foreach (var col in cols)
        {
            // 💡 [핵심] 찾은 콜라이더가 시전자(owner) 본인이거나 자식 오브젝트면 무시!
            if (col.transform.IsChildOf(owner.transform)) continue;
            // 팀일 경우도 무시
            if (col.TryComponentInParent<ITeamAgent>(out var c))
            {
                if (teamAgent != null && c.GetGeneriTeamId() == teamAgent.GetGeneriTeamId())
                    continue;
            }

            float dst = Vector3.Distance(origin, col.transform.position);
            if (dst < minDst)
            {
                minDst = dst;
                nearestPos = col.transform.position;
            }
        }
        return nearestPos;
    }

    // 🎯 범위 내 다수의 적 리스트 찾기 로직
    private List<Vector3> GetMultipleEnemyPositions(GameObject owner, Vector3 origin)
    {
        Collider[] cols = Physics.OverlapSphere(origin, scanRadius, enemyLayer);
        ITeamAgent teamAgent = owner.GetComponent<ITeamAgent>();

        // 거리순으로 정렬 후 개수 제한하여 리스트 생성
        return cols.Where(c =>
                    {
                        // 1. 자기 자신 무시 
                        if (c.transform.IsChildOf(owner.transform)) return false;

                        // 2. 같은 팀 무시
                        if (teamAgent != null && c.TryComponentInParent<ITeamAgent>(out var targetTeam))
                        {
                            if (targetTeam.GetGeneriTeamId() == teamAgent.GetGeneriTeamId())
                                return false;
                        }
                        return true;
                    })
                    .Select(c => c.transform.position)
                   .OrderBy(pos => Vector3.Distance(origin, pos))
                   .Take(maxTargetCount)
                   .ToList();
    }
}

[Serializable]
public class Module_SpawnWeaponVFX : SkillModule
{
    [Header("Muzzle Flash Settings")]
    public GameObject muzzleFlashPrefab;

    // 💡 1. 모든 총구를 다 쓸 것인가? 아니면 특정 총구만 쓸 것인가?
    [Tooltip("체크하면 무기에 달린 모든 총구에서 플래시가 터집니다.")]
    public bool useAllMuzzles = true;

    // 💡 2. 특정 총구만 쓴다면 몇 번 총구를 쓸 것인가? (배열)
    [Tooltip("useAllMuzzles가 꺼져있을 때 작동합니다. (예: 0 넣으면 첫 번째 총구, 0과 1 넣으면 두 개)")]
    public int[] specificMuzzleIndices;

    public override void OnNotify(GameObject owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        if (owner.TryGetComponent<WeaponComponent>(out var weaponComp))
        {
            Weapon currentWeapon = weaponComp.GetCurrentWeapon();

            // 현재 들고 있는 무기가 Gun일 때만 작동
            if (currentWeapon is Gun gun)
            {
                List<Transform> muzzles = gun.GetMuzzleTransforms();
                if (muzzles == null || muzzles.Count == 0) return;

                // 💡 3. 조건에 따라 불을 뿜을 총구를 걸러냅니다.
                if (useAllMuzzles)
                {
                    // [전체 다 쏘기]
                    foreach (var muzzle in muzzles)
                    {
                        SpawnFlash(muzzle);
                    }
                }
                else
                {
                    // [특정 번호만 쏘기]
                    if (specificMuzzleIndices != null)
                    {
                        foreach (int idx in specificMuzzleIndices)
                        {
                            // 인덱스가 배열 범위를 벗어나지 않도록 안전장치(방어 코드)
                            if (idx >= 0 && idx < muzzles.Count)
                            {
                                SpawnFlash(muzzles[idx]);
                            }
                        }
                    }
                }
            }
        }
    }

    private void SpawnFlash(Transform muzzleTransform)
    {
        if (muzzleFlashPrefab != null)
        {
            // ObjectPooler가 있다면 ObjectPooler.SpawnFromPool(...) 로 교체하세요!
            GameObject.Instantiate(muzzleFlashPrefab, muzzleTransform.position, muzzleTransform.rotation);
        }
    }
}