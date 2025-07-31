using UnityEngine;

[CreateAssetMenu(fileName = "SO_PlayerObjects", menuName = "Scriptable Objects/SO_PlayerObjects")]
public class SO_PlayerObjects : SO_CharacterObjects
{
    public GameObject GetPlayerObject(int id)
    {
        if (charTable.TryGetValue(id, out CharacterObject pc))
        {
            return pc.obj;
        }

        return null;
    }
}