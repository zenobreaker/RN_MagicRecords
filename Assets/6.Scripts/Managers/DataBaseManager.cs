using UnityEngine;

public class DataBaseManager : MonoBehaviour
{
    private StageDataBase stageDataBase;
    private MonsterDataBase monsterDataBase;
    private ItemDataBase itemDataBase; 

    private void Awake()
    {
        if (TryGetComponent<StageDataBase>(out stageDataBase))
            stageDataBase.InitializeStageData();

        if (TryGetComponent<MonsterDataBase>(out monsterDataBase))
            monsterDataBase.InitializeData();

        if (TryGetComponent<ItemDataBase>(out itemDataBase))
            itemDataBase.InitialzeEquipmentItemData();
    }

    public int GetRandomStageID(int chapter)
    {
        if (stageDataBase == null) return -1;

        return stageDataBase.GetRandomStageIDByChater(chapter);
    }

    public StageInfo GetStageInfo(int stageID)
    {
        if (stageDataBase == null) return null;

        return stageDataBase.GetStageInfo(stageID);
    }

    public MonsterGroupData GetMonsterGroupData(int groupID)
    {
        if (monsterDataBase == null) return null;

        return monsterDataBase.GetMonsterGroupData(groupID);
    }

    public MonsterStatData GetMonsterStatData(int monsterID)
    {
        if(monsterDataBase == null) return null;
        return monsterDataBase.GetMonsterStatData(monsterID);
    }

    public EquipmentItem GetEquipmentItem(int itemId)
    {
        return itemDataBase?.GetEquipmentItemData(itemId);
    }
}
