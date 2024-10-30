using System;
using UnityEngine;

[Serializable]
public class ActionData : ICloneable, IEquatable<ActionData>
{
    [Header("Power Settings")]
    public float Pwoer;
    public float Distance;
    public float HeightValue;
    public int StopFrame; 

    [Header("Launch & Down Settings")]
    public bool bDownable = false;
    public bool bLauncher = false;

    [Header("Camera Shake")]
    public Vector3 impulseDirection;
    //TODO: Noise 가져오기
    //public Cinemachine settings;

    [Header("Hit")]
    public int HitImpactIndex;
    public string HitSoundName;

    public bool bCanMove;

    public object Clone()
    {
        throw new NotImplementedException();
    }

    public bool Equals(ActionData other)
    {
        throw new NotImplementedException();
    }
}



public class Weapon : MonoBehaviour
{


    private void Start()
    {
        
    }

}
