using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerUIController : UIController
{
    [SerializeField] private Button pauseButton;
    [SerializeField] private GameObject[] combatHUD;

    /// <summary>이동 조작은 유지하면서 무기와 스킬 HUD만 표시하거나 숨깁니다.</summary>
    public void SetCombatHUDVisible(bool visible)
    {
        foreach (var target in combatHUD)
        {
            target.SafeInvoke(value => value.SetActive(visible));
        }
    }

    private void Awake()
    {
        ManagerWaiter.RegisterManagerEvent<UIManager>(this,
            onRegister: ui =>
            {
                if (ui != null)
                {
                    if (pauseButton != null)
                    {
                        pauseButton.onClick.AddListener(() =>
                        {
                            ui.OpenPausePopUp();
                        });
                    }
                }

            },
            onUnregister: ui =>
            {
            });

    }
}
