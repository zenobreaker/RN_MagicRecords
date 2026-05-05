using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class StageManager : MonoBehaviour
{
    public enum StageState
    {
        Begin_Stage,
        Create_Map,
        Spawn_Player,
        Spawn_Enemy,
        Process_Battle,
        Process_Wave,
        Finish_Stage,
    };
    public StageState stageState;

    private int currentStageChapter;
    public int CurStageChapter { get => currentStageChapter; }

    private StageInfo currentStage;

    private SpawnManager spawnManager;
    private RoomManager roomManager;

    private int currentWave = 1;
    private bool isEntered = false; 
    private List<Transform> mainSpawnPoints;
    private List<Transform> spawnPoints;

    private bool bEnableSpawn = false;
    private bool bStageClearSuccess = false;

    public event Action OnPreProcess;
    public event Action OnProcessBattle;
    public event Action OnFinishStage;
    public event Action OnSucccedStage;
    public event Action OnFailedStage;

    private void Awake()
    {
        if (TryGetComponent<SpawnManager>(out var spawnManager))
        {
            this.spawnManager = spawnManager;

            spawnManager.OnCompleteSpawnedPlayer -= OnCompleteSpawnedPlayer;
            spawnManager.OnCompleteSpawnedEnemy -= OnCompleteSpawnedEnemy;
            spawnManager.OnAllPlayersDead -= OnAllPlayersDead;
            spawnManager.OnAllEnemiesDead -= OnAllEnemiesDead;

            spawnManager.OnCompleteSpawnedPlayer += OnCompleteSpawnedPlayer;
            spawnManager.OnCompleteSpawnedEnemy += OnCompleteSpawnedEnemy;
            spawnManager.OnAllPlayersDead += OnAllPlayersDead;
            spawnManager.OnAllEnemiesDead += OnAllEnemiesDead;
        }

        ManagerWaiter.WaitForManager<GameManager>((gameManager) =>
        {
            gameManager.OnBeginStage -= Instance_OnBattleStage;
            gameManager.OnBeginStage += Instance_OnBattleStage;
        });

        roomManager = GetComponent<RoomManager>();

    }
    private void Instance_OnBattleStage()
    {
        SetBeginStage();
    }

    private void OnEnable()
    {
        ObjectPooler.OnPoolInitialized += OnStartStage;
    }

    private void OnDisable()
    {
        if (GameManager.Instance)
            GameManager.Instance.OnBeginStage -= Instance_OnBattleStage;

        ObjectPooler.OnPoolInitialized -= OnStartStage;
    }


    // 씬에 들어왔을 때 이전 데이터가 남지 않도록 강제 초기화
    public void ResetStageData()
    {
        if (this == null) return;

        StopAllCoroutines();
        bEnableSpawn = false;
        currentWave = 1;
        bStageClearSuccess = false;
        
        if(isEntered == false)
            currentStage = null;
        
        isEntered = false; 
        mainSpawnPoints = new List<Transform>();
        spawnPoints = new List<Transform>();
        stageState = StageState.Begin_Stage;
    }

    private void OnPoolReady()
    {
        Debug.Log("[StageManager] Pool Ready! 스테이지 생성을 시작합니다.");
        bEnableSpawn = true;
        // AwaitStage 코루틴이 이 플래그를 보고 루프를 탈출함
    }

    public void SetBeginStage() => ChangedState(StageState.Begin_Stage);
    public void SetCreateMap() => ChangedState(StageState.Create_Map);
    public void SetSpawnPlayer() => ChangedState(StageState.Spawn_Player);
    public void SetSpwawnEnemy() => ChangedState(StageState.Spawn_Enemy);
    public void SetProcessBattle() => ChangedState(StageState.Process_Battle);
    public void SetProcessWave() => ChangedState(StageState.Process_Wave);
    public void SetFinishStage() => ChangedState(StageState.Finish_Stage);


    private void ChangedState(StageState state)
    {
        stageState = state;
        switch (stageState)
        {
            case StageState.Begin_Stage: BeginStage(); break;
            case StageState.Create_Map: CreateMap(); break;
            case StageState.Spawn_Player: SpawnPlayer(); break;
            case StageState.Spawn_Enemy: SpawnEnemy(); break;
            case StageState.Process_Battle: ProcessBattle(); break;
            case StageState.Process_Wave: ProcessWave(); break;
            case StageState.Finish_Stage: FinishStage(); break;

        }
    }

    public void SetEnteredStage(StageInfo stage)
    {
        if (stage == null) return;

        currentStage = stage;
        isEntered = true;
    }

    private void OnStartStage()
    {
        bEnableSpawn = true;
    }


    #region Stage Flow 
    public void BeginStage()
    {
        // 나 자신이 살아있는지 확인
        if (this == null || !gameObject.activeInHierarchy) return;

#if UNITY_EDITOR
        Debug.Log($"Stage Manager Begin Stage Time : {Time.timeScale}");
#endif
        // 사전 데이터 준비 State
        ResetStageData();
        StartCoroutine(AwaitStage());
    }

    private IEnumerator AwaitStage()
    {
        // ObjectPooler가 OnPoolInitialized를 날릴 때까지 안전하게 대기
        // 만약 이미 초기화가 끝난 상태라면 즉시 통과하게 로직 보완 가능
        while (!bEnableSpawn)
        {
            yield return null;
        }
        SetCreateMap();
    }

    private void CreateMap()
    {
        if (currentStage == null) return;

        // Load Map 
        roomManager?.LoadRoom(currentStage, ref mainSpawnPoints, ref spawnPoints);

        SetSpawnPlayer();
    }

    private void SpawnPlayer()
    {
        if (currentStage == null || mainSpawnPoints == null || mainSpawnPoints.Count <= 0) return;

        //TODO : 캐릭터 ID 선택 기능으로 선택한 ID 값으로 스폰하도록 수정
        // Spawn Character
        spawnManager?.SpawnCharacter(1, mainSpawnPoints);
    }

    private void OnCompleteSpawnedPlayer()
    {
        SetSpwawnEnemy();
    }

    private void SpawnEnemy()
    {
        if (currentStage == null || spawnPoints == null || spawnPoints.Count <= 0) return;

        var groupIds = currentStage.groupIds;
        if (groupIds.Count == 0)
            return;

        // Spawn Enemy
        spawnManager?.SpawnNPC(groupIds[currentWave - 1], spawnPoints, true);
    }


    // 스폰 피니쉬 콜백 
    public void OnCompleteSpawnedEnemy()
    {
        SetProcessBattle();
    }

    private void ProcessBattle()
    {
        // 게임 매니저에게 게임할 준비가 되었다고 전달
        OnProcessBattle?.Invoke();
    }

    private void ProcessWave()
    {
        // 웨이브가 최대 웨이브라면 게임 종료 
        if (currentWave < currentStage.wave)
        {
#if UNITY_EDITOR
            Debug.Log("Stage : Next Wave");
#endif
            currentWave++;
            // 웨이브 수 증가 후 다시 로직 에너미 생성부터 
            SetSpwawnEnemy();
            return;
        }

#if UNITY_EDITOR
        Debug.Log("Stage : Finish Wave");
#endif
        bStageClearSuccess = true;
        SetFinishStage();
    }

    private void FinishStage()
    {

        // 탐사 전체가 끝났는가? 
        bool isRunCompletelyFinished = !bStageClearSuccess || AppManager.Instance.GetExploreManager().AllStageClear;

        if (isRunCompletelyFinished)
        {
            // 탐사 완전 종료 - 로비로 가야하는 상황
            if (bStageClearSuccess == false)
            {
                OnFailedStage?.Invoke();
            }
            else
            {
                currentStage.bIsCleared = true;
                OnSucccedStage?.Invoke();
            }

            // 스테이지 팝업을 스킵하고 총 결산 팝업을 띄움 
            UIManager.Instance.OpenExploreResultPopUp();
        }
        else
        {
            // 일반 스테이지 클리어 - 다음 노드로 가야할 때 
            currentStage.bIsCleared = true;
            OnSucccedStage?.Invoke();

            UIManager.Instance?.ShowStageResultUI(bStageClearSuccess);
        }

        // Reset Data 
        ResetStageData();

        // 보상 처리 
        // 구독자에게 스테이지 클리어 했다는 정보 전달
        OnFinishStage?.Invoke();
    }

    private void OnAllEnemiesDead()
    {
        SetProcessWave();
    }

    private void OnAllPlayersDead()
    {
        bStageClearSuccess = false;
        SetFinishStage();
    }

    #endregion

}
