using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[ModuleCategory("Combat/Assist Weapon Executor")]
public sealed class Module_AssistExecutor : SkillModule
{
    private const int TotalShots = 6;
    [Min(0f)] public float searchRadius = 10f;
    [Range(1, 6)] public int maxTargets = 6;
    public string weaponPoolName = "AssistExecutorWeapon";
    public string projectilePoolName = "AssistExecutorBullet";
    public Vector3 spawnOffset = new Vector3(0f, 1.5f, 0f);
    [Min(0f)] public float formationRadius = 1.5f;
    [Min(0.01f)] public float weaponLifetime = 0.6f;

    /// <summary>주변의 서로 다른 적을 등급 내림차순, 같은 등급에서는 거리순으로 찾습니다.</summary>
    private List<Character> FindTargets(Character owner)
    {
        List<Character> targets = new List<Character>();
        HashSet<Character> found = new HashSet<Character>();
        Vector3 origin = owner.transform.position;
        float radius = Mathf.Max(0f, searchRadius);
        Collider[] colliders = Physics.OverlapSphere(origin, radius, Physics.AllLayers,
            QueryTriggerInteraction.Collide);
        foreach (Collider collider in colliders)
        {
            Character target = collider.GetComponentInParent<Character>();
            if (target == null || target == owner || found.Add(target) == false ||
                target.isActiveAndEnabled == false ||
                (target.transform.position - origin).sqrMagnitude > radius * radius ||
                target.GetComponent<HealthPointComponent>()?.Dead != false ||
                CombatHelper.IsFriendly(owner, target.gameObject, null))
            {
                continue;
            }
            targets.Add(target);
        }
        targets.Sort((a, b) =>
        {
            int gradeOrder = GetTargetGrade(b).CompareTo(GetTargetGrade(a));
            if (gradeOrder != 0)
            {
                return gradeOrder;
            }
            int distanceOrder = (a.transform.position - origin).sqrMagnitude.CompareTo(
                (b.transform.position - origin).sqrMagnitude);
            return distanceOrder != 0 ? distanceOrder : a.GetInstanceID().CompareTo(b.GetInstanceID());
        });
        return targets;
    }

    /// <summary>직접 배치된 보스와 등급 미설정 일반 적도 실제 역할에 맞게 우선순위를 계산합니다.</summary>
    private static MonsterGrade GetTargetGrade(Character target)
    {
        MonsterGrade grade = target.GetGrade();
        if (target is Enemy enemy)
        {
            if (enemy.Boss)
            {
                return MonsterGrade.BOSS;
            }
            if (grade == MonsterGrade.NONE)
            {
                return MonsterGrade.NORMAL;
            }
        }
        return grade;
    }

    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        if (owner == null || skill == null || ObjectPooler.Instance == null)
        {
            return;
        }
        var targets = FindTargets(owner);
        int count = Mathf.Min(Mathf.Clamp(maxTargets, 1, TotalShots), targets.Count);
        if (count == 0)
        {
            return;
        }
        // 우선순위 순환 배정으로 총 6발을 유지하며 나머지는 첫 대상부터 추가합니다.
        for (int i = 0; i < TotalShots; i++)
        {
            Character target = targets[i % count];
            float angle = Mathf.PI * 2f * i / TotalShots;
            Vector3 position = owner.transform.position + spawnOffset +
                new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * formationRadius;
            Vector3 direction = target.transform.position + Vector3.up - position;
            Quaternion rotation = direction.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(direction) : owner.transform.rotation;
            GameObject obj = ObjectPooler.DeferredSpawnFromPool(weaponPoolName, position, rotation);
            if (obj == null)
            {
                continue;
            }
            if (obj.TryGetComponent<AssistDrone>(out var weapon))
            {
                weapon.SetPoolSpawn(true);
                weapon.SetLifeTime(weaponLifetime);
                weapon.SetupExecutor(owner, target, skill.CreateDamageSnapshot(skill.damageData),
                    skill.Runtime.Combat.IsCritical, skill.Runtime.DamageMultiplier, projectilePoolName);
            }
            ObjectPooler.FinishSpawn(obj);
        }
    }
}
