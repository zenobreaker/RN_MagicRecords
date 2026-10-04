using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(this.gameObject);
    }

    private void Update()
    {
        if (CheatConsoleUI.CapturesInput) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if(UIManager.Instance != null)
                UIManager.Instance.CloseTopUI();
        }
    }

}
