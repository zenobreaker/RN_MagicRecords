using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CharacterObject
{
    public int id;
    public GameObject obj;
}


public class SO_CharacterObjects : ScriptableObject
{
    public List<CharacterObject> list = new();
    protected Dictionary<int, CharacterObject> charTable = new();

    public void Init()
    {
        foreach (CharacterObject co in list)
            charTable[co.id] = co;
    }
}
