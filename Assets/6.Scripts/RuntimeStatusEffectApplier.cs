using UnityEngine;

public class RuntimeStatusEffectApplier : MonoBehaviour
{
    private AbstractProjectile proj;

    // 💡 어떤 상태이상을 걸지 기억해둘 변수 추가
    private string effectID;
    private float duration;
    private float power;
    private bool isInitialized = false;

    // 패시브가 이 총알을 세팅할 때 상태이상 이름("Burn", "Bleed" 등)도 같이 넘겨줍니다.
    public void Setup(string effectID, float duration, float power)
    {
        this.effectID = effectID;
        this.duration = duration;
        this.power = power;

        if (proj == null)
        {
            proj = GetComponent<AbstractProjectile>();
            if (proj != null)
            {
                proj.OnProjectileHit += HandleHit;
            }
        }

        isInitialized = true;
    }

    private void HandleHit(Collider myCol, Collider targetCol, Vector3 hitPos)
    {
        if (!isInitialized || targetCol == null) return;

        if (EffectManager.Instance != null && proj.Owner != null)
        {
            // 💡 EffectManager의 통합 메서드를 호출해서 어떤 상태이상이든 자유자재로 부여!
            EffectManager.Instance.RegisterDotEffect(
                effectID,
                targetCol.gameObject,
                proj.Owner.gameObject,
                duration,
                power
            );
        }
    }

    private void OnDisable()
    {
        isInitialized = false;
    }

    private void OnDestroy()
    {
        if (proj != null)
        {
            proj.OnProjectileHit -= HandleHit;
        }
    }
}