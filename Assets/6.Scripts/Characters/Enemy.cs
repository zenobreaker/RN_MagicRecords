using Newtonsoft.Json.Bson;
using System.Collections;
using UnityEngine;

public class Enemy
    : Character
    , IDamagable
    , ILaunchable
    , IActionable
{
    [Header("Material Settings")]
    [SerializeField] private string[] surfaceNames;
    [SerializeField] private Color damageColor;
    [SerializeField] private float changeColorTime = 0.15f;

    private Color[] originColors;
    private Material[] skinMaterials;

    [SerializeField] private bool isBoss = false;
    public bool Boss { get => isBoss; set { isBoss = value; } }

    private DamageHandleComponent damageHandle;
    private LaunchComponent launch;
    protected ActionComponent currentAction;
    protected SkillComponent skill;
    protected WeaponComponent weapon;

    private MonsterGrade grade; 

    protected override void Awake()
    {
        base.Awake();

        int index = 0;
        skinMaterials = new Material[surfaceNames.Length];
        originColors = new Color[surfaceNames.Length];
        foreach (string name in surfaceNames)
        {
            Transform surface = transform.FindChildByName(name);
            if (surface == null)
                continue;

            if (surface.TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer skin))
            {
                skinMaterials[index] = skin.material;
                originColors[index] = skin.material.color;
            }

            index++;
        }

        currentAction = GetComponent<ActionComponent>();
        damageHandle = GetComponent<DamageHandleComponent>();
        launch = GetComponent<LaunchComponent>();

        skill = GetComponent<SkillComponent>();
        weapon = GetComponent<WeaponComponent>(); 

        Awake_SkillEventHandle(skill, weapon); 
    }

    private void Awake_SkillEventHandle(SkillComponent skill, WeaponComponent weapon)
    {
        if (skill == null || weapon == null) return;

        skill.OnSkillUse += OnSkillUse;
        skill.OnBeginDoAction += weapon.OnBeginSkillAction;
        skill.OnEndDoAction += weapon.OnEndSkillAction;
    }

    protected override void Start()
    {
        base.Start();
        SetGenericTeamId(2);
    }

    protected void OnEnable()
    {
        if (state != null)
            state.OnStateTypeChanged += ChangeType;

        BattleManager.Instance?.ResistEnemy(this);
    }

    protected virtual void OnDisable()
    {
        if (state != null)
            state.OnStateTypeChanged -= ChangeType; 

        BattleManager.Instance?.UnreistEnemy(this);
        CancelInvoke();
        ObjectPooler.ReturnToPool(gameObject);
    }

    public void OnSkillUse(bool bIsUse)
    {
        if (bIsUse)
        {
            SetActionComponent(skill); 
        }
    }

    public override void Start_DoAction()
    {
        base.Start_DoAction();
        currentAction?.StartAction();
    }

    public override void End_DoAction()
    {
        currentAction?.EndDoAction();
    }

    public override void Begin_JudgeAttack(AnimationEvent e)
    {
        base.Begin_JudgeAttack(e);
        currentAction?.BeginJudgeAttack(e);
    }

    public override void End_JudgeAttack(AnimationEvent e)
    {
        base.End_JudgeAttack(e);
        currentAction?.EndJudgeAttack(e);
    }

    public void OnDamage(GameObject attacker,
        Weapon causer, Vector3 hitPoint, DamageEvent damageEvent)
    {
        if (healthPoint != null && healthPoint.Dead)
            return;
        
        damageHandle?.OnDamage(attacker, damageEvent);

        // Look Attacker 
        LookAttacker(attacker);
        ApplyLaunch(attacker, causer, damageEvent.hitData);

        StartCoroutine(Change_Color(changeColorTime));

        if (healthPoint.Dead == false)
        {
            state?.SetDamagedMode();

            return;
        }

        // Dead..
        state?.SetDeadMode();
        Collider collider = GetComponent<Collider>();
        collider.isTrigger = true;
        rigidbody.isKinematic = true;

        animator?.SetTrigger("Dead");
        //MovableStopper.Instance.Delete(this);
        //MovableSlower.Instance.Delete(this);
        //Destroy(gameObject, 5);
        StartCoroutine(HandleDeath());
    }


    private IEnumerator Change_Color(float time)
    {
        foreach (Material mat in skinMaterials)
        {
            mat.color = damageColor;
        }

        yield return new WaitForSeconds(time);

        int index = 0;
        foreach (Material mat in skinMaterials)
        {
            mat.color = originColors[index];
            index++;
        }
    }

    public override void End_Damaged()
    {
        base.End_Damaged();

        state?.SetIdleMode();
        currentAction?.EndDoAction();
    }

    private IEnumerator HandleDeath()
    {
        yield return new WaitForSeconds(2.0f);
        Dead(); 
    }

    protected override void Dead()
    {
        base.Dead();
        gameObject.SetActive(false);
    }

    private void ChangeType(StateType prevType, StateType newType)
    {
        if(newType == StateType.Dead)
        {
            OnDead?.Invoke(this); 
        }
    }

    private void LookAttacker(GameObject attacker)
    {
        if (attacker == null) return;

        transform.LookAt(attacker.transform, Vector3.up);
    }

    public void ApplyLaunch(GameObject attacker, Weapon causer, HitData hitData)
    {
        launch?.ApplyLaunch(attacker, causer, hitData);
    }


    public void SetGrade(MonsterGrade monsterGrade)
    {
        grade = monsterGrade;
        if (grade == MonsterGrade.BOSS)
            isBoss = true; 
    }

    public void SetGrade(MonsterData data)
    {
        if(data != null)
            SetGrade(data.monsterGrade); 
    }

    public void SetStatData(MonsterStatData statData)
    {
        if (statData == null || status == null) return;

        status.SetStatusValue(StatusType.ATTACK, statData.attack);
        status.SetStatusValue(StatusType.DEFENSE, statData.defense);
        status.SetStatusValue(StatusType.MOVESPEED, statData.speed);
        
        if(healthPoint != null)
            healthPoint.SetMaxHP = statData.hp;
    }

    public void SetActionComponent(ActionComponent action)
    {
        currentAction = action; 
    }

    public ActionComponent GetCurrentAction()
    {
        return currentAction; 
    }
}
