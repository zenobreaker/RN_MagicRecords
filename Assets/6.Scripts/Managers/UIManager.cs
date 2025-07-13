using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private Stack<UiBase> openedUIs = new Stack<UiBase>();

    public UiBase soundUI; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // GameManager를 씬 전환 간 유지
        }
        else
        {
            Destroy(gameObject);
        }

    }


    public void TogglePauseMenu()
    {
        
    }

    public void OpenUI (UiBase ui)
    {
        if (ui == null) return; 
        ui.gameObject.SetActive(true);
        openedUIs.Push(ui); 
    }

    public void CloseTopUI()
    {
        if(openedUIs.Count > 0)
        {
            var top = openedUIs.Pop();
            top.CloseUI();
        }
    }

    public void CloseAllOpenedUI()
    {
        while(openedUIs.Count > 0)
        {
            var top = openedUIs.Pop();
            top.CloseUI();
        }
    }

    #region GameOver
    //-------------------------------------------------------------------------
    // Game Over 
    //-------------------------------------------------------------------------

    public void ShowGameOverUI()
    {
        
    }

    public void HideGameOverUI()
    {
        
    }
    #endregion


    #region Sound 
    //-------------------------------------------------------------------------
    // Sound 
    //-------------------------------------------------------------------------


    public void ToggleSoundUI()
    {
        bool? isActive = soundUI?.gameObject.activeSelf;
        soundUI?.gameObject.SetActive(!isActive.Value);
    }

    public void ShowSoundControlUI()
    {
        soundUI?.gameObject.SetActive(true);
    }

    public void HidSoundControlUI()
    {
        soundUI?.gameObject.SetActive(false);
    }


    #endregion

}
