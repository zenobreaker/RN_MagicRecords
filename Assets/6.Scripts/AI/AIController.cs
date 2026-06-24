using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;

public enum AICombatStyle
{
    RANGE,     // 원거리 유지형 (쿨타임이어도 최대 거리에서 대기. 다가오면 근접 공격으로 방어)
    HYBRID,  // 하이브리드 돌진형 (원거리 쏘고 쿨타임이면, 즉시 근접 공격하러 달려감)
}

[RequireComponent(typeof(PerceptionComponent))]
[RequireComponent(typeof(AIBehaviourComponent))]
public class AIController : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private AICombatStyle combatStyle = AICombatStyle.RANGE;

    [SerializeField] protected List<PatternEntry> patternEntries = new();
    [SerializeField] protected PatternEntry defaultAttack;

    protected AIContext context;

    protected BehaviorGraphAgent bgAgent;
    protected PerceptionComponent perception;
    protected AIBehaviourComponent aiBehaivour;
    protected StateComponent state;
    protected SkillComponent skill;
    protected WeaponComponent weapon;

    private PatternEntry currentPattern;

    public StateComponent State => state;

    protected virtual void Awake()
    {
        bgAgent = GetComponent<BehaviorGraphAgent>();
        perception = GetComponent<PerceptionComponent>();
        aiBehaivour = GetComponent<AIBehaviourComponent>();
        state = GetComponent<StateComponent>();
        skill = GetComponent<SkillComponent>();
        weapon = GetComponent<WeaponComponent>();

        context = new AIContext(this.gameObject);
    }

    protected void Start()
    {
        foreach (PatternEntry entry in patternEntries)
            AddPattern(entry);

        defaultAttack?.InitiaizePattern();

        if (skill != null)
        {
            skill.OnEndDoAction += () =>
            {
                currentPattern?.ResetCondition();
            };
        }

        if (weapon != null)
        {
            weapon.OnEndDoAction += () =>
            {
                currentPattern?.ResetCondition();
            };
        }
    }

    public void Update()
    {
        UpdateContext();

        if (aiBehaivour != null)
        {
            // 💡 쿨타임 상황과 무관하게, 이 몬스터의 '전투 포지션'을 세팅.
            float targetRange = GetEngagementRange();
            aiBehaivour.SetAttackDistance(targetRange);
        }

        foreach (var pattern in patternEntries)
        {
            pattern.Update(Time.deltaTime, context);
        }
        defaultAttack?.Update(Time.deltaTime, context);

        if (perception == null || aiBehaivour == null) return;

        if (aiBehaivour.GetCanMove() == false)
            return;

        if (aiBehaivour.GetTarget() == null)
        {
            GameObject target = perception.GetTarget();
            aiBehaivour.SetTarget(target);
        }
    }

    private void ExecutePattern(PatternEntry patternEntry)
    {
        if (patternEntry == null) return;

        currentPattern = patternEntry;

        if (patternEntry.slotName != "default")
        {
            skill.UseSkill(patternEntry.slotName.ToUpper());
        }
        else
        {
            weapon.DoActionWithIndex();
        }
    }

    public void DoAction()
    {
        // 상태가 Idle이 아니면 실행 불가 (기절, 피격 등 방어)
        if (state != null && state.IdleMode == false) return;

        // 이미 평타를 치고 있거나 스킬을 쓰는 중(InAction)이라면,
        // 새로운 패턴을 찾거나 덮어쓰지 않고 즉시 리턴합니다!
        if ((weapon != null && weapon.InAction) || (skill != null && skill.InAction))
            return;

        bool skillExecuted = false;

        foreach (var pattern in patternEntries)
        {
            if (pattern.CheckUsePattern(context))
            {
                ExecutePattern(pattern);
                skillExecuted = true;
                break;
            }
        }

        if (!skillExecuted && defaultAttack != null && defaultAttack.CheckUsePattern(context))
        {
            ExecutePattern(defaultAttack);
        }
    }

    private void UpdateContext()
    {
        if (context == null)
        {
            context = new AIContext(this.gameObject);
        }
        context.Target = perception.GetTarget();
    }

    private void AddPattern(PatternEntry entry)
    {
        if (skill == null) return;
        if (entry == null) return;

        entry.InitiaizePattern();
        skill.SetActiveSkill(entry.slotName.ToUpper(), entry.GetActiveSkill());
    }

    public void InjectCompanionSkills(List<SO_ActiveSkillData> equippedSkills)
    {
        // 1. 기존 에디터에 세팅된 멍청한 패턴을 싹 지웁니다.
        patternEntries.Clear();
        //skill.ClearAllSkills(); // (스킬 컴포넌트 내부의 슬롯 초기화 함수가 있다고 가정)

        // 2. 장착된 스킬들을 순회하며 AI가 쓸 수 있는 '패턴'으로 변환합니다.
        for (int i = 0; i < equippedSkills.Count; i++)
        {
            var equippedSkill = equippedSkills[i];
            if (equippedSkill == null) continue;

            // 새로운 패턴 껍데기 생성
            PatternEntry dynamicPattern = new PatternEntry();
            dynamicPattern.slotName = $"COMPANION_SKILL_{i}"; // 슬롯 이름 동적 할당
            dynamicPattern.skill = equippedSkill;

            // 💡 [핵심] 스킬 데이터에 적힌 사거리(Range) 값을 읽어와서,
            // "타겟과의 거리가 사거리 이하일 때 발동해라" 라는 조건을 코드로 달아줍니다!
            PatternCondition distanceCondition = new PatternCondition
            {
                type = PatternConditionType.Distance,
                ctype = ComparisonType.LessThanOrEqual,
                value = equippedSkill.Range // 스킬의 고유 사거리
            };
            dynamicPattern.conditions.Add(distanceCondition);

            // 💡 [추가] 쿨타임 조건도 코드로 추가 (스킬에 쿨타임 데이터가 있다면)
            PatternCondition cooldownCondition = new PatternCondition
            {
                type = PatternConditionType.Cooldown,
                ctype = ComparisonType.GreaterThanOrEqual,
                value = equippedSkill.GetCooldown()
            };
            dynamicPattern.conditions.Add(cooldownCondition);

            // 3. 완성된 패턴을 AIController에 등록!
            AddPattern(dynamicPattern);
        }

        // BT가 이 동적으로 변한 사거리를 읽어갈 수 있도록 갱신
        if (bgAgent != null)
        {
            aiBehaivour.SetAttackDistance(GetCurrentReadyAttackRange());    
        }
    }

    // 현재 가진 스킬 중 '가장 긴 사거리'를 몬스터의 기본 포지션으로 잡습니다.
    public float GetEngagementRange()
    {
        if (combatStyle == AICombatStyle.HYBRID)
        {
            // [마법검사 성향] : 지금 당장 때릴 수 있는 스킬의 사거리로 계속 파고듭니다.
            return GetCurrentReadyAttackRange();
        }
        else
        {
            // [스나이퍼 성향] : 쿨타임과 무관하게 자기가 가진 가장 긴 사거리를 유지합니다.
            return GetMaxRange();
        }
    }


    private float GetMaxRange()
    {
        float maxRange = defaultAttack != null ? defaultAttack.GetAttackRange() : 1.5f;
        foreach (var pattern in patternEntries)
        {
            float range = pattern.GetAttackRange();
            if (range > maxRange) maxRange = range;
        }
        return maxRange;
    }

    private float GetCurrentReadyAttackRange()
    {
        // 1. 특수 패턴(스킬)들 중 쿨타임이 다 돌았고, 사용 가능한 상태인 첫 번째 패턴의 사거리를 찾습니다.
        // (PatternEntry 클래스 내부에 Range 속성과 IsReady() 같은 쿨타임 체크 함수가 있다고 가정)
        foreach (var pattern in patternEntries)
        {
            // 거리 조건(CheckUsePattern)이 아니라, 단순히 쿨타임/마나 등 '준비 상태'만 체크합니다.
            if (pattern.IsReadyToUse(context))
            {
                return pattern.GetAttackRange(); // ex) 원거리 스킬이 준비되었으면 10 반환
            }
        }

        // 2. 만약 모든 스킬이 쿨타임이라면, 평타(기본 근접 공격)의 사거리를 반환합니다.
        if (defaultAttack != null)
        {
            return defaultAttack.GetAttackRange(); // ex) 평타면 2 반환
        }

        return 1.5f; // 최후의 기본값 (안전 장치)
    }

}