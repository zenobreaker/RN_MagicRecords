using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CharacterInfo
{
    public int id;
    public Sprite charSprite;
    public string name; 
    public GameObject obj;
}


public class SO_CharacterObjects : ScriptableObject
{
    public List<CharacterInfo> list = new();
    protected Dictionary<int, CharacterInfo> charTable = new();

    public void Init()
    {
        foreach (CharacterInfo co in list)
            charTable[co.id] = co;
    }
}
