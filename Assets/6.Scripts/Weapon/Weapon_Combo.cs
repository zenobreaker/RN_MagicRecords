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

        this.index = index;

        Debug.Assert(so_Combo.comboDatas.Count > 0);
        Debug.Assert(so_Combo.comboDatas[index] != null);

        // Set Override 
        { 
            if(actionDatas[index].AnimatorOv != null)
                animator.runtimeAnimatorController = actionDatas[index].AnimatorOv;
            weaponController?.SetWeaponAnimation(actionDatas[index].WeaponAnimOv);
        }

        // Play Animation 
        {
            animator.SetFloat(actionDatas[index].ActionSpeedHash, actionDatas[index].ActionSpeed);
            animator.SetTrigger(actionDatas[index].StateName);
            weaponController?.DoAction(actionDatas[index].StateName);

#if UNITY_EDITOR
            if (bDebug)
                Debug.Log($"Combo Play: {this.index} {actionDatas[index].StateName}");
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
        damageDatas[index].PlayHitSound(); 

        if(other.TryGetComponent<IDamagable>(out IDamagable damage))
        {
            Vector3 hitPoint = colliders[index].ClosestPoint(other.transform.position);
            hitPoint = other.transform.InverseTransformPoint(hitPoint);
            damage.OnDamage(rootObject, this, hitPoint, damageDatas[index].GetMyDamageEvent(rootObject)); 
        }
    }
}
