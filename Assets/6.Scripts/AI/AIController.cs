using Unity.Behavior;
using UnityEngine;

[RequireComponent(typeof(PerceptionComponent))]
[RequireComponent(typeof(AIBehaviourComponent))]
public abstract class AIController : MonoBehaviour
{
    [SerializeField] protected BehaviorGraphAgent bgAgent;

    protected PerceptionComponent perception;
    protected AIBehaviourComponent aiBehaivour;
  
    protected virtual void Awake()
    {
        perception = GetComponent<PerceptionComponent>();
        aiBehaivour = GetComponent<AIBehaviourComponent>();
    }
}
