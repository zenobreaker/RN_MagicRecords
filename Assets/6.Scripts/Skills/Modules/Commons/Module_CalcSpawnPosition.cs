using System;
using UnityEngine;
using static UnityEngine.UI.Image;

public enum SpawnPositionType
{
    Caster,
    WeaponMuzzle,
    Transform,
    Target,
    Custom
}

[ModuleCategory("Common/CalcSpawnPosition")]
[Serializable]
public class Module_CalcSpawnPosition : SkillModule
{
    public SpawnPositionType type;

    public override void OnNotify(
        Character owner,
        ActiveSkill skill,
        PhaseSkill phaseSkill)
    {
        skill.Runtime.Spawn.SpawnPositions.Clear();

        switch (type)
        {
            case SpawnPositionType.Caster:
                Add(owner.transform, skill);
                break;

            case SpawnPositionType.WeaponMuzzle:
                AddWeaponMuzzles(owner.transform, skill);
                break;
        }
    }

    private void Add(Transform target, ActiveSkill skill)
    {
        if (skill == null) return;

        skill.Runtime.Spawn.SpawnPositions.Add(
            new SpawnPointData(
                target.position,
                target.rotation));
    }

    private void AddWeaponMuzzles(Transform target, ActiveSkill skill)
    {
        if (skill == null) return;

        if (target == null) return;

        if (target.TryGetComponent<WeaponComponent>(out var weaponComponent))
        {
            if (weaponComponent.GetCurrentWeapon() is IAttackOriginProvider origin)
            {
                foreach(Transform t in origin.GetAttackOrigins())
                    skill.Runtime.Spawn.SpawnPositions.Add(
                        new SpawnPointData(
                            t.position,
                            target.rotation)
                        );
            }
        }
    }
}