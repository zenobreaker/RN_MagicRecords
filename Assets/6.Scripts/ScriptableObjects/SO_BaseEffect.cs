using UnityEngine;


[CreateAssetMenu(fileName = "SO_Effect", menuName = "Scriptable Objects/SO_BaseEffect ")]
public class SO_BaseEffect : ScriptableObject
{
    public string id;
    public string description; 
    public float tickInterval;

    public Sprite icon;
    public GameObject vfxObject;
}

