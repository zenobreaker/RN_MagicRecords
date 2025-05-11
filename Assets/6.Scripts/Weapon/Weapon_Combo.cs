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


    protected int index;
    protected bool bEnable;
    protected bool bExist; 

    protected override void Awake()
    {
        base.Awake();
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

        //this.index = index &= so_Combo.comboDatas.Count;
        if (animator == null)
        {
            Debug.LogError("Animator is Null");
            return;
        }

        this.index = index;

        Debug.Assert(so_Combo.comboDatas.Count > 0);
        Debug.Assert(so_Combo.comboDatas[index] != null);

        // Set Override 
        {
            //animator.runtimeAnimatorController = actionDatas[index].AnimatorOv;
            weaponController?.SetWeaponAnimation(actionDatas[index].WeaponAnimOv);
        }

        // Play Animation 
        {

            animator.SetFloat(actionDatas[index].ActionSpeedHash, actionDatas[index].ActionSpeed);
            //animator.Play(actionDatas[index].StateName, 0, 0);
            animator.SetTrigger(actionDatas[index].StateName);
            weaponController?.DoAction(actionDatas[index].StateName);


#if UNITY_EDITOR
            if (bDebug)
                Debug.Log($"Combo Play: {this.index} {actionDatas[index].StateName}");
#endif

        }
    }
}
