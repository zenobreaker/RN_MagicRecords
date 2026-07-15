using System;
using UnityEngine;

[ModuleCategory("Passive/Skill Modifier/스파이럴 끌어당김")]
[Serializable]
public class Module_Passive_HitOnInhalation : PassiveModule
{
    public int targetSkillID;
    public float radius = 5.0f;
    public float force = 10.0f;
    public LayerMask enemyLayer;

    public Module_Passive_HitOnInhalation()
    {
        triggerTime = PassiveTriggerTime.OnSpawnObject;
    }

    public override void OnSpawnObject(ISkillEffect spawnedObject, ActiveSkill casterSkill)
    {
        if (targetSkillID != 0 && casterSkill.SkillID != targetSkillID)
            return;

        if (spawnedObject is AbstractProjectile proj)
        {
            // 💡 람다식을 이용해 로직을 직접 주입
            proj.OnTargetHitEvent += (target, hitPos) =>
            {
                ApplyPullEffect(target, hitPos);
            };
        }
    }

    private void ApplyPullEffect(GameObject target, Vector3 center)
    {
        Collider[] hitCols = Physics.OverlapSphere(center, radius, enemyLayer);
        foreach (var col in hitCols)
        {
            // 탄환에 맞은 타겟은 제외하거나, 원하는 대로 처리
            if (col.gameObject == target) continue;

            // 2. 끌어당기는 힘 적용 (Rigidbody가 있다면 AddForce, 없다면 위치 강제 이동)
            if (col.TryGetComponent<Rigidbody>(out var rb))
            {
                Vector3 pullDir = (center - col.transform.position).normalized;
                rb.AddForce(pullDir * force, ForceMode.Impulse);
            }
            else
            {
                // Rigidbody가 없는 몬스터라면 매 프레임 위치를 보간해서 옮겨주는 유틸리티 호출 등
            }
        }
    }
}
