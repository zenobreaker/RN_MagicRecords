using UnityEngine;
using TMPro;
using System.Collections;

public class DamageText : MonoBehaviour
{

    TextMeshProUGUI text;
    private float lifeTime = 3.0f;
    private float currentTime; 
    private Vector3 tdPos;

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
        Vector3 upper = new Vector3(0, 1.0f * Time.deltaTime, 0);
        tdPos += upper;
        this.transform.position = Camera.main.WorldToScreenPoint(tdPos);

        currentTime -= Time.deltaTime;
        if (currentTime <= 0.0f)
        {
            currentTime = 0.0f;
            gameObject.SetActive(false);
        }
    }

    

    public void DrawDamage(Vector3 position, float value, bool isCrit = false)
    {
        if (text == null) return;

        int finalValue = Mathf.RoundToInt(value);
        text.text = finalValue.ToString();

        currentTime = lifeTime;

        tdPos = position;

        gameObject.SetActive(true);

        transform.SetAsLastSibling();// 가장 앞에 그려지게 하기 위해 사용 
    }
}
