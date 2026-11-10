using System;

public enum TutorialStep
{
    None,
    Movement,
    Rescue,
    Record,
    RecordEffect
}

public sealed partial class ExploreManager
{
    public event Action OnTutorialStepChanged;

    public TutorialStep TutorialStep
    {
        get;
        private set;
    }

    public bool IsTutorialStage => TutorialStep == TutorialStep.Movement || TutorialStep == TutorialStep.Rescue;

    /// <summary>진행 단계는 PlayerData에 기록하고 전투 복원 상태는 탐사에 저장합니다.</summary>
    public void SetTutorialStep(TutorialStep step)
    {
        TutorialStep = step;
        SaveExploreMap();
        PlayerManager.Instance.SafeInvoke(players => players.SetTutorialProgress(step));
        OnTutorialStepChanged?.Invoke();
    }

    /// <summary>튜토리얼 재도전 시 사망 직전의 체력을 다음 스폰에 복원하지 않습니다.</summary>
    public void ResetTutorialHealth()
    {
        runHealth.Clear();
        liveHealth.Clear();
        SaveExploreMap();
    }

    /// <summary>일반 탐사 첫 전투에서 레코드 사용을 경험한 후 완료 처리합니다.</summary>
    public void CompleteTutorialBattle(bool success)
    {
        if (success && TutorialStep == TutorialStep.RecordEffect)
        {
            PlayerManager.Instance.SafeInvoke(players => players.CompleteTutorial());
            SetTutorialStep(TutorialStep.None);
        }
    }
}
