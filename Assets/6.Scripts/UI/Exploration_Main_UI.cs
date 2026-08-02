using UnityEngine;

public class Exploration_Main_UI : UiBase
{
    protected override void OnEnable()
    {
        base.OnEnable();
    }

    public void EnterTheExploration()
    {
        if (AppManager.Instance == null) return;

        ExploreRunSaveData loadData = SaveManager.LoadExploreRun();

        if (loadData == null)
        {
            StartNewExplorationSetup();
            return;
        }

        switch (loadData.runStatus)
        {
            case RunStatus.NoSave: // (예외 처리용)
                StartNewExplorationSetup();
                break;

            case RunStatus.SetupIncomplete:
                //  캐릭터/스킬 세팅하다가 꺼진 상태
                // 팝업 묻지 않고 바로 이어하기 모드로 셋업 UI를 열어줍니다.
                Debug.Log("[이어하기] 세팅 도중 종료됨. 셋업 창을 복구합니다.");
                UIManager.Instance.SafeInvoke(v =>
                {
                    v.OpenExplorationSetupPopUp(
                        (setup)=>
                        {
                            setup.OpenForContinue(loadData.setupData);
                        });
                });
                break;
            case RunStatus.MidRun:
                // 평범하게 진행 중이던 상태 -> 이어하기 팝업 호출
                UIManager.Instance.OpenTwoButtonPopUp(
                    title: "알림",
                    message: "진행 중인 탐사 기록이 있습니다.\n이어서 진행하시겠습니까?",
                    confirmText: "이어서 하기",
                    cancelText: "새로 시작",
                    onConfirm: () =>
                    {
                        AppManager.Instance.ContinueExplorationProcess();
                    },
                    onCancel: () =>
                    {
                        UIManager.Instance.OpenTwoButtonPopUp("경고", "기존 기록이 사라집니다. 새로 시작합니까?", "확인", "취소",
                            () => AppManager.Instance.EnterTheExplorationProcess(), null);
                    }
                );
                break;

            case RunStatus.ChapterCleared:
            case RunStatus.FinalRunCleared:
                // 크래시가 나서 보상을 못 받고 꺼졌던 상태 -> 팝업 없이 강제 진입시켜서 결산창을 띄워줌!
                Debug.LogWarning("[구제 시스템] 보상 미수령 세이브 발견! 결산 처리를 위해 강제 진입합니다.");
                AppManager.Instance.ContinueExplorationProcess();
                break;
        }
    }

    // 새 탐사 세팅을 시작할 때 부르는 헬퍼 함수
    private void StartNewExplorationSetup()
    {
        ExploreManager exploreManager = AppManager.Instance.SafeInvoke(v => v.GetExploreManager());
        if (exploreManager == null) return;

        exploreManager.Init(true); // 매니저 초기화 (껍데기 준비)

        UIManager.Instance.SafeInvoke(v =>
        {
            v.OpenExplorationSetupPopUp();
        });
    }
}
