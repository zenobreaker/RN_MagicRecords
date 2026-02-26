using NUnit.Framework;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIResultPopUp : UIPopUp
{
    [SerializeField] private Image portrait;
    [SerializeField] private TextMeshProUGUI characterNameText; 
    [SerializeField] private Image jobIcon;
    [SerializeField] private TextMeshProUGUI jobNameText; 
    [SerializeField] protected TextMeshProUGUI playTimeText;
    [SerializeField] protected TextMeshProUGUI killCountText;
    [SerializeField] protected TextMeshProUGUI maxExploreText;
    [SerializeField] protected GameObject recordParent;
    [SerializeField] protected GameObject recordCard; 

    protected override void DrawPopUp()
    {
        // 플레이한 캐릭터 정보 그리기 
        DrawCharInfo();

        // 탐사 정도 정보 그리기 
        DrawExploreInfo(); 

        // 레코드 그리기
        DrawRecords();
    }

    private void DrawExploreInfo()
    {
        
    }

    private void InactiveUIElements()
    {
        portrait?.gameObject.SetActive(false);
        characterNameText?.gameObject.SetActive(false);
        jobIcon?.gameObject.SetActive(false);
        jobNameText?.gameObject.SetActive(false);
        playTimeText?.gameObject.SetActive(false);
        killCountText?.gameObject.SetActive(false);
        maxExploreText?.gameObject.SetActive(false);
    }

    private void DrawCharInfo()
    {
        InactiveUIElements();

        if (PlayerManager.Instance == null) return; 

        // 캐릭터 정보 처리 
        var player = PlayerManager.Instance.GetCurrentPlayer();
        if (player == null)
        {
            return;
        }
        else
        {
            var playerInfo = PlayerManager.Instance.GetCharacterInfo(player.CharID); 
            if(playerInfo == null) return;

            if (portrait != null)
            {
                portrait.sprite = playerInfo.charSprite;
                portrait.gameObject.SetActive(true); 
            }

            if (characterNameText != null)
            {
                characterNameText.text = playerInfo.name;
                characterNameText.gameObject.SetActive(true); 
            }
        }

        int jobID = player.JobID;
        // 직업 정보 처리 
        var jobInfo = PlayerManager.Instance.GetJobInfo(jobID);
        if (jobInfo != null)
        {
            if (jobIcon != null)
            {
                jobIcon.sprite = jobInfo.jobSprite;
                jobIcon.gameObject.SetActive(true);
            }

            if (jobNameText != null)
            {
                jobNameText.text = jobInfo.jobName;
                jobNameText.gameObject.SetActive(true);
            }
        }
    }

    private void DrawRecords()
    {
        List<RecordData> records = AppManager.Instance?.GetRecordManager()?.GetPossesRecord();
        if(records == null || recordParent == null || recordCard == null) return;

        UIListDrawer.DrawListToTarget<RecordCard, RecordData>(
            recordParent.transform,
            recordCard,
            records,
            (slot, data, index) =>
            {
                slot.Setup(data);
            });
    }

}
