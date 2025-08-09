using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager
    : Singleton<GameManager>
{
    public enum GameState
    {
        None,
        Begin_Stage,
        Process_Battle,

        Finish_Stage,
    };

    private GameState state;

    public event Action OnBeginStage;
    public event Action OnBattleStage;
    public event Action OnFinishStage;
    public event Action OnSuccedStage;
    public event Action OnFailedStage; 
    

    private StageManager stageManager;

    protected override void Awake()
    {
        base.Awake();

        Awake_StageManager();
        Awake_BattleManager();
    }

    #region AWAKE_FUNC
    private void Awake_StageManager()
    {
        stageManager = GetComponent<StageManager>();
        if (stageManager == null) return;
        stageManager.OnProcessBattle += ProcessBattle;
        stageManager.OnFinishStage += FinishStage;
        stageManager.OnSucccedStage += SuccedStage;
        stageManager.OnFailedStage += FailedStage;
    }


    private void Awake_BattleManager()
    {
        BattleManager battleManager = GetComponent<BattleManager>();
        if (battleManager == null) return;

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

    public void SetProcessBattle() => SetGameState(GameState.Process_Battle);
    public void SetFinishStage() => SetGameState(GameState.Finish_Stage);

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
            case GameState.Process_Battle:
                {
                    OnBattleStage?.Invoke();
                }
                break;

            case GameState.Finish_Stage:
                {
                    OnFinishStage?.Invoke();
#if UNITY_EDITOR
                    Debug.Log("Finish Stage");
#endif              
                    //TODO : 여기에서 챕터가 더 이상 없으면 처리해야할까
                    // 앱매니저에게 처리해달라고 해볼까
                    SceneManager.LoadScene(1);
                }
                break;
        }
    }

    private void ProcessBattle() => SetProcessBattle();
    public void FinishStage() => SetFinishStage();

    public void SuccedStage()
    {
        OnSuccedStage?.Invoke();
    }

    public void FailedStage()
    {
        OnFailedStage?.Invoke(); 
    }

    public void EnterStage(StageInfo stageInfo)
    {
        if (stageInfo == null) return;

        stageManager.SetEnteredStage(stageInfo);

        SetBeginStage();

        SceneManager.LoadScene(2);
    }
}
