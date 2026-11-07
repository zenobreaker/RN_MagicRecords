using System;
using UnityEngine;

/// <summary>게임 전역에서 사용하는 설정 테이블 참조를 제공합니다.</summary>
[CreateAssetMenu(fileName = "SO_GameSettings", menuName = "Scriptable Objects/Game Settings")]
public sealed class SO_GameSettings : SO_Singleton<SO_GameSettings>
{
    [SerializeField] private SO_RecordRarityTable recordRarityTable;

    public SO_RecordRarityTable RecordRarityTable
    {
        get
        {
            if (recordRarityTable == null)
            {
                throw new InvalidOperationException("SO_GameSettings에 레코드 등급 확률 테이블을 연결해주세요.");
            }

            return recordRarityTable;
        }
    }
}
