using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>현재 마탄 버프를 유지하고 일곱 번 발사할 때마다 다음 종류로 교체합니다.</summary>
public sealed class RecordBulletModeEffect : RecordDefinedEffect
{
    public const string EffectID = "MythBulletMode";
    private int fired;
    private readonly Sprite[] modeIcons;
    private readonly Sprite fallbackIcon;
    public override bool HasPersistentHUDState => true;
    public override bool ShowSingleStack => true;
    public override int DisplayStackCount => fired;
    public float RapidCooldownReduction
    {
        get;
    }
    public int Mode
    {
        get;
        private set;
    } = 1;
    private static readonly string[] Names =
    {
        "필중의 마탄", "관통의 마탄", "분열의 마탄", "파열의 마탄", "속사의 마탄", "집행의 마탄", "운명의 마탄"
    };
    /// <summary>마탄 종류별 아이콘을 보관하고 현재 종류를 바로 표시합니다.</summary>
    public RecordBulletModeEffect(SO_BaseEffect definition, float rapidCooldownReduction = 0.3f,
        Sprite[] modeIcons = null) : base(EffectID, definition, 0f)
    {
        RapidCooldownReduction = rapidCooldownReduction;
        this.modeIcons = modeIcons != null ? (Sprite[])modeIcons.Clone() : Array.Empty<Sprite>();
        fallbackIcon = FxIcon;
        RefreshModeDisplay();
    }

    /// <summary>현재 마탄의 이름과 전용 아이콘을 함께 갱신합니다.</summary>
    private void RefreshModeDisplay()
    {
        Name = Names[Mode - 1];
        Sprite icon = Mode <= modeIcons.Length ? modeIcons[Mode - 1] : null;
        FxIcon = icon != null ? icon : fallbackIcon;
    }

    /// <summary>발사 수를 표시하고 일곱 발째에 다음 마탄으로 전환하며 표시 수치를 초기화합니다.</summary>
    public void CountShot()
    {
        fired++;
        if (fired >= 7)
        {
            fired = 0;
            Mode = Mode % 7 + 1;
            RefreshModeDisplay();
        }

        owner.SafeInvoke(target => target.GetComponent<EffectComponent>())
            .SafeInvoke(effects => effects.RefreshEffectUI(this));
    }
}

[Serializable]
[ModuleCategory("Record/탄환/마력탄의 사수")]
public sealed class RecordMythBulletModule : RecordEventModule
{
    public SO_BaseEffect buff;
    [Tooltip("필중, 관통, 분열, 파열, 속사, 집행, 운명의 마탄 순서입니다.")]
    public Sprite[] modeIcons = Array.Empty<Sprite>();
    public SO_BaseEffect mark;
    public float markChance = 0.5f;
    public float markDuration = 5f;
    public float normalMarkBonus = 0.1f;
    public float fateMarkBonus = 0.3f;
    public float homingRadius = 8f;
    public float homingTurnSpeed = 10f;
    public string childProjectileTag = "Bullet";
    public int childCount = 3;
    public float childRatio = 0.3f;
    public float explosionRadius = 3f;
    public float explosionRatio = 0.5f;
    public float missingHealthBonus = 0.4f;
    public float rapidCooldownReduction = 0.3f;

    public override void OnAcquire(GameObject skillOwner, int level)
    {
        base.OnAcquire(skillOwner, level);
        ApplyOwned(new RecordBulletModeEffect(buff, rapidCooldownReduction, modeIcons));
    }

    /// <summary>동시 발사 패턴은 한 발사 단위이며 모든 투사체가 같은 버프 스냅샷을 사용합니다.</summary>
    public static IReadOnlyList<BulletData> PrepareShot(GameObject owner, IReadOnlyList<BulletData> bullets)
    {
        if (owner == null || bullets == null || bullets.Count == 0 ||
            owner.GetComponent<EffectComponent>()?.HasEffect(RecordBulletModeEffect.EffectID) is not RecordBulletModeEffect mode)
        {
            return bullets;
        }
        BulletData[] shot = new BulletData[bullets.Count];
        for (int i = 0; i < bullets.Count; i++)
        {
            shot[i] = bullets[i];
            shot[i].recordMode = mode.Mode;
        }
        if (mode.Mode == 5)
        {
            RecordRuntime.ReduceCooldowns(owner, mode.RapidCooldownReduction);
        }
        mode.CountShot();
        return Array.AsReadOnly(shot);
    }

    protected override void BeforeOwnHit(GameObject target, DamageEvent hit)
    {
        if (hit.SpecialBulletMode != 6)
        {
            return;
        }
        HealthPointComponent health = target.GetComponent<HealthPointComponent>();
        if (health != null && health.GetMaxHP > 0f)
        {
            hit.BaseDamage *= 1f + missingHealthBonus * Mathf.Clamp01(1f - health.GetCurrentHP / health.GetMaxHP);
        }
    }

    protected override void AfterOwnHit(GameObject target, DamageEvent hit, float damage)
    {
        if (UnityEngine.Random.value >= markChance || target.GetComponent<HealthPointComponent>()?.Dead != false)
        {
            return;
        }
        EffectComponent targetEffects = target.GetComponent<EffectComponent>();
        if (targetEffects == null)
        {
            return;
        }
        float bonus = hit.SpecialBulletMode == 7 ? fateMarkBonus : normalMarkBonus;
        if (targetEffects.HasEffect("RecordMark") is RecordMarkEffect existing && existing.DamageBonus < bonus)
        {
            targetEffects.RemoveEffect(existing);
        }
        ApplyRecordEffect(targetEffects, new RecordMarkEffect("RecordMark", mark, markDuration, bonus), target);
    }

    public override void OnSpawnObject(ISkillEffect spawnedObject, ActiveSkill casterSkill)
    {
        if (casterSkill?.Owner == null || casterSkill.Owner.gameObject != owner || spawnedObject is not BaseProjectile projectile ||
            projectile.ConsumedBullets.Count == 0)
        {
            return;
        }
        int mode = projectile.ConsumedBullets[0].recordMode;
        if (mode == 1)
        {
            AddHoming(projectile);
        }
        else if (mode == 2 && projectile is IProjectile pierce)
        {
            pierce.PierceCount += 3;
        }
        else if (mode == 3)
        {
            bool split = false;
            projectile.OnTargetHitEvent += (target, position) =>
            {
                if (split)
                {
                    return;
                }
                split = true;
                Split(projectile, target, position);
            };
        }
        else if (mode == 4)
        {
            HashSet<GameObject> exploded = new();
            projectile.OnTargetHitEvent += (target, position) =>
            {
                if (exploded.Add(target))
                {
                    Explode(projectile, position);
                }
            };
        }
    }

    private void AddHoming(BaseProjectile projectile)
    {
        if (projectile.TryGetComponent<RuntimeHomingBehaviour>(out var homing) == false)
        {
            homing = projectile.gameObject.AddComponent<RuntimeHomingBehaviour>();
        }
        homing.Setup(homingRadius, homingTurnSpeed, LayerMask.GetMask("Enemy"), projectile.Ignores);
    }

    private void Split(BaseProjectile projectile, GameObject firstTarget, Vector3 position)
    {
        if (projectile.LastHit == null || projectile.Owner == null)
        {
            return;
        }
        List<Character> candidates = RecordRuntime.Enemies(owner, position, homingRadius);
        candidates.RemoveAll(candidate => candidate.gameObject == firstTarget);
        if (candidates.Count == 0)
        {
            return;
        }
        for (int i = 0; i < childCount; i++)
        {
            Vector3 direction = candidates[i % candidates.Count].transform.position - position;
            direction.y = 0f;
            Quaternion rotation = direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction) : Quaternion.identity;
            GameObject child = ObjectPooler.DeferredSpawnFromPool(childProjectileTag, position, rotation);
            if (child != null && child.TryGetComponent<BaseProjectile>(out var childProjectile))
            {
                DamageData childDamage = new DamageData
                {
                    baseDamage = projectile.LastHit.BaseDamage * childRatio,
                    CannotCrit = true,
                    SuppressRecordTriggers = true
                };
                childProjectile.SetDamageInfo(projectile.Owner, childDamage);
                childProjectile.AddIgnore(projectile.Owner);
                childProjectile.AddIgnore(firstTarget);
                AddHoming(childProjectile);
            }
            ObjectPooler.FinishSpawn(child);
        }
    }

    private void Explode(BaseProjectile projectile, Vector3 position)
    {
        if (projectile.LastHit == null || projectile.Owner == null)
        {
            return;
        }
        foreach (Character target in RecordRuntime.Enemies(owner, position, explosionRadius))
        {
            CombatHelper.ApplyDamage(projectile.Owner, new DamageEvent(projectile.LastHit.BaseDamage * explosionRatio)
            {
                SuppressRecordTriggers = true
            }, target.gameObject, position);
        }
    }
}
