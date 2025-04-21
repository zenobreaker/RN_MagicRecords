using UnityEngine;

public class Cheater : MonoBehaviour
{
    Player player;

    bool bStunToggle = false;

    private void Start()
    {
        player = FindAnyObjectByType<Player>();
    }

    private void Update()
    {
        // 오브젝트 풀러 테스트
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            ObjectPooler.SpawnFromPool("Bullet", Vector2.up);
        }

        // 스턴 테스트 
        if (Input.GetKeyDown(KeyCode.Keypad7))
            Test_Stun();
    }


    private void Test_Stun()
    {
        if (player != null)
        {
            bStunToggle = !bStunToggle;

            StatusEffectComponent sfc = player.GetComponent<StatusEffectComponent>();
            if (sfc != null && bStunToggle)
                sfc.AddStunEffect();
            else if( sfc!=null && bStunToggle == false)
                sfc.RemoveStunEffect();
        }
    }
}
