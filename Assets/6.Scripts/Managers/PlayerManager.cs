using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : 
    Singleton<PlayerManager>
{
    private List<int> characterIds = new List<int>();
    private Dictionary<int, CharStatusData> charStatusDatas = new();
    private Dictionary<int, CharEquipmentData> charEquipments = new(); 

    protected override void Awake()
    {
        base.Awake();
    
        // 임시적으로 리스트에 추가
        characterIds.Clear();
        characterIds.Add(1);

        charStatusDatas.Clear();
        charStatusDatas.Add(1, new TurtleInfoData(1, 1));

        charEquipments.Clear();
        charEquipments.Add(1, new CharEquipmentData { characterId = 1, });
        foreach (var ce in charEquipments)
        {
            ce.Value.Init();
        }
    }

    public CharEquipmentData GetCharEquipmentData(int charId)
    {
        charEquipments.TryGetValue(charId, out CharEquipmentData ce);
        return ce; 
    }

    public CharStatusData GetCharacterStatus(int charId)
    {
        charStatusDatas.TryGetValue(charId, out CharStatusData ce);
        return ce;
    }

}
