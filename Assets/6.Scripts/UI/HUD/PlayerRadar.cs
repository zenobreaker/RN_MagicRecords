using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>화면 밖 적의 방향을 HUD 가장자리에 표시합니다.</summary>
public sealed class PlayerRadar : MonoBehaviour
{
    [SerializeField] private RectTransform radarLayer;
    [SerializeField] private Image arrowTemplate;
    [SerializeField] private Sprite edgeArrow;
    [SerializeField] private Color enemyColor = Color.red;
    [SerializeField, Min(1f)] private float edgeSize = 144f;
    [SerializeField, Min(0f)] private float edgePadding = 12f;

    private readonly List<Image> arrows = new();
    private int used;

    private void LateUpdate()
    {
        used = 0;
        var camera = Camera.main;
        var player = PlayerManager.Instance.SafeInvoke(manager => manager.GetCurrentPlayer());
        bool ready = camera != null && player != null && player.isActiveAndEnabled;
        if (ready)
        {
            var battle = BattleManager.Instance;
            if (battle != null)
            {
                var enemies = battle.CurrentEnemies;
                for (int i = 0; i < enemies.Count; i++)
                {
                    var enemy = enemies[i];
                    if (enemy == null || enemy.isActiveAndEnabled == false ||
                        (enemy.TryGetComponent<HealthPointComponent>(out var health) && health.Dead))
                    {
                        continue;
                    }

                    DrawEdgeArrow(camera, enemy.transform.position);
                }
            }

        }

        for (int i = used; i < arrows.Count && arrows[i].enabled; i++)
        {
            arrows[i].enabled = false;
        }
    }

    private Vector2 LocalPoint(Vector2 screenPoint)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(radarLayer, screenPoint, null, out var point);
        return point;
    }

    private void DrawEdgeArrow(Camera camera, Vector3 target)
    {
        Vector3 screen = camera.WorldToScreenPoint(target);
        Rect view = camera.pixelRect;
        if (screen.z > 0f && view.Contains(screen))
        {
            return;
        }

        Vector2 direction = LocalPoint(screen) - LocalPoint(view.center);
        if (screen.z < 0f && camera.orthographic == false)
        {
            direction = -direction;
        }
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector2.down;
        }
        direction.Normalize();

        Rect safe = Screen.safeArea;
        Vector2 min = LocalPoint(Vector2.Max(view.min, safe.min));
        Vector2 max = LocalPoint(Vector2.Min(view.max, safe.max));
        Vector2 half = (max - min) * 0.5f - Vector2.one * edgePadding;
        if (half.x <= 0f || half.y <= 0f)
        {
            return;
        }

        float distance = Mathf.Min(half.x / Mathf.Max(Mathf.Abs(direction.x), 0.0001f),
            half.y / Mathf.Max(Mathf.Abs(direction.y), 0.0001f));
        // 스프라이트의 피벗이 레이더 중심에 있으므로 끝점이 화면 안에 오도록 이동합니다.
        Vector2 position = (min + max) * 0.5f + direction * (distance - edgeSize * 0.5f);
        DrawArrow(edgeArrow, position, direction, edgeSize, enemyColor);
    }

    private void DrawArrow(Sprite sprite, Vector2 center, Vector2 direction, float size, Color color)
    {
        if (used == arrows.Count)
        {
            var instance = Instantiate(arrowTemplate, radarLayer);
            instance.gameObject.SetActive(true);
            arrows.Add(instance);
        }

        var image = arrows[used++];
        image.sprite = sprite;
        image.color = color;
        image.enabled = true;
        var rect = image.rectTransform;
        rect.anchoredPosition = center;
        rect.sizeDelta = Vector2.one * size;
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
    }
}
