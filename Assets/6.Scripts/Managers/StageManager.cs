using System;
using System.Collections;
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

    public event Action OnBeginSpawn;

    public event Action OnFinishedBeginStage;
    //public event Action OnFinishedEndSpawn;

    private Action OnBeginState;
    private Action OnWaitState;
    private Action OnFinishState;

    private void Awake()
    {
        SpawnManager spawnManager = GetComponent<SpawnManager>();
        if (spawnManager != null)
        {
            spawnManager.OnCompleteSpawn += OnCompleteSpawn;
        }

        OnFinishedBeginStage += GameManager.Instance.OnFinishedBeginStage;
        
        OnBeginState += OnBeginStage_Begin;
        OnWaitState += OnBeginStage_Wait;
        OnFinishState += OnBeginStage_Finish;
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
}
