using UnityEngine;

public class Weapon_Combo : Weapon
{
    [SerializeField] protected SO_Combo so_Combo;

    public SO_Combo ComboData { get => so_Combo; }

    bool bEnable;
    bool bExist; 
    int index;

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

        animator.Play(so_Combo.comboDatas[index].ComboName);
        if (bDebug)
            Debug.Log($"Combo Play: {index} {so_Combo.comboDatas[index].ComboName}");
    }


}
