#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Explicit file request for the running editor; never runs merely on import.
[InitializeOnLoad]
public static class ShopRegression
{
    const string Request = "Library/ShopRegression.request";
    const string Result = "Library/ShopRegression-result.txt";
    static ShopRegression()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.EnteredEditMode)
                SessionState.EraseString("ShopRegression.SaveDirectory");
        };
    }
    [MenuItem("Tools/Shop/1 Start Sandbox Play Mode (Lobby)")]
    public static void StartSandbox()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Run from Edit Mode.");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Lobby")
            throw new Exception("Open the Lobby scene before starting shop regression tests.");
        SessionState.SetString("ShopRegression.SaveDirectory", Path.GetFullPath("Library/ShopRegressionSave-" + Guid.NewGuid().ToString("N")));
        EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        string action;
        try { action = File.ReadAllText(Request).Trim(); File.Delete(Request); }
        catch (IOException) { return; } // Writer has not finished the explicit request yet.
        try
        {
            if (action == "checks") { ShopRegressionChecks.Run(); return; }
            if (action == "explore") { ShopRegressionChecks.PrepareExplore(); return; }
            if (action == "explore-checks") { ShopRegressionChecks.Explore(); return; }
            if (action == "chapter-checks") { ShopRegressionChecks.Chapters(); return; }
            if (action == "dash") { SkillDashRegression.RunBatch(); return; }
            if (action == "restore-lobby" && !EditorApplication.isPlaying)
            { EditorSceneManager.OpenScene("Assets/8.Scenes/Lobby.unity"); return; }
            if (action == "play")
            {
                StartSandbox();
                return;
            }
            if (action == "stop") { EditorApplication.ExitPlaymode(); return; }
        }
        catch (Exception e) { File.AppendAllText(Result, e.ToString()); Debug.LogException(e); }
    }
}
#endif
