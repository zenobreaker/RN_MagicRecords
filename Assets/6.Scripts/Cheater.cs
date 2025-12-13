using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
public class Cheater 
    : Singleton<Cheater>
{

    Player player;

    bool bStunToggle = false;

    protected override void Start()
    {
        player = FindAnyObjectByType<Player>();
        base.Start();
    }

    protected override void SyncDataFromSingleton()
    {
        player = Instance.player;
        bStunToggle = Instance.bStunToggle;
    }

    private void Update()
    {
        // 오브젝트 풀러 테스트
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            ObjectPooler.SpawnFromPool("Bullet", Vector2.up);
        }

        // 스턴 테스트 
        if (Input.GetKeyDown(KeyCode.Keypad7))
            Test_Stun();

        // 필드에 모든 적 체력 0 
        if(Input.GetKeyDown(KeyCode.Keypad8))
        {
            Test_AllEnemyDead(); 
        }

        // 플레이이어 무적
        if(Input.GetKeyDown(KeyCode.Keypad9))
        {
            Test_PlayerInvicible();
        }

        // 플레이어 공격력 버프 온
        if (Input.GetKeyDown(KeyCode.Keypad4))
        {
            Test_PlayerAttackBuff();
            Test_PlayerAddBurn();
        }

        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            Test_SetSkill();
        }

        if (Input.GetKeyDown(KeyCode.Keypad5))
            Test_PlayerRemoveBuff();

        if (Input.GetKeyDown(KeyCode.Keypad1))
            Test_RewardPopUp();

        if (Input.GetKeyDown(KeyCode.Keypad2))
            Test_AddItem();

        if (Input.GetKeyDown(KeyCode.Keypad3))
            Test_AddCurrencies();
    }

    private void Test_AddCurrencies()
    {
        CurrencyManager.Instance.Cheat_AddedCurrenices();
    }

    private void Test_AddItem()
    {
        InventoryManager.Instance.TestAddItems();
    }

    private void Test_PlayerRemoveBuff()
    {
        if (player == null) return;

        EffectComponent buff = player.GetComponent<EffectComponent>();
        if (buff == null) return;

        buff.RemoveEffect("AttackBuff");
        Debug.Log("Cheater - Buff Off");
    }

    private void Test_PlayerAttackBuff()
    {
        if (player == null) return;

        EffectComponent buff = player.GetComponent<EffectComponent>();
        if (buff == null) return;

        Debug.Log("Buff On");


        StatBuffEffect attackbuff = new StatBuffEffect("AttackBuff", 10.0f, StatusType.ATTACK, 0.2f);
        buff.ApplyEffect(attackbuff, player, null); 
    }

    private void Test_PlayerAddBurn()
    {
        if(player == null) return;  
        if(EffectManager.Instance == null) return;

        Debug.Log("Debuff On");

        //BurnEffect burn = new BurnEffect("burn", "", 10.0f, 3.0f, 10.0f);
        //EffectManager.Instance.RegisterEffect(player, null, burn);
        EffectManager.Instance.RegisterEffect_Burn(player, null, 10.0f, 10.0f);
        EffectManager.Instance.RegisterEffect_Bleed(player, null, 10.0f, 3.0f);
        EffectManager.Instance.RegisterEffect_Poison(player, null, 10.0f, 3.0f);
    }


    private void Test_SetSkill()
    {
        SkillComponent skill = player?.GetComponent<SkillComponent>();
        //if (skill != null || skillData != null)
        //{
        //    ActiveSkill activeSkill = (ActiveSkill)skillData.CreateSkill();

        //    if (activeSkill != null)
        //    {
        //        skill.SetActiveSkill(SkillSlot.Slot1, activeSkill);
        //        Debug.Log($"skill 장착 완료");
        //    }
        //}
    }

    private void Test_PlayerInvicible()
    {
        
    }

    private void Test_AllEnemyDead()
    {
        Debug.Log("All Enemy Kill!");
        Enemy[] enemies = GameObject.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        
        foreach(var enemy in enemies)
        {
            if (enemy != null)
            {
                if(enemy.TryGetComponent<HealthPointComponent>(out var health))
                {
                    DamageEvent e = new DamageEvent(health.GetMaxHP);
                    enemy.OnDamage(null, null, Vector3.zero, ref e);
                }
            }
        }
    }

    private void Test_Stun()
    {
        if (player != null)
        {
            bStunToggle = !bStunToggle;

            StatusEffectComponent sfc = player.GetComponent<StatusEffectComponent>();
            if (sfc != null && bStunToggle)
                sfc.AddStunEffect();
            else if( sfc!=null && bStunToggle == false)
                sfc.RemoveStunEffect();
        }
    }


    private void Test_RewardPopUp()
    {
        if (AppManager.Instance == null) return;

       var  rewardData = AppManager.Instance.GetRewardData(1);
        if (rewardData == null) return;

        Debug.Log("Data 정상 생성");

        RewardManager rm = FindAnyObjectByType<RewardManager>();
        if (rm == null) return;

        rm.AddReward(rewardData);
    }


    List<Vector3> gizmoPoints = new List<Vector3>();
    public void DrawSphereWithPoints(List<Vector3> points)
    {
        gizmoPoints.Clear();
        gizmoPoints = points;
    }

    private void OnDrawGizmos()
    {
        if (gizmoPoints.Count == 0) return;

        Gizmos.color = Color.green;
        int cnt = 1; 
        foreach(var point in gizmoPoints)
        {
            Gizmos.DrawWireSphere(point, 0.5f);
            Handles.color = Color.white;
            Handles.Label(point + Vector3.up * 0.5f, $"#{cnt++}");
        }
    }
}

#endif