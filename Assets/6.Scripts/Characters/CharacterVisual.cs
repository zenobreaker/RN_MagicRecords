using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

[System.Serializable]
public class DamageMotionData
{
    public DamageType damageType;
    public AnimationClip damageMotion;
}

[Serializable]
public class DamageMotionNameData
{
    public DamageType damageType;
    public string DanameAnimStateSubfixName;
}


[RequireComponent(typeof(Animator))]
public sealed class CharacterVisual : MonoBehaviour
{
    [Header("Model Settings")]
    [SerializeField] private GameObject modelRoot;

    [Header("Damage Animation Settings")]
    [Tooltip("체크 해제 시 피격 애니메이션을 생략합니다. 피격 시간은 StateComponent가 관리합니다.")]
    public bool useAnimationEvents = true;
    public string DamageAnimStateName = "Hit";
    public RuntimeAnimatorController baseController;
    public List<DamageMotionData> damageMotions = new List<DamageMotionData>();

    [Header("Movement Animation")]
    private static readonly int SPEED_HASH = Animator.StringToHash("SpeedY");
    private static readonly int WEAPONTYPE_HASH = Animator.StringToHash("WeaponType");

    [Header("Dash Animation Settings")]
    private static readonly int EVADE_HASH = Animator.StringToHash("Evade");
    private static readonly int DASH_HASH = Animator.StringToHash("Dash");

    // --- Components & State ---
    public Animator Animator
    {
        get;
        private set;
    }
    private Character character; // 💡 상위 객체에 있는 논리 컴포넌트
    private float originAnimSpeed;
    private readonly HashSet<int> supersededActionStates = new HashSet<int>();
    private string expectedActionState;
    private string expectedActionFullPath;
    private AnimatorOverrideController overrideController;
    private Dictionary<DamageType, List<DamageMotionData>> damageMotionTable;

    // --- Visual Data ---
    private MeshRenderer[] renderers;
    private List<Material> materials = new();

    // 💡 Fade 처리용 취소 토큰
    private CancellationTokenSource fadeCts;

    private void Awake()
    {
        // 1. 모델 렌더러 & 매테리얼 세팅
        if (modelRoot == null)
        {
            modelRoot = transform.Find("Model")?.gameObject;
        }

        if (modelRoot != null)
        {
            renderers = modelRoot.GetComponentsInChildren<MeshRenderer>();
            foreach (var rend in renderers)
            {
                materials.Add(rend.material);
            }
        }

        // 2. 상위(Root) 오브젝트에서 Character 컴포넌트 가져오기
        character = GetComponentInParent<Character>();

        // 3. 애니메이터 초기화
        Animator = GetComponent<Animator>();
        if (Animator != null)
        {
            originAnimSpeed = Animator.speed;
        }

        // 4. 데미지 모션 테이블 세팅
        InitDamageMotions();
    }

    public float GetAnimSpeed()
    {
        return originAnimSpeed;
    }

    private void InitDamageMotions()
    {
        damageMotionTable = new Dictionary<DamageType, List<DamageMotionData>>();

        for (int i = 0; i < (int)DamageType.MAX; i++)
        {
            damageMotionTable.Add((DamageType)i, new List<DamageMotionData>());
        }

        foreach (DamageMotionData data in damageMotions)
        {
            damageMotionTable[data.damageType].Add(data);
        }

        // 오버라이드 컨트롤러 생성
        if (Animator != null && baseController != null)
        {
            overrideController = new AnimatorOverrideController(baseController);
            Animator.runtimeAnimatorController = overrideController;
        }
    }

    #region Model Display & Material Controls
    // 숨김 / 표시
    public void HideModel()
    {
        modelRoot?.SetActive(false);
    }
    public void ShowModel()
    {
        modelRoot?.SetActive(true);
    }

    // 투명도 적용
    public void SetAlpha(float alpha)
    {
        foreach (var mat in materials)
        {
            if (mat.HasProperty("_Color"))
            {
                Color c = mat.color;
                c.a = alpha;
                mat.color = c;
            }
        }
    }

    // Outline On/Off
    public void SetOutline(bool enable)
    {
        foreach (var mat in materials)
        {
            if (enable)
            {
                mat.EnableKeyword("_OUTLINE_ON");
                mat.DisableKeyword("_OUTLINE_OFF"); // 💡 안전을 위해 반대 키워드는 꺼주는 것이 좋습니다.
            }
            else
            {
                mat.DisableKeyword("_OUTLINE_ON");
                mat.EnableKeyword("_OUTLINE_OFF");
            }
        }
    }
    #endregion

    #region Animation Controls
    // 💡 1. 액션(스킬/공격) 애니메이션 재생
    public void PlayActionAnimation(ActionData actionData, int layer, float statSpeedMultiplier, bool restartFromBeginning = false)
    {
        if (actionData == null || Animator == null)
        {
            return;
        }

        expectedActionState = actionData.StateName;
        expectedActionFullPath = Animator.GetLayerName(layer) + "." + expectedActionState;
        float finalSpeed = actionData.ActionSpeed * statSpeedMultiplier;
        Animator.SetFloat(actionData.ActionSpeedHash, finalSpeed);
        Debug.Log($"{actionData.StateName} play anim");
        AnimatorStateInfo currentState = Animator.GetCurrentAnimatorStateInfo(layer);
        bool sameState = currentState.IsName(expectedActionState) || currentState.IsName(expectedActionFullPath);
        if (Animator.IsInTransition(layer))
        {
            AnimatorStateInfo nextState = Animator.GetNextAnimatorStateInfo(layer);
            sameState |= nextState.IsName(expectedActionState) || nextState.IsName(expectedActionFullPath);
        }
        // 같은 클립을 쓰는 평타와 스킬도 이전 재생 시간을 이어받지 않습니다.
        if (restartFromBeginning || sameState)
        {
            Animator.Play(actionData.StateName, layer, 0f);
        }
        else
        {
            Animator.CrossFade(actionData.StateName, 0.1f, layer, 0f);
        }
    }

    // 💡 2. 피격 애니메이션 재생
    public void PlayDamageAnimation(HitData data, HitReactionType reaction = HitReactionType.None)
    {
        if (data == null)
        {
            return;
        }

        // 애니메이터가 없거나 이벤트 사용 안 함 체크 시
        if (Animator == null || Animator.runtimeAnimatorController == null || useAnimationEvents == false)
        {
            // 부모의 Character 로직에게 "애니메이션 없으니 피격 모션 캔슬해라" 라고 알림
            character?.End_Damaged();
            return;
        }

        if (damageMotionTable == null)
        {
            return;
        }

        // Reuse serialized animation keys without changing the attack snapshot.
        DamageType motionType = reaction switch
        {
            HitReactionType.Light => DamageType.NORMAL,
            HitReactionType.Heavy => DamageType.STRONG,
            HitReactionType.Knockback => DamageType.KNOCKBACK,
            _ => data.DamageType,
        };
        if (overrideController != null && damageMotionTable.TryGetValue(motionType, out List<DamageMotionData> list))
        {
            if (list.Count > 0 && data.HitImpactIndex >= 0 && data.HitImpactIndex < list.Count)
            {
                overrideController["Hit"] = list[data.HitImpactIndex].damageMotion;
            }
        }

        foreach (var parameter in Animator.parameters)
        {
            if (parameter.type != AnimatorControllerParameterType.Trigger || parameter.name != DamageAnimStateName)
            {
                continue;
            }

            Animator.SetTrigger(parameter.nameHash);
            break;
        }
    }


    public void PlayDeadAnimation()
    {
        Animator.SetTrigger("Dead");
    }

    // 💡 3. 애니메이션 배속 설정 (Slow 적용 시 호출)
    public void SetAnimationSpeedMultiplier(float multiplier)
    {
        if (Animator != null)
        {
            Animator.speed = originAnimSpeed * multiplier;
        }
    }

    public void SetMovementAnimation(float speedValue)
    {
        if (Animator != null)
        {
            Animator.SetFloat(SPEED_HASH, speedValue);
        }
    }

    public void SetWeaponEquipAnimation(int weaponType)
    {
        if (Animator != null)
        {
            Animator.SetBool("isEquipping", true);
            Animator.SetInteger(WEAPONTYPE_HASH, weaponType);
        }
    }

    public void PlayDashAnimation(bool isBackwardEvade)
    {
        if (Animator != null)
        {
            if (isBackwardEvade)
            {
                Animator.SetTrigger(EVADE_HASH);
            }
            else
            {
                Animator.SetTrigger(DASH_HASH);
            }
        }
    }

    #endregion

    #region Fade Async Task (UniTask)
    // Fade In/Out 실행 함수
    public void FadeTo(float targetAlpha, float duration)
    {
        if (fadeCts != null)
        {
            fadeCts.Cancel();
            fadeCts.Dispose();
        }

        fadeCts = new CancellationTokenSource();
        FadeTask(targetAlpha, duration, fadeCts.Token).Forget();
    }

    private async UniTaskVoid FadeTask(float targetAlpha, float duration, CancellationToken token)
    {
        try
        {
            // 매테리얼 배열 방어 코드
            if (materials.Count == 0)
            {
                return;
            }

            float startAlpha = materials[0].color.a;
            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = time / duration;
                float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                SetAlpha(newAlpha);

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
            }

            SetAlpha(targetAlpha);
        }
        catch (OperationCanceledException)
        {
            // 취소 시 조용히 종료
        }
    }
    #endregion

    #region Memory Management
    private void OnDisable()
    {
        CleanupTask();
    }

    private void OnDestroy()
    {
        CleanupTask();
    }

    private void CleanupTask()
    {
        expectedActionState = null;
        expectedActionFullPath = null;
        supersededActionStates.Clear();
        if (fadeCts != null)
        {
            fadeCts.Cancel();
            fadeCts.Dispose();
            fadeCts = null;
        }
    }
    #endregion

    #region Animation Event Bridge
    /// <summary>새 단독 시전 전에 이전 애니메이션의 지연 이벤트를 무효화합니다.</summary>
    public void BeginActionAnimationScope()
    {
        expectedActionState = null;
        expectedActionFullPath = null;
        supersededActionStates.Clear();
        if (Animator == null || Animator.runtimeAnimatorController == null)
        {
            return;
        }
        for (int layer = 0; layer < Animator.layerCount; layer++)
        {
            supersededActionStates.Add(Animator.GetCurrentAnimatorStateInfo(layer).fullPathHash);
            if (Animator.IsInTransition(layer))
            {
                supersededActionStates.Add(Animator.GetNextAnimatorStateInfo(layer).fullPathHash);
            }
        }
    }

    /// <summary>현재 시전의 재생 상태에서 발생한 이벤트만 액션 로직으로 전달합니다.</summary>
    private bool IsCurrentActionEvent(AnimationEvent animationEvent)
    {
        // 코드가 직접 호출하는 이벤트와 스킬 모듈의 타이머 경로는 그대로 허용합니다.
        if (animationEvent == null || animationEvent.isFiredByAnimator == false)
        {
            return true;
        }
        return IsCurrentActionState(animationEvent.animatorStateInfo);
    }

    /// <summary>전환 중인 이전 상태와 이번 시전의 이벤트 발생 상태를 구분합니다.</summary>
    private bool IsCurrentActionState(AnimatorStateInfo source)
    {
        if (string.IsNullOrEmpty(expectedActionState) == false)
        {
            return source.IsName(expectedActionState) || source.IsName(expectedActionFullPath);
        }
        // 파라미터로 전환되는 시전 동작도 이전 평타 상태의 이벤트는 받지 않습니다.
        return supersededActionStates.Contains(source.fullPathHash) == false;
    }

    public void Start_DoAction(AnimationEvent animationEvent = null)
    {
        if (IsCurrentActionEvent(animationEvent) == false)
        {
            return;
        }
        character.SafeInvoke(v => v.Start_DoAction());
    }
    public void Begin_DoAction(AnimationEvent animationEvent = null)
    {
        if (IsCurrentActionEvent(animationEvent) == false)
        {
            return;
        }
        character.SafeInvoke(v => v.Begin_DoAction());
    }
    public void End_DoAction(AnimationEvent animationEvent = null)
    {
        if (IsCurrentActionEvent(animationEvent) == false)
        {
            return;
        }
        character.SafeInvoke(v => v.End_DoAction());
    }
    public void Begin_JudgeAttack(AnimationEvent e = null)
    {
        if (IsCurrentActionEvent(e) == false)
        {
            return;
        }
        character.SafeInvoke(v => v.Begin_JudgeAttack(e));
    }
    public void End_JudgeAttack(AnimationEvent e = null)
    {
        if (IsCurrentActionEvent(e) == false)
        {
            return;
        }
        character.SafeInvoke(v => v.End_JudgeAttack(e));
    }
    public void Play_Sound(AnimationEvent animationEvent = null)
    {
        if (IsCurrentActionEvent(animationEvent) == false)
        {
            return;
        }
        character.SafeInvoke(v => v.Play_Sound());
    }
    public void Play_CameraShake(AnimationEvent animationEvent = null)
    {
        if (IsCurrentActionEvent(animationEvent) == false)
        {
            return;
        }
        character.SafeInvoke(v => v.Play_CameraShake());
    }
    public void End_Damaged()
    {
        character.SafeInvoke(v => v.End_Damaged());
    }
    #endregion
}
