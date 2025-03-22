using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public UiBase soundUI; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // GameManager�� �� ��ȯ �� ����
        }
        else
        {
            Destroy(gameObject);
        }

    }


    public void TogglePauseMenu()
    {
        
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
