using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIPopUpPause : UIPopUpBase
{
    [SerializeField] protected Button exitButton;
    [SerializeField] protected Button stageContinueButton; 
    [SerializeField]  protected UIRecordInventory inventory;

    protected override void Awake()
    {
        base.Awake();

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(() =>
            {
                GameManager.Instance?.FinishStage();
                SceneManager.LoadScene(1);
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
            inventory?.SetRecordManager(AppManager.Instance.GetRecordManager());
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
