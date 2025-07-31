using UnityEngine;

[CreateAssetMenu(fileName = "SO_NPCObjects", menuName = "Scriptable Objects/SO_NPCObjects")]
public class SO_NPCObjects : SO_CharacterObjects
{
    public GameObject GetNpcObject(int id)
    {
        if(charTable.TryGetValue(id, out CharacterObject npc))
        {
            return npc.obj;
        }

        return null;
    }
}
