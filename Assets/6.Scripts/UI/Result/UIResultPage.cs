using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIResultPage : UiBase
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Image illust;
    [SerializeField] private Image fadeImage; // 전체화면을 덮는 검은 반투명 이미지
    [SerializeField] private GameObject contentGroup; // 텍스트, 일러스트, 버튼을 담은 부모 객체
    [SerializeField] private Button confirmButton;

    [Header("Resources")]
    [SerializeField] private Sprite winSprite;
    [SerializeField] private Sprite loseSprite;

    private bool isSuccess;

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmClicked);

        if (fadeImage != null)
            fadeImage.gameObject.SetActive(false);

        if (contentGroup != null)
            contentGroup.gameObject.SetActive(false);
    }

    public void Show(bool isWin)
    {
        isSuccess = isWin;

        if (fadeImage != null)
            fadeImage.gameObject.SetActive(true);

        if (contentGroup != null)
            contentGroup.gameObject.SetActive(true);


        if (isWin)
        {
            if (resultText != null)
                resultText.text = "STAGE CLEAR";
        }
        else
        {
            if (resultText != null)
                resultText.text = "STAGE FAILED";
        }
    }

    private void OnConfirmClicked()
    {
        // 일반적인 스테이지 클리어로 인한 퇴장 
        // 스테이지 선택 씬으로 이동 
        SceneManager.LoadScene(1);

        UIManager.Instance?.CloseTopUI();
    }
}


