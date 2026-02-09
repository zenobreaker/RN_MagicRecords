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

        if (recordIcon != null)
        {
            recordIcon.sprite = recordData.icon;
        }

        if (nameText != null)
        {
            nameText.text = recordData.recordName;
        }

        if (descText != null)
        {
            descText.text = recordData.description;
        }
    }
}
