using UnityEngine;

public class WheelRolling : MonoBehaviour
{
    public GameObject tireObject;
    public float torque = 10.0f;

    private PlayerMovingComponent movingComponent;

    private void Awake()
    {
        movingComponent = GetComponentInParent<PlayerMovingComponent>();
        Debug.Log(movingComponent != null);
    }

    private void LateUpdate()
    {
        if (movingComponent == null)
            return;

        RollingWheel(movingComponent.DeltaSpeed);
    }

    public void RollingWheel(float speed)
    {
        if (tireObject == null)
            return; 

        tireObject.transform.Rotate(speed  * Time.deltaTime * torque, 0, 0);
    }
}
