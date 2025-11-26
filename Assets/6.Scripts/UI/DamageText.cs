using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI;

public class DamageText : MonoBehaviour
{

    TextMeshProUGUI text;
    private float lifeTime = 3.0f;
    private float currentTime; 
    private Vector3 tdPos;
    [SerializeField] Color critColor; 

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    private void OnDisable()
    {
        ObjectPooler.ReturnToPool(gameObject);
    }

    private void LateUpdate()
    {
        tdPos += Vector3.up * Time.deltaTime;
        this.transform.position = Camera.main.WorldToScreenPoint(tdPos);
        
        currentTime -= Time.deltaTime;
        if (currentTime <= 0.0f)
        {
            currentTime = 0.0f;
            gameObject.SetActive(false);
        }
    }

    
    public void DrawDamage(Vector3 position, DamageEvent damageEvent)
    {
        DrawDamage(position, damageEvent.value, damageEvent.isCrit);
    }

    public void DrawDamage(Vector3 position, float value, bool isCrit = false)
    {
        if (text == null) return;

        int finalValue = Mathf.RoundToInt(value);

        string colorTag = "FFFFFF"; 
        if(isCrit)
        {
            colorTag = ColorUtility.ToHtmlStringRGB(critColor);
            Debug.Log("is Critical");
        }

        text.text = $"<color=#{colorTag}>{finalValue}</color>";

        currentTime = lifeTime;

        tdPos = position;
        transform.position = tdPos;

        gameObject.SetActive(true);

        transform.SetAsLastSibling();// 가장 앞에 그려지게 하기 위해 사용 
    }

    public void DrawDamage(Vector3 position, float value, DamageEvent evt)
    {
        if (text == null) return;

        int finalValue = Mathf.RoundToInt(value);
        bool isCrit = evt.isCrit;

        string colorTag = "FFFFFF";
        if (isCrit)
        {
            colorTag = ColorUtility.ToHtmlStringRGB(critColor);
        }

        if(evt.hitData.DamageType == DamageType.DOT_BLEED )
        {
            colorTag = Constants.BLEED_DMG_COLOR;
        }
        else if(evt.hitData.DamageType == DamageType.DOT_BURN)
        {
            colorTag = Constants.BURN_DMG_COLOR;
        }
        else if(evt.hitData.DamageType == DamageType.DOT_POISON)
        {
            colorTag = Constants.POISON_DMG_COLOR;
        }

        text.text = $"<color=#{colorTag}>{finalValue}</color>";

        currentTime = lifeTime;

        tdPos = position;
        transform.position = tdPos;

        gameObject.SetActive(true);

        transform.SetAsLastSibling();
    }
}
