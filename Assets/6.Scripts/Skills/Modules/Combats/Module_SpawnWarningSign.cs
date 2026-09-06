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

    private bool isIstantState = false;

    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        isIstantState = phaseSkill.isInstant;


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

                sign.OnEndSign = () =>
                {
                    Vector3 impactPosition = sign.transform.position;
                    Quaternion impactRotation = sign.transform.rotation;

                    SkillChainContext contenxt = new SkillChainContext
                    {
                        Position = impactPosition,
                        Rotation = impactRotation,
                        PatternIndex = patternIndex,
                        WarningSign = sign
                    };

                    foreach (var chainModule in onSignEndModules)
                    {
                        if (chainModule == null)
                            continue;

                        skill.Runtime.Spawn.TargetPosition =
                            sign.transform.position;

                        chainModule.OnNotify(
                            owner,
                            skill,
                            phaseSkill
                        );
                    }

                    finishedCount++;

                    if (finishedCount >= finalSpawnCount &&
                        endPhaseOnFinish)
                    {
                        skill.EndPhaseAndNext();
                    }
                };

                ObjectPooler.FinishSpawn(sign.gameObject);

                if (endPhaseOnFinish)
                    skill.AddTrackedEffect(sign.gameObject);
            }
        } // for(i) end 
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

    public override bool ControlsPhaseLifecycle()
    {
        //즉발이 아니면서, "종료 시 페이즈를 넘긴다(endPhaseOnFinish)"가 켜져 있을 때만 페이즈를 홀드합니다.
        return !isIstantState && endPhaseOnFinish;
    }

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
        foreach (var mod in this.onSignEndModules)
        {
            clone.onSignEndModules.Add(mod.Clone());
        }
        return clone;
    }
}