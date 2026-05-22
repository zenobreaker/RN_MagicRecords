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

    [Tooltip("Multiply 모드 시 부모 데미지에 곱해질 비율 (0.5 = 50%)")]
    public float damageMultiplier = 1.0f;

    // Override 모드일때만 사용 
    [Tooltip("Override 모드일 때만 사용되는 전용 데미지 데이터")]
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
    public GameObject spawnObj;
    public string objectName;

    [Header("Lifetime Settings")]
    [Tooltip("소환수/투사체의 기본 수명 (0이면 무한 또는 프리팹 기본값)")]
    public float baseLifeTime = -1.0f;

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

        // A.투사체 개수: 내 인스펙터 값(Base) + 블랙보드의 추가 개수(Bonus)
        // 레코드가 없다면 GetValue가 0을 반환하므로, this.spawnCount(4) + 0 = 4발이 나갑니다.
        int bonusCount = skill.Blackboard.GetValue<int>("BonusSpawnCount", 0);
        finalSpawnCount = this.spawnCount + bonusCount;

        // B. 집탄률(각도): 내 인스펙터 값(Base) * 블랙보드의 배율(Multiplier)
        // 각도는 더하기/빼기보다 곱하기(%)로 처리해야 여러 스킬에 범용적으로 적용하기 좋습니다.
        // 레코드가 없다면 GetValue가 1.0f를 반환하므로, this.angleBetween(15) * 1.0 = 15도가 유지됩니다.
        float angleMultiplier = skill.Blackboard.GetValue<float>("AngleMultiplier", 1.0f);
        finalAngleBetween = this.angleBetween * angleMultiplier;

        // 3. 부모 페이즈 스킬과 나의 세팅을 비교해서 '최종 데미지' 데이터를 확정
        DamageData finalDamageData = GetEffectiveDamageData(phaseSkill);

        // 4. 하이브리드 수명 게산 (레코드나 특성으로 수명이 늘어날 수 있음) 
        float finalLifeTime = useBlackboardPattern
        ? skill.Blackboard.GetValue<float>("LifeTime", baseLifeTime)
        : this.baseLifeTime;

        // 3. 다중 생성 루프 
        for (int i = 0; i < finalSpawnCount; i++)
        {
            Quaternion finalRotation;

            // 여기서 생성 타입에 따라 회전값 계산 방식을 가름
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

            GameObject obj = null;
            bool isPooled = false; // 💡 풀링 생성 여부를 체크할 플래그

            // [분기 1] 이름이 입력되어 있다면 풀러에서 가져옴
            if (!string.IsNullOrEmpty(objectName))
            {
                obj = ObjectPooler.DeferredSpawnFromPool(objectName, basePosition, finalRotation);
                isPooled = true;
            }
            // [분기 2] 이름은 없지만 프리팹이 직접 할당되어 있다면 Instantiate!
            else if (spawnObj != null)
            {
                obj = UnityEngine.Object.Instantiate(spawnObj, basePosition, finalRotation);
            }

            // 오브젝트 생성 후 공통 셋업 처리
            if (obj != null)
            {
                if (obj.TryGetComponent<ISkillEffect>(out var projectile))
                {
                    bool isCrit = skill?.Blackboard.GetValue<bool>("isCrit", false) ?? false;
                    projectile.SetDamageInfo(owner, finalDamageData, isCrit);
                    projectile.AddIgnore(owner);
                }

                if (obj.TryGetComponent<ITargetableEffect>(out var targetable))
                {
                    Vector3 targetPos = skill.Blackboard.GetValue<Vector3>("TargetPosition");
                    targetable.SetTargetPosition(targetPos);
                }

                if(finalLifeTime > 0f && obj.TryGetComponent<ILifetimeSetup>(out var lifetimeObj))
                {
                    lifetimeObj.SetLifeTime(finalLifeTime);
                }

                if (obj.TryGetComponent<IOwnerSetup>(out var ownerSetup))
                {
                    ownerSetup.SetupOwner(owner);
                }

                // 💡 [핵심] 풀링으로 생성된 녀석만 FinishSpawn을 호출합니다!
                if (isPooled)
                {
                    ObjectPooler.FinishSpawn(obj);
                }
            }
        }
    }

    // ====================================================================
    // 💡 데미지 계산 도우미 함수
    // ====================================================================
    private DamageData GetEffectiveDamageData(PhaseSkill parentPhase)
    {
        switch (damageApplyType)
        {
            case DamageApplyType.Override:
                return damageData; // 모듈 인스펙터에 적힌 값 강제 사용

            case DamageApplyType.Multiply:
                if (parentPhase != null && parentPhase.damageData != null)
                {
                    // 부모의 데이터를 복사하되, 데미지(Power)와 계수만 배율에 맞게 깎아서 줍니다.
                    // (클래스 참조가 꼬이지 않도록 새로운 객체를 하나 만들어서 넘겨줍니다)
                    return new DamageData
                    {
                        damageType = parentPhase.damageData.damageType,
                        baseDamage = parentPhase.damageData.baseDamage * damageMultiplier,
                        statCoefficient = parentPhase.damageData.statCoefficient * damageMultiplier,

                        // 나머지 데이터는 부모의 것을 그대로 씁니다.
                        bDownable = parentPhase.damageData.bDownable,
                        bLauncher = parentPhase.damageData.bLauncher,
                        SoundName = parentPhase.damageData.SoundName,
                        impulseDirection = parentPhase.damageData.impulseDirection,
                        settings = parentPhase.damageData.settings,
                        hitData = parentPhase.damageData.hitData
                    };
                }
                break;

            case DamageApplyType.Inherit:
            default:
                if (parentPhase != null && parentPhase.damageData != null)
                {
                    return parentPhase.damageData; // 100% 부모 데미지
                }
                break;
        }

        return new DamageData(); // 예외 상황 (에러 방지)
    }

    public override SkillModule Clone()
    {
        // 1. 얕은 복사: int, float, Vector3, bool, string, GameObject 참조 등은 여기서 완벽히 복사됨
        Module_SpawnObject clone = (Module_SpawnObject)base.Clone();

        // 2. 깊은 복사: 클래스(class) 타입인 커스텀 데이터만 새로 메모리를 할당해서 덮어씌움
        if (this.damageData != null)
        {
            clone.damageData = this.damageData.Clone();
        }

        // (기존에 작성하시던 patternData는 현재 클래스 변수에 없으므로 지웠습니다!)

        return clone;
    }
}
