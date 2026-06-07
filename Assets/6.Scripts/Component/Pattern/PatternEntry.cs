using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;


public enum PatternConditionType
{
    Cooldown, 
    Health, 
    ElapseTime,
    Distance, 
    Custom,
}

public enum ComparisonType
{
    GreaterThan,
    LessThan,
    Equal,
    NotEqual,
    GreaterThanOrEqual,
    LessThanOrEqual
}

[System.Serializable]
public struct PatternCondition 
{
    public PatternConditionType type; 
    public ComparisonType ctype ; 
    public float value;

}

[System.Serializable]
public class PatternEntry
{
    public string slotName;
    public List<PatternCondition> conditions = new List<PatternCondition>();
    public SO_ActiveSkillData skill;

    // 이렇게 하면 중복 조건(예: 최소 거리 & 최대 거리)을 동시에 사용할 수 있습니다.
    private List<(IPatternCondition instance, float targetValue)> runtimeConditions = new();

    public void InitiaizePattern()
    {
        runtimeConditions.Clear();

        foreach (var condition in conditions)
        {
            IComparisonOperator op = ComparisonOperatorFactory.Create(condition.ctype);

            // 기본적으로 인스펙터에 적힌 값을 타겟값으로 세팅합니다.
            float targetValue = condition.value;

            if (condition.type == PatternConditionType.Distance)
            {
                targetValue = GetAttackRange();
            }

            IPatternCondition instance = condition.type switch
            {
                PatternConditionType.Cooldown => new CooldownCondition(op, targetValue),
                PatternConditionType.Health => new HealthCondition(op, targetValue),
                PatternConditionType.Distance => new DistanceCondition(op, targetValue),
                _ => throw new NotImplementedException($"Condition {condition.type} is not implemented.")
            };

            // 생성된 조건 인스턴스와 최종 결정된 비교 값을 함께 저장합니다.
            runtimeConditions.Add((instance, targetValue));
        }
    }

    public ActiveSkill GetActiveSkill()
    {
        ActiveSkill activeSkill = null;
        if (skill != null)
        {
            // skill.CreateSkill()의 반환형은 Skill이므로, ActiveSkill로 캐스팅 필요
            activeSkill = skill.CreateSkill() as ActiveSkill;
        }

#if UNITY_EDITOR
        if (activeSkill == null)
        {
            Debug.LogWarning($"[PatternEntry] '{slotName}'의 스킬이 생성되지 않았습니다.");
        }
#endif
        return activeSkill;
    }

    public void ResetCondition()
    {
        foreach ((IPatternCondition instance, float targetValue) in runtimeConditions)
        {
            instance.ResetCondition();
        }
    }

    public void Update(float time, AIContext ctx)
    {
        foreach (var cond in runtimeConditions)
        {
            // 💡 C# 7.0 패턴 매칭을 통해 가독성 향상
            if (cond.instance is IUpdatableCondition updatable)
            {
                updatable.Update(time);
            }
        }
    }

    public bool CheckUsePattern(AIContext ctx)
    {
        foreach (var cond in runtimeConditions)
        {
            // 💡 3. Short-Circuit (빠른 종료) 도입
            // 조건 중 단 하나라도 실패하면, 뒤의 조건은 볼 필요 없이 즉시 false 반환 (성능 최적화)
            if (!cond.instance.Evaluate(cond.targetValue, ctx))
            {
                return false;
            }
        }

        return true;
    }


    // 단순히 쿨타임/마나 등 '자체 준비'가 끝났는지 확인 (거리 무관)
    // (이전의 AIController에서 GetCurrentReadyAttackRange() 를 구현할 때 쓰기 위함)
    public bool IsReadyToUse(AIContext ctx)
    {
        // 여기서는 조건 중 '거리(Distance)'나 '타겟(Target)' 관련 조건을 뺀, 
        // 오직 쿨타임/체력 등 본인의 상태 조건만 체크하도록 커스텀 할 수도 있습니다.
        return CheckUsePattern(ctx);
    }

    public float GetAttackRange()
    {
        // 1. 스킬 데이터가 존재하고, 사거리 값이 유효하다면(0보다 크다면) 무조건 최우선으로 적용!
        if (skill != null && skill.Range > 0f)
        {
            return skill.Range;
        }

        // 2. 스킬에 사거리가 없다면, 인스펙터에서 수기로 작성했던 Distance 조건을 찾습니다.
        foreach (var condition in conditions)
        {
            if (condition.type == PatternConditionType.Distance)
            {
                return condition.value;
            }
        }

        // 3. 둘 다 설정되어 있지 않다면 기본 근접 공격 사거리로 간주 (안전 장치)
        return 2.0f;
    }
}