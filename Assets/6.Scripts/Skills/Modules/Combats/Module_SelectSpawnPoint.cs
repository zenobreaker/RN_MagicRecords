using System;
using UnityEngine;

public enum SpawnPointSelectType
{
    All,
    Specific,
    Sequential,
    Random
}

[ModuleCategory("Combat/Select Spawn Point")]
[Serializable]
public class Module_SelectSpawnPoint : SkillModule
{
    public SpawnPointSelectType selectType = SpawnPointSelectType.All;

    [Tooltip("Specific일 경우 사용할 SpawnPoint 인덱스")]
    public int spawnPointIndex = 0;

    [Tooltip("Specific일 때 사용할 총구 인덱스")]
    public int[] indices;

    public override void OnNotify(
        Character owner,
        ActiveSkill skill,
        PhaseSkill phaseSkill)
    {
        var spawn = skill.Runtime.Spawn;

        spawn.SelectedSpawnPointIndices.Clear();

        switch (selectType)
        {
            case SpawnPointSelectType.All:

                for (int i = 0; i < spawn.SpawnPoints.Count; i++)
                    spawn.SelectedSpawnPointIndices.Add(i);

                break;

            case SpawnPointSelectType.Specific:
                    spawn.SelectedSpawnPointIndices.Add(spawnPointIndex);
                break;
            
        }
    }
}