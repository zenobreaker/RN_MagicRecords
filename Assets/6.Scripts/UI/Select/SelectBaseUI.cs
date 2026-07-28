
using UnityEngine;
using UnityEngine.UI;

public class SelectBaseUI : UIPopUp
{
    [Header("Confirm UI")]
    [SerializeField] private Button completeButton;


    protected override void Awake()
    {
        base.Awake();
        if (completeButton != null)
            completeButton.onClick.AddListener(OnCompleteSelect);
    }


    protected override void DrawPopUp()
    {

    }

    protected virtual void OnCompleteSelect()
    {
       
    }
}
