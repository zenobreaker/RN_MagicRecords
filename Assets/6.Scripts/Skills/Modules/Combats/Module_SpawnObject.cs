using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

[MovedFrom(true, null, null, "Module_SpawnEffect")]
[ModuleCategory("Combat/Spawn Object")]
[Serializable]
public class Module_SpawnObject : SkillModule
{
    #region Damage

    [Header("Damage Settings")]
    public DamageApplyType damageApplyType = DamageApplyType.Inherit;

    [Tooltip("Multiply 모드 시 부모 데미지에 곱해질 기본 비율")]
    public float damageMultiplier = 1.0f;

    [Tooltip("Override 모드일 때만 사용하는 전용 데미지")]
    public DamageData damageData;

    #endregion


    #region Spawn

    [Header("Spawn Settings")]

    [Tooltip("SpawnPoint가 없을 경우 사용할 기존 로컬 위치")]
    public Vector3 spawnPosition;

    [SerializeField]
    private Vector3 spawnRotation;

    public Quaternion ValidSpawnQuaternion =>
        spawnRotation == Vector3.zero
            ? Quaternion.identity
            : Quaternion.Euler(spawnRotation);

    #endregion


    #region Pattern

    [Header("Pattern Settings")]

    [Tooltip("투사체 생성 방향 패턴")]
    public FirePatternType patternType = FirePatternType.RegularFan;

    [Tooltip("기본적으로 생성할 투사체 개수")]
    public int baseSpawnCount = 1;

    [Tooltip("투사체 사이의 각도")]
    public float baseAngleBetween = 0f;

    #endregion


    #region Prefab

    [Header("Spawn Prefab")]

    public GameObject spawnObj;

    public string objectName;

    #endregion


    #region Lifetime

    [Header("Lifetime Settings")]

    [Tooltip("0 이하이면 프리팹 기본 수명을 사용")]
    public float baseLifeTime = -1.0f;

    #endregion


    public override void OnNotify(
        Character owner,
        ActiveSkill skill,
        PhaseSkill phaseSkill)
    {
        if (owner == null || skill == null)
            return;


        // ============================================================
        // 1. 최종 Spawn Count
        // ============================================================
        //
        // SpawnCount는 SpawnPoint 선택과 완전히 별개다.
        //
        // 예:
        // SpawnPoint 2개
        // SpawnCount 3
        //
        // => 각 SpawnPoint에서 3개씩 생성
        // => 총 6개
        //
        int finalSpawnCount =
            skill.Runtime.PatternCount > 0
                ? skill.Runtime.PatternCount
                : baseSpawnCount;


        // ============================================================
        // 2. 최종 Pattern 값
        // ============================================================

        float finalAngleBetween =
            skill.Runtime.PatternAngle != 0f
                ? skill.Runtime.PatternAngle
                : baseAngleBetween;


        // ============================================================
        // 3. 최종 Damage
        // ============================================================

        float finalDamageMultiplier =
            skill.Runtime.DamageMultiplier;

        bool isCrit =
            skill.Runtime.Combat.IsCritical;

        DamageData finalDamageData =
            GetEffectiveDamageData(
                skill,
                finalDamageMultiplier);


        // ============================================================
        // 4. 사용할 SpawnPoint 결정
        // ============================================================

        if (skill.Runtime.Spawn.SelectedSpawnPointIndices != null &&
            skill.Runtime.Spawn.SelectedSpawnPointIndices.Count > 0)
        {
            SpawnFromSelectedPoints(
                owner,
                skill,
                finalSpawnCount,
                finalAngleBetween,
                finalDamageData,
                isCrit);
        }
        else
        {
            // SpawnPoint 선택 모듈을 사용하지 않은 기존 스킬의
            // 하위 호환용 fallback
            SpawnFromOwnerTransform(
                owner,
                skill,
                finalSpawnCount,
                finalAngleBetween,
                finalDamageData,
                isCrit);
        }
    }


    // ====================================================================
    // Selected SpawnPoint 기반 생성
    // ====================================================================

    private void SpawnFromSelectedPoints(
        Character owner,
        ActiveSkill skill,
        int spawnCount,
        float angleBetween,
        DamageData damageData,
        bool isCrit)
    {
        var spawnContext = skill.Runtime.Spawn;

        foreach (int spawnPointIndex
                 in spawnContext.SelectedSpawnPointIndices)
        {
            if (spawnPointIndex < 0 ||
                spawnPointIndex >= spawnContext.SpawnPoints.Count)
            {
                Debug.LogWarning(
                    $"[{GetType().Name}] 잘못된 SpawnPoint Index: {spawnPointIndex}");

                continue;
            }


            SpawnPointData spawnPoint =
                spawnContext.SpawnPoints[spawnPointIndex];


            // --------------------------------------------------------
            // SpawnPoint의 위치 / 방향
            // --------------------------------------------------------

            Vector3 basePosition =
                spawnPoint.position;

            Quaternion baseRotation =
                spawnPoint.rotation;


            // --------------------------------------------------------
            // 해당 SpawnPoint에서 SpawnCount만큼 생성
            // --------------------------------------------------------

            SpawnProjectiles(
                owner,
                skill,
                basePosition,
                baseRotation,
                spawnCount,
                angleBetween,
                damageData,
                isCrit);
        }
    }


    // ====================================================================
    // 기존 방식 fallback
    // ====================================================================

    private void SpawnFromOwnerTransform(
        Character owner,
        ActiveSkill skill,
        int spawnCount,
        float angleBetween,
        DamageData damageData,
        bool isCrit)
    {
        Vector3 basePosition =
            owner.transform.TransformPoint(spawnPosition);

        Quaternion baseRotation =
            owner.transform.rotation *
            ValidSpawnQuaternion;


        SpawnProjectiles(
            owner,
            skill,
            basePosition,
            baseRotation,
            spawnCount,
            angleBetween,
            damageData,
            isCrit);
    }


    // ====================================================================
    // 실제 생성
    // ====================================================================

    private void SpawnProjectiles(
        Character owner,
        ActiveSkill skill,
        Vector3 basePosition,
        Quaternion baseRotation,
        int spawnCount,
        float angleBetween,
        DamageData damageData,
        bool isCrit)
    {
        if (spawnCount <= 0)
            return;


        for (int i = 0; i < spawnCount; i++)
        {
            // --------------------------------------------------------
            // 1. 발사 방향 계산
            // --------------------------------------------------------

            Quaternion finalRotation;

            switch (patternType)
            {
                case FirePatternType.RegularFan:

                    finalRotation =
                        PositionHelpers.GetDirection(
                            owner.transform,
                            i,
                            spawnCount,
                            angleBetween,
                            0f);

                    break;


                case FirePatternType.RandomSpread:

                    finalRotation =
                        PositionHelpers.GetRandomSpread(
                            baseRotation,
                            angleBetween);

                    break;


                default:

                    finalRotation =
                        baseRotation;

                    break;
            }


            // --------------------------------------------------------
            // 2. 실제 Spawn
            // --------------------------------------------------------

            GameObject obj = SpawnObject(
                skill,
                basePosition,
                finalRotation);


            if (obj == null)
                continue;


            // --------------------------------------------------------
            // 3. ISkillEffect 초기화
            // --------------------------------------------------------

            if (obj.TryGetComponent<ISkillEffect>(
                    out var skillEffect))
            {
                skillEffect.SetDamageInfo(
                    owner,
                    damageData,
                    isCrit,
                    skill.Runtime.DamageMultiplier);

                skillEffect.AddIgnore(owner);
            }


            // --------------------------------------------------------
            // 4. Target Position
            // --------------------------------------------------------

            if (obj.TryGetComponent<ITargetableEffect>(
                    out var targetable))
            {
                targetable.SetTargetPosition(
                    skill.Runtime.Spawn.TargetPosition);
            }


            // --------------------------------------------------------
            // 5. Lifetime
            // --------------------------------------------------------

            if (baseLifeTime > 0f &&
                obj.TryGetComponent<ILifetimeSetup>(
                    out var lifetime))
            {
                lifetime.SetLifeTime(baseLifeTime);
            }


            // --------------------------------------------------------
            // 6. Owner
            // --------------------------------------------------------

            if (obj.TryGetComponent<IOwnerSetup>(
                    out var ownerSetup))
            {
                ownerSetup.SetupOwner(owner);
            }


            // --------------------------------------------------------
            // 7. Passive OnSpawn
            // --------------------------------------------------------

            if (skillEffect != null)
            {
                AppManager.Instance.SafeInvoke(
                    v => v.GetPassiveSystem()
                        ?.BroadcastOnSpawnObject(
                            skillEffect,
                            skill));
            }
        }
    }


    // ====================================================================
    // Object 생성
    // ====================================================================

    private GameObject SpawnObject(
        ActiveSkill skill,
        Vector3 position,
        Quaternion rotation)
    {
        string finalName =
            !string.IsNullOrEmpty(
                skill.Runtime.Spawn.OverridePrefabName)
                    ? skill.Runtime.Spawn.OverridePrefabName
                    : objectName;


        // ------------------------------------------------------------
        // Pool
        // ------------------------------------------------------------

        if (!string.IsNullOrEmpty(finalName))
        {
            GameObject obj =
                ObjectPooler.DeferredSpawnFromPool(
                    finalName,
                    position,
                    rotation);

            if (obj != null)
            {
                ObjectPooler.FinishSpawn(obj);
            }

            return obj;
        }


        // ------------------------------------------------------------
        // Instantiate
        // ------------------------------------------------------------

        if (spawnObj != null)
        {
            return UnityEngine.Object.Instantiate(
                spawnObj,
                position,
                rotation);
        }


        Debug.LogWarning(
            $"[{GetType().Name}] Spawn할 Object가 없습니다.");

        return null;
    }


    // ====================================================================
    // Damage
    // ====================================================================

    private DamageData GetEffectiveDamageData(
        ActiveSkill skill,
        float combinedMultiplier)
    {
        switch (damageApplyType)
        {
            case DamageApplyType.Override:

                return damageData;


            case DamageApplyType.Multiply:

                if (skill != null &&
                    skill.damageData != null)
                {
                    DamageData source =
                        skill.damageData;

                    DamageData result =
                        new DamageData
                        {
                            damageType =
                                source.damageType,

                            baseDamage =
                                source.baseDamage *
                                combinedMultiplier,

                            statCoefficient =
                                source.statCoefficient *
                                combinedMultiplier,

                            bDownable =
                                source.bDownable,

                            bLauncher =
                                source.bLauncher,

                            SoundName =
                                source.SoundName,

                            impulseDirection =
                                source.impulseDirection,

                            csp =
                                source.csp,

                            hitData =
                                source.hitData
                        };

                    return result;
                }

                break;


            case DamageApplyType.Inherit:
            default:

                if (skill != null &&
                    skill.damageData != null)
                {
                    return skill.damageData;
                }

                break;
        }


        return new DamageData();
    }


    public override SkillModule Clone()
    {
        Module_SpawnObject clone =
            (Module_SpawnObject)base.Clone();

        if (damageData != null)
        {
            clone.damageData =
                damageData.Clone();
        }

        return clone;
    }
}