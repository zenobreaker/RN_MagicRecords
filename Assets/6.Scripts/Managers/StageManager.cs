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
    public StageInfo CurrentStageInfo => currentStage;

    private SpawnManager spawnManager;
    private RoomMaker roomMaker;


    public event Action OnProcessBattle;

    private void Awake()
    {
        spawnManager = GetComponent<SpawnManager>();
        roomMaker = new RoomMaker();
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
    }

    public void ResetStageData()
    {
        stageState = StageState.None;
    }

    /// <summary>
    /// 외부(ExploreManager)에서 이 함수를 await로 호출하여 스테이지를 진행합니다.
    /// </summary>
    public async UniTask<StageResult> RunStageFlowAsync(CancellationToken token)
    {
        try
        {
            if (currentStage == null || roomMaker == null || spawnManager == null)
                throw new InvalidOperationException("Stage preparation dependencies are missing.");

            stageState = StageState.Preparing;
            int currentWave = 1;

            // 풀러 대기
            while (ObjectPooler.Instance == null || !ObjectPooler.Instance.IsInitialized)
            {
                if (ObjectPooler.Instance != null && ObjectPooler.Instance.InitializationError != null)
                    throw new InvalidOperationException("Object pool initialization failed.", ObjectPooler.Instance.InitializationError);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
            }

            // 맵 로드
            spawnManager.ResetStageSpawns();
            RoomData roomData = roomMaker.CreateRoom(currentStage);
            // Room Start methods may build the NavMesh used by character agents.
            await UniTask.NextFrame(cancellationToken: token);

            // 플레이어 스폰 대기
            await spawnManager.SpawnCharacterAsync(1, roomData.MainSpawnPoints, token);
            if (spawnManager.ActivePlayerCount == 0)
                throw new InvalidOperationException("Stage player could not be spawned.");

            bool isPlayerDead = false;

            // 웨이브 루프 시작!
            while (currentWave <= currentStage.wave)
            {
                var groupIds = currentStage.groupIds;
                if (groupIds != null && groupIds.Count > 0)
                {
                    // 적 스폰 대기
                    if (currentWave > groupIds.Count) throw new InvalidOperationException("Missing monster group for stage wave.");
                    await spawnManager.SpawnNPCAsync(groupIds[currentWave - 1], roomData.EnemySpawnPoints, true, token);
                }

                if (currentWave == 1)
                {
                    await UniTask.NextFrame(cancellationToken: token);
                    await SceneLoadingController.CompleteStagePreparationAsync(token);
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

            if (currentStage.wave <= 0)
                await SceneLoadingController.CompleteStagePreparationAsync(token);
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
            throw;
        }
    }

    public void SetEnteredStage(StageInfo stage)
    {
        if (stage == null) return;

        currentStage = stage;
    }

}
