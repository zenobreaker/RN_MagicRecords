using UnityEngine;

public class Cheater : MonoBehaviour
{
    private void Start()
    {
        
    }

    private void Update()
    {
        // ������Ʈ Ǯ�� �׽�Ʈ
        if(Input.GetKeyDown(KeyCode.Alpha8))
        {
            ObjectPooler.SpawnFromPool("Bullet", Vector2.up);
        }
    }

}
