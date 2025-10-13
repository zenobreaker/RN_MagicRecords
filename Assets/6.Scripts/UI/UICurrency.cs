using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICurrency : MonoBehaviour
{
    [SerializeField] private CurrencyType type;
    [SerializeField] private Image currencyIcon;
    [SerializeField] private TextMeshProUGUI currencyText;

    public CurrencyType Type => type; 
    private int currencyValue;

    private void Awake()
    {
        currencyIcon = GetComponentInChildren<Image>();
        currencyText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Draw()
    {
        if(currencyIcon != null)
        {

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
}
