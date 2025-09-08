using TMPro;
using UnityEngine;

public class UIStatRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI valueText;

    public void SetStat(string title, string value)
    {
        if (titleText != null)
            titleText.text = title;
        if (valueText != null)
            valueText.text = value;
    }
}
