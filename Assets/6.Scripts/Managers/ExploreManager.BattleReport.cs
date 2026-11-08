using UnityEngine;
using UnityEngine.SceneManagement;

public sealed partial class ExploreManager
{
    public BattleReport BattleReport
    {
        get;
        private set;
    } = new();

    /// <summary>
    /// 전투가 진행된 시간만 누적한다. 일시정지 시간은 포함하지 않는다.
    /// </summary>
    public void RecordBattleTime(float deltaTime)
    {
        if (RunStatus == RunStatus.MidRun && BattleManager.Instance.SafeInvoke(battle => battle.IsInBattle) &&
            SceneManager.GetActiveScene().name == "Stage")
        {
            BattleReport.playTime += Mathf.Max(0f, deltaTime);
        }
    }

    /// <summary>
    /// 피해 확정 시 플레이어의 실제 체력 피해와 적 처치를 기록한다.
    /// </summary>
    public void RecordBattleDamage(GameObject attacker, GameObject target, float damage, bool killedTarget)
    {
        if (RunStatus != RunStatus.MidRun || BattleManager.Instance.SafeInvoke(battle => battle.IsInBattle) == false ||
            target == null || damage <= 0f ||
            SceneManager.GetActiveScene().name != "Stage")
        {
            return;
        }

        if (target.TryGetComponent<Player>(out var player))
        {
            BattleReport.totalDamageTaken += damage;
        }

        if (attacker != null && attacker.TryGetComponent<Player>(out var attackerPlayer) &&
            target.TryGetComponent<Enemy>(out var enemy))
        {
            BattleReport.totalDamageDealt += damage;
            if (killedTarget)
            {
                BattleReport.killCount++;
            }
        }
    }

    /// <summary>
    /// 현재 탐사 위치와 획득 레코드를 결과 데이터에 보존한다.
    /// </summary>
    public BattleReport CaptureBattleReport()
    {
        BattleReport.characterId = CurrentSetupData.SelectedCharacterId;
        BattleReport.jobId = CurrentSetupData.SelectedClassId;
        int depth = Mathf.Max(0, stageReplacer?.GetNodeLevel(MapNodeID) ?? 0);
        if (Chapter > BattleReport.reachedChapter ||
            (Chapter == BattleReport.reachedChapter && depth > BattleReport.reachedDepth))
        {
            BattleReport.reachedChapter = Chapter;
            BattleReport.reachedDepth = depth;
        }

        BattleReport.recordIds ??= new();
        var records = AppManager.Instance.SafeInvoke(app => app.GetRecordManager())
            .SafeInvoke(records => records.GetPossesRecord());
        if (records != null)
        {
            BattleReport.recordIds.Clear();
            foreach (var record in records)
            {
                if (record != null && record.id > 0)
                {
                    BattleReport.recordIds.Add(record.id);
                }
            }
        }

        return BattleReport;
    }
}
