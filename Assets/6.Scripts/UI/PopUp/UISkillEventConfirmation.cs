using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UISkillEventConfirmation : UIPopUp
{
    [SerializeField] private TMP_Text titleText, messageText;
    [SerializeField] private Button confirmButton, cancelButton;
    [SerializeField] private Toggle skipToggle;
    private Action<bool> onConfirm;
    public void SetData(string title, string message, bool showCheckbox, Action<bool> confirm)
    {
        titleText.text = title; messageText.text = message;
        skipToggle.gameObject.SetActive(showCheckbox);
        skipToggle.SetIsOnWithoutNotify(false);
        onConfirm = confirm;
        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(Confirm);
        cancelButton.onClick.RemoveAllListeners();
        cancelButton.onClick.AddListener(CloseUI);
        ShowPopUp();
    }
    private void Confirm()
    {
        var action = onConfirm;
        bool skip = skipToggle.gameObject.activeSelf && skipToggle.isOn;
        onConfirm = null;
        CloseUI(); // Remove this popup before invoking code which can open/close another UI.
        action?.Invoke(skip);
    }
    protected override void OnDisable() { onConfirm = null; base.OnDisable(); }
    protected override void DrawPopUp() { }
}
