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
    [SerializeField] private string impactSoundName = "";

    private GameObject ownerObject;
    private DamageData cachedDamageData;
    private float cachedMultiplier = 1.0f;
    private bool cachedCrit;

    public void AddIgnore(GameObject ignore)
    {
        
    }

    public void SetDamageInfo(GameObject attacker, DamageData damageData,
        bool bExtraCrit = false, float multiplier = 1.0f)
    {
        ownerObject = attacker;
        cachedDamageData = damageData;
        cachedMultiplier = multiplier;
        cachedCrit = bExtraCrit;
    }

    public void SetTargetPosition(Vector3 targetPosition)
    {
        // GetCancellationTokenOnDestroy()는 이 오브젝트가 파괴되거나 꺼지면 Task도 같이 죽기
        FlyAndExplodeTask(transform.position, targetPosition, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private void OnDisable()
    {
        ObjectPooler.ReturnToPool(this.gameObject);
    }

    // 2. 비동기 흐름 제어
    private async UniTaskVoid FlyAndExplodeTask(Vector3 start, Vector3 target, CancellationToken token)
    {
        float progress = 0f;

        // while문이 Update를 대체
        while (progress < 1f)
        {
            progress += Time.deltaTime / flightTime;

            Vector3 currentPos = Vector3.Lerp(start, target, progress);
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * arcHeight;

            Vector3 moveDir = currentPos - transform.position;
            if (moveDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(moveDir);

            transform.position = currentPos;

            // 3. 다음 프레임까지 대기
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
        }

        // 4. 루프가 끝났다? = 도착했다! 터져라!
        Explode();
    }

    private void Explode()
    {
        GameObject explosion = ObjectPooler.DeferredSpawnFromPool(explosionEffectName, transform);
        if (explosion != null && explosion.TryGetComponent<ISkillEffect>(out var effect))
        {
            effect.SetDamageInfo(ownerObject, cachedDamageData, cachedCrit, cachedMultiplier);
            effect.AddIgnore(ownerObject);
        }

        // Play Sound
        {
            SoundManager.Instance.SafeInvoke(v => v.PlaySFX(impactSoundName));
        }
        ObjectPooler.FinishSpawn(explosion);
        gameObject.SetActive(false);
    }
}
