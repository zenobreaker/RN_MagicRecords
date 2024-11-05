using System;
using UnityEngine;

[Serializable]
public class DoActionData : ICloneable, IEquatable<DoActionData>
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

    public bool Equals(DoActionData other)
    {
        throw new NotImplementedException();
    }
}



public class Weapon : MonoBehaviour
{
    [SerializeField] protected WeaponType type;
    [SerializeField] protected DoActionData[] doActionDatas;
    public WeaponType Type { get => type; }

    private bool bEquipped;
    public bool Equipped { get => bEquipped; }
    protected int currentComboCount = 0;

    protected GameObject rootObject;    // 무기를 가진 대상
    protected Animator animator;
    
    protected StateComponent state;
    protected PlayerMovingComponent moving;

    public bool bDebug = false;

    protected virtual void Awake()
    {
        rootObject = transform.root.gameObject;
        Debug.Assert(rootObject != null);

        state = rootObject.GetComponent<StateComponent>();
        animator = rootObject.GetComponent<Animator>();
        moving = rootObject.GetComponent<PlayerMovingComponent>();  
    }

    protected virtual void Start()
    {
        
    }

    public void Equip()
    {
        Debug.Log($"Equip : {type.ToString()}");

        //TODO : 장착 애니메이션이 없으므로 여기서 이 함수를 콜함
        Begin_Equip();
    }

    public virtual void Begin_Equip()
    {

    }

    public virtual void End_Equip()
    {
        bEquipped = true; 
    }

    public virtual void Unequip()
    {
        bEquipped = false;
    }


    public void DoIdleAction()
    {
        //animator?.Play
    }

    public virtual void DoAction()
    {
        if (state.IdleMode == false)
            return; 

        CheckStop(0);
    }

    public virtual void DoAction(int index = 0)
    {
        if (state.IdleMode == false)
            return;

        CheckStop(index);
    }

    public virtual void Begin_DoAction()
    {

    }

    public virtual void End_DoAction()
    {
        
    }


    ////////////////////////////////////////////////////////////////////////////////////
    
    protected void Move()
    {
        moving?.Move();
    }

    protected void Stop()
    {
        moving?.Stop();
    }

    protected void CheckStop(int index)
    {
        if (doActionDatas[index].bCanMove == false)
            Stop();
    }
}
