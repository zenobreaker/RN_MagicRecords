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

        // 1. 가벼운 MapData만 쓱 읽어옵니다.
        MapData loadMap = SaveManager.LoadMap();

        // 2. 세이브가 없으면 묻지도 따지지도 않고 새 게임
        if (loadMap == null || loadMap.nodes.Count == 0)
        {
            AppManager.Instance.EnterTheExplorationProcess();
            return;
        }

        // 3. 💡 저장된 명시적 상태(RunStatus)를 기반으로 깔끔하게 처리합니다!
        switch (loadMap.runStatus)
        {
            case RunStatus.MidRun:
            case RunStatus.NoSave: // (예외 처리용)
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
}
