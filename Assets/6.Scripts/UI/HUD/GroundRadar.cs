using System.Collections.Generic;
using UnityEngine;

/// <summary>캐릭터 발치의 스프라이트 평면에서 적과 목적지의 방향을 표시합니다.</summary>
public sealed class GroundRadar : MonoBehaviour
{
    [SerializeField] private SpriteRenderer arrowTemplate;
    [SerializeField] private Color enemyColor = Color.red;
    [SerializeField] private Color objectiveColor = Color.yellow;

    private readonly List<SpriteRenderer> arrows = new();
    private int used;

    private void LateUpdate()
    {
        used = 0;
        var battle = BattleManager.Instance;
        if (battle != null)
        {
            var enemies = battle.CurrentEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy != null && enemy.isActiveAndEnabled &&
                    (enemy.TryGetComponent<HealthPointComponent>(out var health) == false || health.Dead == false))
                {
                    PointTo(enemy.transform.position, enemyColor);
                }
            }
        }

        foreach (var objective in RadarObjective.Active)
        {
            PointTo(objective.transform.position, objectiveColor);
        }

        for (int i = used; i < arrows.Count && arrows[i].enabled; i++)
        {
            arrows[i].enabled = false;
        }
    }

    private void PointTo(Vector3 position, Color color)
    {
        Vector3 local = transform.InverseTransformPoint(position);
        if (local.x * local.x + local.y * local.y < 0.0001f)
        {
            return;
        }

        if (used == arrows.Count)
        {
            var instance = Instantiate(arrowTemplate, transform);
            instance.gameObject.SetActive(true);
            arrows.Add(instance);
        }

        var arrow = arrows[used++];
        arrow.color = color;
        arrow.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-local.x, local.y) * Mathf.Rad2Deg);
        arrow.enabled = true;
    }
}
