using UnityEngine;

public class DynamicPartRotator : MonoBehaviour
{
    [Header("Target & Axis")]
    public Transform targetPart; // 돌릴 부위 (타이어, 프로펠러 등)
    public Vector3 rotationAxis = new Vector3(1, 0, 0); // 회전 축 (기본값 X축)

    [Header("Rotation Speeds")]
    public float baseSpeed = 0f; // 기본 회전 속도 (가만히 있어도 도는 속도)
    public float moveMultiplier = 10.0f; // 이동 시 추가 회전 가중치

    private Vector3 lastPosition;

    private void Start()
    {
        // 시작할 때 내 위치를 기억해둠
        lastPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (targetPart == null) return;

        // 1. 이번 프레임에 실제로 얼마나 이동했는지 거리(magnitude) 계산
        Vector3 deltaMove = transform.position - lastPosition;

        // 2. 이동 거리를 시간으로 나누어 실제 '속도(Speed)'를 구함
        float currentMoveSpeed = deltaMove.magnitude / Time.deltaTime;

        // 3. 총 회전량 = (기본 속도 + 실제 이동 속도 * 가중치) * Time.deltaTime
        float totalRotation = (baseSpeed + currentMoveSpeed * moveMultiplier) * Time.deltaTime;

        // 4. 지정된 축을 기준으로 굴리기
        targetPart.Rotate(rotationAxis * totalRotation);

        // 5. 다음 프레임 비교를 위해 현재 위치 업데이트
        lastPosition = transform.position;
    }
}