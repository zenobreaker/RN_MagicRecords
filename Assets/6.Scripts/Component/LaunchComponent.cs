using System.Collections;
using UnityEngine;

public class LaunchComponent : MonoBehaviour
{
    [SerializeField]
    private int ChangeFrame = 5; 

    private Rigidbody rigid;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        Debug.Assert(rigid != null); 
    }

    public void ApplyLaunch(GameObject attacker, Weapon causer, HitData hitData)
    {
        if (rigid == null || hitData == null || attacker == null) return;

        Vector3 dir = attacker.transform.forward; 
        float lauch = rigid.mass * hitData.Distance;

#if UNITY_EDITOR
        Debug.Log("Launch Start");
#endif
        rigid.isKinematic = false;
        rigid.AddForce(dir * lauch, ForceMode.Impulse);

        StartCoroutine(Change_IsKinematics(ChangeFrame));
    }


    private IEnumerator Change_IsKinematics(int frame)
    {

        for (int i = 0; i < frame; i++)
            yield return new WaitForFixedUpdate();

        rigid.isKinematic = true;

#if UNITY_EDITOR
        Debug.Log("Launch End");
#endif
    }

}
