using System;
using UnityEngine;

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
    public bool isSetTargetPos = false;

    [Header("Multi-Spawn Settings")]
    public int fallbackSpawnCount = 1;
    public float fallbackAngleBetween = 0f;
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
        int baseCount = skill.Runtime.BasePatternCount > 0 ? skill.Runtime.BasePatternCount : fallbackSpawnCount;
        int finalSpawnCount = baseCount + skill.Runtime.PatternCountAdd;


        float finalAngleBetween = skill.Runtime.BasePatternCount > 0 ? skill.Runtime.BasePatternAngle : 
            fallbackAngleBetween;

        //  만약 내가 인스펙터 값을 썼다면, 다음 페이즈를 위해 블랙보드에 갱신
        if (!useBlackboardPattern)
        {
            skill.Runtime.PatternCount = finalSpawnCount;
            skill.Runtime.PatternAngle = finalAngleBetween;
        }

        Vector3 basePosition = skill.Runtime.TargetPosition;
        if (isSetTargetPos == false)
            basePosition = owner.transform.position;

        basePosition.y += 0.01f;// y축 보정

        // 여러 개의 장판 중 몇 개가 끝나는지 카운팅하기 위한 변수
        int finishedCount = 0;

        for (int i = 0; i < finalSpawnCount; i++)
        {
            // 1. 각도 계산 
            // EX) 3개고 간격이 30도라면 -> -30, 0, 30 도 회전
            Quaternion rotation = PositionHelpers.GetDirection
                (owner.transform, i, finalSpawnCount, finalAngleBetween, 0f);

            // 2. 생성 
            WarningSign sign = ObjectPooler.DeferredSpawnFromPool<WarningSign>(GetSignName(),
            basePosition, rotation);

            sign.Setup(this, duration);

            // 3. 종료 로직 ( 중요 : 모든 장판이 끝났을 때만) 
            sign.OnEndSign = () =>
            {
                finishedCount++;
                if (finishedCount >= finalSpawnCount)
                    skill.EndPhaseAndNext();
            };

            ObjectPooler.FinishSpawn(sign.gameObject);

            skill.AddTrackedEffect(sign.gameObject);
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