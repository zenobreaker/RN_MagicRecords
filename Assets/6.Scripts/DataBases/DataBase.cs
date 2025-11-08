using UnityEngine;

public abstract class DataBase : MonoBehaviour
{
    [SerializeField] protected TextAsset jsonAsset;

    public abstract void Initialize();

    protected Sprite GetSprite(string path)
    {
        //Assets/7.Sprites/Resources/Equipments/Weapons/img_equipment_weapon_gun_0.png
        var sprite = Resources.Load<Sprite>(path);
        return sprite;
    }

}
