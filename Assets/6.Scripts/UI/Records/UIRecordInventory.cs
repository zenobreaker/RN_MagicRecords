using System;
using System.Collections.Generic;
using UnityEngine;

public class UIRecordInventory : UiBase
{
    private RecordManager recordManager;

    protected override void OnEnable()
    {
        base.OnEnable();
        RefreshUI();
    }

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
        List<RecordData> list = recordManager?.GetPossesRecord();

        UIListDrawer.DrawList<RecordCard, RecordData>(list, (slot, item, index) =>
        {
            slot.Setup(item, OnRecordClick);
            if (slot.gameObject.activeSelf == false)
                slot.gameObject.SetActive(true);
            slot.OnRecordData += OnRecordData;
        },
        slot =>
        {
            slot.OnRecordData -= OnRecordData;
            slot.gameObject.SetActive(false);
            slot.ClearEvent();
        },
            InitReplaceContentObject,
            SetContentChildObjectsCallback<RecordCard>
        );
    }

    private void OnRecordClick()
    {
   
    }

    private void OnRecordData(RecordData data)
    {
        UIManager.Instance.OpenRecordInfoPopUp(data);
    }

}
