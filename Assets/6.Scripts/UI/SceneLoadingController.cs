using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Lives on the persistent AppManager prefab; stage readiness is signalled by StageManager.
public sealed class SceneLoadingController : MonoBehaviour
{
    public static SceneLoadingController Instance { get; private set; }
    public static bool IsLoading => Instance != null && Instance.loading;

    [SerializeField] private Sprite spinnerSprite;
    [SerializeField, Min(0f)] private float fadeDuration = .25f;
    [SerializeField] private float rotationSpeed = 240f;
    [SerializeField] private Vector2 spinnerSize = new Vector2(96f, 96f);

    private CanvasGroup overlay;
    private RectTransform spinner;
    private bool loading;
    private bool pauseHeld;
    private UniTaskCompletionSource stageReady;
    private UniTaskCompletionSource completed;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        var root = new GameObject("Scene Loading", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasGroup), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(root);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        overlay = root.GetComponent<CanvasGroup>();
        overlay.alpha = 0f;
        overlay.blocksRaycasts = false;
        var background = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(root.transform, false);
        var rect = (RectTransform)background.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        background.GetComponent<Image>().color = Color.black;
        var icon = new GameObject("Spinner", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(root.transform, false);
        spinner = (RectTransform)icon.transform;
        spinner.sizeDelta = spinnerSize;
        var image = icon.GetComponent<Image>();
        image.sprite = spinnerSprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        icon.SetActive(false);
    }

    private void Update()
    {
        if (loading && spinner.gameObject.activeSelf)
            spinner.Rotate(0, 0, -rotationSpeed * Time.unscaledDeltaTime);
    }

    public static bool LoadScene(string sceneName, bool waitForStage = false)
    {
        if (IsLoading) return false;
        if (Instance == null)
        {
            Debug.LogError("SceneLoadingController is missing from AppManager_Instance.");
            return false;
        }
        Instance.Claim(waitForStage);
        Instance.LoadAsync(sceneName).Forget();
        return true;
    }

    private void Claim(bool waitForStage)
    {
        loading = true;
        completed = new UniTaskCompletionSource();
        stageReady = waitForStage ? new UniTaskCompletionSource() : null;
        overlay.blocksRaycasts = true;
        PauseManager.RequestPause();
        pauseHeld = true;
    }

    private async UniTask LoadAsync(string sceneName)
    {
        var token = this.GetCancellationTokenOnDestroy();
        try
        {
            await Fade(1f, token);
            spinner.gameObject.SetActive(true);
            await UniTask.NextFrame(cancellationToken: token);
            var operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null) throw new InvalidOperationException($"Cannot load {sceneName}.");
            await operation.ToUniTask(cancellationToken: token);
            // Allow scene Start methods and UI setup to finish before revealing it.
            await UniTask.NextFrame(cancellationToken: token);
            if (stageReady != null) await stageReady.Task.AttachExternalCancellation(token);
        }
        catch (OperationCanceledException) { }
        catch (Exception error) { Debug.LogException(error, this); }
        finally { await Reveal(token); }
    }

    public static void BeginStagePreparation()
    {
        if (Instance == null || IsLoading) return;
        // Also supports opening Stage directly in the editor.
        Instance.Claim(true);
        Instance.overlay.alpha = 1f;
        Instance.spinner.gameObject.SetActive(true);
        Instance.WaitForStageAsync().Forget();
    }

    private async UniTask WaitForStageAsync()
    {
        var token = this.GetCancellationTokenOnDestroy();
        try { await stageReady.Task.AttachExternalCancellation(token); }
        catch (OperationCanceledException) { }
        catch (Exception error) { Debug.LogException(error, this); }
        finally { await Reveal(token); }
    }

    public static async UniTask CompleteStagePreparationAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!IsLoading || Instance.stageReady == null) return;
        var finished = Instance.completed;
        Instance.stageReady.TrySetResult();
        await finished.Task.AttachExternalCancellation(token);
    }

    public static void FailStagePreparation(Exception error)
    {
        if (IsLoading) Instance.stageReady?.TrySetException(error);
    }

    private async UniTask Reveal(CancellationToken token)
    {
        try
        {
            if (!token.IsCancellationRequested)
            {
                spinner.gameObject.SetActive(false);
                await Fade(0f, token);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (overlay != null) { overlay.alpha = 0f; overlay.blocksRaycasts = false; }
            loading = false;
            if (pauseHeld) { pauseHeld = false; PauseManager.RequestResume(); }
            completed?.TrySetResult();
        }
    }

    private async UniTask Fade(float target, CancellationToken token)
    {
        float start = overlay.alpha;
        for (float elapsed = 0f; elapsed < fadeDuration; elapsed += Time.unscaledDeltaTime)
        {
            overlay.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
        overlay.alpha = target;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        stageReady?.TrySetCanceled();
        completed?.TrySetCanceled();
        if (pauseHeld) { pauseHeld = false; PauseManager.RequestResume(); }
        if (overlay != null) Destroy(overlay.gameObject);
        Instance = null;
    }
}
