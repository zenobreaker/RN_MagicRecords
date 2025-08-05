using System;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager
    : Singleton<GameManager>
{
    public enum GameState
    {
        None,
        Begin_Stage,
        Begin_Battle,
        In_Battle,
        End_Battle,
        Begin_Boss,
        In_Boss,
        End_Boss,
        End_Stage,
    };

    private GameState state;

    public event Action OnBeginStage;
    public event Action OnBeginBattle;
    public event Action OnInBattle;
    public event Action OnEndBattle;
    public event Action OnBeginBoss;
    public event Action OnInBoss;
    public event Action OnEndBoss;
    public event Action OnEndStage;

    private StageManager stageManager;

    protected override void Awake()
    {
        base.Awake();
        Instance = this;

        Awake_StageManager();
        Awake_BattleManager();
    }

    #region AWAKE_FUNC
    private void Awake_StageManager()
    {
        stageManager = GetComponent<StageManager>();
        if (stageManager == null) return;

        OnBeginStage += stageManager.OnBeginStage;
        OnEndStage += stageManager.OnEndStage;
    }
    private void Awake_BattleManager()
    {
        BattleManager battleManager = GetComponent<BattleManager>();
        if (battleManager == null) return;

        OnBeginBattle += battleManager.OnBeginBattle;
        OnInBattle += battleManager.OnInBattle;
        OnEndBattle += battleManager.OnEndBattle;

        OnBeginBoss += battleManager.OnBeginBossBattle;
        OnInBoss += battleManager.OnInBossBattle;
        OnEndBoss += battleManager.OnEndBossBattle;
    }
    #endregion

    private void Start()
    {
       // SetBeginStage(); 
    }
    private void Update()
    {
        switch (state)
        {
            case GameState.Begin_Stage:
                break;

        }
    }

    #region SET_STATE
    public void SetBeginStage() => SetGameState(GameState.Begin_Stage);
    public void SetBeginBattle() => SetGameState(GameState.Begin_Battle);
    public void SetInBattle() => SetGameState(GameState.In_Battle);
    public void SetEndBattle() => SetGameState(GameState.End_Battle);
    public void SetBeginBoss() => SetGameState(GameState.Begin_Boss);
    public void SetInBoss() => SetGameState(GameState.In_Boss);
    public void SetEndBoss() => SetGameState(GameState.End_Boss);
    public void SetEndStage() => SetGameState(GameState.End_Stage);

    private void SetGameState(GameState newState)
    {
        state = newState;
        HandleGameState();
    }
    #endregion

    private void HandleGameState()
    {
        switch (state)
        {
            case GameState.Begin_Stage: OnBeginStage?.Invoke(); break;
            case GameState.Begin_Battle: OnBeginBattle?.Invoke(); break;
            case GameState.In_Battle: OnInBattle?.Invoke(); break;
            case GameState.End_Battle: OnEndBattle?.Invoke(); break;
            case GameState.Begin_Boss: OnBeginBoss?.Invoke(); break;
            case GameState.In_Boss: OnInBoss?.Invoke(); break;
            case GameState.End_Boss: OnEndBoss?.Invoke(); break;
            case GameState.End_Stage:OnEndStage?.Invoke();break;
        }
    }

    public void OnFinishedBeginStage() => SetBeginBattle();

    public void OnFinishedBeginBattle() => SetInBattle();

    public void OnFinishedInBattle() => SetEndBattle();

    public void OnFinishedEndBattle()
    {

    }


    public void EnterStage(StageInfo stageInfo)
    {
        if (stageInfo == null) return;

        stageManager.SetEnteredStage(stageInfo);

        SetBeginStage();
    }
}
