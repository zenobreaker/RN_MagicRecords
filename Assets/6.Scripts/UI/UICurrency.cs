using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICurrency : MonoBehaviour
{
    [SerializeField] private CurrencyType type;
    [SerializeField] private Image currencyIcon;
    [SerializeField] private TextMeshProUGUI currencyText;

    [Header("Database")]
    [SerializeField] private SO_CurrencyIconDatabase iconDatabase; // SO 연결 슬롯

    public CurrencyType Type => type; 
    private int currencyValue;
    private TMP_FontAsset numberFont;

    private void Awake()
    {
        // Null 레퍼런스 방지를 위해 Transform 캐싱 후 GetComponent 수행
        Transform iconTf = transform.FindChildByName("Icon");
        if (iconTf != null) currencyIcon = iconTf.GetComponentInChildren<Image>();

        Transform costTf = transform.FindChildByName("Cost");
        if (costTf != null) currencyText = costTf.GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Draw()
    {
        if(currencyIcon != null)
        {
            // 타입에 맞는 아이콘을 SO에서 찾아 적용
            currencyIcon.sprite = iconDatabase != null ? iconDatabase.GetIcon(type) : null;
        }


        if(currencyText != null)
        {
            currencyText.text = currencyValue.ToString();
        }
    }

    public void SetValue(int value)
    {
        currencyValue = value;
        Draw(); 
    }

    public void SetValue(int value, CurrencyType currencyType, string label = null, TMP_FontAsset labelFont = null)
    {
        type = currencyType;
        if (currencyText != null)
        {
            if (numberFont == null) numberFont = currencyText.font;
            currencyText.font = label != null && labelFont != null ? labelFont : numberFont;
        }
        SetValue(value);
        if (label != null && currencyText != null) currencyText.text = label;
    }
}
