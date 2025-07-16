using UnityEngine;
using UnityEngine.UI;

public class CustomLine : MonoBehaviour
{
    [SerializeField] private RectTransform rect;

    public void DrawLine(Vector2 start, Vector2 end)
    {
        if (rect == null) return;

        // 시작노드에서 끝 노드를 바라보드는 방향 벡ㅌㅓ
        Vector2 dir = end - start;

        // 선의 길이
        float distance = dir.magnitude;

        // 선의 각도
        float angle = Vector2.SignedAngle(Vector2.right, dir);

        // Image의 RectTransform 설정
        rect.anchoredPosition = start;

        // 선의 길이
        rect.sizeDelta = new Vector2(distance, rect.sizeDelta.y);

        // 선의 회전
        rect.rotation = Quaternion.Euler(0, 0, angle); 
    }
}
