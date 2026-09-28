using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public interface IOwnerSetup
{
    // 스폰 모듈이 생성 직후 이 함수를 통해 주인을 찔러넣어 줄 겁니다.
    void SetupOwner(GameObject owner);
}

public enum DroneAttackType
{
    None,           // 드론은 아무것도 안 함
    Normal,         // 일반 사격
    RapidFire,      // 연사
    PlasmaLaser,    // 플라즈마 레이저
    HyperLaser      // 하이퍼 레이저
}

public class AssistDrone 
    : MonoBehaviour
    , ILifetimeSetup
    , IOwnerSetup
    , ISpawnActivationHandler
{
    [Header("Muzzles")]
    [SerializeField] private Transform[] muzzles;
    [SerializeField] private string droneNormalProj = "Bullet";
    [SerializeField] private string droneBeamProj = "AssistDroneBeam";
    [SerializeField] private string droneHyperBeamProj = "AssistDroneHyperBeam";

    private Character ownerCharacter;
    private Animator[] anims;
    private CancellationTokenSource cts;
    private CancellationTokenSource lifetimeCts;
    private float lifetime;
    private bool spawnActivated;
    private bool isPoolSpawn;
    private Transform poolParent;
    private Sprite lifetimeBuffIcon;
    private BaseEffect lifetimeBuff;
    private EffectComponent lifetimeBuffOwner;

    // 소환물별 ID를 사용해 먼저 사라진 드론이 다른 드론의 표시를 지우지 않게 합니다.
    private sealed class LifetimeBuff : BaseEffect
    {
        public LifetimeBuff(int instanceID, float duration, Sprite icon)
            : base($"AssistGuns:{instanceID}", "어시스트 건즈 유지 중", duration)
        {
            Type = EffectType.BUFF;
            FxIcon = icon;
        }
    }

    private void Awake() => anims = GetComponentsInChildren<Animator>();

    public Transform[] GetAllMuzzles()
    {
        return muzzles;
    }

    public void HandlePlayerAttack(ActionData actionData, Character attacker)
    {
        if (actionData == null || !spawnActivated || !isActiveAndEnabled || attacker != ownerCharacter) return;

        CancelToken(ref cts);
        cts = new CancellationTokenSource();

        // 넘어온 식별자(skillID)에 따라 드론의 행동을 완벽하게 분기!
        switch (actionData.droneReactionType)
        {
            case DroneAttackType.Normal:
                DroneNormalAttackAsync(actionData, attacker, cts.Token).Forget();
                break;
                //DroneNormalAttackAsync(actionData, attacker, cts.Token).Forget();
                //break;
            case DroneAttackType.HyperLaser:
                DroneHyperLaserAttackAsync(actionData, attacker, cts.Token).Forget();
                break;
            case DroneAttackType.PlasmaLaser:
                DroneLaserAttackAsync(actionData, attacker, cts.Token).Forget();
                break;

            default:
                Debug.Log($"드론 모르는 기술입니다. 가만히 있겠습니다.");
                break;
        }
    }

    // ==========================================
    // ⬇️ 드론 전용 비동기 공격 로직들 (UniTask)
    // ==========================================

    private async UniTaskVoid DroneNormalAttackAsync(ActionData data, Character attacker, CancellationToken token)
    {
        // 드론만의 선딜레이 0.1초
        await UniTask.Delay(TimeSpan.FromSeconds(0.1f), cancellationToken: token);
        if (token.IsCancellationRequested) return;

        foreach(var anim in anims)  
            anim.SetTrigger("Fire");

        // 좌우 포신에서 평타 1발씩 일제 사격
        foreach (var muzzle in muzzles)
        {
            SpawnProjectile(droneNormalProj, muzzle, attacker, isNormalProjectile: true);
        }
    }

    private async UniTaskVoid DroneRapidFireAsync(ActionData data, Character attacker, CancellationToken token)
    {
        try
        {
            // 드론 전용 연사 (예: 5발 다다다닥)
            for (int i = 0; i < 5; i++)
            {
                if (token.IsCancellationRequested) return;
                
                foreach (var anim in anims)
                    anim.SetTrigger("Fire");

                foreach (var muzzle in muzzles)
                {
                    SpawnProjectile(droneNormalProj, muzzle, attacker, isNormalProjectile: true);
                }

                // 0.1초 간격 연사
                await UniTask.Delay(TimeSpan.FromSeconds(0.1f), cancellationToken: token);
            }
        }
        finally
        {
            //anim?.SetBool("IsRapidFiring", false);
        }
    }

    private async UniTaskVoid DroneLaserAttackAsync(ActionData data, Character attacker, CancellationToken token)
    {
        // 레이저 발사 기믹 (예: LineRenderer 켜기, 이펙트 활성화 등)

        foreach (var anim in anims)
            anim.SetTrigger("Fire");
        Debug.Log("드론 좌우 포신에서 굵은 레이저 출력 시작!");

        foreach (var muzzle in muzzles)
        {
            SpawnProjectile(droneBeamProj, muzzle, attacker, isNormalProjectile: false);
        }

        // 레이저 유지 시간 대기 후 종료
        await UniTask.Delay(TimeSpan.FromSeconds(1.0f), cancellationToken: token);
        Debug.Log("드론 레이저 출력 종료.");
    }

    private async UniTaskVoid DroneHyperLaserAttackAsync(ActionData data, Character attacker, CancellationToken token)
    {
        // 레이저 발사 기믹 (예: LineRenderer 켜기, 이펙트 활성화 등)

        foreach (var anim in anims)
            anim.SetTrigger("Fire");
        Debug.Log("드론 좌우 포신에서 굵은 레이저 출력 시작!");

        foreach (var muzzle in muzzles)
        {
            SpawnProjectile(droneHyperBeamProj, muzzle, attacker, isNormalProjectile: false);
        }

        // 레이저 유지 시간 대기 후 종료
        await UniTask.Delay(TimeSpan.FromSeconds(1.0f), cancellationToken: token);
        Debug.Log("드론 레이저 출력 종료.");
    }

    // 헬퍼 함수: 투사체 스폰 및 데미지 세팅
    private void SpawnProjectile(string projName, Transform muzzle, Character attacker, bool isNormalProjectile)
    {
        GameObject obj = ObjectPooler.DeferredSpawnFromPool(projName, muzzle.position, muzzle.rotation);
        if (obj != null && obj.TryGetComponent<ISkillEffect>(out var projectile))
        {
            projectile.SetDamageInfo(attacker, new DamageData(), false);
            projectile.AddIgnore(attacker);
            projectile.AddIgnore(this.gameObject);

            if (isNormalProjectile)
                AppManager.Instance.SafeInvoke(v => v.GetPassiveSystem()
                    ?.BroadcastOnAssistDroneNormalProjectile(projectile, attacker));
        }

        ObjectPooler.FinishSpawn(obj); 
    }

    private static void CancelToken(ref CancellationTokenSource source)
    {
        var previous = source;
        source = null;
        if (previous == null) return;
        previous.Cancel();
        previous.Dispose();
    }

    private void ReleaseRuntime()
    {
        RemoveLifetimeBuff();
        lifetimeBuffIcon = null;
        if (ownerCharacter != null)
            ownerCharacter.OnAttackExecuted -= HandlePlayerAttack;
        ownerCharacter = null;
        spawnActivated = false;
        lifetime = 0f;
        CancelToken(ref cts);
        CancelToken(ref lifetimeCts);
    }

    private void OnDestroy() => ReleaseRuntime();

    private void OnDisable()
    {
        ReleaseRuntime();
        if (isPoolSpawn)
        {
            gameObject.SetActive(false);
            transform.SetParent(poolParent, true);
            ObjectPooler.ReturnToPool(gameObject);
        }
    }

    public void SetPoolSpawn(bool pooled)
    {
        isPoolSpawn = pooled;
        poolParent = pooled ? transform.parent : null;
    }

    public void SetLifeTime(float time)
    {
        CancelToken(ref lifetimeCts);
        lifetime = time;
        if (spawnActivated) StartLifetime();
    }

    public void SetLifetimeBuffIcon(Sprite icon) => lifetimeBuffIcon = icon;

    private void RegisterLifetimeBuff()
    {
        RemoveLifetimeBuff();
        if (ownerCharacter == null || lifetimeBuffIcon == null ||
            !ownerCharacter.TryGetComponent<EffectComponent>(out var effects)) return;

        lifetimeBuffOwner = effects;
        lifetimeBuff = new LifetimeBuff(GetInstanceID(), lifetime, lifetimeBuffIcon);
        EffectManager.Instance.SafeInvoke(manager =>
            manager.RegisterEffect(ownerCharacter.gameObject, ownerCharacter.gameObject, lifetimeBuff));
    }

    private void RemoveLifetimeBuff()
    {
        lifetimeBuffOwner.SafeInvoke(effects => effects.RemoveEffect(lifetimeBuff));
        lifetimeBuff = null;
        lifetimeBuffOwner = null;
    }

    public void OnSpawnActivated()
    {
        if (spawnActivated || !isActiveAndEnabled) return;
        spawnActivated = true;
        StartLifetime();
    }

    private void StartLifetime()
    {
        if (float.IsNaN(lifetime) || float.IsInfinity(lifetime) || lifetime <= 0f)
        {
            Debug.LogWarning("[AssistDrone] 유효한 스킬 Duration이 없습니다. 소환물을 제거합니다.", this);
            Despawn();
            return;
        }
        RegisterLifetimeBuff();
        lifetimeCts = new CancellationTokenSource();
        StartLifetimeTimerAsync(lifetime, lifetimeCts.Token).Forget();
    }

    private async UniTaskVoid StartLifetimeTimerAsync(float duration, CancellationToken token)
    {
        bool cancelled = await UniTask.Delay(TimeSpan.FromSeconds(duration),
            ignoreTimeScale: false, cancellationToken: token).SuppressCancellationThrow();
        if (!cancelled && !token.IsCancellationRequested) Despawn();
    }

    private void Despawn()
    {
        bool pooled = isPoolSpawn;
        gameObject.SetActive(false);
        if (!pooled) Destroy(gameObject);
    }

    public void SetupOwner(GameObject owner)
    {
        if (ownerCharacter != null)
            ownerCharacter.OnAttackExecuted -= HandlePlayerAttack;
        ownerCharacter = owner.SafeInvoke(value => value.GetComponent<Character>());
        if(ownerCharacter == null) return;

        ownerCharacter.OnAttackExecuted += HandlePlayerAttack;
        this.gameObject.transform.SetParent(ownerCharacter.transform, true);
    }
}
