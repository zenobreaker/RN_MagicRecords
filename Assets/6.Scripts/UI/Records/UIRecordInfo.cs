using System.Net.NetworkInformation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIRecordInfo : UiBase
{
    [SerializeField] private Image recordIcon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;

    private RecordData recordData;
    public void SetData(RecordData recordData)
    { this.recordData = recordData; }

    protected override void OnEnable()
    {
        base.OnEnable();
        RefreshUI();
    }

    public override void RefreshUI()
    {
        base.RefreshUI();

        DrawUI();
    }

    private void DrawUI()
    {
        if (recordData == null) return;

        Debug.Assert(LocalizationManager.Instance != null);

        if (recordIcon != null)
        {
            recordIcon.sprite = recordData.icon;
        }

        if (nameText != null)
        {
            nameText.text = LocalizationManager.Instance.GetText(recordData.recordName);
        }

        if (descText != null)
        {
            descText.text = LocalizationManager.Instance.GetText(recordData.description);
        }
    }
}
