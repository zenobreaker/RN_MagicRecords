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
                UIManager.Instance.ClosePopup();
            });
        }
    }


    protected override void OnEnable()
    {
        base.OnEnable();
        Time.timeScale = 0;
        if (AppManager.Instance != null)
            inventory?.SetRecordManager(AppManager.Instance.GetRecordManager());
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        Time.timeScale = 1;
    }

    protected override void DrawPopUp()
    {
        
    }
}
