using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public struct StageResult
{
    public bool IsSuccess;
    public bool IsPlayerDead;
    public bool IsExploreFinished;
    public int ClearedWave;
}


public sealed class StageManager : MonoBehaviour
{
    public enum StageState
    {
        None,
        Preparing,
        Battle,
        Result
    };

    public StageState stageState;

    private int currentStageChapter;
    public int CurStageChapter { get => currentStageChapter; }

    private StageInfo currentStage;

    private SpawnManager spawnManager;
    private RoomManager roomManager;

    private bool bEnableSpawn = false;

    public event Action OnProcessBattle;

    private void Awake()
    {
        spawnManager = GetComponent<SpawnManager>();
        roomManager = GetComponent<RoomManager>();
    }

    private void OnEnable()
    {
        ObjectPooler.OnPoolInitialized += OnPoolReady;
    }

    private void OnDisable()
    {
        ObjectPooler.OnPoolInitialized -= OnPoolReady;
    }

    private void OnPoolReady()
    {
        Debug.Log("[StageManager] Pool Ready! 스테이지 생성을 시작합니다.");
        bEnableSpawn = true;
        // AwaitStage 코루틴이 이 플래그를 보고 루프를 탈출함
    }

    public void ResetStageData()
    {
        bEnableSpawn = false;
        stageState = StageState.None;
    }

    /// <summary>
    /// 외부(ExploreManager)에서 이 함수를 await로 호출하여 스테이지를 진행합니다.
    /// </summary>
    public async UniTask<StageResult> RunStageFlowAsync(CancellationToken token)
    {
        try
        {
            stageState = StageState.Preparing;
            int currentWave = 1;

            // 풀러 대기
            while (!bEnableSpawn)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
            }

            // 맵 로드 
            RoomData roomData = roomManager.LoadRoom(currentStage);

            // 플레이어 스폰 대기
            await spawnManager.SpawnCharacterAsync(1, roomData.MainSpawnPoints, token);

            bool isPlayerDead = false;

            // 웨이브 루프 시작!
            while (currentWave <= currentStage.wave)
            {
                var groupIds = currentStage.groupIds;
                if (groupIds.Count > 0)
                {
                    // 적 스폰 대기
                    await spawnManager.SpawnNPCAsync(groupIds[currentWave - 1], roomData.EnemySpawnPoints, true, token);
                }

                stageState = StageState.Battle;
                OnProcessBattle?.Invoke();

                // 💡 [핵심] 전투 끝날 때까지 여기서 무한 대기! (이벤트 체인 불필요)
                await UniTask.WaitUntil(() =>
                    spawnManager.ActiveEnemyCount == 0 || spawnManager.ActivePlayerCount == 0,
                    cancellationToken: token);

                if (spawnManager.ActivePlayerCount == 0)
                {
                    isPlayerDead = true;
                    break; // 사망 시 즉시 루프 탈출
                }

                currentWave++;
            }

            stageState = StageState.Result;

            // 결과 포장해서 던지기
            return new StageResult
            {
                IsSuccess = !isPlayerDead,
                IsPlayerDead = isPlayerDead,
                ClearedWave = currentWave - 1
            };
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[StageManager] 스테이지 진행 취소됨");
            return new StageResult { IsSuccess = false, IsPlayerDead = true, ClearedWave = 0 };
        }
    }

    public void SetEnteredStage(StageInfo stage)
    {
        if (stage == null) return;

        currentStage = stage;
    }

    private void OnStartStage()
    {
        bEnableSpawn = true;
    }
}
