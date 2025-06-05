
public interface IBuff
{
    void OnApply(Character target);
    void OnUpdate(float deltaTime);
    void OnRemove();
    void ResetDuration();
    bool IsExpired { get; }
}
