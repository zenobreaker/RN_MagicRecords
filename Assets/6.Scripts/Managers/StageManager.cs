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
    public int CurStageChapter {get => currentStageChapter; }

    private StageDataBase stageDataBase;
    private MonsterDataBase monsterDataBase;
    private StageInfo currentStage;

    private int currentWave = 1; 

    public event Action<int> OnBeginSpawn;
    public event Action OnFinishedBeginStage;
    //public event Action OnFinishedEndSpawn;

    private Action OnBeginState;
    private Action OnWaitState;
    private Action OnFinishState;

    private void Awake()
    {
        if (TryGetComponent<SpawnManager>(out var spawnManager))
        {
            spawnManager.OnCompleteSpawn += OnCompleteSpawn;
        }

        OnFinishedBeginStage += GameManager.Instance.OnFinishedBeginStage;
        
        OnBeginState += OnBeginStage_Begin;
        OnWaitState += OnBeginStage_Wait;
        OnFinishState += OnBeginStage_Finish;

        if (TryGetComponent<StageDataBase>(out stageDataBase))
            stageDataBase.InitializeStageData();

        if (TryGetComponent<MonsterDataBase>(out monsterDataBase))
            monsterDataBase.InitializeData();
    }

    private void Start()
    {

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

    // 먼저 DB로부터 챕터별 값을 전달하면 그 챕터에 맞는 스테이지 아이디를 받는다./
    // 이를 리스트로 한 꺼번에 받을지, 하나만 받을지? 
    public int GetRandomStageID()
    {
        return GetRandomStageID(currentStageChapter);
    }

    public int GetRandomStageID(int chapter)
    {
        if (stageDataBase == null) return -1;

        return stageDataBase.GetRandomStageIDByChater(chapter);
    }

    public StageInfo GetStageInfo(int stageID)
    {
        if (stageDataBase == null) return null;

        return stageDataBase.GetStageInfo(stageID);
    }

    public MonsterGroupData GetMonsterGroupData(int groupID)
    {
        if (monsterDataBase == null) return null;

        return monsterDataBase.GetMonsterGroupData(groupID);
    }

    public void SetEnteredStage(StageInfo stageInfo)
    {
        currentStage = stageInfo;
    }

    #region Stage Flow 
    public void OnBeginStage()
    {
#if UNITY_EDITOR
        Debug.Log("Stage Manager Begin Stage");
#endif
        SetBeginState();    
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
        int groupID = currentStage.groupIds[currentWave - 1];

        OnBeginSpawn?.Invoke(groupID);
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
}
