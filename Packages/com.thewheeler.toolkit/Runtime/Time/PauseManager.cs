using UnityEngine;

public static class PauseManager 
{
    private static int _pauseCount = 0;

    public static void RequestPause()
    {
        _pauseCount++;
        UpdateTimeScale();
    }

    public static void RequestResume()
    {
        _pauseCount = Mathf.Max(0, _pauseCount - 1);
        UpdateTimeScale();
    }

    private static void UpdateTimeScale()
    {
        // 멈춰달라고 한 UI가 하나라도 있으면 0, 없으면 1
        Time.timeScale = (_pauseCount > 0) ? 0f : 1f;
        Debug.Log($"현재 타임스케일: {Time.timeScale} (활성 UI: {_pauseCount}개)");
    }

    // 씬 전환 시 강제 초기화용
    public static void Reset()
    {
        _pauseCount = 0;
        Time.timeScale = 1f;
    }
}
