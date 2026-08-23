using System;
using UnityEngine;

public enum SpawnPointSelectType
{
    All,
    Specific,
    Sequential,
    Random,
    TargetPointInBlackboard,
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

            case SpawnPointSelectType.Random:

                int randomIndex =
                    UnityEngine.Random.Range(
                        0,
                        spawn.SpawnPoints.Count);

                spawn.SelectedSpawnPointIndices.Add(
                    randomIndex);

                break;

            case SpawnPointSelectType.Sequential:

                // 이건 나중에 Runtime에
                // CurrentSpawnPointIndex 같은 상태를
                // 둘 필요가 있음.
                break;


            case SpawnPointSelectType.TargetPointInBlackboard:
                {
                    spawn.SpawnPoints.Clear();
                    spawn.SelectedSpawnPointIndices.Clear();

                    spawn.SpawnPoints.Add(new SpawnPointData
                    {
                        position = skill.Runtime.Spawn.TargetPosition,
                        rotation = owner.transform.rotation
                    });

                    spawn.SelectedSpawnPointIndices.Add(0);
                }
                break;
        }
    }
}