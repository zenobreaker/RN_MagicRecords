using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class LaunchComponent : MonoBehaviour
{
    [SerializeField]
    private int ChangeFrame = 5; 

    private Rigidbody rigid;
    private NavMeshAgent agent; 
    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
    }

    public void ApplyLaunch(GameObject attacker, Weapon causer, HitData hitData)
    {
        if (rigid == null || hitData == null || attacker == null) return;

        Vector3 dir = attacker.transform.forward; 
        float lauch = rigid.mass * hitData.Distance;

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.ResetPath();
            agent.enabled = false;
        }
        rigid.isKinematic = false;
        rigid.AddForce(dir * lauch, ForceMode.Impulse);

        StartCoroutine(Change_IsKinematics(ChangeFrame));
    }


    private IEnumerator Change_IsKinematics(int frame)
    {

        for (int i = 0; i < frame; i++)
            yield return new WaitForFixedUpdate();

        if (agent != null)
            agent.enabled = true;
        rigid.isKinematic = true;
    }
}
