using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class RuntimeAutoTargetingOrb 
    : MonoBehaviour
    , ISkillEffect
{
    [Header("Orb Settings")]
    [SerializeField] private float delayTime = 2.0f;       // 발사 전 대기 시간
    [SerializeField] private float searchRadius = 20.0f;   // 탐색 반경
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private string beamPrefabName = "Beam"; // 쏠 광선
    [SerializeField] private string createOrbSoundName = ""; 
    [SerializeField] private string watingShootSoundName = ""; 
    [SerializeField] private string shootSoundName = ""; 
    [SerializeField] private string destroyOrbSoundName = ""; 


    private Character owner;
    private DamageData cachedDamageData;
    private GenenricTeamId myTeamId = GenenricTeamId.NoTeamId;
    private HashSet<GameObject> ignores = new HashSet<GameObject>();
    private float multiplier = 1.0f; 
    private bool isExtraCrit = false; 

    // 💡 풀링 환경에서 비동기 작업을 안전하게 취소하기 위한 토큰
    private CancellationTokenSource cancelTokenSource;

    private void OnEnable()
    {
        // 활성화될 때마다 새로운 취소 토큰 생성
        cancelTokenSource = new CancellationTokenSource();
        SoundManager.Instance.SafeInvoke(v => v.PlaySFX(createOrbSoundName));
        // UniTask 실행 (Forget()을 붙여 경고 메시지 제거 및 Fire-and-forget 처리)
        OrbFireSequenceAsync(cancelTokenSource.Token).Forget();
    }

    private void OnDisable()
    {
        // 💡 [핵심] 구체가 풀로 돌아가거나 파괴될 때, 대기 중이던 UniTask를 즉시 취소!
        if (cancelTokenSource != null)
        {
            cancelTokenSource.Cancel();
            cancelTokenSource.Dispose();
            cancelTokenSource = null;
        }

        SoundManager.Instance.SafeInvoke(v => v.PlaySFX(destroyOrbSoundName));
        ObjectPooler.ReturnToPool(gameObject);    // 한 객체에 한번만 
    }

    // IEnumerator 대신 async UniTaskVoid 사용
    private async UniTaskVoid OrbFireSequenceAsync(CancellationToken token)
    {
        // 1. 구체 설치 후 일정 시간 대기
        // SuppressCancellationThrow()를 사용하면 에러(Exception)를 발생시키지 않고 깔끔하게 bool 값으로 취소 여부를 반환합니다.
        SoundManager.Instance.SafeInvoke(v => v.PlaySFX(watingShootSoundName));
        bool isCanceled = await UniTask.Delay(System.TimeSpan.FromSeconds(delayTime), cancellationToken: token).SuppressCancellationThrow();

        // 대기 도중 OnDisable이 호출되어 취소되었다면 아래 로직은 무시
        if (isCanceled) return;

        // 2. 발사 직전에 최우선 타겟 탐색
        Transform target = FindPriorityTarget();

        // 3. 타겟이 있다면 그쪽을 바라봄 (없으면 설치 시 바라보던 정면 유지)
        if (target != null)
        {
            Vector3 dir = (target.position - transform.position).normalized;
            dir.y = 0; // 탑다운 평면 유지
            transform.rotation = Quaternion.LookRotation(dir);
        }

        // 4. 조준된 방향으로 진짜 파괴광선 스폰
        GameObject beam = ObjectPooler.DeferredSpawnFromPool(beamPrefabName, transform.position, transform.rotation);
        if(beam != null)
        {
            if (beam.TryGetComponent<ISkillEffect>(out var effect))
            {
                effect.SetDamageInfo(owner, cachedDamageData, isExtraCrit, multiplier);
                effect.SetIgnores(ignores); 
            }
        }
        SoundManager.Instance.SafeInvoke(v => v.PlaySFX(shootSoundName));
        ObjectPooler.FinishSpawn(beam);


        // 5. 발사 후 구체 자신은 풀로 반환
        gameObject.SetActive(false);
    }

    private Transform FindPriorityTarget()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, searchRadius, enemyLayer);

        if (cols.Length == 0) return null;

        var enemyList = new List<(Transform transform, MonsterGrade grade, float distance)>();

        foreach (var col in cols)
        {
            if (col.TryGetComponent<Character>(out var character))
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);
                enemyList.Add((col.transform, character.GetGrade(), dist)); 
            }
        }

        if (enemyList.Count == 0) return null;

        // 1순위: 등급이 높은 순, 2순위: 거리가 가까운 순
        var bestTarget = enemyList
            .OrderByDescending(e => e.grade)
            .ThenBy(e => e.distance)
            .FirstOrDefault();

        return bestTarget.transform;
    }

    public void SetDamageInfo(Character attacker, DamageData damageData, bool bExtraCrit = false, float multiplier = 1)
    {
        owner = attacker;
        cachedDamageData = damageData;
        isExtraCrit = bExtraCrit;
        this.multiplier = multiplier;

        myTeamId = TeamUtility.GetTeamId(attacker); 
    }

    public void AddIgnore(GameObject ignore)
    {
        ignores?.Add(ignore);
    }

    public void SetIgnores(HashSet<GameObject> ignores)
    {
        this.ignores = ignores;
    }
}