using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    //TODO: 스테이지 정보를 받아와서 처리해야함 
    //TODO: 지금은 일단 임시로 생성만 

    public enum StageStage
    {
        Begin,
        Wait,
        Finish,
    };
    public StageStage stageState;

    private int currentStageChapter;
    public int CurStageChapter { get => currentStageChapter; }

    private StageInfo currentStage;

    private SpawnManager spawnManager;
    private RoomManager roomManager;

    private int currentWave = 1;

    public event Action OnBeginSpawn;
    public event Action OnFinishedBeginStage;
    //public event Action OnFinishedEndSpawn;

    private Action OnBeginState;
    private Action OnWaitState;
    private Action OnFinishState;

    private void Awake()
    {
        if (TryGetComponent<SpawnManager>(out var spawnManager))
        {
            this.spawnManager = spawnManager;
            spawnManager.OnCompleteSpawn += OnCompleteSpawn;
            spawnManager.OnAllPlayersDead += OnStageFailure;
            spawnManager.OnAllEnemiesDead += OnStageClear;
        }

        roomManager = GetComponent<RoomManager>();

        OnFinishedBeginStage += GameManager.Instance.OnFinishedBeginStage;

        OnBeginState += OnBeginStage_Begin;
        OnWaitState += OnBeginStage_Wait;
        OnFinishState += OnBeginStage_Finish;
    }

    private void OnEnable()
    {
        ObjectPooler.OnPoolInitialized += StartStage;
    }

    private void OnDisable()
    {
        ObjectPooler.OnPoolInitialized -= StartStage;
    }

    public void SetBeginState() => ChangedState(StageStage.Begin);
    public void SetWaitState() => ChangedState(StageStage.Wait);
    public void SetFinishState() => ChangedState(StageStage.Finish);

    private void ChangedState(StageStage state)
    {
        stageState = state;
        HandleState();
    }

    private void HandleState()
    {
        switch (stageState)
        {
            case StageStage.Begin: OnBeginState?.Invoke(); break;
            case StageStage.Wait: OnWaitState?.Invoke(); break;
            case StageStage.Finish: OnFinishState?.Invoke(); break;
        }
    }

    public void SetEnteredStage(StageInfo stageInfo)
    {
        currentStage = stageInfo;
    }

    private void StartStage()
    {
        SetBeginState();
    }


    #region Stage Flow 
    public void OnBeginStage()
    {
#if UNITY_EDITOR
        Debug.Log("Stage Manager Begin Stage");
#endif

    }

    // 스폰 피니쉬 콜백 
    public void OnCompleteSpawn()
    {
        SetFinishState();
    }

    private void OnBeginStage_Begin()
    {
#if UNITY_EDITOR
        Debug.Log("BeginStage - Begin");
#endif
        SetWaitState();
    }

    private void OnBeginStage_Wait()
    {
#if UNITY_EDITOR
        Debug.Log("BeginStage - Wait");
#endif
        int groupID = 0;
        int mapID = 0;

        // Set Stage Info 
        if (currentStage != null)
        {
            groupID = currentStage.groupIds[currentWave - 1];
            mapID = currentStage.mapID;
        }

        List<Transform> mainSpawnPoints = new();
        List<Transform> spawnPoints = new();
        // Load Map 
        roomManager?.LoadRoom(mapID, ref mainSpawnPoints, ref spawnPoints);

        // Spawn Character
        spawnManager?.SpawnCharacter(1, mainSpawnPoints);

        // Spawn Enemy
        spawnManager?.SpawnNPC(groupID, spawnPoints);

        OnBeginSpawn?.Invoke();
    }

    private void OnBeginStage_Finish()
    {
#if UNITY_EDITOR
        Debug.Log("BeginStage - Finish");
#endif
        OnFinishedBeginStage?.Invoke();
    }

    public void OnEndStage()
    {
        Debug.Log("Stage Manager End Stage");
    }
    #endregion

    private void OnStageClear()
    {
        Debug.Log("Stage Clear!");

        currentStage.bIsCleared = true;


        // 다시 맵 선택 UI로 

        currentStage = null;
    }

    private void OnStageFailure()
    {
        Debug.Log("Stage Fail!");
    }

}
