using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_Action", menuName = "Scriptable Objects/SO_Action")]
public class SO_Action : ScriptableObject
{
    private int index;

    public List<ActionData> actionDatas = new List<ActionData>();

    public bool GetCanMove(int index)
    { 
        return actionDatas.Count > index && actionDatas[index].bCanMove;
    }
}
