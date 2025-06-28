using UnityEngine;

public class WarningSign : MonoBehaviour
{
    [SerializeField] protected Transform mainPlane;
    [SerializeField] protected Transform subPlane;

    protected float duration = 1.0f;
    protected float startTime;

    private float maxScale = 2.0f;
    private float currentScale;

    protected virtual void OnEnable()
    {
        startTime = Time.time;
        currentScale = 0.0f; 
        subPlane.localScale = Vector3.zero;
        mainPlane.localScale = Vector3.one * maxScale;
    }

    protected virtual void OnDisable()
    {
        ObjectPooler.ReturnToPool(gameObject);
    }

    protected virtual void Update()
    {
        if (currentScale < maxScale)
        {
            float elapsedTime = Time.time - startTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            currentScale = Mathf.Lerp(0f, maxScale, t);

            subPlane.localScale = Vector3.one * currentScale;
        }
        else
            gameObject.SetActive(false);
    }

    public void SetData(float scale, float duration = 1.0f)
    {
        maxScale = scale;
        this.duration = duration;
    }
}
