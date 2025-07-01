using UnityEngine;

public class WarningSign_Rect : WarningSign
{
    private Vector2 maxRectScale;
    private Vector2 currentRectScale;

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

    protected override void OnEnable()
    {
        startTime = Time.time;
        initScale = currentRectScale;
        subPlane.localScale = new Vector3(currentRectScale.x, 1f, currentRectScale.y);
        mainPlane.localScale = new Vector3(maxRectScale.x, 1f, maxRectScale.y);

        SetMainPlanePosition();
        SetSubPosBottom();
    }

    protected override void Update()
    {
        if (currentRectScale.x < maxRectScale.x || currentRectScale.y < maxRectScale.y)
        {
            float elapsedTime = Time.time - startTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            currentRectScale = Vector2.Lerp(initScale, maxRectScale, t);

            UpdateSubPlanePositionAndScale();
        }
        else
            gameObject.SetActive(false);
    }

    public void SetRectData(float maxWidth, float maxHeight, 
        float width = 0.0f, float height = 0.0f, float duration = 1.0f)
    {
        maxRectScale = new Vector2(maxWidth , maxHeight);
        currentRectScale = new Vector2(width, height);
        this.duration = duration;

        subPlane.localScale = new Vector3(currentRectScale.x, 1f, currentRectScale.y);
        mainPlane.localScale = new Vector3(maxRectScale.x, 1f, maxRectScale.y);

        SetMainPlanePosition();
        SetSubPosBottom();
    }

    private void UpdateSubPlanePositionAndScale()
    {
        subPlane.transform.localScale = new Vector3(currentRectScale.x, 1.0f, currentRectScale.y);

        float currentRealHeight = subPlaneMeshFilter.mesh.bounds.size.z * currentRectScale.y;
        subPlane.localPosition = initialSubPlaneCenterPosition + subPlane.transform.forward * (currentRealHeight / 2f);
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
