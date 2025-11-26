using UnityEngine;

/// <summary>
/// 주기적으로 일정 시간 동작시키는 트리거
/// </summary>
public class PeriodicTickTrigger
    : IEffectTrigger
{
    private float interval;
    private float timer;

    public PeriodicTickTrigger(float interval)
    {
        this.interval = interval;
    }

    public bool CheckTrigger(GameObject owner, object context = null)
    {
        timer += Time.deltaTime;
        if (timer >= interval)
        {
            timer -= interval;
            return true;
        }

        return false;
    }
}