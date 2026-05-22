using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

[ModuleCategory("Combat/Channeling")]
[Serializable]
public class Module_Channeling : SkillModule
{
    [Header("Channel Object Settings")]
    [SerializeField] private string objectName = "Hyperbeam_Object";
    [Tooltip("유지되는 시간 (특정 오브젝트의 lifeTime과 똑같이 맞추세요!)")]
    [SerializeField] private float channelingDuration = 2.0f;

    [Header("Damage Settings")]
    public DamageData damageData;

    public override void OnNotify(GameObject owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        CancellationToken token = owner.GetCancellationTokenOnDestroy();
        ExecuteChannelingAsync(owner, skill, token).Forget();
    }

    private async UniTaskVoid ExecuteChannelingAsync(GameObject owner, ActiveSkill skill, CancellationToken token)
    {
        Character playerChar = owner.GetComponent<Character>();

        try
        {
            // 💡 1. 빔 발사 시작! 캐릭터 이동/다른 행동 잠금 (채널링 시작)
            if (playerChar != null)
            {
                // TODO: 개발자님의 Character 스크립트에 맞게 상태 락 함수를 호출하세요.
                // 예: playerChar.SetState(State.Channeling);
                // 예: playerChar.canMove = false;
            }

            // 2. 빔 투사체 스폰 (기존 BeamProjectile 생성)
            Vector3 spawnPos = owner.transform.position; // 적절히 오프셋 추가
            Quaternion spawnRot = owner.transform.rotation;

            GameObject obj = ObjectPooler.DeferredSpawnFromPool(objectName, spawnPos, spawnRot);
            if (obj != null && obj.TryGetComponent<ISkillEffect>(out var effect))
            {
                // 데미지 세팅...
                effect.SetDamageInfo(owner, damageData, false);
                effect.AddIgnore(owner);

                // 💡 팁: 빔이 캐릭터를 따라가야(회전/이동) 한다면 여기서 부모(parent)를 owner로 묶어주거나, 
                // BeamProjectile 내부 Update에서 owner 위치를 추적하게 만들어야 합니다.
                // obj.transform.SetParent(owner.transform); 
            }

            // 💡 3. [핵심] 빔이 끝날 때까지 유니태스크로 캐릭터를 대기시킵니다!
            await UniTask.Delay(TimeSpan.FromSeconds(channelingDuration), cancellationToken: token);
        }
        finally
        {
            // 💡 4. 시간이 다 되거나 몬스터한테 맞아서 스킬이 취소되면?
            // 무조건 캐릭터의 상태 잠금을 풀어줍니다!
            if (playerChar != null)
            {
                // TODO: 개발자님의 Character 스크립트에 맞게 상태 해제 함수 호출
                // 예: playerChar.SetState(State.Idle);
                // 예: playerChar.EndAction();
                // 예: playerChar.canMove = true;
            }
        }
    }
}