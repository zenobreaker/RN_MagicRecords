using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class ComboComponent : MonoBehaviour
{
    class InputElement
    {
        public string InputType; // 입력 타입
        public float TimeStamp;  // 입력이 발생한 시간
        public int comboCount;
    }

    public bool bDebug;

    //[Header("Timer Settings")]
    private float comboCheckTime = 0.5f;         // 공격 후 다음 공격이 있는지 확인할 시간
    private float lastInputCheckTime = 0.25f;    // 다음 콤보를 입력을 바라는 제한 시간
    private float comboMaintainTime = 1.0f;        // 콤보(입력 큐) 유지 시간 
    private float curr_MaintainTime;

    private float lastInputTime = 0.0f;             // 마지막에 입력한 콤보 입력 시간  
    private float lastComboEnd = 0.0f;              // 마지막 동작 종료 시간
    private int comboCount = 0;

    private Queue<InputElement> inputQueue;

    [SerializeField] private SO_Combo currComboObj;
    private WeaponComponent weapon;

    private Coroutine comboMaintainCoroutine;

    private void Awake()
    {
        weapon = GetComponent<WeaponComponent>();
        Debug.Assert(weapon != null);
        weapon.OnWeaponTypeChanged_Combo += OnWeaponTypeChanged_Combo;
        weapon.OnBeginDoAction += OnBeginDoAction;
        weapon.OnEndDoAction += OnEndDoAction;

        inputQueue = new Queue<InputElement>();

        //Awake_Draw();
    }

    private void ResetTimerValue(ComboData data)
    {
        comboCheckTime = data.LastComboCheckTime;
        lastInputCheckTime = data.LastInputCheckTime;
        comboMaintainTime = data.ComboMaintainTime;
    }

    private void OnWeaponTypeChanged_Combo(SO_Combo comboData)
    {
        if (comboData == null)
            return;

        currComboObj = comboData;
        ResetCombo();


        // comboMaintainTimeGauge?.InitializeGauge(comboMaintainTime);

    }




    private void ExecuteAttack(ref InputElement inputElement)
    {
        if (currComboObj == null)
            return;

        ComboData data = currComboObj.GetComboDataByRewind(comboCount);
        //Create_ComboUI();

        // 시간 초기화 
        {
            ResetTimerValue(data);
        }

        // Action 실행
        {
            if (bDebug)
            {
                Debug.Log($"Execute Combodata {data.ComboIndex} / {data.StateName} / Time stamp {inputElement.TimeStamp}");
            }
            weapon.DoAction(data.ComboIndex);

            if (comboMaintainCoroutine == null)
                comboMaintainCoroutine = StartCoroutine(ComboMaintainCoroutine());
        }

        //UI 처리 
        {
            //  comboMaintainTimeGauge?.SetMaxValue(data.comboMaintainTime);
        }
    }



    public void InputCombo(KeyCode keycode)
    {
        // 마지막 콤보가 끝난 후 해당 시간이 경과했는지 확인
        if (Time.time - lastComboEnd >= comboCheckTime)
        {
            // 콤보 타이머 체크를 중단
            if (comboMaintainCoroutine != null)
            {
                StopCoroutine(comboMaintainCoroutine);
                comboMaintainCoroutine = null;
            }

            // 마지막 입력 후 해당 시간 만큼 지났는지 
            if (Time.time - lastInputTime >= lastInputCheckTime)
            {
                // 다음 콤보 실행 
                float currentTime = Time.time;
                var inputElement = new InputElement
                {
                    InputType = keycode.ToString(),
                    TimeStamp = currentTime,
                    comboCount = this.comboCount
                };

                ExecuteAttack(ref inputElement);
                comboCount++;

                lastInputTime = Time.time; // 값 최신화 
            }
        }
    }


    // 입력 제한 시간 안에 입력 받았는지 검사한다. 
    private IEnumerator ComboMaintainCoroutine()
    {
        curr_MaintainTime = comboMaintainTime;
        while (curr_MaintainTime > 0)
        {
            curr_MaintainTime -= Time.deltaTime;
            yield return null;
        }

        ResetCombo();
    }

    public void OnBeginDoAction()
    {

    }

    public void OnEndDoAction()
    {
        // 진행했던 애니메이션이 끝나고 이곳을 호출하게 되면 종료자를 호출한다. 
        comboMaintainCoroutine = StartCoroutine(ComboMaintainCoroutine());
    }


    private void ResetCombo()
    {
        comboCount = 0;
        var data = currComboObj?.GetComboDataByRewind(0);
        ResetTimerValue(data);


        lastComboEnd = Time.time;
        comboMaintainCoroutine = null;

        inputQueue.Clear();
        currComboObj?.ResetComboIndex();

        DestroyComboUIObjs();
    }

}
