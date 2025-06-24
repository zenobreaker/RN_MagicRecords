using System;
using System.Collections;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;

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
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private Image resetTimeGauge;
    [SerializeField] private TextMeshProUGUI inputEnabledText;
    [SerializeField] private Image enableTimeGauge;
    [SerializeField] private TextMeshProUGUI inputBufferedText;
    [SerializeField] private Image bufferTimeGauge;

    Color inputEnabledOriginColor;
    Color inputBufferdOriginColor;

    Coroutine enableTimeCoroutine;
    Coroutine bufferTimeCoroutine;

    private Character debug_Character;

    private void Awake()
    {
        debug_Character = FindAnyObjectByType<Player>();
        timescaleSlider.onValueChanged.AddListener(OnValueChanged);

        if (debug_Character != null)
        {
            ComboComponent combo = debug_Character.GetComponent<ComboComponent>();
            Debug.Assert(combo != null);
            Debug.Assert(combo.ComboInputHanlder != null);
            combo.ComboInputHanlder.OnInputResetTime += Draw_InputResetTime;
            combo.ComboInputHanlder.OnComboIndex += Draw_ComboText;
            combo.ComboInputHanlder.OnInputCommandType += Draw_InputText;
            combo.ComboInputHanlder.OnInputEnabled += Draw_InputEnabled;
            combo.ComboInputHanlder.OnInputEnableTime += Draw_InputEnableTime;
            combo.ComboInputHanlder.OnInputBuffered += Draw_InputBuffered;
            combo.ComboInputHanlder.OnInputBufferTime += Draw_InputBufferTime;
            combo.ComboInputHanlder.OnInputReset+= Draw_InputReset;
        }
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

    private void OnEnable()
    {
        if (inputEnabledText != null)
        {
            inputEnabledOriginColor = inputEnabledText.color;
        }

        if (inputBufferedText != null)
        {
            inputBufferdOriginColor = inputBufferedText.color;
        }
    }


    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bDebugView = !bDebugView;
        }

        OnEnable_DebugUI(bDebugView);

        Draw_PlayerState();

        if (Input.GetKeyDown(KeyCode.Alpha9))
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
            ActiveSkill activeSkill = new ReinforcedMagicBullet("Skills/Shooter/reinforecedmaigcbullet");

            if (activeSkill != null)
            {
                skill.SetActiveSkill(SkillSlot.Slot1, activeSkill);
                Debug.Log($"skill 장착 완료");
            }
        }
    }


#region Input & Comob

    private void Draw_InputText(InputCommandType inputCommandType)
    {
        if (inputText == null) return; 
        inputText.text ="Input : " + inputCommandType.ToString(); 
    }

    private void Draw_ComboText(int count)
    {
        if (comboText == null) return;
        comboText.text = "Combo : " + count.ToString(); 
    }

    private void Draw_InputResetTime(float time, float maxTime)
    {
        if (resetTimeGauge == null) return; 
        resetTimeGauge.fillAmount = time / maxTime; 
    }

    private void Draw_InputEnabled(bool enabled)
    {
        if (inputEnabledText == null) return;

      
        if (enabled)
        {
            inputEnabledText.color = inputEnabledOriginColor;
            inputEnabledText.text = "Combo Timing In";
        }
        else
        {
            inputEnabledText.color = Color.red;
            inputEnabledText.text = "Combo Timing Out";
        }
    }


    private void Draw_InputEnableTime(float time)
    {
        ResetEnableTime();

        enableTimeCoroutine = StartCoroutine(Time_Gauage(enableTimeGauge, time));
    }

    private void ResetEnableTime()
    {
        if (enableTimeGauge != null)
            enableTimeGauge.fillAmount = 0;

        if (enableTimeCoroutine != null)
        {
            StopCoroutine(enableTimeCoroutine);
            enableTimeCoroutine = null;
        }
    }

    private void Draw_InputBuffered(bool inBuffered)
    {
        if (inputBufferedText== null) return;

        inputBufferdOriginColor = inputBufferedText.color;
        if (inBuffered)
        {
            inputBufferedText.color = inputBufferdOriginColor;
            inputBufferedText.text = "Combo Buffered In";
        }
        else
        {
            inputBufferedText.color = Color.red;
            inputBufferedText.text = "Combo Buffered Out";
        }
    }


    private void Draw_InputBufferTime(float time)
    {
        ResetBufferTime();

        bufferTimeCoroutine = StartCoroutine(Time_Gauage(bufferTimeGauge, time));
    }

    private void ResetBufferTime()
    {
        if (bufferTimeGauge != null)
            bufferTimeGauge.fillAmount = 0;


        if (bufferTimeCoroutine != null)
        {
            StopCoroutine(bufferTimeCoroutine);
            bufferTimeCoroutine = null;
        }
    }

    private IEnumerator Time_Gauage(Image gauge, float time)
    {
        if (gauge == null)
            yield break; 

        float currentTime = time;
        while (currentTime >= 0.0f)
        {
            currentTime -= Time.deltaTime;
            gauge.fillAmount = currentTime / time;
            yield return null;
        }
    }


    private void Draw_InputReset()
    {
        if (inputText == null) return;
        if (comboText == null) return;
        if (resetTimeGauge == null) return;
        if (inputEnabledText == null) return;
        if (inputBufferedText == null) return;

        //inputText.text = "Input :";
        comboText.text = "Combo : ";
        resetTimeGauge.fillAmount = 0;
        inputEnabledText.color = inputEnabledOriginColor;
        inputEnabledText.text = "Combo Enabled";
        inputBufferedText.color = inputBufferdOriginColor;
        inputBufferedText.text = "Combo Buffered";

        ResetEnableTime();
        ResetBufferTime();
    }

 

    #endregion
}
