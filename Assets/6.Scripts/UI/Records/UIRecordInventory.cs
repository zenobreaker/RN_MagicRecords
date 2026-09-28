using System;
using System.Collections.Generic;
using UnityEngine;

public class UIRecordInventory : UiBase
{
    private RecordManager recordManager;

    protected override void OnDisable()
    {
        base.OnDisable();
    }

    public override void RefreshUI()
    {
        base.RefreshUI();
        DrawInventory();
    }

    public void SetRecordManager(RecordManager record)
    {
        this.recordManager = record;
    }

    private void DrawInventory()
    {
        var list = ExploreBuildViewData.ReadInventory(recordManager);

        UIListDrawer.DrawList<RecordCard, BuildEntryViewData>(list, (slot, item, index) =>
        {
            slot.SetupBuildEntry(item, () => UIManager.Instance.SafeInvoke(ui => ui.OpenBuildEntryInfoPopUp(item)));
            if (slot.gameObject.activeSelf == false)
                slot.gameObject.SetActive(true);
        },
        slot =>
        {
            slot.gameObject.SetActive(false);
            slot.ClearEvent();
        },
            InitReplaceContentObject,
            SetContentChildObjectsCallback<RecordCard>
        );
        // 패시브 추가로 여러 행이 생겨도 기존 ScrollRect에서 모두 볼 수 있게 합니다.
        if (content != null && content.TryGetComponent<UnityEngine.UI.GridLayoutGroup>(out var grid))
        {
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)content.transform;
            if (rect.rect.width > 0f)
            {
                grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = Mathf.Max(1, Mathf.FloorToInt(
                    (rect.rect.width - grid.padding.horizontal + grid.spacing.x) / (grid.cellSize.x + grid.spacing.x)));
            }
            int columns = Mathf.Max(1, grid.constraintCount);
            int rows = Mathf.CeilToInt(list.Count / (float)columns);
            float height = grid.padding.vertical + rows * grid.cellSize.y + Mathf.Max(0, rows - 1) * grid.spacing.y;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }
    }

}
