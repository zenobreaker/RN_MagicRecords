using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Bson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Enemy
    : Character
    , IDamagable
    , ILaunchable
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

    private List<ActionComponent> actionComponents = new();
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

        damageHandle = GetComponent<DamageHandleComponent>();
        launch = GetComponent<LaunchComponent>();
        skill = GetComponent<SkillComponent>();
        weapon = GetComponent<WeaponComponent>();

        if (weapon != null) actionComponents.Add(weapon);
        if (skill != null) actionComponents.Add(skill);
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

    protected override void OnDisable()
    {
        base.OnDisable();

        if (state != null)
            state.OnStateTypeChanged -= ChangeType; 

        BattleManager.Instance?.UnreistEnemy(this);
        CancelInvoke();
        ObjectPooler.ReturnToPool(gameObject);
    }

    public override void Start_DoAction()
    {
        base.Start_DoAction();
        foreach (var ac in actionComponents)
            if (ac.InAction) ac.StartAction();
    }

    public override void End_DoAction()
    {
        base.End_DoAction();
        bInAction = false;
        foreach (var ac in actionComponents)
            if (ac.InAction) ac.EndDoAction();
        
        OnEndDoAction?.Invoke();
    }

    public override void Begin_JudgeAttack(AnimationEvent e)
    {
        base.Begin_JudgeAttack(e);
        foreach (var ac in actionComponents)
            if (ac.InAction) ac.BeginJudgeAttack(e);
    }

    public override void End_JudgeAttack(AnimationEvent e)
    {
        base.End_JudgeAttack(e);
        foreach (var ac in actionComponents)
            if (ac.InAction) ac.EndJudgeAttack(e);
    }

    public void OnDamage(GameObject attacker,
        Weapon causer, Vector3 hitPoint, DamageEvent damageEvent)
    {
        if (healthPoint != null && healthPoint.Dead)
            return;
        
        damageHandle?.OnDamage(attacker, damageEvent);

        // Look Attacker 
        LookAttacker(attacker);
        ApplyLaunch(attacker, causer, damageEvent);

        if (healthPoint.Dead == false)
            return;

        // Dead..
        state?.SetDeadMode();
        Collider collider = GetComponent<Collider>();
        collider.isTrigger = true;
        rigidbody.isKinematic = true;

        visual?.PlayDeadAnimation();
        HandleDeath().Forget();
    }


    // 💡 IEnumerator -> async UniTaskVoid 로 변경
    private async UniTaskVoid Change_Color(float time, CancellationToken token)
    {
        try
        {
            foreach (Material mat in skinMaterials)
            {
                if (mat != null) mat.color = damageColor;
            }

            await UniTask.Delay(TimeSpan.FromSeconds(time), cancellationToken: token);

            int index = 0;
            foreach (Material mat in skinMaterials)
            {
                if (mat != null) mat.color = originColors[index];
                index++;
            }
        }
        catch (OperationCanceledException)
        {
            // 💡 몹이 죽거나 파괴되어 취소되었을 때 색깔 원상복구
            int index = 0;
            foreach (Material mat in skinMaterials)
            {
                if (mat != null) mat.color = originColors[index];
                index++;
            }
        }
    }

    public override void End_Damaged()
    {
        base.End_Damaged();

        state?.SetIdleMode();
        foreach (var ac in actionComponents)
            ac.EndDoAction();
    }

    private async UniTaskVoid HandleDeath()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(2.0f));
        Dead(); 
    }

    protected override void Dead()
    {
        base.Dead();
        gameObject?.SetActive(false);
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

    public void ApplyLaunch(GameObject attacker, Weapon causer, DamageEvent devt)
    {
        ApplyLaunch(attacker, causer, devt?.hitData);
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
        status.SetStatusValue(StatusType.ATTACKSPEED, 1.0f);

        if (healthPoint != null)
            healthPoint.SetMaxHP = statData.hp;
    }
}
