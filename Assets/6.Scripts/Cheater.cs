using UnityEngine;

public class Cheater : MonoBehaviour
{
    private void Start()
    {
        
    }

    private void Update()
    {
        // ������Ʈ Ǯ�� �׽�Ʈ
        if(Input.GetKeyDown(KeyCode.Alpha9))
        {
            ObjectPooler.SpawnFromPool("Bullet", Vector2.up);
        }
    }

}
