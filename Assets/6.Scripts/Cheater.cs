using UnityEngine;

public class Cheater : MonoBehaviour
{
    private void Start()
    {
        
    }

    private void Update()
    {
        // 오브젝트 풀러 테스트
        if(Input.GetKeyDown(KeyCode.Alpha8))
        {
            ObjectPooler.SpawnFromPool("Bullet", Vector2.up);
        }
    }

}
