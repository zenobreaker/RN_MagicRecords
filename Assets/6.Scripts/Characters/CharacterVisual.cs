using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterVisual : MonoBehaviour
{
    [SerializeField] private GameObject modelRoot;

    private MeshRenderer[] renderers;
    private List<Material> materials = new();

    private Coroutine fadeCoroutine; 

    private void Awake()
    {
        if (modelRoot == null)
            modelRoot = transform.Find("Model")?.gameObject;

        renderers = modelRoot?.GetComponentsInChildren<MeshRenderer>();

        foreach (var rend in renderers)
            materials.Add(rend.material);
    }

    // 숨김 / 표시 
    public void HideModel() => modelRoot?.SetActive(false);
    public void ShowModel() => modelRoot?.SetActive(true);

    // 투명도 적용
    public void SetAlpha(float alpha)
    {
        foreach(var mat in materials)
        {
            if(mat.HasProperty("_Color"))
            {
                Color c = mat.color;
                c.a = alpha;
                mat.color = c; 
            }
        }
    }

    // Fade In/Out 코루틴
    public void FadeTo(float targetAlpha, float duration)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeCoroutine(targetAlpha, duration));
    }

    private IEnumerator FadeCoroutine(float targetAlpha, float duration)
    {
        float startAlpha = materials[0].color.a;
        float time = 0f; 

        while(time< duration)
        {
            float t = time / duration;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            SetAlpha(newAlpha);

            time += Time.deltaTime;
            yield return null;
        }

        SetAlpha(targetAlpha);
        fadeCoroutine = null;
    }

    // Outline On/Off
    public void SetOutline(bool enable)
    {
        foreach(var mat in materials)
        {
            if (enable)
                mat.EnableKeyword("_OUTLINE_ON");
            else 
                mat.EnableKeyword("_OUTLINE_OFF");

        }
    }

}
