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
public class CharacterVisual : MonoBehaviour
{
    [Header("Model Settings")]
    [SerializeField] private GameObject modelRoot;

    [Header("Damage Animation Settings")]
    [Tooltip("체크 해제 시, 피격 애니메이션을 재생하지 않고 즉시 로직을 종료합니다.")]
    public bool useAnimationEvents = true;
    public string DamageAnimStateName = "Hit";
    public RuntimeAnimatorController baseController;
    public List<DamageMotionData> damageMotions = new List<DamageMotionData>();

    // --- Components & State ---
    public Animator Animator { get; private set; }
    private Character character; // 💡 상위 객체에 있는 논리 컴포넌트
    private float originAnimSpeed;
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
            modelRoot = transform.Find("Model")?.gameObject;

        if (modelRoot != null)
        {
            renderers = modelRoot.GetComponentsInChildren<MeshRenderer>();
            foreach (var rend in renderers)
                materials.Add(rend.material);
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
    public void HideModel() => modelRoot?.SetActive(false);
    public void ShowModel() => modelRoot?.SetActive(true);

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
    public void PlayActionAnimation(ActionData actionData, int layer, float statSpeedMultiplier)
    {
        if (actionData == null || Animator == null) return;

        float finalSpeed = actionData.ActionSpeed * statSpeedMultiplier;
        Animator.SetFloat(actionData.ActionSpeedHash, finalSpeed);
        Animator.CrossFade(actionData.StateName, 0.1f, layer);
    }

    // 💡 2. 피격 애니메이션 재생
    public void PlayDamageAnimation(HitData data)
    {
        if (data == null) return;

        // 애니메이터가 없거나 이벤트 사용 안 함 체크 시
        if (Animator == null || Animator.runtimeAnimatorController == null || useAnimationEvents == false)
        {
            // 부모의 Character 로직에게 "애니메이션 없으니 피격 모션 캔슬해라" 라고 알림
            character?.End_Damaged();
            return;
        }

        if (damageMotionTable == null) return;

        if (overrideController != null && damageMotionTable.TryGetValue(data.DamageType, out List<DamageMotionData> list))
        {
            if (list.Count > 0 && data.HitImpactIndex < list.Count)
            {
                overrideController["Hit"] = list[data.HitImpactIndex].damageMotion;
            }
        }

        Animator.SetTrigger(DamageAnimStateName);
    }

    // 💡 3. 애니메이션 배속 설정 (Slow 적용 시 호출)
    public void SetAnimationSpeedMultiplier(float multiplier)
    {
        if (Animator != null)
            Animator.speed = originAnimSpeed * multiplier;
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
            if (materials.Count == 0) return;

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
    protected virtual void OnDisable()
    {
        CleanupTask();
    }

    protected virtual void OnDestroy()
    {
        CleanupTask();
    }

    private void CleanupTask()
    {
        if (fadeCts != null)
        {
            fadeCts.Cancel();
            fadeCts.Dispose();
            fadeCts = null;
        }
    }
    #endregion
}