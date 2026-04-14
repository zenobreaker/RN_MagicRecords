using JetBrains.Annotations;
using System;
using UnityEngine;
using UnityEngine.UIElements;


public enum WarningSignType { Circle, Rectangle, Fan }

[Serializable]
public class Module_SetPatternData : SkillModule
{
    [Header("Pattern Settings")]
    [Tooltip("발사할 투사체/장판의 개수")]
    public int spawnCount = 1;

    [Tooltip("투사체 사이의 각도")]
    public float angleBetween = 0f;

    // 필요하다면 타겟팅 관련 값도 여기서 한 번에 세팅 가능
    // public TargetPositionType targetType = TargetPositionType.CasterForward;

    public override void OnNotify(GameObject owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        // 아주 심플하게 블랙보드에 값만 올려두고 끝납니다.
        skill.Blackboard.SetValue(Constants.PatternCount, spawnCount);
        skill.Blackboard.SetValue(Constants.PatternAngle, angleBetween);
    }
}

[Serializable]
public class Module_SpawnWarningSign : SkillModule, IWarningData
{
    [Header("Pattern Override")]
    [Tooltip("체크하면 인스펙터 값 대신 블랙보드의 값을 강제로 가져와서 씁니다.")]
    public bool useBlackboardPattern = false; // 💡 보통 장판 뒤에 오니까 기본값을 true로 두면 편합니다.

    [Header("Warning Sign")]
    public WarningSignType signType;
    public float duration = 1.0f;
    public bool isSetTargetPos = false; 

    [Header("Multi-Spawn Settings")]
    public int spawnCount = 1;
    public float angleBetween = 0f;
    public float startAngleOffset = 0f;

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

    public override void OnNotify(GameObject owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        // 값을 결정합니다 (인스펙터 값 쓸래? 블랙보드 값 쓸래?)
        int finalSpawnCount = useBlackboardPattern
            ? skill.Blackboard.GetValue<int>(Constants.PatternCount, 1)
            : this.spawnCount;

        float finalAngleBetween = useBlackboardPattern
            ? skill.Blackboard.GetValue<float>(Constants.PatternAngle, 0f)
            : this.angleBetween;

        //  만약 내가 인스펙터 값을 썼다면, 다음 페이즈를 위해 블랙보드에 갱신
        if (!useBlackboardPattern)
        {
            skill.Blackboard.SetValue(Constants.PatternCount, finalSpawnCount);
            skill.Blackboard.SetValue(Constants.PatternAngle, finalAngleBetween);
        }

        Vector3 basePosition = skill.Blackboard.GetValue<Vector3>(Constants.TargetPos);
        if (isSetTargetPos == false)
            basePosition = owner.transform.position; 
        
        basePosition.y += 0.01f;// y축 보정

        // 여러 개의 장판 중 몇 개가 끝나는지 카운팅하기 위한 변수
        int finishedCount = 0;

        for (int i = 0; i < spawnCount; i++)
        {
            // 1. 각도 계산 
            // EX) 3개고 간격이 30도라면 -> -30, 0, 30 도 회전
            Quaternion rotation = PositionHelpers.GetDirection(owner.transform, i, spawnCount, angleBetween, 0f);

            // 2. 생성 
            WarningSign sign = ObjectPooler.DeferredSpawnFromPool<WarningSign>(GetSignName(),
            basePosition, rotation);

            sign.Setup(this, duration);

            // 3. 종료 로직 ( 중요 : 모든 장판이 끝났을 때만) 
            sign.OnEndSign = () =>
            {
                finishedCount++;
                if(finishedCount >= spawnCount)
                    skill.EndPhaseAndNext();
            };

            ObjectPooler.FinishSpawn(sign.gameObject);
        }
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
}

public interface ISkillEffect
{
    void SetDamageInfo(GameObject attacker, DamageData damageData, bool bExtraCrit = false);
    void AddIgnore(GameObject ignore);
}

public interface ITargetableEffect
{
    void SetTargetPosition(Vector3 targetPosition);
}

[Serializable]
public class Module_SpawnEffect : SkillModule
{
    [Header("Damage Data")]
    public DamageData damageData;

    [Header("Spawn Data")]
    public Vector3 spawnPosition;

    [SerializeField]
    private Vector3 spawnRotation;
    public Quaternion ValidSpawnQuaternion =>
        spawnRotation.Equals(new Quaternion(0, 0, 0, 0)) ? Quaternion.identity : Quaternion.Euler(spawnRotation);

    [Header("Pattern Settings")]
    public FirePatternType patternType = FirePatternType.RegularFan;

    // 오브젝트 풀링 적용 상태라면 굳이 오브젝트 자체를 가질 필요가 없긴함.
    [Header("Spawn Prefab")]
    public GameObject skillObject;
    public string objectName;

    [Header("Pattern Override")]
    [Tooltip("체크하면 인스펙터 값 대신 블랙보드의 값을 강제로 가져와서 씁니다.")]
    public bool useBlackboardPattern = true; // 💡 보통 장판 뒤에 오니까 기본값을 true로 두면 편합니다.

    [Header("Multi-Spawn Settings (블랙보드 안 쓸 때 사용)")]
    public int spawnCount = 1;
    public float angleBetween = 0f;

    public override void OnNotify(GameObject owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        // 1. 스폰 위치 계산 
        Vector3 basePosition = owner.transform.TransformPoint(spawnPosition); // 로컬 -> 월드 좌표로 변경

        // 2. 하이브리드 값 결정 로직
        int finalSpawnCount = useBlackboardPattern
            ? skill.Blackboard.GetValue<int>(Constants.PatternCount, 1)
            : this.spawnCount;

        float finalAngleBetween = useBlackboardPattern
            ? skill.Blackboard.GetValue<float>(Constants.PatternAngle, 0f)
            : this.angleBetween;

        // 3. 다중 발사 루프 
        for (int i = 0; i < finalSpawnCount; i++)
        {
            Quaternion finalRotation;

            // 여기서 발사 타입에 따라 회전값 계산 방식을 가름
            if (patternType == FirePatternType.RegularFan)
            {
                // [기존 방식] 일정한 간격으로 예쁘게 퍼지는 부채꼴
                finalRotation = PositionHelpers.GetDirection(owner.transform, i, finalSpawnCount, finalAngleBetween, 0f)
                                * ValidSpawnQuaternion;
            }
            else // FirePatternType.RandomSpread
            {
                // [새로운 방식] 중심 방향을 잡고, patternAngle(집탄율) 범위 안에서 무작위 난사!
                Quaternion centerDir = owner.transform.rotation * ValidSpawnQuaternion;
                finalRotation = PositionHelpers.GetRandomSpread(centerDir, finalAngleBetween);
            }

            GameObject obj = ObjectPooler.DeferredSpawnFromPool(objectName, basePosition, finalRotation);
            if (obj != null && obj.TryGetComponent<ISkillEffect>(out var projectile))
            {
                // Helper : 확장 메서드로 값이 없더라도 에러가 없도록 
                bool isCrit = (bool)skill?.Blackboard.GetValue<bool>("isCrit", false);
                projectile.SetDamageInfo(owner, damageData, isCrit);
                projectile.AddIgnore(owner);
            }

            if(obj != null && obj.TryGetComponent<ITargetableEffect>(out var targetable))
            {
                Vector3 targetPos = skill.Blackboard.GetValue<Vector3>("TargetPosition");
                targetable.SetTargetPosition(targetPos);
            }
            
            ObjectPooler.FinishSpawn(obj); 
        }
    }
}

