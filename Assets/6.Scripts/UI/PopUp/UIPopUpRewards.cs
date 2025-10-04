using UnityEngine;
using UnityEngine.UI;

public class UIPopUpRewards : UIPopUp
{
    [SerializeField] private Button exitButton;

    private ItemData[] rewardItems;

    public void SetData(ItemData[] items)
    {
        this.rewardItems = items;
        DrawPopUp();
    }

    protected override void DrawPopUp()
    {
        InitReplaceContentObject(rewardItems.Length);

        int index = 0;
        SetContentChildObjectsCallback<UIRewardSlot>(slot =>
        {
            if(index <  rewardItems.Length)
            {
                slot.SetItemData(rewardItems[index]);
                slot.gameObject.SetActive(true);
                index++; 
            }
            else
            {
                slot.gameObject.SetActive(false);
            }
        });
    }
}
