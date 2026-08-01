using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum ExplorationStep
{
    Character = 0,
    Skill = 1,
    Final = Skill,
}

public class UIExplorationSetup : UIPopUp
{
    [Header("UI Pages (Inspector에서 할당)")]
    [SerializeField] private GameObject characterPageObj;
    [SerializeField] private GameObject skillPageObj;

    [Header("Navigation Buttons")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button startButton;

    private Dictionary<ExplorationStep, IExplorationSetupPage> pages = new();
    private Dictionary<ExplorationStep, GameObject> pageObjects = new();

    private ExplorationStep currentStep = ExplorationStep.Character;
    private ExplorationSetupData setupContext = new();

    protected override void Awake()
    {
        base.Awake();

        // 페이지 객체 매핑
        pageObjects[ExplorationStep.Character] = characterPageObj;
        pageObjects[ExplorationStep.Skill] = skillPageObj;

        // 인터페이스 캐싱
        foreach (var kvp in pageObjects)
        {
            if (kvp.Value != null)
            {
                pages[kvp.Key] = kvp.Value.GetComponent<IExplorationSetupPage>();
                if (kvp.Value.TryGetComponent<SelectBaseUI>(out var selectBase))
                {
                    selectBase.OnSelectionChanged -= RefreshNavigationButtons;
                    selectBase.OnSelectionChanged += RefreshNavigationButtons;
                }
            }
        }

        if (nextButton != null)
            nextButton.onClick.AddListener(OnClickNext);
        if (prevButton != null)
            prevButton.onClick.AddListener(OnClickPrev);
        if (startButton != null)
            startButton.onClick.AddListener(OnClickStart);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        OpenSetupFlow();
    }

    public void OpenSetupFlow()
    {
        setupContext.Reset();
        ChangeStep(ExplorationStep.Character);
        ShowPopUp();
    }

    protected override void DrawPopUp()
    {

    }

    private void ChangeStep(ExplorationStep targetStep)
    {
        // 1. 모든 페이지 끄기
        foreach (var obj in pageObjects.Values)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        // 2. 타겟 페이지 켜고 데이터 주입
        currentStep = targetStep;
        pageObjects[currentStep].SetActive(true);
        pages[currentStep].OnShowPage(setupContext);

        RefreshNavigationButtons();
    }

    // 하위 페이지에서 선택이 바뀔 때마다 호출하여 버튼 상태 갱신
    public void RefreshNavigationButtons()
    {
        bool isReady = pages[currentStep].IsReadyToProceed();

        prevButton.SafeInvoke(v => v.gameObject.SetActive(currentStep != ExplorationStep.Character));

        if (currentStep == ExplorationStep.Final) // 마지막 단계
        {

            if (nextButton != null)
                nextButton.gameObject.SetActive(false);

            if (startButton != null)
            {
                startButton.gameObject.SetActive(true);
                startButton.interactable = isReady;
            }
        }
        else // 중간 단계
        {
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = isReady;
            }

            if (startButton != null)
                startButton.gameObject.SetActive(false);
        }
    }
    private void ForceProceedToNextStep()
    {
        if (currentStep < ExplorationStep.Final)
            ChangeStep(currentStep + 1);
    }

    private void OnClickNext()
    {
        if (pageObjects[currentStep] != null 
            & pageObjects[currentStep].TryGetComponent<SkillSelectUI>(
                out var skillpage))
        {
            bool isValid 
                = skillpage.CheckSkillSlotValidation(ForceProceedToNextStep);

            if (isValid)
                ForceProceedToNextStep();
        }
        else  if (currentStep < ExplorationStep.Final)
            ChangeStep(currentStep + 1);
    }

    private void OnClickPrev()
    {
        if (currentStep > ExplorationStep.Character)
            ChangeStep(currentStep - 1);
    }

    private void OnClickStart()
    {
        if (!pages[currentStep].IsReadyToProceed()) return;

        CloseUI();

        SceneManager.LoadScene("StageSelectScene");
    }

}