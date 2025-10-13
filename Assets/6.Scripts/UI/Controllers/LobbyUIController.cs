using UnityEngine;

public class LobbyUIController 
    : UIController
{
    protected override void OnEnable()
    {
        base.OnEnable();

        ManagerWaiter.WaitForManager<UIManager>(uiManager =>
        {
            uiManager.OnJoinedLobby += UpdateCurrencies;
        });
    }

    protected void OnDisable()
    {
        if(ManagerWaiter.TryGetManager<UIManager>(out UIManager ui))
            ui.OnJoinedLobby -= UpdateCurrencies;
        
    }
}
