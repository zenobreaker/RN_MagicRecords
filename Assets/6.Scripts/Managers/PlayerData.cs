/// <summary>탐사 종료와 관계없이 유지되는 플레이어의 초기 튜토리얼 기록입니다.</summary>
[System.Serializable]
public sealed class PlayerData
{
    public bool hasCompletedTutorial;
    public TutorialStep tutorialStep = TutorialStep.None;

    /// <summary>완료 기록과 저장 가능한 진행 단계가 서로 모순되지 않도록 보정합니다.</summary>
    public void NormalizeTutorialProgress()
    {
        if (hasCompletedTutorial || tutorialStep < TutorialStep.None || tutorialStep > TutorialStep.RecordEffect)
        {
            tutorialStep = TutorialStep.None;
        }
    }
}
