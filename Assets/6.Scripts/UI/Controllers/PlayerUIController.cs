using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerUIController : UIController
{
    [SerializeField] private Button pauseButton; 

    private void Awake()
    {
        ManagerWaiter.RegisterManagerEvent<UIManager>(this,
            onRegister: ui =>
            {
                if (ui != null)
                {
                    if(pauseButton != null)
                    {
                        pauseButton.onClick.AddListener(() =>
                        {
                            ui.OpenPausePopUp(); 
                        });
                    }
                }

            },
            onUnregister: ui => { });
    }

}
