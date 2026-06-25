using UnityEngine;

public class SpikeShot 
    : ActiveSkill

{
    private WarningSign_Rect rectSign; 

    public SpikeShot(SO_SkillData skillData) : base(skillData)
    {
    }

    protected override void ApplyEffects()
    {
        
    }

    protected override void ExecutePhase(int phaseIndex)
    {
        SetCurrentPhaseSkill(phaseIndex);
        if (phaseSkill == null || actionData == null)
            return;

        Vector3 pos = ownerObject.transform.position;
        pos.y = 0.01f; 


        rectSign = ObjectPooler.DeferredSpawnFromPool<WarningSign_Rect>("WarningSign_Rect", pos, ownerObject.transform.rotation);
        rectSign.SetRectData(0.5f, 2, 0, 2, 2.0f);
        rectSign.OnEndSign += OnEndSign;
        ObjectPooler.FinishSpawn(rectSign.gameObject);
    }

    public void OnEndSign()
    {
        // 💡 [방어선 1] 만약 스킬이 이미 취소되어서 장판 연결이 끊겼다면 무시합니다!
        if (actionData == null || rectSign == null)
            return;

        ownerCharacter.SafeInvoke(v => v.PlayAction(actionData));
        weaponController.SafeInvoke(v => v.DoAction(actionData));
    }


    public override void End_DoAction()
    {
        base.End_DoAction();

        phaseSkill = null;

        // 적에게 맞아서 강제로 취소(End_DoAction 호출)되었을 때!
        if (rectSign != null)
        {
            // 1. "장판아, 타이머 다 돌아도 나한테 이제 알려주지 마!" (이벤트 구독 해제)
            rectSign.OnEndSign -= OnEndSign;

            // 2. 바닥에 깔려있는 장판 오브젝트를 즉시 치워버립니다 (풀에 반환).
            // WarningSign_Rect 내부에 진행을 멈추는 함수(ex: StopSign)가 있다면 그걸 먼저 호출해주면 더 좋습니다.
            if (rectSign.gameObject.activeInHierarchy)
            {
                ObjectPooler.ReturnToPool(rectSign.gameObject);
            }

            // 3. 참조 비우기
            rectSign = null;
        }
    }

    public override void Begin_JudgeAttack(AnimationEvent e)
    {
        if (phaseSkill == null) return;

        base.Begin_JudgeAttack(e);


        // 가시 오브젝트 생성 
        Vector3 localOffset = phaseSkill.spawnPosition; // 스폰 위치(로컬 기준)
        Vector3 position = ownerObject.transform.TransformPoint(localOffset); // 로컬 -> 월드 좌표로 변경
        Quaternion rotation = ownerObject.transform.localRotation * phaseSkill.ValidSpawnQuaternion;

        GameObject obj = ObjectPooler.SpawnFromPool(phaseSkill.objectName, position, rotation);
        if (obj.TryGetComponent<ISkillEffect>(out var projectile))
        {
            projectile.SetDamageInfo(ownerCharacter, damageData);
            projectile.AddIgnore(ownerObject);
        }
    }

    public override void Play_Sound()
    {
        if (phaseSkill == null) return;
        base.Play_Sound();

        actionData.Play_Sound();
    }

    public override void Play_CameraShake()
    {
        if (phaseSkill == null) return;
        base.Play_CameraShake();

        actionData.Play_CameraShake();
    }
}
