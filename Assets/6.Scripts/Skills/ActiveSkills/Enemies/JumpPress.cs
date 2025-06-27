using NUnit.Framework.Constraints;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class JumpPress
    : ActiveSkill
{

    private float soarSpeed = 10.0f;
    private float maxSoar = 20.0f;
    private bool isSoaring = false;

    private float fallSpeed = 10.0f;
    private bool isFalling = false; 
    private float groundCheckDistance = 0.2f;
    private LayerMask groundMask;

    private bool isGrounded;
    private bool wasGrounded;

    private CharacterVisual visual;
    private NavMeshAgent agent;

    public JumpPress(string path) : base(path)
    {
        groundMask = 1 << LayerMask.NameToLayer("Default");
    }

    public JumpPress(SO_ActiveSkillData skillData) : base(skillData)
    {
        groundMask = 1 << LayerMask.NameToLayer("Default");
    }

    public override void SetOwner(GameObject gameObject)
    {
        base.SetOwner(gameObject);
        agent = gameObject.GetComponent<NavMeshAgent>();
        visual = gameObject.GetComponent<Character>()?.Visual;
    }

    protected override void ApplyEffects()
    {
        
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        if (phaseIndex == 0  && isSoaring)
        {
            Soaring(deltaTime); 
        }
        else if(phaseIndex == 1 && isFalling)
        {
            Press(deltaTime);
        }
    }

    protected override void ExecutePhase(int phaseIndex)
    {
        SetCurrentPhaseSkill(phaseIndex);
        if (phaseSkill == null || phaseSkill.actionData == null)
            return;

        if (phaseIndex == 1)
            ExecutePhase_1();

        if (string.IsNullOrEmpty(phaseSkill?.actionData?.StateName) == false)
        {
            animator.SetFloat(phaseSkill.actionData.ActionSpeedHash, phaseSkill.actionData.ActionSpeed);
            animator.Play(phaseSkill?.actionData?.StateName, 0, 0);
            weaponController?.DoAction(phaseSkill?.actionData?.StateName);
        }
    }

    private void ExecutePhase_1()
    {
        visual?.ShowModel();
        PerceptionComponent percept = ownerObject.GetComponent<PerceptionComponent>();
        if (percept != null)
        {
            GameObject target = percept.GetTarget();
            if (target != null)
            {
                ownerObject.transform.position = new Vector3(target.transform.position.x, ownerObject.transform.position.y, target.transform.position.z);
                WarningSign sign = ObjectPooler.DeferedSpawnFromPool<WarningSign>("WarningSign_Circle", target.transform.position);
                sign.SetData(0.5f, 2.0f);
                ObjectPooler.Instance.FinishSpawn(sign.gameObject);
            }
        }
    }

    private void Soaring(float deltaTime)
    {
        if (ownerObject.transform.position.y >= maxSoar)
        {
            visual?.HideModel();
            isFalling = true; 
            ExecutePhase(phaseIndex + 1);
        }
        else
        {
            ownerObject.transform.position += Vector3.up * deltaTime * soarSpeed;
        }
    }

    private void Press(float deltaTime)
    {
        if (IsCurrentlyOnGround() == false)
        {
            ownerObject.transform.position += Vector3.down * deltaTime * fallSpeed;
            return;
        }
        
        isFalling = false; 
        ExecutePhase(phaseIndex + 1);
    }


    private bool CheckGround()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(ownerObject.transform.position, groundCheckDistance, groundMask);

        return !wasGrounded && isGrounded;
    }

    private bool IsCurrentlyOnGround()
    {
        Collider[] hits = Physics.OverlapSphere(ownerObject.transform.position, groundCheckDistance, groundMask);

        foreach(var hit in hits)
        {
            if (hit.gameObject == ownerObject 
                || hit.gameObject.transform.root == ownerObject.transform) continue;
            return true;
        }

        return false;
    }

    public override void Start_DoAction()
    {
        if (phaseIndex == 0)
        {
            isGrounded = true; 
            isSoaring = true;
            
            if(agent != null)
                agent.enabled = false;
        }
    }

    public override void End_DoAction()
    {
        base.End_DoAction();

        if (phaseIndex == 2)
            agent.enabled = true; 
    }


    public override void Begin_JudgeAttack(AnimationEvent e)
    {
        base.Begin_JudgeAttack(e);
        // Create Effect and Judge
        Collider[] colliders = Physics.OverlapSphere(ownerObject.transform.position, 2.0f);
        foreach (var collider in colliders)
        {
            var other = collider.gameObject;
            if (TeamUtility.IsSameTeam(other, ownerObject))
                continue;

            if (other.TryGetComponent<IDamagable>(out var damage))
            {
                damage.OnDamage(ownerObject, null, other.transform.position, phaseSkill.damageData.GetMyDamageEvent(ownerObject));
            }
        }
    }
}
