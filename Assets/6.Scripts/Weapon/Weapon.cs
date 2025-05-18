using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HitData
{
    public DamageType DamageType;
    public float Distance;
    public float HeightValue;
    public int StopFrame;
    public int HitImpactIndex;
    public string HitSoundName;
    public GameObject HitParticle;
    public Vector3 HitParticlePositionOffset = Vector3.zero;
    public Vector3 HitParticleSacleOffset = Vector3.one;
}


[Serializable]
public class DamageData
{
    [Header("Power Settings")]
    public DamageType damageType;
    public float Power = 1.0f;


    [Header("Launch & Down Settings")]
    public bool bDownable = false;
    public bool bLauncher = false;

    [Header("Sound")]
    public string SoundName;

    [Header("Camera Shake")]
    public Vector3 impulseDirection;
    //TODO: Noise 가져오기
    public Unity.Cinemachine.NoiseSettings settings;

    [Header("Hit")]
    public HitData hitData;

    public DamageEvent GetMyDamageEvent(GameObject attacker, bool bFirstHit = false)
    {
        if (attacker == null)
            return null;

        return GetMyDamageEvent(attacker, attacker.GetComponent<StatusComponent>(), bFirstHit);
    }

    public DamageEvent GetMyDamageEvent(GameObject attacker, StatusComponent status, bool bFirstHit = false)
    {
        if (attacker == null)
            return null;

        if (status == null)
            return null;

        return DamageCalculator.GetMyDamageEvent(status, this, bFirstHit);
    }

}

[Serializable]
public class DamageSequence
{
    public float hitDelay = -1.0f;
    public List<DamageData> damageDatas = new List<DamageData>();
}


[Serializable]
public class ActionData
{

    [Header("Action State")]
    [SerializeField]
    private string stateName;
    public string StateName { get => stateName; }

    [Header("Action Speed")]
    [SerializeField] private float actionSpeed = 1.0f;
    public float ActionSpeed { get => actionSpeed; }
    // StateName을 해시 값으로 저장
    private int actionSpeedHash = -1;
    public int ActionSpeedHash
    {
        get
        {
            if (actionSpeedHash == -1)
                actionSpeedHash = Animator.StringToHash("ActionSpeed");
            return actionSpeedHash;
        }
    }

    [Header("Character Anim")]
    [SerializeField]
    private AnimatorOverrideController animatorOv;
    public AnimatorOverrideController AnimatorOv => animatorOv;

    [Header("Weapon Anim")]
    [SerializeField]
    private AnimatorOverrideController weaponAnimOv;
    public AnimatorOverrideController WeaponAnimOv => weaponAnimOv;

    [Header("Sound")]
    public string SoundName;

    [Header("Camera Shake")]
    public Vector3 impulseDirection;
    //TODO: Noise 가져오기
    public Unity.Cinemachine.NoiseSettings settings;


    [Header("ETC")]
    public bool bCanMove;
    //public bool bFixedCamera;

    public virtual ActionData DeepCopy()
    {
        ActionData actionData = new ActionData();

        actionData.animatorOv = animatorOv;
        actionData.weaponAnimOv = weaponAnimOv;

        actionData.bCanMove = bCanMove;

        return actionData;
    }

    public void Play_Sound()
    {
        SoundManager.Instance.PlaySFX(SoundName);
    }

    public void Play_CameraShake()
    {
        if (MovableCameraShaker.Instance != null)
            MovableCameraShaker.Instance.Play_Impulse(settings);
    }
}

public class DamageEvent
{
    public float value;
    public bool isCrit;
    public bool isFisrtHit;

    public HitData hitData;

    public DamageEvent(float value, bool isCrit = false, bool isFisrtHit = false, HitData hitData = null)
    {
        this.value = value;
        this.isCrit = isCrit;
        this.isFisrtHit = isFisrtHit;
        this.hitData = hitData;
    }
}



public class Weapon : MonoBehaviour
{
    public bool bDebug = false;

    [Header("Weapon Settings")]
    [SerializeField] protected WeaponType type;
    public WeaponType Type { get => type; }

    [SerializeField] protected SO_Action so_Action;
    public SO_Action GetActionData { get => so_Action; }
    [SerializeField] protected SO_Damage so_Damage;
    public SO_Damage GetDamageData { get => so_Damage; }

    protected List<ActionData> actionDatas;
    protected List<DamageData> damageDatas;

    private bool bEquipped;
    public bool Equipped { get => bEquipped; }
    protected int currentComboCount = 0;

    private bool bDirtyMove = false;

    protected GameObject rootObject;    // 무기를 가진 대상
    protected Animator animator;
    protected WeaponController weaponController;

    protected StateComponent state;
    protected StatusComponent status;
    protected PlayerMovingComponent moving;

    private DashComponent dash;

    protected virtual void Awake()
    {
        rootObject = transform.root.gameObject;
        Debug.Assert(rootObject != null);

        state = rootObject.GetComponent<StateComponent>();
        status = rootObject.GetComponent<StatusComponent>();
        animator = rootObject.GetComponent<Animator>();
        moving = rootObject.GetComponent<PlayerMovingComponent>();
        dash = rootObject.GetComponent<DashComponent>();
        Debug.Assert(dash != null);


        //TODO : 아직은 시기 상조 공격 후, 대쉬로 후딜 캔슬
        //dash.OnBeginDash += End_DoAction;

        if (so_Action != null)
            actionDatas = so_Action.actionDatas;
        if (so_Damage != null)
            damageDatas = so_Damage.damageDatas;
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

    public virtual void Begin_Equip() { }

    public virtual void End_Equip()
    {
        bEquipped = true;
    }

    public virtual void Unequip()
    {
        bEquipped = false;
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

    public virtual void Begin_DoAction() { }

    public virtual void End_DoAction()
    {
        state.SetIdleMode();

        if (bDirtyMove)
        {
            bDirtyMove = false;
            Move();
        }
    }

    public virtual void Begin_JudgeAttack() { }
    public virtual void End_JudgeAttack() { }

    public virtual void Play_PlaySound() { }

    public virtual void Play_CameraShake() { }

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
        if (so_Action != null && so_Action.GetCanMove(index) == false)
        {
            Stop();
            bDirtyMove = true;
        }
    }
}
