using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[ModuleCategory("Common/CalcTargetPosition")]
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

    [Tooltip("지정된 특정 좌표값")]
    public Vector3 fixedLocalOffset;

    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
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
                skill.Runtime.Spawn.TargetPosition = finalTargetPos;
                break;

            case TargetPositionType.MultipleEnemies:
                // 여러 적의 위치 리스트를 가져와서 블랙보드에 저장
                var targetList = GetMultipleEnemyPositions(owner, owner.transform.position);

                if (targetList.Count > 0)
                {
                    skill.Runtime.Spawn.TargetPositions = targetList;
                    // 💡 보너스: 찾은 적의 숫자를 패턴 카운트로 자동 설정해줄 수도 있습니다!
                    skill.Runtime.Combat.PatternCountBonus = targetList.Count;
                    // 첫 번째 타겟 위치는 기본값으로 저장
                    finalTargetPos = targetList[0];
                    skill.Runtime.Spawn.TargetPosition = finalTargetPos;
                }
                break;
        }

        // 💡 계산이 끝난 최종 좌표를 블랙보드에 "TargetPosition"이라는 이름으로 예쁘게 올려둡니다.
        skill.Runtime.Spawn.TargetPosition = finalTargetPos;
    }

    // 🎯 가장 가까운 적 찾기 로직
    private Vector3 GetNearestEnemyPosition(Character owner, Vector3 origin)
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
    private List<Vector3> GetMultipleEnemyPositions(Character owner, Vector3 origin)
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