using System;
using UnityEngine;
using UnityEngine.UI;

public class HealthPointComponent : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealthPoint = 100;
    private float currentHealthPoint;

    [Header("Gauge Settings")]
    [SerializeField] private string uiEnemyName = "EnemyHealthbar";
    [SerializeField] private float speed = 2.0f;
    [SerializeField] private float hideTime = 3.0f;

    [Header("HUD Hanlder")]
    [SerializeField] private SO_HUDHandler handler;
    private readonly string path = "SO_HUDHandler";

    private Image hpGauge;
    private Image delayGauge;
    private Canvas uiEnemyCanvas;

    private float currentHideTime;
    private bool isShow;
    private bool isEnemy;

    public float SetMaxHP { set => maxHealthPoint = value; }
    public float GetMaxHP { get => maxHealthPoint; }
    public float GetCurrentHP { get => currentHealthPoint; }
    public float GetCurrentHpRatio { get => currentHealthPoint / maxHealthPoint; }

    public bool Dead { get => currentHealthPoint <= 0.0f; }


    private void Start()
    {
        if (handler == null)
        {
            handler = Resources.Load<SO_HUDHandler>(path);
        }
        Debug.Log($"[{gameObject.name}] maxHealthPoint (Awake) = {maxHealthPoint}");
        InitCurrentHealth();

        isShow = false;

        if (GetComponent<Enemy>() != null)
        {
            isEnemy = true;
            var enemy = (Enemy)GetComponent<Enemy>();
            uiEnemyCanvas = UIHelpers.CreateBillboardCanvas(uiEnemyName, transform, Camera.main);
            uiEnemyCanvas.gameObject.SetActive(false);

            Transform gauge = uiEnemyCanvas.transform.FindChildByName("Image_Foreground");
            Transform delay = uiEnemyCanvas.transform.FindChildByName("Image_Foreground_Delay");
            hpGauge = gauge.GetComponent<Image>();
            delayGauge = delay.GetComponent<Image>();
        }
    }

    private void LateUpdate()
    {
        if (uiEnemyCanvas != null)
            uiEnemyCanvas.transform.rotation = Camera.main.transform.rotation;

        if (isEnemy)
        {
            if (isShow == false)
                return;

            if (currentHideTime > 0.0f)
            {
                currentHideTime -= Time.deltaTime;
            }
            else
            {
                uiEnemyCanvas.gameObject.SetActive(false);
                isShow = false;
                currentHideTime = hideTime;
            }

            if (hpGauge != null)
            {
                if (hpGauge.fillAmount != delayGauge.fillAmount)
                {
                    if (Mathf.Abs(delayGauge.fillAmount) > Mathf.Epsilon)
                        delayGauge.fillAmount = Mathf.Lerp(delayGauge.fillAmount, hpGauge.fillAmount, Time.deltaTime * speed);
                    else
                        delayGauge.fillAmount = hpGauge.fillAmount;
                }
            }
        }
    }

    public void InitCurrentHealth()
    {
        currentHealthPoint = maxHealthPoint;

        if(GetComponent<Player>() != null ) 
            handler?.OnInitValue_HP(currentHealthPoint);
    }

    public void SetHealthPoint(float value)
    {
        maxHealthPoint = value; 
        InitCurrentHealth();
    }

    public void Damage(float amount)
    {
        if (amount < 1.0f)
            return;

        currentHealthPoint += (amount * -1.0f);
        currentHealthPoint = Mathf.Clamp(currentHealthPoint, 0, maxHealthPoint);

        if (isEnemy == false)
        {
            handler?.OnChangeValue_HP(currentHealthPoint, maxHealthPoint);
        }

        if (hpGauge != null)
        {
            isShow = true;
            currentHideTime = hideTime;

            hpGauge.fillAmount = currentHealthPoint / maxHealthPoint;
            uiEnemyCanvas?.gameObject.SetActive(true);
        }
    }
}
