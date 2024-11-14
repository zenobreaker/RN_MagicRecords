using UnityEngine;

public class Weapon_Combo : Weapon
{
    [SerializeField] protected SO_Combo so_Combo;
    [SerializeField] protected string comboPrefixName = "Combo";
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
            doActionDatas[count] = data.doActionData;
            count++;
        }
    }

    public override void DoAction(int index)
    {
        base.DoAction(index);

        //this.index = index &= so_Combo.comboDatas.Count;
        
        this.index = index;
        string animName = comboPrefixName + "." + so_Combo.comboDatas[index].ComboName;
        animator.Play(animName);
        
        if (bDebug)
            Debug.Log($"Combo Play: {this.index} {so_Combo.comboDatas[index].ComboName}");
    }


}
