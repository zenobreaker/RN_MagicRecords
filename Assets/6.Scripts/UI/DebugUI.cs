using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugUI : MonoBehaviour
{

    [SerializeField] private bool bDebugView = false;
    [Header("State Text")]
    [SerializeField] private TextMeshProUGUI stateText;
    //[SerializeField] private string PlayeName = "Namsaengyi";
    [Header("Time Scale")]
    [SerializeField] private Slider timescaleSlider;

    [Header("Combo & Input")]
    [SerializeField] private TextMeshProUGUI inputText;
    [SerializeField] private TextMeshProUGUI ComboText;
    [SerializeField] private Image remainGauge;


    private Character debug_Character;

    private void Awake()
    {
        debug_Character = FindAnyObjectByType<Player>();
        timescaleSlider.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnEnable_DebugUI(bool bView)
    {
        for (int i = 0; i < gameObject.transform.childCount; i++)
            gameObject.transform.GetChild(i).gameObject.SetActive(bView);
    }

    private void Start()
    {
        OnEnable_DebugUI(bDebugView);
    }


    private void LateUpdate()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            bDebugView = !bDebugView;
        }

        OnEnable_DebugUI(bDebugView);

        Draw_PlayerState();

        if(Input.GetKeyDown(KeyCode.Alpha9))
        {
            Test_SetSkill();
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            UIManager.Instance?.ToggleSoundUI();
        }
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

    private void Test_SetSkill()
    {
        SkillComponent skill = debug_Character.GetComponent<SkillComponent>();
        if (skill != null)
        {
            ActiveSkill activeSkill = new ReinforcedMagicBullet();
            SO_ActiveSkillData so_ActiveSkillData =
                Resources.Load<SO_ActiveSkillData>("Skills/Shooter/reinforecedmaigcbullet");

            if (so_ActiveSkillData != null)
            {
                activeSkill.SO_SkillData = so_ActiveSkillData;
                skill.SetActiveSkill(SkillSlot.Slot1, activeSkill);
                Debug.Log($"skill ÀåÂø ¿Ï·á");
            }
        }
    }


#region Input & Comob

    private void Draw_InputText()
    {

    }

    private void Draw_ComboText()
    {

    }

    private void Draw_RemainGauge()
    {

    }


#endregion
}
