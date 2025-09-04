using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class ItemDataJson
{
    public int id;
    public string keycode;
    public string name;
    public string description;
    public string imagePath;
}

[System.Serializable]
public class EquipmentItemDataJson : ItemDataJson
{
    public int equipParts;
    public int rank;
    public int statusType;
    public float statusValue;
    public bool isPercent;
    public float itemValue;
}

public class EquipmentItemDataJsonAllData
{
    public List<EquipmentItemDataJson> equipmentDataJson;
}

public class ItemDataBase : MonoBehaviour
{
    [Header("장비 아이템")]
    [SerializeField] private TextAsset equipmentItemJson;
    [SerializeField] private Dictionary<int, EquipmentItem> equipmentItems = new();

    public void InitialzeEquipmentItemData()
    {
        if (equipmentItemJson == null) return;

        Debug.Log("Equipment Database Init");

        JsonLoader.LoadJsonList<EquipmentItemDataJsonAllData, EquipmentItemDataJson, EquipmentItem>
            (
                equipmentItemJson,
                root => root.equipmentDataJson,

                json =>
                {
                    var equipmentData = new EquipmentItem
                    {
                        id = (int)json.id,
                        name = (string)json.name,
                        description = (string)json.description,
                        parts = (EquipParts)json.equipParts,
                        mainStatus = (StatusType)json.statusType,
                        mainValue = (float)json.statusValue,
                        
                    };
                    return equipmentData;
                },

                equipment =>
                {
                    equipmentItems.TryAdd(equipment.id, equipment);
                }
            );

        // Complete Message 
        Debug.Log("===================================================");
        Debug.Log($"Complete Message => equipmentItems : {equipmentItems.Count}");
    }

        
    public EquipmentItem GetEquipmentItemData(int itemId)
    { 
        return equipmentItems.TryGetValue(itemId, out var equipmentItem) ? equipmentItem : null;
    }
}
