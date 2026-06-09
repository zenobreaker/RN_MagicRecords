using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIPopUpTwoButton : UIPopUp
{
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI contextText;
    [SerializeField] TextMeshProUGUI confirmText;
    [SerializeField] TextMeshProUGUI cancelText;
    [SerializeField] Button confirmButton;
    [SerializeField] Button cancelButton;

    string title, context, confirm, cancel;

    public void SetData(string title, string messsage, string confirmText,
        string cancelText, UnityAction onConfirm, UnityAction onCancel)
    {
        this.title = title;
        this.context = messsage;
        this.confirm = confirmText;
        this.cancel = cancelText;


        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(onConfirm);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(onCancel);
        }

        ShowPopUp();
    }

    protected override void DrawPopUp()
    {
        if (titleText != null)
        {
            titleText.text = LocalizationManager.Instance.SafeInvoke(v => v.GetText(title));
        }

        if(contextText != null)
        {
            contextText.text = LocalizationManager.Instance.SafeInvoke(v => v.GetText(context));
        }

        if (confirmText != null)
        {
            confirmText.text = LocalizationManager.Instance.SafeInvoke(v => v.GetText(confirm));
        }

        if (cancelText != null)
        {
            cancelText.text = LocalizationManager.Instance.SafeInvoke(v => v.GetText(cancel));
        }
    }
}
