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

    //private void Update()
    //{
    //    if (movingComponent == null)
    //        return;

    //    RollingWheel(movingComponent.DeltaSpeed);
    //}

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

        Debug.Log($"Wheel : {speed * Time.deltaTime * torque}");
        tireObject.transform.Rotate(speed  * Time.deltaTime * torque, 0, 0);
    }
}
