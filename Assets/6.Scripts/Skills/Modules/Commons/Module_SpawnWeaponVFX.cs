using System;
using UnityEngine;
using UnityEngine.Serialization;

[ModuleCategory("Common/SpawnWeaponVFX")]
[Serializable]
public class Module_SpawnWeaponVFX : SkillModule
{
    [Header("Effect Spawn Settings")]
    [FormerlySerializedAs("muzzleFlashPrefab")]
    public GameObject effectPrefab;
    [Tooltip("무기 대신 Character 위치에 생성합니다.")]
    public bool spawnAtOwner;

    [Tooltip("무기의 모든 이펙트 생성 지점에 생성합니다.")]
    [FormerlySerializedAs("useAllMuzzles")]
    public bool useAllEffectSpawnPoints = true;

    [Tooltip("전체 생성이 꺼져 있을 때 사용할 생성 지점 인덱스입니다. (0부터 시작)")]
    [FormerlySerializedAs("specificMuzzleIndices")]
    public int[] specificEffectSpawnIndices;

    protected virtual string EffectId => string.Empty;
    protected virtual bool FollowEffectSpawnPoint => false;
    protected virtual bool ReplaceExistingEffects => false;

    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        if (owner == null || !owner.isActiveAndEnabled || effectPrefab == null) return;
        if (spawnAtOwner)
        {
            if (!owner.TryGetComponent<SkillVFXComponent>(out var effects))
                effects = owner.gameObject.AddComponent<SkillVFXComponent>();
            if (!effects.isActiveAndEnabled) return;
            if (ReplaceExistingEffects) effects.RemoveEffects(skill, EffectId);
            SpawnEffect(owner.transform, effects, skill);
            return;
        }
        if (!owner.TryGetComponent<WeaponComponent>(out var weaponComponent)) return;
        if (!(weaponComponent.GetCurrentWeapon() is IAttackOriginProvider originProvider)) return;

        var effectSpawnPoints = originProvider.GetAttackOrigins();
        if (effectSpawnPoints == null || effectSpawnPoints.Count == 0) return;

        if (!owner.TryGetComponent<SkillVFXComponent>(out var skillVFX))
            skillVFX = owner.gameObject.AddComponent<SkillVFXComponent>();
        if (!skillVFX.isActiveAndEnabled) return;

        if (ReplaceExistingEffects)
            skillVFX.RemoveEffects(skill, EffectId);

        if (useAllEffectSpawnPoints)
        {
            foreach (var effectSpawnPoint in effectSpawnPoints)
                SpawnEffect(effectSpawnPoint, skillVFX, skill);
        }
        else if (specificEffectSpawnIndices != null)
        {
            foreach (int index in specificEffectSpawnIndices)
            {
                if (index >= 0 && index < effectSpawnPoints.Count)
                    SpawnEffect(effectSpawnPoints[index], skillVFX, skill);
            }
        }
    }

    private void SpawnEffect(Transform effectSpawnPoint, SkillVFXComponent skillVFX, ActiveSkill skill)
    {
        if (effectSpawnPoint == null) return;

        var instance = UnityEngine.Object.Instantiate(effectPrefab,
            effectSpawnPoint.position, effectSpawnPoint.rotation,
            FollowEffectSpawnPoint ? effectSpawnPoint : null);
        skillVFX.RegisterEffect(skill, EffectId, instance);
    }
}
