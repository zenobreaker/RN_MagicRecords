using System;
using UnityEngine;

[Serializable]
public class DamageInfo
{
    public int HitImpactIndex;
    public string HitSoundName;
    public GameObject HitParticle;
    public Vector3 HitParticlePositionOffset = Vector3.zero;
    public Vector3 HitParticleSacleOffset = Vector3.one;
}


[Serializable]
public class DoActionData : ICloneable, IEquatable<DoActionData>
{
    [Header("Power Settings")]
    public float Power;
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

    public DamageInfo damageInfo = null; 
    public bool bCanMove;

    public virtual DoActionData DeepCopy()
    {
        DoActionData doActionData = new DoActionData();
        doActionData.Power = Power;
        doActionData.Distance = Distance;
        doActionData.HeightValue = HeightValue;
        doActionData.StopFrame = StopFrame;

        doActionData.bDownable = bDownable;
        doActionData.bLauncher = bLauncher;
        doActionData.impulseDirection = impulseDirection;

        doActionData.damageInfo = new DamageInfo
        {
            HitImpactIndex = damageInfo.HitImpactIndex,
            HitSoundName = damageInfo.HitSoundName,
            HitParticle = damageInfo.HitParticle,
            HitParticlePositionOffset = damageInfo.HitParticlePositionOffset,
            HitParticleSacleOffset = damageInfo.HitParticlePositionOffset
        };

        doActionData.bCanMove = bCanMove;

        return doActionData;
    }

    public virtual object Clone()
    {
       return this.MemberwiseClone();
    }

    public bool Equals(DoActionData other)
    {
        throw new NotImplementedException();
    }
}



public class Weapon : MonoBehaviour
{
    public bool bDebug = false;

    [Header("Weapon Settings")]
    [SerializeField] protected WeaponType type;
     protected DoActionData[] doActionDatas;
    public WeaponType Type { get => type; }

    private bool bEquipped;
    public bool Equipped { get => bEquipped; }
    protected int currentComboCount = 0;

    protected GameObject rootObject;    // 무기를 가진 대상
    protected Animator animator;
    protected WeaponController weaponController;

    protected StateComponent state;
    protected PlayerMovingComponent moving;

    private DashComponent dash;

    protected virtual void Awake()
    {
        rootObject = transform.root.gameObject;
        Debug.Assert(rootObject != null);

        state = rootObject.GetComponent<StateComponent>();
        animator = rootObject.GetComponent<Animator>();
        moving = rootObject.GetComponent<PlayerMovingComponent>();
        dash = rootObject.GetComponent<DashComponent>();
        Debug.Assert(dash != null);


        //TODO : 아직은 시기 상조 공격 후, 대쉬로 후딜 캔슬
        //dash.OnBeginDash += End_DoAction;
    }

    protected virtual void Start()
    {
        
    }

    public void Equip()
    {
        Debug.Log($"Equip : {type.ToString()}");

        //TODO : 장착 애니메이션이 없으므로 여기서 이 함수를 콜함

        if (rootObject.TryGetComponent(out IWeaponUser user))
        {
            weaponController = user.GetWeaponController();
        }

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

        state.SetActionMode();
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
        state.SetIdleMode();
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
