using System;
using System.Collections.Generic;
using UnityEngine;

[ModuleCategory("Passive/Spawn Modifier/자탄 분열")]
[Serializable]
public sealed class Module_Passive_SplitProjectile : PassiveModule
{
    [Header("Target Filter")]
    [Tooltip("어떤 스킬(ID)에만 적용할 것인가? (예: 1002 = 매그넘샷)")]
    public int targetSkillID;

    [Header("Split Settings")]
    public int splitCount = 6;
    public string splitPrefabName; 
    public GameObject splitPrefab;

    [Header("Spawn Origin Bonus")]
    public Vector3 bonusPos; 

    public Module_Passive_SplitProjectile()
    {
        triggerTime = PassiveTriggerTime.OnSpawnObject;
    }

    public override void OnSpawnObject(ISkillEffect spawnedObject, ActiveSkill casterSkill)
    {
        if (targetSkillID != 0 && casterSkill.SkillID != targetSkillID)
            return;

        if (spawnedObject is BaseProjectile proj)
        {
            bool hasSplit = false;

            proj.OnTargetHitEvent += (target, hitPoint) =>
            {
                if (hasSplit) return; // 이미 분열했다면 무시 (관통 총알인 경우 첫 타에만 분열)
                hasSplit = true;

                PerformSplit(target.transform.position, proj.transform.rotation,
                    proj.Owner, proj.DamageData, proj.Ignores, target);
            };
        }
    }

    private void PerformSplit(Vector3 position, Quaternion baseRotation, Character owner, DamageData cachedData, 
        HashSet<GameObject> cachedIgnores, GameObject target)
    {
        float angleStep = 360f / splitCount;

        for (int i = 0; i < splitCount; i++)
        {
            Quaternion rot = baseRotation * Quaternion.Euler(0, angleStep * i, 0);

            Vector3 finalPos = position + bonusPos;

            // 자탄 생성!
            GameObject childBullet = null;
            if (string.IsNullOrEmpty(splitPrefabName) == false)
            {
                childBullet = ObjectPooler.DeferredSpawnFromPool(splitPrefabName, finalPos, rot);

            }
            else if(splitPrefab != null)
            {
                childBullet = ObjectPooler.DeferredSpawnFromPool(splitPrefab.name, position, rot);
            }

            // (자탄 데미지 세팅 로직 추가...)
            if(childBullet != null && childBullet.TryGetComponent<ISkillEffect>(out var effect))
            {
                effect.AddIgnore(owner);
                effect.AddIgnore(target);
                effect.SetDamageInfo(owner, cachedData);
            }

            ObjectPooler.FinishSpawn(childBullet);
        }
    }
}
