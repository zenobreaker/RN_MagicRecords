using UnityEngine;

public class WarningSign_Circle : WarningSign
{
 
    MeshFilter mainPlaneMeshFilter; 
    MeshFilter subPlaneMeshFilter;

    private Vector2 initScale;
    private Vector3 initialSubPlaneCenterPosition;
    private Vector3 lookForward;
    private void Awake()
    {
        mainPlaneMeshFilter = mainPlane.GetComponent<MeshFilter>();
        subPlaneMeshFilter = subPlane.GetComponent<MeshFilter>();
    }

    public override void Setup(IWarningData data, float duration)
    {
        base.Setup(data, duration);


        SetMainPlanePosition();
        SetSubPosBottom();
    }


  

    private void SetMainPlanePosition()
    {
        float mainPlaneRealHeight = mainPlaneMeshFilter.mesh.bounds.size.z * mainPlane.localScale.z;

        mainPlane.localPosition =  Vector3.forward * (mainPlaneRealHeight / 2f);
    }

    // height 기준으로 위치를 옮긴다. 
    // sub 위치를 main 의 사이즈 기준으로 bottom에 붙게 놓는 헬퍼 함수
    private void SetSubPosBottom()
    {
        float mainPlaneRealHeight = mainPlaneMeshFilter.mesh.bounds.size.z * mainPlane.localScale.z;

        initialSubPlaneCenterPosition = mainPlane.localPosition - mainPlane.forward * (mainPlaneRealHeight / 2f);
        subPlane.localPosition = initialSubPlaneCenterPosition;
    }
}
