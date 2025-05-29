using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
public class Cheater : MonoBehaviour
{
    private static Cheater instance;
    public static Cheater Instance { get 
        { 
            return instance; 
        } }

    Player player;

    bool bStunToggle = false;

    private void Awake()
    {
        if(instance == null)
            instance = this;
    }

    private void Start()
    {
        player = FindAnyObjectByType<Player>();
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