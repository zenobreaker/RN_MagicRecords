using System.Collections.Generic;
using UnityEngine;

public class RuntimeHomingBehaviour : MonoBehaviour
{
    private float radius;
    private float turnSpeed;
    private LayerMask enemyLayer;
    private HashSet<GameObject> ignores;

    private Rigidbody rb;
    private Transform targetEnemy;
    private float currentSpeed;
    private bool isInitialized = false;

    // 💡 패시브 모듈이 풀에서 꺼낼 때마다 호출해주는 초기화 함수
    public void Setup(float radius, float turnSpeed, LayerMask enemyLayer, HashSet<GameObject> ignores)
    {
        this.radius = radius;
        this.turnSpeed = turnSpeed;
        this.enemyLayer = enemyLayer;
        this.ignores = ignores; // 부모 투사체의 실시간 무시 목록 참조

        if (rb == null) rb = GetComponent<Rigidbody>();

        currentSpeed = 0f; // 속도는 FixedUpdate에서 지연 캐싱
        targetEnemy = null;

        FindClosestEnemy();
        isInitialized = true;
    }

    private void FixedUpdate()
    {
        if (!isInitialized || rb == null) return;

        // 🎯 1. 속도 지연 캐싱 (물리 프레임이 돌아 linearVelocity가 적용된 직후에 캐싱)
        if (currentSpeed <= 0.1f)
        {
            currentSpeed = rb.linearVelocity.magnitude;
            if (currentSpeed <= 0.1f) return;
        }

        // 🎯 2. 타겟 갱신 조건: 타겟이 없거나, 죽었거나, 이미 부모가 때려서 무시 목록에 들어간 경우
        if (targetEnemy == null ||
            !targetEnemy.gameObject.activeInHierarchy ||
            (ignores != null && ignores.Contains(targetEnemy.gameObject)))
        {
            FindClosestEnemy(); // 새로운 타겟 탐색
        }

        // 🎯 3. 유도 기동
        if (targetEnemy != null)
        {
            float dist = Vector3.Distance(transform.position, targetEnemy.position);

            // [위성 현상 방지] 거리가 3.0 이내로 가까워지면 회전 속도를 대폭 증폭시킵니다.
            float dynamicTurnSpeed = turnSpeed;
            if (dist < 3.0f)
            {
                // 분모가 0이 되는 것을 방지하기 위해 Mathf.Max 사용
                dynamicTurnSpeed = turnSpeed * (3.0f / Mathf.Max(dist, 0.1f));
            }

            Vector3 targetDir = (targetEnemy.position - transform.position).normalized;
            targetDir.y = 0; // 2.5D 탑다운 평면 유지

            Vector3 currentDir = rb.linearVelocity.normalized;
            Vector3 newDir = Vector3.Slerp(currentDir, targetDir, Time.fixedDeltaTime * dynamicTurnSpeed).normalized;

            rb.linearVelocity = newDir * currentSpeed;
            transform.rotation = Quaternion.LookRotation(newDir);
        }
    }

    private void FindClosestEnemy()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, radius, enemyLayer);
        float closestDist = float.MaxValue;
        Transform closest = null;

        foreach (var col in cols)
        {
            // 무시 목록에 있는 녀석은 탐색 대상에서 원천 제외
            if (ignores != null && ignores.Contains(col.gameObject)) continue;

            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = col.transform;
            }
        }

        targetEnemy = closest;
    }

    // 🎯 4. 오브젝트 풀링 호환 (Destroy 대신 변수 초기화)
    private void OnDisable()
    {
        isInitialized = false;
        targetEnemy = null;
        ignores = null; // 참조 해제
    }
}