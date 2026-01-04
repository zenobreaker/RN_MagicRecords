using JetBrains.Annotations;
using System;
using UnityEngine;
using UnityEngine.UIElements;


public enum WarningSignType { Circle, Rectangle, Fan }

[Serializable]
public class Module_SpawnWarningSign : SkillModule, IWarningData
{
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
        skill?.Blackboard?.SetValue(Constants.PatternCount, spawnCount);
        skill?.Blackboard?.SetValue(Constants.PatternAngle, angleBetween);

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
            WarningSign sign = ObjectPooler.DeferedSpawnFromPool<WarningSign>(GetSignName(),
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

[Serializable]
public class Module_SpawnProjectile : SkillModule
{
    [Header("Damage Data")]
    public DamageData damageData;

    [Header("Spawn Data")]
    public Vector3 spawnPosition;

    [SerializeField]
    private Quaternion spwanQuaternion;
    public Quaternion ValidSpawnQuaternion =>
        spwanQuaternion.Equals(new Quaternion(0, 0, 0, 0)) ? Quaternion.identity : spwanQuaternion;

    // 오브젝트 풀링 적용 상태라면 굳이 오브젝트 자체를 가질 필요가 없긴함.
    [Header("Spawn Prefab")]
    public GameObject skillObject;
    public string objectName;

    public override void OnNotify(GameObject owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        // 1. 스폰 위치 계산 
        Vector3 basePosition = owner.transform.TransformPoint(spawnPosition); // 로컬 -> 월드 좌표로 변경

        // 2. 패턴 데이터 로드 
        int spawnCount = skill.Blackboard.GetValue<int>(Constants.PatternCount, 1);
        float angleBetween = skill.Blackboard.GetValue<float>(Constants.PatternAngle, 0f);
        
        // 3. 다중 발사 루프 
        for (int i = 0; i < spawnCount; i++)
        {
            // 방향 계산 
            Quaternion rotation = PositionHelpers.GetDirection(owner.transform, i, spawnCount, angleBetween, 0f) 
                * ValidSpawnQuaternion;
     
            GameObject obj = ObjectPooler.SpawnFromPool(objectName, basePosition, rotation);
            if (obj.TryGetComponent<Projectile>(out var projectile))
            {
                // Helper : 확장 메서드로 값이 없더라도 에러가 없도록 
                bool isCrit = (bool)skill?.Blackboard.GetValue<bool>("isCrit", false);
                projectile.SetDamageInfo(owner, damageData, isCrit);
                projectile.AddIgnore(owner);
            }
        }
    }
}

