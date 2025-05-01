using Unity.Cinemachine;
using UnityEngine;

public class Weapon_Combo : Weapon
{
    [SerializeField] protected SO_Combo so_Combo;
    //[SerializeField] protected string comboPrefixName = "";
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

        doActionDatas = new DoActionData[so_Combo.comboDatas.Count];
        int count = 0; 
        foreach (var data in so_Combo?.comboDatas)
        {
            doActionDatas[count] = data.DoAction;
            count++;
        }
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
            animator.runtimeAnimatorController = so_Combo.comboDatas[index].AnimatorOv;
            weaponController?.SetWeaponAnimation(so_Combo.comboDatas[index].WeaponAnimOv);
        }

        // Play Animation 
        {
            animator.SetFloat(so_Combo.comboDatas[index].ActionSpeedHash, so_Combo.comboDatas[index].ActionSpeed);
            animator.Play(so_Combo.comboDatas[index].StateName, 0, 0);
            weaponController?.DoAction(so_Combo.comboDatas[index].StateName);
        }

#if UNITY_EDITOR
        if (bDebug)
            Debug.Log($"Combo Play: {this.index} {so_Combo.comboDatas[index].StateName}");
#endif
    }

    public virtual void Play_Impulse(DoActionData data)
    {
        if (impulse == null || data == null)
            return;
        if (data.settings == null)
            return;
#if UNITY_EDITOR
        Debug.Log("Shake!");
#endif
        listener.ReactionSettings.m_SecondaryNoise = data.settings;
        impulse.GenerateImpulse();
    }
}
