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
    public int CurStageChapter
    {
        get
        {
            return currentStageChapter;
        }
    }

    private StageInfo currentStage;
    public StageInfo CurrentStageInfo
    {
        get
        {
            return currentStage;
        }
    }

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
            {
                throw new InvalidOperationException("Stage preparation dependencies are missing.");
            }

            stageState = StageState.Preparing;
            int currentWave = 1;

            // 풀러 대기
            while (ObjectPooler.Instance == null || ObjectPooler.Instance.IsInitialized == false)
            {
                if (ObjectPooler.Instance != null && ObjectPooler.Instance.InitializationError != null)
                {
                    throw new InvalidOperationException("Object pool initialization failed.", ObjectPooler.Instance.InitializationError);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
            }

            // 맵 로드
            spawnManager.ResetStageSpawns();
            RoomData roomData = roomMaker.CreateRoom(currentStage);
            // Room Start methods may build the NavMesh used by character agents.
            await UniTask.NextFrame(cancellationToken: token);

            // 플레이어 스폰 대기
            var explore = AppManager.Instance?.GetExploreManager();
            if (explore == null)
            {
                throw new InvalidOperationException("Exploration manager is missing.");
            }

            explore.Init(false);
            if (explore.EnsureStartingPassives(out var passiveError) == false)
            {
                throw new InvalidOperationException(passiveError);
            }

            await spawnManager.SpawnCharacterAsync(explore.CurrentSetupData.SelectedCharacterId,
                            roomData.MainSpawnPoints, token);
            if (spawnManager.ActivePlayerCount == 0)
            {
                throw new InvalidOperationException("Stage player could not be spawned.");
            }

            bool isPlayerDead = false;

            var tutorial = FindFirstObjectByType<TutorialSequence>();
            if (explore.IsTutorialStage)
            {
                if (tutorial == null)
                {
                    throw new InvalidOperationException("Tutorial sequence is missing from Stage.");
                }

                UIManager.Instance.SafeInvoke(ui => ui.SetGameplayHUDVisible(false));
                await UniTask.NextFrame(cancellationToken: token);
                await SceneLoadingController.CompleteStagePreparationAsync(token);
                stageState = StageState.Battle;
                OnProcessBattle?.Invoke();
                StageResult tutorialResult = await tutorial.RunStageAsync(spawnManager, roomData, token);
                stageState = StageState.Result;
                return tutorialResult;
            }

            if (explore.TutorialStep == TutorialStep.RecordEffect && tutorial != null)
            {
                tutorial.ObserveRecordEffect();
            }

            // 웨이브 루프 시작!
            while (currentWave <= currentStage.wave)
            {
                var groupIds = currentStage.groupIds;
                if (groupIds != null && groupIds.Count > 0)
                {
                    // 적 스폰 대기
                    if (currentWave > groupIds.Count)
                    {
                        throw new InvalidOperationException("Missing monster group for stage wave.");
                    }

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

                if (explore.TutorialStep == TutorialStep.RecordEffect && tutorial != null &&
                    tutorial.HasRecordEffectHit == false)
                {
                    // 레코드가 적용된 스킬을 적에게 사용하도록 같은 연습 무리를 다시 제시합니다.
                    continue;
                }

                currentWave++;
            }

            if (currentStage.wave <= 0)
            {
                await SceneLoadingController.CompleteStagePreparationAsync(token);
            }

            stageState = StageState.Result;
            if (isPlayerDead == false && explore.TutorialStep == TutorialStep.RecordEffect && tutorial != null)
            {
                await tutorial.WaitForRecordEffectAsync(token);
            }
            var player = PlayerManager.Instance.SafeInvoke(manager => manager.GetCurrentPlayer());
            if (player != null)
            {
                player.GetComponent<SkillComponent>().SafeInvoke(skills => skills.CancelCurrentSkill());
                EffectManager.Instance.SafeInvoke(manager => manager.UnregisterAllEffects(player));
            }

            // 결과 포장해서 던지기
            return new StageResult
            {
                IsSuccess = (isPlayerDead == false),
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
        if (stage == null)
        {
            return;
        }

        currentStage = stage;
    }

}
