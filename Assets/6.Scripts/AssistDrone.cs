using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public interface IOwnerSetup
{
    // 스폰 모듈이 생성 직후 이 함수를 통해 주인을 찔러넣어 줄 겁니다.
    void SetupOwner(GameObject owner);
}

public class AssistDrone 
    : MonoBehaviour
    , ILifetimeSetup
    , IOwnerSetup
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

    private void Awake() => anims = GetComponentsInChildren<Animator>();

    public Transform[] GetAllMuzzles()
    {
        return muzzles;
    }

    public void HandlePlayerAttack(string skillID, ActionData actionData, GameObject attacker)
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();

        // 🎯 [핵심] 넘어온 식별자(skillID)에 따라 드론의 행동을 완벽하게 분기!
        switch (skillID)
        {
            case "Normal":
                DroneNormalAttackAsync(actionData, attacker, cts.Token).Forget();
                break;

            case "skill_name_consecutiveshot":
                DroneRapidFireAsync(actionData, attacker, cts.Token).Forget();
                break;

            case "skill_name_hyperbeam":
                DroneHyperLaserAttackAsync(actionData, attacker, cts.Token).Forget();
                break;
            case "skill_name_plasmaray":
                DroneLaserAttackAsync(actionData, attacker, cts.Token).Forget();
                break;

            default:
                Debug.Log($"드론: {skillID}는 모르는 기술입니다. 가만히 있겠습니다.");
                break;
        }
    }

    // ==========================================
    // ⬇️ 드론 전용 비동기 공격 로직들 (UniTask)
    // ==========================================

    private async UniTaskVoid DroneNormalAttackAsync(ActionData data, GameObject attacker, CancellationToken token)
    {
        // 드론만의 선딜레이 0.1초
        await UniTask.Delay(TimeSpan.FromSeconds(0.1f), cancellationToken: token);
        if (token.IsCancellationRequested) return;

        foreach(var anim in anims)  
            anim.SetTrigger("Fire");

        // 좌우 포신에서 평타 1발씩 일제 사격
        foreach (var muzzle in muzzles)
        {
            SpawnProjectile(droneNormalProj, muzzle, attacker);
        }
    }

    private async UniTaskVoid DroneRapidFireAsync(ActionData data, GameObject attacker, CancellationToken token)
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
                    SpawnProjectile(droneNormalProj, muzzle, attacker);
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

    private async UniTaskVoid DroneLaserAttackAsync(ActionData data, GameObject attacker, CancellationToken token)
    {
        // 레이저 발사 기믹 (예: LineRenderer 켜기, 이펙트 활성화 등)

        foreach (var anim in anims)
            anim.SetTrigger("Fire");
        Debug.Log("드론 좌우 포신에서 굵은 레이저 출력 시작!");

        foreach (var muzzle in muzzles)
        {
            SpawnProjectile(droneBeamProj, muzzle, attacker);
        }

        // 레이저 유지 시간 대기 후 종료
        await UniTask.Delay(TimeSpan.FromSeconds(1.0f), cancellationToken: token);
        Debug.Log("드론 레이저 출력 종료.");
    }

    private async UniTaskVoid DroneHyperLaserAttackAsync(ActionData data, GameObject attacker, CancellationToken token)
    {
        // 레이저 발사 기믹 (예: LineRenderer 켜기, 이펙트 활성화 등)

        foreach (var anim in anims)
            anim.SetTrigger("Fire");
        Debug.Log("드론 좌우 포신에서 굵은 레이저 출력 시작!");

        foreach (var muzzle in muzzles)
        {
            SpawnProjectile(droneHyperBeamProj, muzzle, attacker);
        }

        // 레이저 유지 시간 대기 후 종료
        await UniTask.Delay(TimeSpan.FromSeconds(1.0f), cancellationToken: token);
        Debug.Log("드론 레이저 출력 종료.");
    }

    // 헬퍼 함수: 투사체 스폰 및 데미지 세팅
    private void SpawnProjectile(string projName, Transform muzzle, GameObject attacker)
    {
        GameObject obj = ObjectPooler.DeferredSpawnFromPool(projName, muzzle.position, muzzle.rotation);
        if (obj != null && obj.TryGetComponent<ISkillEffect>(out var projectile))
        {
            projectile.SetDamageInfo(attacker, new DamageData(), false);
            projectile.AddIgnore(attacker);
            projectile.AddIgnore(this.gameObject);
        }

        ObjectPooler.FinishSpawn(obj); 
    }

    // 메모리 릭 방지
    private void OnDestroy()
    {
        if (ownerCharacter != null)
            ownerCharacter.OnAttackExecuted -= HandlePlayerAttack;

        cts?.Cancel();
        cts?.Dispose();

        // 오브젝트가 꺼질 때(풀로 돌아갈 때) 안전하게 캔슬
        lifetimeCts?.Cancel();
        lifetimeCts?.Dispose();
        lifetimeCts = null;
    }

    public void SetLifeTime(float time)
    {
        lifetimeCts?.Cancel();
        lifetimeCts = new CancellationTokenSource();

        StartLifetimeTimerAsync(time, lifetimeCts.Token).Forget();
    }

    private async UniTaskVoid StartLifetimeTimerAsync(float duration, CancellationToken token)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: token);

        if (token.IsCancellationRequested) return;

        //ObjectPooler.ReturnToPool(this.gameObject); 
    }

    public void SetupOwner(GameObject owner)
    {
        ownerCharacter = owner.GetComponent<Character>();
        if(ownerCharacter == null) return;

        ownerCharacter.OnAttackExecuted += HandlePlayerAttack;
        this.gameObject.transform.SetParent(ownerCharacter.transform, true);
    }
}