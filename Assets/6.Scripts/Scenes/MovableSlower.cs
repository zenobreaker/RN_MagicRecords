using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class MovableSlower : MonoBehaviour
{
    private static MovableSlower instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;

        DontDestroyOnLoad(gameObject);

        Awake_MainLight();
        Awake_SkyBoxMaterial();
        Awake_EvadeUI();
    }

    public static MovableSlower Instance { get => instance; }

    ///////////////////////////////////////////////////////////////////////////

    // 조명 
    [SerializeField] private Light mainLight;
    private float originalIntensity;
    [SerializeField] private float tragetIntensity = 0.35f; 
    // 스카이 박스
    [SerializeField] private Material originalSkybox;
    [SerializeField] private Material darkSkybox;
    // Evade UI 
    //[SerializeField] private EvadeEffectUIController evadeUiController;

    [SerializeField] private float duration = 2.0f;
    [SerializeField] private float slotFactor = 0.5f;       // 늦춰질 속도 값 
    [SerializeField] private float slowTimeCoolTime = 3.0f; // 슬로우 타임 쿨타임 
    private bool bCanSlowTime = true;                       // 슬로우 타임 가능 여부

    private List<ISlowable> slowers = new List<ISlowable>();

    private void Awake_MainLight()
    {
        if(mainLight != null)
            originalIntensity = mainLight.intensity;
    }

    private void Awake_SkyBoxMaterial()
    {
        originalSkybox = RenderSettings.skybox;
    }

    private void Awake_EvadeUI()
    {
        //if(evadeUiController == null)
        //    evadeUiController = FindObjectOfType<EvadeEffectUIController>();
    }


    public void Regist(ISlowable slower)
    {
        // 추가 등록해버리나?
        slowers.Add(slower);
    }

    public void Delete(ISlowable slower)
    {
        slowers.Remove(slower);
    }

    public void Start_Slow(ISlowable causer = null)
    {
        if (bCanSlowTime == false)
            return;

        bCanSlowTime = false;
        float shortDuration = duration * 0.2f;
        slowers.ForEach(slower =>
        {
            // 슬로우 타임을 일으킨 대상은 더 빨리 풀리도록 
            if (causer != null)
            {
                if (slower == causer)
                {
                    slower.ApplySlow(shortDuration, slotFactor);
                }
            }

            slower.ApplySlow(duration, slotFactor);
        });


        StartCoroutine(Delay_SlowTime());

        //Lighting
        {
            StartCoroutine(Adjusting_Lighting(shortDuration, tragetIntensity));
            StartCoroutine(Reset_Ligiting(duration));
        }

        // Skybox
        {
            AdjustingSkybox();
            StartCoroutine(Reset_Skybox(duration));
        }

        // Evade Effect UI 
        {
            PlayEvadeEffectUI();
        }
    }

    private IEnumerator Delay_SlowTime()
    {
        yield return new WaitForSeconds(slowTimeCoolTime);
        bCanSlowTime = true;
    }

    #region Lighting
    private IEnumerator Adjusting_Lighting(float duration, float targetIntensity)
    {
        float elapsedTime = 0f;
        if (mainLight == null)
            yield break; 
        float currentIntesity = mainLight.intensity;

        while (elapsedTime < duration)
        {
            mainLight.intensity = Mathf.Lerp(currentIntesity, targetIntensity, elapsedTime / duration);

            elapsedTime += Time.deltaTime;
            yield return null; 
        }

        mainLight.intensity = targetIntensity;
    }

    private IEnumerator Reset_Ligiting(float duration)
    {
        yield return new WaitForSeconds(duration);
        StartCoroutine(Adjusting_Lighting(duration * 0.2f, originalIntensity));
    }
    #endregion

    #region Skybox
    private void AdjustingSkybox()
    {
        RenderSettings.skybox = darkSkybox;
    }
    
    private IEnumerator Reset_Skybox(float duration)
    {
        yield return new WaitForSeconds(duration);
        ResetSkybox();
    }

    private void ResetSkybox()
    {
        RenderSettings.skybox = originalSkybox;
    }

    #endregion


    #region Evade Effect UI 
    private void PlayEvadeEffectUI()
    {
        //if (evadeUiController == null)
        //    return;


        //evadeUiController.OnEvadeEffectUi();
    }


    #endregion
}
