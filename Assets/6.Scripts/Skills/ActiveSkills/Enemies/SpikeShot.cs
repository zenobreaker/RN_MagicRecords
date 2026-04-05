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
        if (phaseSkill == null || phaseSkill.actionData == null)
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
        if (phaseSkill == null || phaseSkill.actionData == null)
            return;

        ownerCharacter?.PlayAction(phaseSkill?.actionData);
        weaponController?.DoAction(phaseSkill?.actionData);
    }


    public override void End_DoAction()
    {
        base.End_DoAction();

        phaseSkill = null;
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
            projectile.SetDamageInfo(ownerObject, phaseSkill.damageData);
            projectile.AddIgnore(ownerObject);
        }
    }

    public override void Play_Sound()
    {
        if (phaseSkill == null) return;
        base.Play_Sound();

        phaseSkill.actionData.Play_Sound();
    }

    public override void Play_CameraShake()
    {
        if (phaseSkill == null) return;
        base.Play_CameraShake();

        phaseSkill.actionData.Play_CameraShake();
    }
}
