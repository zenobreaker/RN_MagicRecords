using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIRecordInfo : UiBase
{
    [SerializeField] private Image recordIcon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private TextMeshProUGUI categoryText;
    [SerializeField] private ScrollRect descriptionScroll;

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
        if (descriptionScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            descriptionScroll.StopMovement();
            descriptionScroll.verticalNormalizedPosition = 1f;
        }
    }

    public override void CloseUI()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.CloseSpecificUI(this);
        else
            base.CloseUI();
    }

    private void DrawUI()
    {
        var icon = buildEntry != null ? buildEntry.Icon : recordData?.icon;
        var title = buildEntry != null ? buildEntry.Name : recordData?.recordName;
        var description = buildEntry != null ? buildEntry.Description : recordData?.description;
        var category = buildEntry != null ? buildEntry.Label : "획득 레코드";

        if (recordIcon != null)
        {
            recordIcon.sprite = icon;
            recordIcon.enabled = icon != null;
        }
        if (nameText != null) nameText.text = title ?? "";
        if (categoryText != null) categoryText.text = title != null ? category : "";
        if (descText != null) descText.text = description ?? "";
    }
}
