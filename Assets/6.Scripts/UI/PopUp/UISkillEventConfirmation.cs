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
    private Action onCancel;
    private bool resolved = true;
    protected override void Awake()
    {
        base.Awake();
        // Older prefabs can predate UIPopUp's RequireComponent.
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }
    public void SetData(string title, string message, bool showCheckbox, Action<bool> confirm, Action cancel = null)
    {
        CancelPending();
        titleText.text = title;
        messageText.text = message;
        skipToggle.gameObject.SetActive(showCheckbox);
        skipToggle.SetIsOnWithoutNotify(false);
        onConfirm = confirm;
        onCancel = cancel;
        resolved = false;
        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(Confirm);
        cancelButton.onClick.RemoveAllListeners();
        cancelButton.onClick.AddListener(CloseUI);
        ShowPopUp();
    }
    private void Confirm()
    {
        if (resolved || isActiveAndEnabled == false)
        {
            return;
        }

        var action = onConfirm;
        bool skip = skipToggle.gameObject.activeSelf && skipToggle.isOn;
        onConfirm = null;
        onCancel = null;
        resolved = true;
        base.CloseUI(); // Confirm is the only path that may invoke the purchase callback.
        action?.Invoke(skip);
    }
    private void CancelPending()
    {
        var cancel = resolved ? null : onCancel;
        resolved = true;
        onConfirm = null;
        onCancel = null;
        cancel?.Invoke();
    }
    public override void CloseUI()
    {
        CancelPending();
        base.CloseUI();
    }
    protected override void OnDisable()
    {
        CancelPending();
        base.OnDisable();
    }
    protected override void DrawPopUp()
    {
    }
}
