using Cysharp.Threading.Tasks;
using System;
using System.Threading;
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
    private CancellationTokenSource stageLifetime;

    public event Action OnBeginStage;
    public event Action OnBattleStage;
    public event Action OnFinishStage;
    public event Action<float> OnUpdated;

    private StageManager stageManager;
    public StageManager StageManager => stageManager;

    protected override void Awake()
    {
        base.Awake();

        if (IsDuplicate) return;

        stageManager = GetComponent<StageManager>();

        if (Instance == this)
            SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnEnable()
    {
        if(stageManager != null)
        {
            stageManager.OnProcessBattle += OnPrecessBattle;
        }
    }

    private void OnDisable()
    {
        if(stageManager != null)
        {
            stageManager.OnProcessBattle -= OnPrecessBattle;
        }
    }

    protected override void SyncDataFromSingleton()
    {
        base.SyncDataFromSingleton();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        this.stageManager = Instance.StageManager;
    }

    protected void Update()
    {
        if (state == GameState.PROCESS_BATTLE)
        {
            OnUpdated?.Invoke(Time.deltaTime);
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        stageLifetime?.Cancel();
        stageLifetime?.Dispose();
        stageLifetime = null;
        if (scene.name == "Stage")
        {
            stageLifetime = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            RunStageAsync(stageLifetime.Token).Forget();
        }
    }

    private async UniTaskVoid RunStageAsync(CancellationToken token)
    {
        SceneLoadingController.BeginStagePreparation();
        try
        {
            SetGameState(GameState.BEGIN_STAGE);
            StageResult result = await stageManager.RunStageFlowAsync(token);
            token.ThrowIfCancellationRequested();
            SetGameState(GameState.FINISH_STAGE);
            AppManager.Instance.HandleStageResult(result);
        }
        catch (OperationCanceledException error)
        {
            SceneLoadingController.FailStagePreparation(error);
        }
        catch (Exception error)
        {
            Debug.LogException(error, this);
            SceneLoadingController.FailStagePreparation(error);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        stageLifetime?.Cancel();
        stageLifetime?.Dispose();
    }

    public void OnPrecessBattle()
    {
        SetGameState(GameState.PROCESS_BATTLE);
    }

    private void SetGameState(GameState newState)
    {
        state = newState;
        switch (state)
        {
            case GameState.BEGIN_STAGE: OnBeginStage?.Invoke(); break;
            case GameState.PROCESS_BATTLE: OnBattleStage?.Invoke(); break;
            case GameState.FINISH_STAGE: OnFinishStage?.Invoke(); break;
        }
    }

    public void EnterStage(StageInfo info)
    {
        if (info == null || SceneLoadingController.IsLoading) return;

        state = GameState.NONE;
        stageManager.SetEnteredStage(info);
        stageManager.ResetStageData();
        SceneLoadingController.LoadScene("Stage", true);
    }
}
