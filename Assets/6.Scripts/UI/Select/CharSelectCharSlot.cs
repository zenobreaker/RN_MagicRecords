using TMPro;
using UnityEngine;
using UnityEngine.UI;



public class CharData
{
    public int charId;
    public string charName;
    public Sprite charIcon;
}

public class CharSelectCharSlot : UISlot<CharData>
{
    [SerializeField] private Sprite noDataIcon;
    [SerializeField] protected Image portImage;
    [SerializeField] protected Image slotBackgroundImage; 
    [SerializeField] protected Image selectFrame; 
    [SerializeField] protected TMP_Text nameText; 

    protected CharData charData;
    public int CurrentCharId => charData?.charId ?? -1; 

    public void SetCharData(CharData charData)
    {
        this.charData = charData;
        
        Refresh(); 
    }

    public override void Refresh()
    {
        if(portImage != null)
        {
            if (charData == null)
                portImage.sprite = noDataIcon;
            else
                portImage.sprite = charData.charIcon;
        }

        if(nameText != null)
        {
            nameText.text = LocalizationManager.Instance.SafeInvoke(v => v.GetText(charData.charName));  
        }

        if (selectFrame != null)
            selectFrame.gameObject.SetActive(false); 
    }

    public override void OnClick()
    {
        InvokeClick(charData);
    }

    public void SetSelected(bool isSelect)
    {
        if (selectFrame != null)
            selectFrame.gameObject.SetActive(isSelect);
    }
}
