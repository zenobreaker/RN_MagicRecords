using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;

[RequireComponent(typeof(PerceptionComponent))]
[RequireComponent(typeof(AIBehaviourComponent))]
public class AIController : MonoBehaviour
{
    [SerializeField] protected List<PatternEntry> patternEntries = new();
    [SerializeField] protected PatternEntry defaultAttack;

    protected AIContext context;

    // 💡 삭제: 이제 조종할 대상의 액션을 강제로 갈아끼울 필요가 없으므로 IActionable 삭제!
    // protected IActionable actor;  

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

        // 💡 훌륭한 이벤트 연결! (그대로 유지)
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

        // 💡 여기가 바로 새 아키텍처의 꽃입니다!
        // 갈아끼울 필요 없이 그냥 찌르기만 하면 끝납니다.
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
        // 💡 1. 상태가 Idle이 아니면 실행 불가 (기절, 피격 등 방어)
        if (state != null && state.IdleMode == false) return;

        // 💡 2. [핵심 방어선] 
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
}