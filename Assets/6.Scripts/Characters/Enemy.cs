using System.Collections;
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

    private DamageHandleComponent damageHandle;
    private LaunchComponent launch;

    protected ActionComponent currentAction;

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
    }

    protected override void Start()
    {
        base.Start();
        SetGenericTeamId(2);
        BattleManager.Instance.ResistEnemy(this);
    }

    protected virtual void OnDisable()
    {
        ObjectPooler.ReturnToPool(gameObject);
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
        
        damageHandle?.OnDamage(damageEvent);

        // Look Attacker 
        LookAttacker(attacker);
        ApplyLaunch(attacker, causer, damageEvent?.hitData);

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

        animator.SetTrigger("Dead");
        MovableStopper.Instance.Delete(this);
        MovableSlower.Instance.Delete(this);
        Destroy(gameObject, 5);
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

    protected override void End_Damaged()
    {
        base.End_Damaged();

        state?.SetIdleMode();
        currentAction?.EndDoAction();
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
}
