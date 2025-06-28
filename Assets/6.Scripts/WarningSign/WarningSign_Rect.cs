using UnityEngine;

public class WarningSign_Rect : WarningSign
{
    private Vector2 maxRectScale;
    private Vector2 currentScale;

    MeshFilter mainPlaneMehsFilter; 
    MeshFilter subPlaneMeshFilter;

    private Vector2 initScale;
    private Vector3 initialSubPlaneCenterPosition;
    private void Awake()
    {
        mainPlaneMehsFilter = mainPlane.GetComponent<MeshFilter>();
        subPlaneMeshFilter = subPlane.GetComponent<MeshFilter>();
    }

    protected override void OnEnable()
    {
        startTime = Time.time;
        initScale = currentScale;
        subPlane.localScale = new Vector3(currentScale.x, 1f, currentScale.y);
        mainPlane.localScale = new Vector3(maxRectScale.x, 1f, maxRectScale.y);

        SetMainPlanePosition();
        SetSubPosBottom();
    }

    protected override void Update()
    {
        if (currentScale.x < maxRectScale.x || currentScale.y < maxRectScale.y)
        {
            float elapsedTime = Time.time - startTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            currentScale = Vector2.Lerp(initScale, maxRectScale, t);

            UpdateSubPlanePositionAndScale();
        }
        else
            gameObject.SetActive(false);
    }

    public void SetRectData(float maxWidth, float maxHeight, 
        float width = 0.0f, float height = 0.0f, float duration = 1.0f)
    {
        maxRectScale = new Vector2(maxWidth , maxHeight);
        currentScale = new Vector2(width, height);
        this.duration = duration;
        
        SetMainPlanePosition();
        SetSubPosBottom();
    }

    private void UpdateSubPlanePositionAndScale()
    {
        subPlane.transform.localScale = new Vector3(currentScale.x, 1.0f, currentScale.y);

        float currentRealHeight = subPlaneMeshFilter.mesh.bounds.size.z * currentScale.y;
        subPlane.position = initialSubPlaneCenterPosition + subPlane.transform.forward * (currentRealHeight / 2f);
    }

    private void SetMainPlanePosition()
    {
        float mainPlaneRealHeight = mainPlaneMehsFilter.mesh.bounds.size.z * mainPlane.localScale.z;
        mainPlane.position = mainPlane.forward * (mainPlaneRealHeight / 2f);
    }

    // height �������� ��ġ�� �ű��. 
    // sub ��ġ�� main �� ������ �������� bottom�� �ٰ� ���� ���� �Լ�
    private void SetSubPosBottom()
    {
        float mainPlaneRealHeight = mainPlaneMehsFilter.mesh.bounds.size.z * mainPlane.localScale.z;

        initialSubPlaneCenterPosition = mainPlane.position - mainPlane.forward * (mainPlaneRealHeight / 2f);
        subPlane.position = initialSubPlaneCenterPosition;
    }
}
