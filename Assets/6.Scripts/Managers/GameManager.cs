using Cysharp.Threading.Tasks;
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
    public event Action<float> OnUpdated;

    private StageManager stageManager;
    public StageManager StageManager => stageManager;

    protected override void Awake()
    {
        base.Awake();
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
        if (scene.name == "Stage" || scene.name == "UnitTest")
        {
            RunStageAsync().Forget();
        }
    }

    private async UniTaskVoid RunStageAsync()
    {
        SetGameState(GameState.BEGIN_STAGE);

        // 스테이지 돌리고 결과 가져오기
        StageResult result = await stageManager.RunStageFlowAsync(this.GetCancellationTokenOnDestroy());

        SetGameState(GameState.FINISH_STAGE);

        // 💡 결과를 God Object(AppManager)에게 넘겨서 뒷수습을 맡깁니다.
        AppManager.Instance.HandleStageResult(result);
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
        if (info == null) return;

        state = GameState.NONE;
        stageManager.SetEnteredStage(info);
        SceneManager.LoadScene(2);
    }
}
