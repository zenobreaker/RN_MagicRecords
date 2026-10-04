using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-10000)]
public sealed class CheatConsoleUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_InputField commandInput;
    [SerializeField] private TMP_Text output;
    [SerializeField] private ScrollRect outputScroll;
    [SerializeField] private Button executeButton;
    [SerializeField] private Button closeButton;
    private static CheatConsoleUI instance;
    private static int closedFrame = -1;
    private readonly List<InputAction> suspendedActions = new();
    private readonly Queue<string> messages = new();
    private GameObject previousSelection;
    private bool opened;
    private bool focusPending;
    public static bool CapturesInput => (instance != null && instance.opened) || closedFrame == Time.frameCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; closedFrame = -1; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (instance != null) return;
        var prefab = Resources.Load<CheatConsoleUI>("CheatConsoleUI");
        if (prefab != null) Instantiate(prefab);
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        panel.SetActive(false);
        output.richText = false;
        commandInput.onValidateInput = (text, index, character) => character == '`' || character == '~' ? '\0' : character;
        commandInput.onSubmit.AddListener(Submit);
        executeButton.onClick.AddListener(() => Submit(commandInput.text));
        closeButton.onClick.AddListener(Close);
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.backquoteKey.wasPressedThisFrame)
        {
            if (opened) Close();
            else if (!SceneLoadingController.IsLoading) Open();
            return;
        }
        if (opened && (keyboard.escapeKey.wasPressedThisFrame || SceneLoadingController.IsLoading)) Close();
    }

    private void LateUpdate()
    {
        if (!opened) return;
        // Also catch a player spawned while the console was already open.
        SuspendPlayerInput();
        if (!focusPending) return;
        focusPending = false;
        commandInput.Select();
        commandInput.ActivateInputField();
    }

    public void Open()
    {
        if (opened) return;
        opened = true;
        previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        SuspendPlayerInput();
        PauseManager.RequestPause();
        panel.SetActive(true);
        commandInput.SetTextWithoutNotify("");
        focusPending = true;
    }

    private void SuspendPlayerInput()
    {
        foreach (var input in PlayerInput.all)
        {
            var map = input.actions != null ? input.actions.FindActionMap("Player", false) : null;
            if (map == null) continue;
            foreach (var action in map.actions)
            {
                if (!action.enabled) continue;
                if (!suspendedActions.Contains(action)) suspendedActions.Add(action);
                action.Disable();
            }
        }
    }

    public void Close()
    {
        if (!opened) return;
        opened = false;
        closedFrame = Time.frameCount;
        focusPending = false;
        commandInput.DeactivateInputField();
        panel.SetActive(false);
        foreach (var action in suspendedActions)
            if (action.actionMap?.asset != null) action.Enable();
        suspendedActions.Clear();
        PauseManager.RequestResume();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy ? previousSelection : null);
        previousSelection = null;
    }

    private void Submit(string command)
    {
        if (!opened || string.IsNullOrWhiteSpace(command)) return;
        messages.Enqueue($"> {command.Trim()}\n{ExecuteCommand(command)}");
        while (messages.Count > 4) messages.Dequeue();
        output.text = string.Join("\n", messages);
        Canvas.ForceUpdateCanvases();
        if (outputScroll != null) outputScroll.verticalNormalizedPosition = 0f;
        commandInput.SetTextWithoutNotify("");
        focusPending = true;
    }

    public static string ExecuteCommand(string command) => CheatCommands.Execute(command);

    private void OnSceneChanged(Scene previous, Scene next) => Close();
    private void OnDisable() => Close();
    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
        if (instance == this) instance = null;
    }
}
