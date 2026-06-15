using UnityEngine;

public class DamageTextPooler : Singleton<DamageTextPooler>
{
    [SerializeField] private DamageText damageTextPrefab;
    [SerializeField] private int poolSize = 50;

    private DamageText[] textPools;
    private int currentIndex = 0;

    protected override void Awake()
    {
        base.Awake();

        textPools = new DamageText[poolSize];   

        // 시작할 때 전부 하위 캔버스에 미리 만들어놓기
        for(int i = 0; i < poolSize; i++)
        {
            textPools[i] = Instantiate(damageTextPrefab, transform);
            textPools[i].gameObject.SetActive(false); 
        }
    }

    public void ShowDamage(Vector3 worldPos, float damage, DamageEvent damgeEvent )
    {
        // 1. 현재 인덱스의 텍스트를 가져옵니다. (사용 중이든 아니든 무조건 가져옵니다)
        DamageText dt = textPools[currentIndex];

        // 2. 만약 이전 타격 때문에 아직 화면에 떠있다면 강제로 초기화하고 뺏어옵니다. (한도 초과 에러 원천 차단)
        dt.gameObject.SetActive(false);

        // 3. 미세한 랜덤 오프셋 적용
        Vector3 jitter = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0f, 0.5f), 0f);

        // 4. 값 세팅 및 켜기
        Vector3 finalPos =  worldPos + jitter;
        dt.gameObject.SetActive(true);
        dt.DrawDamage(finalPos, damage, damgeEvent);

        // 5. 다음 인덱스로 이동 (끝에 도달하면 다시 0번으로 돌아갑니다)
        currentIndex = (currentIndex + 1) % poolSize;
    }
}
