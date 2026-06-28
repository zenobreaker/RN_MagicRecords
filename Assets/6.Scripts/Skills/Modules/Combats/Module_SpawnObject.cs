using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

[MovedFrom(true, null, null, "Module_SpawnEffect")]
[ModuleCategory("Combat/Spawn Object")]
[Serializable]
public class Module_SpawnObject : SkillModule
{
    [Header("Damage Settings")]
    public DamageApplyType damageApplyType = DamageApplyType.Inherit;

    [Tooltip("Multiply 모드 시 부모 데미지에 곱해질 기본 비율 (0.5 = 50%)")]
    public float damageMultiplier = 1.0f;

    [Tooltip("Override 모드일 때만 사용되는 전용 데미지 데이터")]
    public DamageData damageData;

    [Header("Spawn Data")]
    public Vector3 spawnPosition;

    [SerializeField]
    private Vector3 spawnRotation;
    public Quaternion ValidSpawnQuaternion =>
        spawnRotation.Equals(Vector3.zero) ? Quaternion.identity : Quaternion.Euler(spawnRotation);

    [Header("Pattern Settings")]
    public FirePatternType patternType = FirePatternType.RegularFan;

    [Tooltip("기본적으로 발사할 투사체의 개수 (발리샷 등)")]
    public int baseSpawnCount = 1;

    [Tooltip("투사체 사이의 벌어지는 각도")]
    public float baseAngleBetween = 0f;

    [Header("Spawn Prefab")]
    public GameObject spawnObj;
    public string objectName;

    [Header("Lifetime Settings")]
    [Tooltip("소환수/투사체의 기본 수명 (0이면 무한 또는 프리팹 기본값)")]
    public float baseLifeTime = -1.0f;

    [Header("Pattern Override")]
    [Tooltip("체크하면 인스펙터 값 대신 블랙보드의 값을 강제로 가져와서 씁니다.")]
    public bool useBlackboardPattern = true;

    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        // 1. 스폰 위치 계산 (로컬 -> 월드 좌표)
        Vector3 basePosition = owner.transform.TransformPoint(spawnPosition);

        // 2. 💡 강타입 컨텍스트(Runtime)를 이용한 최종 수치 계산
        // [개수] Base 값이 세팅 안 되어있으면 내 Fallback 값 사용 + 레코드가 추가한 Add 값 더하기
        int finalSpawnCount = skill.Runtime.PatternCount > 0 ? skill.Runtime.PatternCount : baseSpawnCount;

        // [각도] Base 값이 세팅 안 되어있으면 내 Fallback 각도 사용
        float finalAngleBetween = skill.Runtime.PatternAngle > 0 ? skill.Runtime.PatternAngle : baseAngleBetween;

        // [데미지 배율] 내 기본 배율 * 레코드가 뻥튀기 시켜준 배율
        float finalDamageMultiplier = skill.Runtime.DamageMultiplier;

        // [기타 수치들]
        float finalLifeTime = this.baseLifeTime; // 필요시 Runtime.LifeTimeAdd 등을 추가하여 연동 가능
        bool isCrit = skill.Runtime.Combat.IsCritical;

        // 3. 최종 데미지 데이터 확정 (계산된 최종 데미지 배율을 넘겨줍니다)
        DamageData finalDamageData = GetEffectiveDamageData(skill, finalDamageMultiplier);

        // 4. 다중 생성 루프 
        for (int i = 0; i < finalSpawnCount; i++)
        {
            Quaternion finalRotation;

            if (patternType == FirePatternType.RegularFan)
            {
                // 일정한 간격으로 퍼지는 부채꼴
                finalRotation = PositionHelpers.GetDirection(owner.transform, i, finalSpawnCount, finalAngleBetween, 0f)
                                * ValidSpawnQuaternion;
            }
            else // FirePatternType.RandomSpread
            {
                // 범위 안에서 무작위 난사
                Quaternion centerDir = owner.transform.rotation * ValidSpawnQuaternion;
                finalRotation = PositionHelpers.GetRandomSpread(centerDir, finalAngleBetween);
            }

            GameObject obj = null;
            bool isPooled = false;

            // [생성 분기] 풀링 vs Instantiate
            if (!string.IsNullOrEmpty(objectName))
            {
                obj = ObjectPooler.DeferredSpawnFromPool(objectName, basePosition, finalRotation);
                isPooled = true;
            }
            else if (spawnObj != null)
            {
                obj = UnityEngine.Object.Instantiate(spawnObj, basePosition, finalRotation);
            }

            // 5. 생성된 오브젝트 공통 셋업
            if (obj != null)
            {
                if (obj.TryGetComponent<ISkillEffect>(out var projectile))
                {
                    projectile.SetDamageInfo(owner, finalDamageData, isCrit, finalDamageMultiplier);
                    projectile.AddIgnore(owner);
                }

                if (obj.TryGetComponent<ITargetableEffect>(out var targetable))
                {
                    // 💡 컨텍스트에 추가해둔 TargetPosition을 바로 가져옵니다.
                    targetable.SetTargetPosition(skill.Runtime.Spawn.TargetPosition);
                }

                if (finalLifeTime > 0f && obj.TryGetComponent<ILifetimeSetup>(out var lifetimeObj))
                {
                    lifetimeObj.SetLifeTime(finalLifeTime);
                }

                if (obj.TryGetComponent<IOwnerSetup>(out var ownerSetup))
                {
                    ownerSetup.SetupOwner(owner);
                }

                if (isPooled)
                {
                    ObjectPooler.FinishSpawn(obj);
                }
            }
        }
    }

    // ====================================================================
    // 💡 데미지 계산 도우미 함수 (combinedMultiplier 파라미터 추가)
    // ====================================================================
    private DamageData GetEffectiveDamageData(ActiveSkill skill, float combinedMultiplier)
    {
        switch (damageApplyType)
        {
            case DamageApplyType.Override:
                return damageData;

            case DamageApplyType.Multiply:
                if (skill != null && skill.damageData != null)
                {
                    return new DamageData
                    {
                        damageType = skill.damageData.damageType,
                        // 💡 여기서 모듈 인스펙터 배율 + 레코드 배율이 모두 합쳐진 combinedMultiplier를 곱해줍니다.
                        baseDamage = skill.damageData.baseDamage * combinedMultiplier,
                        statCoefficient = skill.damageData.statCoefficient * combinedMultiplier,

                        bDownable = skill.damageData.bDownable,
                        bLauncher = skill.damageData.bLauncher,
                        SoundName = skill.damageData.SoundName,
                        impulseDirection = skill.damageData.impulseDirection,
                        csp = skill.damageData.csp,
                        hitData = skill.damageData.hitData
                    };
                }
                break;

            case DamageApplyType.Inherit:
            default:
                if (skill != null && skill.damageData != null)
                {
                    return skill.damageData;
                }
                break;
        }

        return new DamageData();
    }

    public override SkillModule Clone()
    {
        Module_SpawnObject clone = (Module_SpawnObject)base.Clone();
        if (this.damageData != null)
        {
            clone.damageData = this.damageData.Clone();
        }
        return clone;
    }
}