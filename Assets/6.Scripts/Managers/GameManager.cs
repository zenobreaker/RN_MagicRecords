using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager
    : Singleton<GameManager>
{
    public enum GameState
    {
        NONE,
        BEGIN_STAGE,
        PROCESS_BATTLE,

        FINISH_STAGE,
    };

    private GameState state;

    public event Action OnBeginStage;
    public event Action OnBattleStage;
    public event Action OnFinishStage;
    public event Action OnSuccedStage;
    public event Action OnFailedStage;

    public event Action<float> OnUpdated;

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

    protected override void Start()
    {
        // SetBeginStage(); 
    }

    protected void Update()
    {
        if (state == GameState.PROCESS_BATTLE)
        {
            OnUpdated?.Invoke(Time.deltaTime);
        }
    }


    #region SET_STATE
    public void SetBeginStage() => SetGameState(GameState.BEGIN_STAGE);

    public void SetProcessBattle() => SetGameState(GameState.PROCESS_BATTLE);
    public void SetFinishStage() => SetGameState(GameState.FINISH_STAGE);

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
            case GameState.BEGIN_STAGE:
                {
                    OnBeginStage?.Invoke(); 
                }
                break;
            case GameState.PROCESS_BATTLE:
                {
                    OnBattleStage?.Invoke();
                }
                break;

            case GameState.FINISH_STAGE:
                {
                    OnFinishStage?.Invoke();
#if UNITY_EDITOR
                    Debug.Log("Finish Stage");
#endif           
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

    public void EnterStage(StageInfo info)
    {
        if (info == null) return;

        stageManager.SetEnteredStage(info);

        SetBeginStage();

        SceneManager.LoadScene(2);
    }
}
