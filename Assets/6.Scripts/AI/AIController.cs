using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using Unity.Behavior;
using UnityEngine;


/// <summary>
/// AI 기능을 수행하게 만드는 Controller 
/// </summary>
[RequireComponent(typeof(PerceptionComponent))]
[RequireComponent(typeof(AIBehaviourComponent))]
public class AIController : MonoBehaviour
{
    [SerializeField] protected List<PatternEntry> patternEntries = new();
    [SerializeField] protected PatternEntry defaultAttack;

    protected AIContext context;
    protected IActionable actor;  // 조종할 대상이 액션컴포넌트를 이용하는 자인가?

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
   
        actor = GetComponent<IActionable>();

        context = new AIContext(this.gameObject);
    }

    protected void Start()
    {
        foreach (PatternEntry entry in patternEntries)
            AddPattern(entry);

        defaultAttack?.InitiaizePattern();
        // 초기에 액션을 무기로 지정
        if (actor != null)
            actor.SetActionComponent(weapon);

        if(skill != null)
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

        // 패턴 조건 검사
        foreach (var pattern in patternEntries)
        {
            // 패턴 내부의 Cooldown 등 시간 업데이트
            pattern.Update(Time.deltaTime, context);
        }
        defaultAttack.Update(Time.deltaTime, context);

        if (perception == null || aiBehaivour == null) return;

        if (aiBehaivour.GetCanMove() == false)
            return;

        if (aiBehaivour.GetTarget() == null)
        {
            GameObject target = perception.GetTarget();
            aiBehaivour.SetTarget(target);
        }
    }

    // 패턴 수행
    private void ExecutePattern(PatternEntry patternEntry)
    {
        if (patternEntry == null) return;

        // 스킬컴포넌트의 경우 가지고 있는 스킬들을 살피며 사용할 수 있는지 검사 후에 공격
        // 스킬이 수행하지 않으면 웨폰을 수행
        currentPattern = patternEntry; 
        if (patternEntry.slotName != "default")
        {
            actor?.SetActionComponent(skill);
            skill.UseSkill(patternEntry.slotName.ToUpper());
        }
        else
        {
            actor?.SetActionComponent(weapon);
            weapon.DoAction();
        }
    }

    public void DoAction()
    {
        if (state != null && state.IdleMode == false) return;

        bool skillExecuted = false;
        // 패턴 조건 검사
        foreach (var pattern in patternEntries)
        {
            // 조건 충족 확인
            if (pattern.CheckUsePattern(context))
            {
                ExecutePattern(pattern);
                skillExecuted = true;
                break; // 우선순위가 높은 것 하나만 실행하고 탈출
            }
        }

        // 실행된 스킬이 없고 평타 조건이 맞으면 평타 실행 
        if (!skillExecuted && defaultAttack.CheckUsePattern(context))
        {
            ExecutePattern(defaultAttack);
        }
    }

    private void UpdateContext()
    {
        if (context == null)
        {
            // 컨텍스트가 사라졌다면 다시 만든다. 
            context = new AIContext(this.gameObject);
        }

        // 타겟 정보 입력 
        context.Target = perception.GetTarget();
    }

    private void AddPattern(PatternEntry entry)
    {
        if (skill == null) return;
        if (entry == null) return;

        entry.InitiaizePattern();

        skill.SetActiveSkill(entry.slotName.ToUpper(), entry.GetActiveSkill());
    }
}
