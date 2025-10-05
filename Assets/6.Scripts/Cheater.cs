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

    private void Start()
    {
        player = FindAnyObjectByType<Player>();
    }

    protected override void SyncDataFromSingleton()
    {
        player = Instance.player;
        bStunToggle = Instance.bStunToggle;
    }

    private void Update()
    {
        // ������Ʈ Ǯ�� �׽�Ʈ
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            ObjectPooler.SpawnFromPool("Bullet", Vector2.up);
        }

        // ���� �׽�Ʈ 
        if (Input.GetKeyDown(KeyCode.Keypad7))
            Test_Stun();

        // �ʵ忡 ��� �� ü�� 0 
        if(Input.GetKeyDown(KeyCode.Keypad8))
        {
            Test_AllEnemyDead(); 
        }

        // �÷����̾� ����
        if(Input.GetKeyDown(KeyCode.Keypad9))
        {
            Test_PlayerInvicible();
        }

        // �÷��̾� ���ݷ� ���� ��
        if (Input.GetKeyDown(KeyCode.Keypad4))
            Test_PlayerAttackBuff();

        if (Input.GetKeyDown(KeyCode.Keypad5))
            Test_PlayerRemoveBuff();

        if (Input.GetKeyDown(KeyCode.Keypad1))
            Test_RewardPopUp();
    }

    private void Test_PlayerRemoveBuff()
    {
        if (player == null) return;

        BuffComponent buff = player.GetComponent<BuffComponent>();
        if (buff == null) return;

        buff.RemoveBuff("AttackBuff");
        Debug.Log("Cheater - Buff Off");
    }

    private void Test_PlayerAttackBuff()
    {
        if (player == null) return;

        BuffComponent buff = player.GetComponent<BuffComponent>();
        if (buff == null) return;

        Debug.Log("Buff On");

        StatBuff attackbuff = new StatBuff("AttackBuff", 10.0f, StatusType.ATTACK, 0.2f);
        buff.ApplyBuff(attackbuff); 
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
                    enemy.OnDamage(null, null, Vector3.zero, e);
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

        Debug.Log("Data ���� ����");

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