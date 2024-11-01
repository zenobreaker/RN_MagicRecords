using UnityEngine;

public partial class ComboComponent : MonoBehaviour
{
    #region UI
    [SerializeField] GameObject comboInputUIPrefab;
    [SerializeField] GameObject comboInputBase;
    //[SerializeField] InputGaugeUI comboMaintainTimeGauge;
    [SerializeField] bool bDebugDraw = false;

    private string comboInputUIName = "Input_Action";
    private string comboMaintainGaugeName = "InputTimingGauge";
    private string inputBaseName = "InputComboBase";
    private string canvasName = "UserUI";
    private Canvas uiCanvas;

    #endregion

    private void Awake_Draw()
    {
        uiCanvas = GameObject.Find(canvasName).GetComponent<Canvas>();
        Debug.Assert(uiCanvas != null);
        comboInputUIPrefab = Resources.Load<GameObject>(comboInputUIName);

        var targetTransform = uiCanvas.transform.FindChildByName(inputBaseName);
        if (targetTransform != null)
        {
            comboInputBase = targetTransform.gameObject;
        }

        var uiGauge = uiCanvas.transform.FindChildByName(comboMaintainGaugeName);
        Debug.Assert(uiGauge != null);
        if (uiGauge != null)
        {
            // comboMaintainTimeGauge = uiGauge.GetComponent<InputGaugeUI>();
        }
    }

    #region UI Draw & Create & Destroy
    private void Create_ComboUI()
    {
        if (comboInputUIPrefab == null || bDebugDraw == false)
            return;

        // UI Input
        Instantiate(comboInputUIPrefab, comboInputBase.transform);
    }


    private void DestroyComboUIObjs()
    {
        if (comboInputBase == null)
            return;

        for (int i = 0; i < comboInputBase.transform.childCount; i++)
            Destroy(comboInputBase.transform.GetChild(i).gameObject);
    }

    private void DrawInputGauge()
    {
        if (bDebugDraw == false)
        {
            //comboMaintainTimeGauge?.gameObject.SetActive(false);
            return;
        }

        //comboMaintainTimeGauge?.gameObject.SetActive(true);
        //comboMaintainTimeGauge?.SetValue(curr_MaintainTime);
    }

    #endregion

    private void LateUpdate()
    {
        DrawInputGauge();
    }

    //private partial void ExecuteAttack(ref InputElement inputElement)
    //{

    //}
}
