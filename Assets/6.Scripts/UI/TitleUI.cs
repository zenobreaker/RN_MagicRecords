using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TitleUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    [Header("UI References")]
    [Tooltip("인스펙터에서 프리팹이나 씬에 배치된 OptionPopupUI를 할당해주세요.")]
    [SerializeField] private GameObject optionPopupUIPrefab;

    private GameObject activeOptionPopup;

    private void Awake()
    {
        // 💡 1. 버튼 컴포넌트 방어 코드 (혹시 인스펙터에서 밀렸을 때를 대비)
        InitButtonReferences();

        // 💡 2. 버튼 리스너 연결
        startButton.onClick.AddListener(OnStartButtonClicked);
        settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        exitButton.onClick.AddListener(OnExitButtonClicked);
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지를 위한 리스너 해제 (습관화해두면 좋습니다!)
        startButton.onClick.RemoveAllListeners();
        settingsButton.onClick.RemoveAllListeners();
        exitButton.onClick.RemoveAllListeners();
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            if(SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayBGM("MainTheme1");
            }
        }
    }

    /// <summary>
    /// 게임 시작 버튼 -> 로비 씬으로 이동
    /// </summary>
    private void OnStartButtonClicked()
    {
        // 💡 "Lobby" 씬으로 이동 (씬 빌드 세팅에 Lobby 씬이 등록되어 있어야 합니다)
        SceneManager.LoadScene("Lobby");
    }

    /// <summary>
    /// 세팅 버튼 -> 옵션 팝업 UI 생성 및 표시
    /// </summary>
    private void OnSettingsButtonClicked()
    {
        if (optionPopupUIPrefab == null)
        {
            Debug.LogError("[TitleUI] OptionPopupUI 프리팹이 할당되지 않았습니다!");
            return;
        }
        
        if(UIManager.Instance != null)
        {
            UIPopUpOption ui = UIManager.Instance.OpenUI<UIPopUpOption>(true);
            if (ui == null)
                return; 
        }

    }

    /// <summary>
    /// 게임 종료 버튼 -> 프로그램 종료
    /// </summary>
    private void OnExitButtonClicked()
    {
#if UNITY_EDITOR
        // 에디터 환경에서 테스트할 때 플레이 모드를 종료해줍니다.
        UnityEditor.EditorApplication.isPlaying = false;
#else
            // 빌드된 실제 게임 프로그램 종료
            Application.Quit();
#endif
    }

    private void InitButtonReferences()
    {
        if (startButton == null) startButton = transform.Find("Btn_Start")?.GetComponent<Button>();
        if (settingsButton == null) settingsButton = transform.Find("Btn_Settings")?.GetComponent<Button>();
        if (exitButton == null) exitButton = transform.Find("Btn_Exit")?.GetComponent<Button>();
    }
}