using System.Collections.Generic;
using UnityEngine;

// Reaction timing stays with the existing state owner; no second state machine is needed.
public partial class StateComponent
{
    [Header("Hit Reaction")]
    [SerializeField, Min(0f)] private float lightReactionTime = 0.08f;
    [SerializeField, Min(0f)] private float heavyReactionTime = 0.2f;
    [SerializeField, Min(0f)] private float knockbackReactionTime = 0.15f;
    [SerializeField, Min(0f), Tooltip("Heavy/Knockback 종료 후 추가 반응을 무시하는 시간. 피해는 받습니다.")]
    private float reactionRecoveryTime = 0.15f;
    [SerializeField] private HitReactionResistance baseReactionResistance;
    [SerializeField, Tooltip("기존 보스의 피격 반응 면역을 유지합니다.")]
    private bool preserveBossReactionResistance = true;

    private readonly Dictionary<object, HitReactionResistance> reactionResistanceSources = new();
    private LaunchComponent launch;
    private SkillComponent skills;
    private float reactionTimeRemaining;
    private float reactionImmunityRemaining;
    private bool reactionOwnsState;
    private bool legacyDamageAnimation;

    public HitReactionType CurrentHitReaction { get; private set; }
    public bool IsReactionImmune => reactionImmunityRemaining > 0f;
    public HitReactionResistance BaseReactionResistance
    {
        get => baseReactionResistance;
        set => baseReactionResistance = value;
    }
    public HitReactionResistance EffectiveReactionResistance
    {
        get
        {
            var result = baseReactionResistance;
            if (preserveBossReactionResistance && character is Enemy enemy && enemy.Boss)
                result |= HitReactionResistance.All;
            foreach (var resistance in reactionResistanceSources.Values) result |= resistance;
            return result;
        }
    }

    // Buffs and cloned skill modules own separate keys; one removal cannot clear another grant.
    public void SetReactionResistance(object source, HitReactionResistance resistance)
    {
        if (source == null) return;
        if (resistance == HitReactionResistance.None) reactionResistanceSources.Remove(source);
        else reactionResistanceSources[source] = resistance;
    }

    public void RemoveReactionResistance(object source)
    {
        if (source != null) reactionResistanceSources.Remove(source);
    }

    public HitReactionType ResolveHitReaction(DamageEvent damageEvent)
    {
        if (damageEvent == null || !isActiveAndEnabled || DeadMode || StopMode ||
            DamagedMode || IsReactionImmune ||
            (statusEffect != null && !statusEffect.GetMovableCondition())) return HitReactionType.None;
        HitReactionType requested = damageEvent.Reaction;
        HitReactionResistance flag = requested switch
        {
            HitReactionType.Light => HitReactionResistance.Light,
            HitReactionType.Heavy => HitReactionResistance.Heavy,
            HitReactionType.Knockback => HitReactionResistance.Knockback,
            _ => HitReactionResistance.None,
        };
        if (flag == HitReactionResistance.None || (EffectiveReactionResistance & flag) != 0)
            return HitReactionType.None;
        if (requested == HitReactionType.Light && CurrentHitReaction == HitReactionType.Light)
            return HitReactionType.None;
        return requested;
    }

    public HitReactionType ApplyHitReaction(DamageEvent damageEvent, GameObject attacker, Vector3 hitPoint)
    {
        var reaction = ResolveHitReaction(damageEvent);
        if (reaction == HitReactionType.None) return reaction;

        CurrentHitReaction = reaction;
        legacyDamageAnimation = false;
        reactionTimeRemaining = Mathf.Max(0f, reaction switch
        {
            HitReactionType.Light => lightReactionTime,
            HitReactionType.Heavy => heavyReactionTime,
            _ => knockbackReactionTime,
        });
        reactionOwnsState = reaction != HitReactionType.Light;
        if (reactionOwnsState)
        {
            reactionImmunityRemaining = reactionTimeRemaining + Mathf.Max(0f, reactionRecoveryTime);
            ChangeType(StateType.Damaged);
            // State entry cancels skill movement before knockback takes control of the body.
            if (reaction == HitReactionType.Knockback)
                launch?.ApplyKnockback(attacker, hitPoint, damageEvent.hitData.Distance, reactionTimeRemaining);
        }

        // Existing Hit is full-body. Light never replaces an ongoing skill animation.
        if (reactionOwnsState || (IdleMode && !(skills != null && skills.InAction)))
            character?.Visual?.PlayDamageAnimation(damageEvent.hitData, reaction);
        return reaction;
    }

    private void Update()
    {
        reactionImmunityRemaining = Mathf.Max(0f, reactionImmunityRemaining - Time.deltaTime);
        if (CurrentHitReaction == HitReactionType.None) return;
        reactionTimeRemaining -= Time.deltaTime;
        if (reactionTimeRemaining > 0f) return;
        bool restoreState = reactionOwnsState && DamagedMode;
        CancelHitReaction();
        if (restoreState)
        {
            if (statusEffect != null && !statusEffect.GetMovableCondition()) SetStopMode();
            else SetIdleMode();
        }
    }

    public bool EndDamageAnimation()
    {
        if (!legacyDamageAnimation || reactionOwnsState || !DamagedMode) return false;
        legacyDamageAnimation = false;
        if (statusEffect != null && !statusEffect.GetMovableCondition()) SetStopMode();
        else SetIdleMode();
        return true;
    }

    private void CancelHitReaction()
    {
        CurrentHitReaction = HitReactionType.None;
        reactionTimeRemaining = 0f;
        reactionOwnsState = false;
        legacyDamageAnimation = false;
        launch?.CancelLaunch();
    }

    private void OnDisable()
    {
        CancelHitReaction();
        reactionImmunityRemaining = 0f;
        reactionResistanceSources.Clear();
    }

    private void OnEnable()
    {
        // Notify AI/movement subscribers on reuse instead of silently changing the state field.
        if (!IdleMode) SetIdleMode();
    }
}
