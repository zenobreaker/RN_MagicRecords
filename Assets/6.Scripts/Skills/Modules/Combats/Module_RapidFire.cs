using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

[ModuleCategory("Combat/연속 발사 (Rapid Fire)")]
[Serializable]
public class Module_RapidFire : SkillModule
{
    [Header("Spawn Settings")]
    [SerializeField] private string objectName = "Projectile_Normal";

    [Tooltip("캐릭터 기준 발사 위치 (예: Z에 1을 넣으면 캐릭터 1m 앞)")]
    [SerializeField] private Vector3 spawnPosition = new Vector3(0, 1f, 1f);

    [Header("Rapid Fire Settings")]
    [Tooltip("총 몇 발을 연사할 것인가?")]
    [SerializeField] private int totalShots = 10;
    [Tooltip("발사 간격 (초)")]
    [SerializeField] private float fireInterval = 0.1f;

    [Header("Animation Settings")]
    [Tooltip("연사 시 재생할 단타 애니메이션의 상태")]
    [SerializeField] private ActionData actionData; // 애니메이터 창에 있는 노드 이름
    [Tooltip("연사 속도에 맞춰 애니메이션 배속을 조절할 것인가?")]
    [SerializeField] private bool syncAnimSpeed = true;

    [Header("Damage Settings")]
    public DamageApplyType damageApplyType = DamageApplyType.Inherit;
    public float damageMultiplier = 1.0f;
    public DamageData damageData;

    private bool isCharacterComp = false;
    private Character ownerChar;
    private WeaponController weaponCont;
    private Animator anim;
    private float originalAnimSpeed = 1f;
    private ActionData runtimeActionData;

    public override void Init(  Character owner)
    {
        base.Init(owner);

        ownerChar = owner.GetComponent<Character>();
        weaponCont = owner.GetComponent<IWeaponUser>()?.GetWeaponController();
        if (owner != null)
            isCharacterComp = true;

        if (isCharacterComp == false)
            anim = owner.GetComponentInChildren<Animator>();

        if (actionData != null)
            runtimeActionData = actionData.Clone();
    }

    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        CancellationToken token = owner.GetCancellationTokenOnDestroy();
        RapidFireAsync(owner, skill, phaseSkill, token).Forget();
    }

    private async UniTaskVoid RapidFireAsync(Character owner, ActiveSkill skill, PhaseSkill phaseSkill, CancellationToken token)
    {
        DamageData finalDamageData = GetEffectiveDamageData(phaseSkill);
        bool isCrit = skill != null && skill.Runtime.Combat.IsCritical ==  false;

        UnityEngine.AI.NavMeshAgent agent = owner.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.updateRotation = false;
            agent.velocity = Vector3.zero;
        }

        SetAnimSpeed();

        try
        {
            for (int i = 0; i < totalShots; i++)
            {
                if (token.IsCancellationRequested || owner == null || !owner.gameObject.activeInHierarchy)
                    return;

                if (isCharacterComp && ownerChar != null)
                {
                    // 애니메이션 이벤트가 해야 할 일을 코드가 대신 호출
                    ownerChar.Begin_DoAction();
                    ownerChar.Begin_JudgeAttack(null);
                }

                // 1. 탄환 발사 처리
                Vector3 basePosition = owner.transform.TransformPoint(spawnPosition);
                Quaternion finalRotation = owner.transform.rotation;

                GameObject obj = ObjectPooler.DeferredSpawnFromPool(objectName, basePosition, finalRotation);
                if (obj != null && obj.TryGetComponent<ISkillEffect>(out var projectile))
                {
                    projectile.SetDamageInfo(owner, finalDamageData, isCrit);
                    projectile.AddIgnore(owner);
                }

                ObjectPooler.FinishSpawn(obj);

                if (isCharacterComp && ownerChar != null)
                {
                    ownerChar.End_JudgeAttack();
                }

                // 💡 2. [핵심] 3D 애니메이션 0프레임 강제 재시작!
                if (isCharacterComp)
                {
                    if (ownerChar != null)
                        // 트랜지션을 무시하고 해당 상태의 0% 지점(0f)부터 즉시 재생합니다.
                        ownerChar.PlayAction(runtimeActionData);
                    if (weaponCont != null)
                        weaponCont.DoAction(runtimeActionData);
                }
                else
                    anim.Play(runtimeActionData.StateName, 0, 0f);

                if(isCharacterComp && ownerChar != null)
                    ownerChar.PlayAction(runtimeActionData);
                else
                    anim.Play(runtimeActionData.StateName, 0, 0f);

                // 3. 발사 간격 대기
                if (i < totalShots - 1)
                {
                    // 다음 총알을 쏘기 전까지 대기
                    await UniTask.Delay(TimeSpan.FromSeconds(fireInterval), cancellationToken: token);
                }
                else
                {
                    // 마지막 총알 발사 후, 캐릭터가 자연스럽게 동작을 마무리할 시간 주기
                    float lastAnimWaitTime = (actionData != null && actionData.ActionSpeed > 0)
                        ? (1f / actionData.ActionSpeed)
                        : fireInterval * 2f;

                    await UniTask.Delay(TimeSpan.FromSeconds(lastAnimWaitTime), cancellationToken: token);
                }
            }
        }
        finally
        {
            // 💡 4. 연사가 끝나거나 취소되면 애니메이션 속도를 원래대로 복구
            if (isCharacterComp && syncAnimSpeed)
            {
                ReturnAnimSpeed();

                ownerChar.End_DoAction();
                // (선택) 연사가 끝난 후 부드럽게 Idle로 돌아가게 하려면 
                // 애니메이터에 설정해둔 Idle 상태로 강제 전환하거나 Trigger를 보내주면 됩니다.
                // anim.CrossFade("Idle", 0.1f);
            }
            else if (anim != null && syncAnimSpeed)
            {
                ReturnAnimSpeed();
            }

            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.isStopped = false;
                agent.updateRotation = true;
            }
        }
    }

    private void SetAnimSpeed()
    {
        if (syncAnimSpeed)
        {
            originalAnimSpeed = runtimeActionData.ActionSpeed;
            runtimeActionData.ActionSpeed = 1f / fireInterval;
        }

        // 애니메이션 배속 동기화 (선택사항)
        if (isCharacterComp == false && anim != null)
        {
            originalAnimSpeed = anim.speed;
            // 0.1초 간격이면 10배속, 0.2초면 5배속 등 연사 속도에 맞춰 허우적거리는 속도를 보정합니다.
            // (기본 애니메이션 길이가 1초라고 가정했을 때의 대략적인 보정값)
            anim.speed = 1f / fireInterval;
        }
    }

    private void ReturnAnimSpeed()
    {
        runtimeActionData.ActionSpeed = originalAnimSpeed;
        if (isCharacterComp == false && anim != null)
            anim.speed = originalAnimSpeed;
    }

    private DamageData GetEffectiveDamageData(PhaseSkill parentPhase)
    {
        return damageData;
    }

    public override bool HasAnimationData()
    {
        // 모듈에 세팅된 actionData의 서브스테이트 이름이 비어있지 않으면 true 반환!
        return actionData != null && !string.IsNullOrEmpty(actionData.SubStateName);
    }
}