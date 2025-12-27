using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class Weapon_Combo : Weapon
{
    [SerializeField] protected SO_Combo so_Combo;
    public SO_Combo ComboData { get => so_Combo; }


    #region Cinenmachine
    protected CinemachineImpulseSource impulse;
    protected CinemachineImpulseListener listener;
    protected CinemachineBrain brain;
    #endregion

    protected Collider[] colliders;
    protected List<GameObject> hitList;

    protected int index;
    protected bool bEnable;
    protected bool bExist; 

    protected override void Awake()
    {
        base.Awake();

        colliders = GetComponentsInChildren<Collider>();
        hitList = new List<GameObject>();
    }

    protected override void Start()
    {
        base.Start();

        brain = Camera.main.GetComponent<CinemachineBrain>();
        impulse = GetComponent<CinemachineImpulseSource>();
        if(brain != null)
        {
            listener = brain.GetComponent<CinemachineImpulseListener>();
        }
    }

    public override void DoAction(int index)
    {
        base.DoAction(index);

        Debug.Assert(animator != null, "Animation is null");

        this.index = index % so_Combo.MaxComboIndex();

        if(this.index == so_Combo.MaxComboIndex() -1)
        {
            InvokeLastAttackEvent();
        }

        Debug.Assert(so_Combo.comboDatas.Count > 0);
        Debug.Assert(so_Combo.comboDatas[this.index] != null);

  
        // Play Animation 
        {
            animator.SetFloat(actionDatas[this.index].ActionSpeedHash, actionDatas[this.index].ActionSpeed);
            animator.CrossFade(actionDatas[this.index].StateName, 0.1f);
            weaponController?.DoAction(actionDatas[this.index].WeaponActionName);

#if UNITY_EDITOR
            if (bDebug)
                Debug.Log($"Combo Play: {this.index} {actionDatas[this.index].StateName}");
#endif
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == rootObject) return;
        if (other.gameObject.CompareTag(rootObject.tag)) return;
        if (hitList.Contains(other.gameObject)) return;

        hitList.Add(other.gameObject);

        OnDamage(other);
    }

    protected virtual void OnDamage(Collider other)
    {
        damageDatas[this.index].PlayHitSound(); 

        if(other.TryGetComponent<IDamagable>(out IDamagable damage))
        {
            Vector3 hitPoint = colliders[this.index].ClosestPoint(other.transform.position);
            hitPoint = other.transform.InverseTransformPoint(hitPoint);
            damage.OnDamage(rootObject, this, hitPoint,
                damageDatas[this.index].GetMyDamageEvent(rootObject)); 
        }
    }
}
