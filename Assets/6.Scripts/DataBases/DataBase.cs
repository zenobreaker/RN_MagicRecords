using UnityEngine;


[System.Serializable]
public abstract class InfoJson
{
    public int id;
    public string desc; 
}

[System.Serializable]
public abstract class InfoBase
{
    public int id;
    public string desc; 
}

public abstract class DataBase : MonoBehaviour
{
    [SerializeField] protected TextAsset jsonAsset;

    public abstract void Initialize();
}
