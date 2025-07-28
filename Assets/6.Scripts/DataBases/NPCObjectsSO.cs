using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NpcObject
{
    public int id;
    public GameObject obj;
}


[CreateAssetMenu(fileName = "NPCObjectsSO", menuName = "Scriptable Objects/NPCObjectsSO")]
public class NPCObjectsSO : ScriptableObject
{
    public List<NpcObject> list = new();

    private Dictionary<int, NpcObject> npcTable = new(); 
    public void Init()
    {
        foreach(NpcObject npc in list)
        {
            npcTable[npc.id] = npc;
        }
    }

    public GameObject GetNpcObject(int id)
    {
        if(npcTable.TryGetValue(id, out NpcObject npc))
        {
            return npc.obj;
        }

        return null;
    }
}
