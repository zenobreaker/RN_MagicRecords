using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

public class ParabolicProjectile_UniTask
       : MonoBehaviour
    , ISkillEffect
    , ITargetableEffect
{
    [Header("Parabola Settings")]
    [Tooltip("목표 지점까지 날아가는 데 걸리는 시간")]
    [SerializeField] private float flightTime = 1.0f;

    [Tooltip("궤적의 최고점 높이")]
    [SerializeField] private float arcHeight = 5.0f;

    [Tooltip("바닥에 닿았을 때 터질 이펙트(이전에 만든 AoEExplosion 프리팹)")]
    [SerializeField] private string explosionEffectName = "VFX_Explosion";

    private GameObject ownerObject;
    private DamageData cachedDamageData;
    private bool cachedCrit;

    public void AddIgnore(GameObject ignore)
    {
        
    }

    public void SetDamageInfo(GameObject attacker, DamageData damageData, bool bExtraCrit = false)
    {
        ownerObject = attacker;
        cachedDamageData = damageData;
        cachedCrit = bExtraCrit;
    }

    public void SetTargetPosition(Vector3 targetPosition)
    {
        // 💡 1.쏘라고 명령이 들어오면 비동기 함수(Task)를 실행만 시켜두고 본인은 퇴근합니다.
        // GetCancellationTokenOnDestroy()는 이 오브젝트가 파괴되거나 꺼지면 Task도 같이 죽으라는 뜻입니다. (안전장치)
        FlyAndExplodeTask(transform.position, targetPosition, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private void OnDisable()
    {
        ObjectPooler.ReturnToPool(this.gameObject);
    }

    // 💡 2. 비동기 흐름 제어 (이 안에서 모든 게 끝납니다)
    private async UniTaskVoid FlyAndExplodeTask(Vector3 start, Vector3 target, CancellationToken token)
    {
        float progress = 0f;

        // while문이 Update를 완벽하게 대체합니다.
        while (progress < 1f)
        {
            progress += Time.deltaTime / flightTime;

            Vector3 currentPos = Vector3.Lerp(start, target, progress);
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * arcHeight;

            Vector3 moveDir = currentPos - transform.position;
            if (moveDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(moveDir);

            transform.position = currentPos;

            // 💡 3. 다음 프레임까지 대기! (C++ 오버헤드 없이 깔끔하게 대기)
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
        }

        // 💡 4. 루프가 끝났다? = 도착했다! 터져라!
        Explode();
    }

    private void Explode()
    {
        GameObject explosion = ObjectPooler.DeferredSpawnFromPool(explosionEffectName, transform);
        if (explosion != null && explosion.TryGetComponent<ISkillEffect>(out var effect))
        {
            effect.SetDamageInfo(ownerObject, cachedDamageData, cachedCrit);
            effect.AddIgnore(ownerObject);
        }

        ObjectPooler.FinishSpawn(explosion);
        gameObject.SetActive(false);
    }
}
