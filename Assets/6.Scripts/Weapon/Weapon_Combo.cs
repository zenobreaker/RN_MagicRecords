using UnityEngine;

public class Weapon_Combo : Weapon
{
    [SerializeField] protected SO_Combo so_Combo;
    //[SerializeField] protected string comboPrefixName = "";
    public SO_Combo ComboData { get => so_Combo; }

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
        
        animator.runtimeAnimatorController = so_Combo.comboDatas[index].AnimatorOv;
        animator.SetFloat(so_Combo.comboDatas[index].ActionSpeedHash, so_Combo.comboDatas[index].ActionSpeed);
        animator.Play(so_Combo.comboDatas[index].StateName, 0, 0);

        if (bDebug)
            Debug.Log($"Combo Play: {this.index} {so_Combo.comboDatas[index].StateName}");
    }


}
