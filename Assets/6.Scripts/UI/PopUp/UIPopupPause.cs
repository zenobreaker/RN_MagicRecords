using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIPopUpPause : UIPopUp
{
    [SerializeField] protected Button exitButton;
    [SerializeField] protected Button stageContinueButton;
    [SerializeField]  protected UIRecordInventory inventory;
    [SerializeField] private Button statusButton;

    protected override void Awake()
    {
        base.Awake();

        if (statusButton != null)
            statusButton.onClick.AddListener(() => UIManager.Instance.SafeInvoke(ui => ui.OpenCharacterStatusPopUp()));

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(() =>
            {
                SceneManager.LoadScene("StageSelectScene");
            });
        }

        if (stageContinueButton != null)
        {
            stageContinueButton.onClick.AddListener(() =>
            {
                UIManager.Instance.CloseTopUI();
            });
        }
    }


    protected override void OnEnable()
    {
        base.OnEnable();
        PauseManager.RequestPause();
        if (AppManager.Instance != null)
        {
            inventory.SafeInvoke(ui => ui.SetRecordManager(AppManager.Instance.GetRecordManager()));
            inventory.SafeInvoke(ui => ui.RefreshUI());
        }

        ShowPopUp();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        PauseManager.RequestResume();
    }

    protected override void DrawPopUp()
    {

    }
}
