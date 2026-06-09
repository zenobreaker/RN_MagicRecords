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

        // 1. 기존에 진행 중인 탐사 데이터가 있는지 검사합니다.
        bool hasSaveData = AppManager.Instance.HasSavedExploration();

        if (hasSaveData)
        {
            // 2-A. 세이브가 있다면 [이어하기 / 새로하기] 팝업 호출
            UIManager.Instance.OpenTwoButtonPopUp(
                title: "알림",
                message: "진행 중인 탐사 기록이 있습니다.\n이어서 진행하시겠습니까?",
                confirmText: "이어서 하기",
                cancelText: "새로 시작",
                onConfirm: () =>
                {
                    // [이어서 하기] -> 저장된 데이터 유지하고 씬 이동
                    AppManager.Instance.ContinueExplorationProcess();
                },
                onCancel: () =>
                {
                    // [새로 시작] -> 데이터가 날아가므로 2차 경고!
                    UIManager.Instance.OpenTwoButtonPopUp(
                        title: "경고",
                        message: "기존 탐사 기록과 아이템이 모두 삭제됩니다.\n정말 새로 시작하시겠습니까?",
                        confirmText: "확인",
                        cancelText: "취소",
                        onConfirm: () =>
                        {
                            // [확인] -> 데이터 덮어쓰고 1챕터부터 새로 출발!
                            AppManager.Instance.EnterTheExplorationProcess();
                        },
                        onCancel: () => { /* 아무 일도 일어나지 않고 팝업 닫힘 */ }
                    );
                }
            );
        }
        else
        {
            // 2-B. 세이브가 아예 없다면 고민할 필요 없이 새 탐사 진입
            AppManager.Instance.EnterTheExplorationProcess();
        }
    }
}
