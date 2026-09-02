using System;
using System.Collections.Generic;
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

        ShowPopUp();
    }

    protected override void DrawPopUp()
    {
        if (skillController != null)
        {
            skillController.SetSelectPopupType();
            skillController.SetJobID(currentContext.SelectedClassId);
            skillController.RefreshUI();
        }
    }

    public bool CheckSkillSlotValidation(Action onForceProceed)
    {
        List<SkillRuntimeData> list = AppManager.Instance.GetEquippedActiveSkillListByCharID(currentContext.SelectedCharacterId);

        bool hasEquippedSkill = false;

        foreach(var data in list)
        {
            if(data != null && data.GetSkillID() != 0)
            {
                hasEquippedSkill = true; 
                break; 
            }
        }

        if (!hasEquippedSkill)
        {
            UIManager.Instance.SafeInvoke(v => v.OpenTwoButtonPopUp("경고!", "해당 상태로 진행하시겠습니까?",
                confirmText: "확인",
                cancelText: "취소",
                onConfirm: () =>
                {
                    onForceProceed?.Invoke();
                },
                null));

            return false; 
        }

        return true;
    }
}
