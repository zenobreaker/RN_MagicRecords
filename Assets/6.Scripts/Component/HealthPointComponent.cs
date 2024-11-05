using UnityEngine;

public class HealthPointComponent : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealthPoint = 100;
    private float currentHealthPoint;

    //[Header("Gauge Settings")]


    public float GetMaxHP { get => maxHealthPoint; }
    public float GetCurrentHP { get => currentHealthPoint; }
    public float GetCurrentHpRatio {  get => currentHealthPoint / maxHealthPoint; }

    public bool Dead { get => currentHealthPoint <= 0.0f; }


    private void Start()
    {
        InitCurrentHealth();
    }

    public void InitCurrentHealth() => currentHealthPoint = maxHealthPoint;

    public void Damage(float amount)
    {
        if (amount < 1.0f)
            return;

        currentHealthPoint += (amount * -1.0f);
        currentHealthPoint = Mathf.Clamp(currentHealthPoint, 0, maxHealthPoint);

    }
}
