using UnityEngine;
using UnityEngine.UI;

public class UIPopUpRewards : UIPopUp
{
    [SerializeField] private Button exitButton;

    private ItemData[] rewardsItems;

    public void SetData(ItemData[] items)
    {
        this.rewardsItems = items;
        DrawPopUp();
    }
    protected override void DrawPopUp()
    {
        
    }
}
