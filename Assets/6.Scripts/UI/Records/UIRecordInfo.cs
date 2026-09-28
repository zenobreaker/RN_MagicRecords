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
    private BuildEntryViewData buildEntry;
    public void SetData(RecordData recordData)
    { this.recordData = recordData; buildEntry = null; }

    public void SetBuildEntry(BuildEntryViewData entry)
    { buildEntry = entry; recordData = null; }

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
        if (buildEntry != null)
        {
            if (recordIcon != null) { recordIcon.sprite = buildEntry.Icon; recordIcon.enabled = buildEntry.Icon != null; }
            if (nameText != null) nameText.text = buildEntry.Name;
            if (descText != null) descText.text = $"[{buildEntry.Label}]\n{buildEntry.Description}";
            return;
        }
        if (recordData == null) return;

        Debug.Assert(LocalizationManager.Instance != null);

        if (recordIcon != null)
        {
            recordIcon.sprite = recordData.icon;
            recordIcon.enabled = recordData.icon != null;
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
