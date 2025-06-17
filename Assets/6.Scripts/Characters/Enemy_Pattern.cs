using System.Collections.Generic;
using UnityEngine;

public abstract class Enemy_Pattern 
    : Enemy
{
    protected SkillComponent skillComp;
    private readonly List<PatternEntry> patterns = new();

    protected override void Awake()
    {
        base.Awake();
    }

    /// <summary>
    ///  스킬 컴포넌트와 패턴을 연결
    ///  몬스터가 Awake()나 Start()에서 호출해야함 
    /// </summary>
    public void Initialized(SkillComponent skill)
    {
        skillComp = skill;
        DefinePatterns();
    }

    protected abstract void DefinePatterns();

    /// <summary>
    ///  패턴 등록 헬퍼
    /// </summary>
    protected void AddPattern(string slot, ActiveSkill skill, List<PatternCondition> conditions)
    {
        if (skillComp == null) return;

        skillComp.SetActiveSkill(slot, skill);

        patterns.Add(new PatternEntry
        {
            conditions = conditions,
            skill = skill,
        });

    }
}
