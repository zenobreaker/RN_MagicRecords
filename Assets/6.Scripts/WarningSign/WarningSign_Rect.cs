using UnityEngine;

public enum RectFillMode
{
    FillForward,     // 시전 방향으로 길이가 점점 채워짐 (레이저, 돌진)
    ExpandWidth,     // 길이는 고정, 좌우 폭이 사방으로 퍼짐 (가로 베기, 브레스)
    ExpandFromCenter // 가운데에서부터 사방으로 커짐 (사각형 장판 폭발)
}

public class WarningSign_Rect : WarningSign
{
    [Header("Fill Settings")]
    public RectFillMode fillMode = RectFillMode.FillForward;

    private Vector2 maxRectScale;
    private Vector2 currentRectScale;
    private Vector2 initRectcale;

    private MeshFilter mainPlaneMeshFilter;

    private Vector3 meshBaseSize = Vector3.one;

    private void Awake()
    {
        if (mainPlane != null)
        {
            mainPlaneMeshFilter = mainPlane.GetComponent<MeshFilter>();

            if (mainPlaneMeshFilter != null &&
               mainPlaneMeshFilter.mesh != null)
            {
                meshBaseSize = mainPlaneMeshFilter.mesh.bounds.size;
            }
        }
    }

    protected override void OnEnable()
    {
        startTime = Time.time;

        currentRectScale = initRectcale;

        UpdatePlanes();
    }

    public override void Setup(IWarningData data, float duration)
    {
        base.Setup(data, duration);

        maxRectScale = data.MaxRectSize;
        initRectcale = data.RectSize;

        currentRectScale = initRectcale;

        subPlane.localScale = new Vector3(currentRectScale.x, 1f, currentRectScale.y);
        mainPlane.localScale = new Vector3(maxRectScale.x, 1f, maxRectScale.y);

        ApplyInitial();
        UpdatePlanes();
    }

    public void SetRectData(float maxWidth, float maxHeight,
        float width = 0.0f, float height = 0.0f, float duration = 1.0f)
    {
        maxRectScale = new Vector2(maxWidth, maxHeight);
        initRectcale = new Vector2(width, height);
        this.duration = duration;

        ApplyInitial();
        UpdatePlanes();
    }

    private void ApplyInitial()
    {
        if (subPlane != null)
        {
            subPlane.localScale = new Vector3(
                currentRectScale.x, 1.0f, currentRectScale.y);
        }

        if (mainPlane != null)
        {
            mainPlane.localScale = new Vector3(
                maxRectScale.x, 1f, maxRectScale.y);
            float maxLength =
                 meshBaseSize.z * maxRectScale.y;

            // 위치 처리
            switch (fillMode)
            {
                case RectFillMode.FillForward:
                case RectFillMode.ExpandWidth:
                    mainPlane.localPosition =
                    // 시전자 위치가 시작점
                        new Vector3(0f, 0f, maxLength * 0.5f);
                    break;

                case RectFillMode.ExpandFromCenter:
                    // 중앙 기준 확장
                    // 위치를 움직일 필요는 데이터에 따라 정의

                    mainPlane.localPosition = Vector3.zero;
                    break;
            }
        }
    }

    protected override void Update()
    {
        if (duration <= 0)
        {
            currentRectScale = maxRectScale;

            UpdatePlanes();

            OnEndSign?.Invoke();
            gameObject.SetActive(false);

            return;
        }

        float elapsedTime = Time.time - startTime;
        float t = Mathf.Clamp01(elapsedTime / duration);

        float progress = t;

        switch (fillMode)
        {
            case RectFillMode.FillForward:
                UpdateFillForward(progress);
                break;
            case RectFillMode.ExpandWidth:
                UpdateExpandWidth(progress);
                break;
            case RectFillMode.ExpandFromCenter:
                UpdateExpandFromCenter(progress);
                break;
        }

        UpdatePlanes();

        if (t >= 1f)
        {
            OnEndSign?.Invoke();

            gameObject?.SetActive(false);
        }
    }

    /// <summary>
    /// 시전자 위치에서 앞으로 직사각형이 채워짐 
    /// </summary>
    private void UpdateFillForward(float progress)
    {
        currentRectScale.x = maxRectScale.x;

        currentRectScale.y = Mathf.Lerp(initRectcale.y,
            maxRectScale.y, progress);
    }

    /// <summary>
    ///  길이는 최종 길이를 유지하고 좌우폭만 확장
    /// </summary>
    private void UpdateExpandWidth(float progress)
    {
        currentRectScale.x = Mathf.Lerp(initRectcale.x,
            maxRectScale.x, progress);

        currentRectScale.y = maxRectScale.y;
    }

    /// <summary>
    /// 중심을 기준으로 사방으로 확장
    /// </summary>

    private void UpdateExpandFromCenter(float progress)
    {
        currentRectScale.x =
            Mathf.Lerp(
            initRectcale.x,
            maxRectScale.x,
            progress);

        currentRectScale.y =
            Mathf.Lerp(
                initRectcale.y,
                maxRectScale.y,
                progress);
    }

    private void UpdatePlanes()
    {
        if (subPlane == null) return;

        // Sub Plane 크기 갱신 
        subPlane.localScale = new Vector3(
            currentRectScale.x,
            1f,
            currentRectScale.y);

        // 실제 Mesh 크기 갱신
        float currentWidth =
            meshBaseSize.x *
            currentRectScale.x;

        float currentLength =
            meshBaseSize.z *
            currentRectScale.y;

        float maxLength =
            meshBaseSize.z * maxRectScale.y;

        // 위치 처리
        switch(fillMode)
        {
            case RectFillMode.FillForward:
                // 시전자 위치가 시작점
                subPlane.localPosition =
                    new Vector3(0f, 0f, currentLength * 0.5f); 
                break;

            case RectFillMode.ExpandWidth:
                // 길이는 이미 maxLength 
                // 시작점은 시전자 위치 
                subPlane.localPosition =
                   new Vector3(
                       0f,
                       0f,
                       maxLength * 0.5f);

                break;

            case RectFillMode.ExpandFromCenter:
                // 중앙 기준 확장
                // 위치를 움직일 필요는 데이터에 따라 정의

                subPlane.localPosition = Vector3.zero;

                break;
        }
    }



#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        if (mainPlane == null)
            return;

        Vector3 origin =
            transform.position;

        Vector3 forward =
            transform.forward;

        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        forward.Normalize();


        // 현재 표시 크기
        Vector2 size =
            Application.isPlaying
                ? currentRectScale
                : maxRectScale;


        float width =
            meshBaseSize.x * size.x;

        float length =
            meshBaseSize.z * size.y;


        // ----------------------------------------------------
        // FillForward / ExpandWidth
        // ----------------------------------------------------

        if (fillMode == RectFillMode.FillForward ||
            fillMode == RectFillMode.ExpandWidth)
        {
            Vector3 center =
                origin +
                forward * (length * 0.5f);

            DrawRectangleGizmo(
                center,
                forward,
                width,
                length);

            return;
        }


        // ----------------------------------------------------
        // ExpandFromCenter
        // ----------------------------------------------------

        if (fillMode == RectFillMode.ExpandFromCenter)
        {
            Vector3 center = origin;

            DrawRectangleGizmo(
                center,
                forward,
                width,
                length);
        }
    }


    private void DrawRectangleGizmo(
        Vector3 center,
        Vector3 forward,
        float width,
        float length)
    {
        Vector3 right =
            Vector3.Cross(
                Vector3.up,
                forward).normalized;

        Vector3 halfWidth =
            right * (width * 0.5f);

        Vector3 halfLength =
            forward * (length * 0.5f);

        Vector3 p1 =
            center - halfLength - halfWidth;

        Vector3 p2 =
            center - halfLength + halfWidth;

        Vector3 p3 =
            center + halfLength + halfWidth;

        Vector3 p4 =
            center + halfLength - halfWidth;

        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);
    }

#endif
}
