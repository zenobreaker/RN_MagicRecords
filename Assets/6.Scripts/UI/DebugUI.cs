using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugUI : MonoBehaviour
{

    [SerializeField] private bool bDebugView = false; 

    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private string PlayeName = "Namsaengyi";
    [SerializeField] private Slider timescaleSlider;
    private Character debug_Character;

    private void Awake()
    {
        debug_Character = FindAnyObjectByType<Player>();
        timescaleSlider.onValueChanged.AddListener( OnValueChanged);
    }

    private void Start()
    {
        if (bDebugView == false)
            this.gameObject.SetActive(false);
        else 
            this.gameObject.SetActive(true);
    }


    private void LateUpdate()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            bDebugView = !bDebugView;
        }

        if (bDebugView == false)
            this.gameObject.SetActive(false);
        else
            this.gameObject.SetActive(true);

        Draw_PlayerState();
    }


    private void Draw_PlayerState()
    {
        if (debug_Character == null || stateText == null)
            return;

        string state = debug_Character.GetComponent<StateComponent>().Type.ToString();

        stateText.text = "Current : " + state;
    }

    private void OnValueChanged(float value)
    {
        Time.timeScale = value;
    }
}
