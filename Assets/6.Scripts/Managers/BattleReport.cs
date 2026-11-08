using System;
using System.Collections.Generic;

/// <summary>
/// 한 탐사의 누적 전투 통계와 결과 화면에 필요한 정보를 저장한다.
/// </summary>
[Serializable]
public sealed class BattleReport
{
    public int characterId = -1;
    public int jobId = -1;
    public float playTime;
    public int killCount;
    public int reachedChapter;
    public int reachedDepth;
    public float totalDamageDealt;
    public float totalDamageTaken;
    public List<int> recordIds = new();
}
