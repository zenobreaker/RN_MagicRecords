using System;
using System.Collections.Generic;
using UnityEngine;


public enum WarningSpawnPattern
{
    Directional,   // 전방 기준 부채꼴/다방향
    Radial         // 360도 전방위
}

[ModuleCategory("Combat/Spawn Warning Sign")]
[Serializable]
public class Module_SpawnWarningSign : SkillModule, IWarningData
{
    [Header("Pattern Override")]
    [Tooltip("체크하면 인스펙터 값 대신 블랙보드의 값을 강제로 가져와서 씁니다.")]
    public bool useBlackboardPattern = false; // 💡 보통 장판 뒤에 오니까 기본값을 true로 두면 편합니다.

    [Header("Warning Sign")]
    public WarningSignType signType;
    public float duration = 1.0f;
    [Tooltip("체크하면 세팅된 위치 값에 표시")]
    public bool isSetTargetPos = false;

    [Header("Warning Rect Type")]
    public RectFillMode fillMode;

    [Header("Multi-Spawn Settings")]
    public WarningSpawnPattern spawnPattern = WarningSpawnPattern.Directional;

    public int fallbackSpawnCount = 1;
    public float fallbackAngleBetween = 0f;
    public float startAngleOffset = 0f;

    [Header("Lifecycle & Chain Action")]
    [Tooltip("워닝 사인이 끝날 때 다음 페이즈로 강제 이동할지 여부. (투사체 낙하 등 별도 흐름과 병렬 처리하려면 끄세요)")]
    public bool endPhaseOnFinish = true;
    [Tooltip("병렬 사인(endPhaseOnFinish 꺼짐)이 끝날 때까지 스킬의 정상 종료를 기다립니다. 강제 취소는 즉시 정리합니다.")]
    public bool waitForSignBeforeSkillEnd = true;

    [SerializeReference]
    [Tooltip("워닝 사인이 끝나는 시점에 그 위치에서 즉시 실행될 연계 모듈들 (예: 데미지 처리, 이펙트 스폰, 투사체 발사 등)")]
    public List<SkillModule> onSignEndModules = new();

    // --- Circle 전용 ---
    public float radius = 1.0f;

    // --- Rectangle 전용 ---
    public Vector2 rectSize = Vector2.one;
    public Vector2 maxRectSize = Vector2.one;

    // --- Fan(부채꼴) 전용 ---
    public float fanRadius = 2.0f;
    [Range(0, 360)] public float fanAngle = 90f;

    public float Radius => radius;
    public Vector2 RectSize => rectSize;
    public Vector2 MaxRectSize => maxRectSize;

    public float FanAngle => fanAngle;
    public float FanRadius => fanRadius;

    public override void Init(Character owner)
    {
        if (onSignEndModules == null) return;
        foreach (var module in onSignEndModules) module?.Init(owner);
    }

    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        if (owner == null || skill == null || phaseSkill == null || !skill.IsActive || skill.IsEnding) return;


        // 값을 결정합니다 (인스펙터 값 쓸래? 블랙보드 값 쓸래?)
        int baseCount = skill.Runtime.Base.PatternCount > 0
            ? skill.Runtime.Base.PatternCount
            : fallbackSpawnCount;

        int finalSpawnCount =
            baseCount + skill.Runtime.Combat.PatternCountBonus;


        float finalAngleBetween =
            skill.Runtime.Base.PatternAngle > 0
            ? skill.Runtime.Base.PatternAngle
            : fallbackAngleBetween;

        //  만약 내가 인스펙터 값을 썼다면, 다음 페이즈를 위해 블랙보드에 갱신
        if (!useBlackboardPattern)
        {
            skill.Runtime.Base.PatternCount = finalSpawnCount;
            skill.Runtime.Base.PatternAngle = finalAngleBetween;
        }

        Vector3 basePosition = skill.Runtime.Spawn.TargetPosition;
        if (isSetTargetPos == false)
            basePosition = owner.transform.position;

        basePosition.y += 0.01f;// y축 보정

        // 여러 개의 장판 중 몇 개가 끝나는지 카운팅하기 위한 변수
        int finishedCount = 0;
        bool ownsPhase = endPhaseOnFinish;
        var lifetime = ownsPhase ? skill.PhaseToken : skill.SkillToken;
        var runtime = skill.Runtime;
        int spawnedCount = 0;
        int phaseVersion = skill.PhaseVersion;

        for (int i = 0; i < finalSpawnCount; i++)
        {
            Quaternion rotation = GetSpawnRotation(
                owner.transform,
                i,
                finalSpawnCount,
                finalAngleBetween
            );

            WarningSign sign =
                ObjectPooler.DeferredSpawnFromPool<WarningSign>(
                    GetSignName(),
                    basePosition,
                    rotation
                );

            int patternIndex = i;

            if (sign != null)
            {
                SetFillMode(sign);
                sign.Setup(this, duration);

                spawnedCount++;
                Action releaseCompletion = !ownsPhase && waitForSignBeforeSkillEnd
                    ? skill.HoldCompletion() : () => { };
                bool handled = false;
                sign.OnEndSign = () =>
                {
                    if (handled) return;
                    handled = true;
                    if (lifetime.IsCancellationRequested || !skill.IsActive || skill.IsEnding ||
                        (ownsPhase && !skill.IsCurrentPhase(phaseVersion))) return;
                    // Validate the lease first: a canceled callback must not untrack a reused sign.
                    skill.RemoveTrackedEffect(sign.gameObject);
                    int callbackPhaseVersion = skill.PhaseVersion;
                    var context = new SkillChainContext
                    {
                        Position = sign.transform.position,
                        Rotation = sign.transform.rotation,
                        PatternIndex = patternIndex,
                        WarningSign = sign
                    };
                    Vector3 previousTarget = runtime.Spawn.TargetPosition;
                    try
                    {
                        if (onSignEndModules != null)
                            foreach (var chainModule in onSignEndModules)
                            {
                                if (lifetime.IsCancellationRequested || !skill.IsCurrentPhase(callbackPhaseVersion)) break;
                                if (chainModule == null) continue;
                                runtime.Spawn.TargetPosition = context.Position;
                                chainModule.OnChainNotify(owner, skill, phaseSkill, context);
                            }
                    }
                    finally
                    {
                        // Do not overwrite a new phase's target or a new cast's Runtime.
                        if (ReferenceEquals(runtime, skill.Runtime) && skill.IsCurrentPhase(callbackPhaseVersion))
                            runtime.Spawn.TargetPosition = previousTarget;
                        releaseCompletion();
                    }
                    finishedCount++;
                    if (ownsPhase && finishedCount >= spawnedCount &&
                        !lifetime.IsCancellationRequested && skill.IsCurrentPhase(phaseVersion))
                        skill.EndPhaseAndNext();
                };
                skill.AddTrackedEffect(sign.gameObject, untilSkillEnd: !ownsPhase);
                sign.OnStopped = () =>
                {
                    if (!lifetime.IsCancellationRequested) skill.RemoveTrackedEffect(sign.gameObject);
                    releaseCompletion();
                };

                ObjectPooler.FinishSpawn(sign.gameObject);


            }
        } // for(i) end
        if (spawnedCount == 0 && ownsPhase && skill.IsCurrentPhase(phaseVersion))
            skill.EndPhaseAndNext();
    }

    private string GetSignName()
    {
        return signType switch
        {
            WarningSignType.Circle => "WarningSign_Circle",
            WarningSignType.Rectangle => "WarningSign_Rect",
            WarningSignType.Fan => "WarningSign_Fan",
            _ => "",
        };
    }

    public override bool ControlsPhaseLifecycle() => endPhaseOnFinish;

    private void SetFillMode(WarningSign sign)
    {
        if (sign == null)
            return;

        if (sign.TryGetComponent<WarningSign_Rect>(out var rect))
            rect.fillMode = fillMode;
    }

    private Quaternion GetSpawnRotation(
    Transform ownerTransform,
    int index,
    int spawnCount,
    float angleBetween)
    {
        float angle;

        switch (spawnPattern)
        {
            case WarningSpawnPattern.Directional:
                {
                    // 중앙을 기준으로 좌우 대칭
                    float centerOffset =
                        (spawnCount - 1) * 0.5f;

                    angle =
                        (index - centerOffset) * angleBetween
                        + startAngleOffset;

                    break;
                }

            case WarningSpawnPattern.Radial:
                {
                    // 360도를 균등 분할
                    float sectorSize =
                        360f / spawnCount;

                    angle =
                        startAngleOffset
                        + sectorSize * index;

                    break;
                }

            default:
                angle = startAngleOffset;
                break;
        }

        return ownerTransform.rotation *
               Quaternion.Euler(0f, angle, 0f);
    }

    public override SkillModule Clone()
    {
        var clone = (Module_SpawnWarningSign)base.Clone();
        clone.onSignEndModules = new List<SkillModule>();
        foreach (var mod in this.onSignEndModules ?? new List<SkillModule>())
        {
            clone.onSignEndModules.Add(mod?.Clone());
        }
        return clone;
    }
}
