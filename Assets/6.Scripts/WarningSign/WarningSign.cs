using UnityEngine;

public class WarningSign : MonoBehaviour
{
    [SerializeField] private Transform mainPlane;
    [SerializeField] private Transform subPlane;

    [SerializeField] public float growSpeed = 1.0f;
    [SerializeField] public float maxScale = 2.0f;
    [SerializeField] public float duration = 1.0f;

    private float startTime;
    private float currentScale;

    private void OnEnable()
    {
        startTime = Time.time;
        currentScale = 0.0f; 
        subPlane.localScale = Vector3.zero;
        mainPlane.localScale = Vector3.one * maxScale;
    }

    private void OnDisable()
    {
        ObjectPooler.ReturnToPool(gameObject);
    }

    private void Update()
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
