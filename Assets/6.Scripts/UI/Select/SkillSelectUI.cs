using UnityEngine;

public class SkillSelectUI
      : SelectBaseUI
    , IExplorationSetupPage
{
    [Header("References")]
    [SerializeField] private SkillTreeController skillController;


    private ExplorationSetupData currentContext;

    protected override void Awake()
    {
        base.Awake();

    }

    public bool IsReadyToProceed()
    {
        return skillController != null;
    }

    public void OnShowPage(ExplorationSetupData setupData)
    {
        currentContext = setupData;

        if (currentContext == null)
            return; 

        if (skillController != null)
        {
            skillController.SetJobID(currentContext.SelectedClassId);
            skillController.RefreshUI();    
        }
    }
}
