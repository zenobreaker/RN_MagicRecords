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

[System.Serializable]
public class IngredientItemDataJson : ItemDataJson
{
    public int ItemCategory;
}


public class EquipmentItemDataJsonAllData
{
    public List<EquipmentItemDataJson> equipmentDataJson;
}

public class IngredientItemDataJsonAllData
{
    public List<IngredientItemDataJson> ingredientDataJson;
}

public class ItemDataBase : DataBase
{
    [Header("장비 아이템")]
    [SerializeField] private TextAsset equipmentItemJson;

    [Header("재료 아이템")]
    [SerializeField] private TextAsset ingredientItemJson;

    private Dictionary<int, EquipmentItem> equipmentItems = new();
    private Dictionary<int, IngredientItem> ingredientItems = new();


    public override void Initialize()
    {
        InitializeEquipmentItemData();
        InitializeIngredientItemData();
    }

    public void InitializeEquipmentItemData()
    {
        if (equipmentItemJson == null) return;

        Debug.Log("Equipment Database Init");

        JsonLoader.LoadJsonList<EquipmentItemDataJsonAllData, EquipmentItemDataJson, EquipmentItem>
            (
                equipmentItemJson,
                root => root.equipmentDataJson,

                json =>
                {
                    var equipmentData = new EquipmentItem(
                        (int)json.id
                        , GetSprite(json.imagePath)
                        , (string)json.name
                        , (string)json.description
                        , (EquipParts)json.equipParts
                        , (StatusType)json.statusType
                        , (float)json.statusValue
                        , json.isPercent);
                    
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

    public void InitializeIngredientItemData()
    {
        if (ingredientItemJson == null) return;


        JsonLoader.LoadJsonList<IngredientItemDataJsonAllData, IngredientItemDataJson, IngredientItem>
            (
                ingredientItemJson,
                root => root.ingredientDataJson,

                json =>
                {
                    var data = new IngredientItem(
                        (int)json.id
                        , GetSprite(json.imagePath)
                        ,json.ItemCategory
                        );
                    return data;
                },

                ingredient =>
                {
                    ingredientItems.TryAdd(ingredient.id, ingredient);
                }
            );

        Debug.Log("===================================================");
        Debug.Log($"Complete Message => ingredientItems : {ingredientItems.Count}");
    }

        
    public EquipmentItem GetEquipmentItemData(int itemId)
    { 
        return equipmentItems.TryGetValue(itemId, out var equipmentItem) ? equipmentItem : null;
    }

    public IngredientItem GetIngredientItemData(int itemId)
    {
        return ingredientItems.TryGetValue(itemId, out var ingredientItem) ? ingredientItem : null;
    }

    private Sprite GetSprite(string path)
    {
        //Assets/7.Sprites/Resources/Equipments/Weapons/img_equipment_weapon_gun_0.png
        var sprite = Resources.Load<Sprite>(path);
        return sprite; 
    }

}
