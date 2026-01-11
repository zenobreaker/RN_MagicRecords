using System;
using System.Collections.Generic;
using UnityEngine;

public class BossHUDController : MonoBehaviour
{
    [SerializeField] private SO_HUDHandler handler;
    [SerializeField] private GameObject hpbarPrefab;
    [SerializeField] private Transform container;

    private Dictionary<Character, BossGauageUI> bossHpMap = new();

    private void OnEnable()
    {
        handler.OnChangedBossHP_TowParam += UpdateBossHP;
    }


    private void OnDisable()
    {
        handler.OnChangedBossHP_TowParam -= UpdateBossHP;
    }

    private void UpdateBossHP(Character boss, float current, float max)
    {
        // 1. 등록되지 않은 보스라면 새로 생성 
        if (!bossHpMap.ContainsKey(boss))
        {
            CreateNewBossHPBar(boss); 
        }

        // 2. 해당 봇의 HP만 업데이트 
        bossHpMap[boss].OnDrawBossGauge(boss, current, max); 

        // 3. 만약 죽었다면 제거 로직
    }

    private void CreateNewBossHPBar(Character boss)
    {
        var obj = Instantiate(hpbarPrefab, container);
        var hpBarUI = obj.GetComponent<BossGauageUI>();

        if (boss is Enemy enemy) hpBarUI.SetName(boss.name);

        bossHpMap.Add(boss, hpBarUI); 
    }
}
