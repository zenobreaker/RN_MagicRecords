using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public enum StageStage
    {
        Begin_Stage,
        Create_Map,
        Spawn_Player,
        Spawn_Enemy,
        Process_Battle,
        Process_Wave,
        Finish_Stage,
    };
    public StageStage stageState;

    private int currentStageChapter;
    public int CurStageChapter { get => currentStageChapter; }

    private StageInfo currentStage;

    private SpawnManager spawnManager;
    private RoomManager roomManager;

    private int currentWave = 1;
    private List<Transform> mainSpawnPoints;
    private List<Transform> spawnPoints;

    private bool bEnableSpawn = false;

    private bool bStageClearSuccess = false;

    public event Action OnProcessBattle;
    
    
    public event Action OnFinishStage;
    public event Action OnClearedStage;
    public event Action OnFailedStage;

    private void Awake()
    {
        if (TryGetComponent<SpawnManager>(out var spawnManager))
        {
            this.spawnManager = spawnManager;
            spawnManager.OnCompleteSpawnedPlayer += OnCompleteSpawnedPlayer;
            spawnManager.OnCompleteSpawnedEnemy += OnCompleteSpawnedEnemy;
            spawnManager.OnAllPlayersDead += OnAllPlayersDead;
            spawnManager.OnAllEnemiesDead += OnAllEnemiesDead;
        }

        roomManager = GetComponent<RoomManager>();
        
    }

    private void OnEnable()
    {
        GameManager.Instance.OnBeginStage += Instance_OnBattleStage;
        ObjectPooler.OnPoolInitialized += OnStartStage;
    }

    private void Instance_OnBattleStage()
    {
        SetBeginStage();
    }

    private void OnDisable()
    {
        if(GameManager.Instance)
            GameManager.Instance.OnBeginStage -= Instance_OnBattleStage;

        ObjectPooler.OnPoolInitialized -= OnStartStage;
    }

    public void SetBeginStage() => ChangedState(StageStage.Begin_Stage);
    public void SetCreateMap() => ChangedState(StageStage.Create_Map);
    public void SetSpawnPlayer() => ChangedState(StageStage.Spawn_Player);
    public void SetSpwawnEnemy() => ChangedState(StageStage.Spawn_Enemy);
    public void SetProcessBattle() => ChangedState(StageStage.Process_Battle);
    public void SetProcessWave() => ChangedState(StageStage.Process_Wave);
    public void SetFinishStage() => ChangedState(StageStage.Finish_Stage);


    private void ChangedState(StageStage state)
    {
        stageState = state;
        HandleState();
    }

    private void HandleState()
    {
        switch (stageState)
        {
            case StageStage.Begin_Stage: BeginStage(); break;
            case StageStage.Create_Map: CreateMap(); break;
            case StageStage.Spawn_Player: SpawnPlayer(); break;
            case StageStage.Spawn_Enemy: SpawnEnemy(); break;
            case StageStage.Process_Battle: ProcessBattle(); break;
            case StageStage.Process_Wave: ProcessWave(); break;
            case StageStage.Finish_Stage: FinishStage(); break;
                
        }
    }

    public void SetEnteredStage(StageInfo stageInfo)
    {
        currentStage = stageInfo;
    }

    private void OnStartStage()
    {
        bEnableSpawn = true;
    }


    #region Stage Flow 
    public void BeginStage()
    {
#if UNITY_EDITOR
        Debug.Log("Stage Manager Begin Stage");
#endif
        // 사전 데이터 준비 State
        mainSpawnPoints = new();
        spawnPoints = new();

        StopAllCoroutines();
        StartCoroutine(AwaitStage());
    }

    private IEnumerator AwaitStage()
    {
        while(true)
        {
            if(bEnableSpawn)
            {
                SetCreateMap();
                yield break; 
            }

            yield return null;
        }
    }

    private void CreateMap()
    {
        if (currentStage == null) return;

        // Load Map 
        roomManager?.LoadRoom(currentStage.mapID, ref mainSpawnPoints, ref spawnPoints);

        SetSpawnPlayer();
    }

    private void SpawnPlayer()
    {
        if (currentStage == null || mainSpawnPoints  == null || mainSpawnPoints.Count <= 0 ) return;

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

        // Spawn Enemy
        spawnManager?.SpawnNPC(currentStage.groupIds[currentWave-1], spawnPoints);
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

        SetFinishStage();
    }

    private void FinishStage()
    {
        // Reset Data 

        StopAllCoroutines();
        bEnableSpawn = false;
        mainSpawnPoints = null;
        spawnPoints = null;
        currentWave = 1;
        currentStage = null;

        // 성공 처리 
        if (bStageClearSuccess)
        {
            currentStage.bIsCleared = true;
            OnClearedStage?.Invoke();
        }
        // 실패 
        else
        {
            OnFailedStage?.Invoke();
        }

        // 구독자에게 스테이지 클리어 했다는 정보 전달
        OnFinishStage?.Invoke();
    }

    private void OnAllEnemiesDead()
    {
        SetProcessWave();
    }

    private void OnAllPlayersDead()
    {

    }

#endregion

}
