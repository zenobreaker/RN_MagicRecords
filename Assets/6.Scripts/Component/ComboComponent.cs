using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum InputCommandType
{
    None = 0,   
    Action = 1,
    Move,
    Dash,
    Max,
}


public partial class ComboComponent : MonoBehaviour
{
    class InputElement
    {
        public string InputType; // 입력 타입
        public float TimeStamp;  // 입력이 발생한 시간
        public int comboCount;
    }

    class InputCommand
    {
        public InputCommandType InputType; public float TimeStamp;
    }

    public bool bDebug;

    private float comboResetTime = 1.0f;        // 콤보(입력 큐) 유지 시간 

    private float lastInputTime = 0.0f;             // 마지막에 입력한 콤보 입력 시간  
    private float lastComboEnd = 0.0f;              // 마지막 동작 종료 시간
    private int comboIndex = 0;

    private Queue<InputCommand> inputQueue;

    [SerializeField] private SO_Combo currComboObj;
    [SerializeField] private SO_ComboInputHanlder comboInputHandler;
    public SO_ComboInputHanlder ComboInputHanlder {get => comboInputHandler;  }


    private WeaponComponent weapon;

    private Coroutine comboResetCoroutine;

    private void Awake()
    {
        weapon = GetComponent<WeaponComponent>();
        Debug.Assert(weapon != null);
        weapon.OnWeaponTypeChanged_Combo += OnWeaponTypeChanged_Combo;
        weapon.OnBeginDoAction += OnBeginDoAction;
        weapon.OnEndDoAction += OnEndDoAction;

        inputQueue = new Queue<InputCommand>();

        lastInputTime = -999f;
    }

    private void OnWeaponTypeChanged_Combo(SO_Combo comboData)
    {
        if (comboData == null)
            return;

        currComboObj = comboData;
        ResetCombo();
    }

    private bool CanExecuteNextAction()
    {
        if (weapon == null) return false; 

        return weapon.InAction == false;
    }

    private void TryProcessInput(InputCommand newInput)
    {
        if (newInput == null) return;

        comboIndex %= currComboObj.MaxComboIndex();
        ComboData comboData = currComboObj.GetComboData(comboIndex);
        float currentTime = newInput.TimeStamp;

        // 콤보 유지 시간 초과 시 리셋
        if (inputQueue.Count > 0 && currentTime - lastComboEnd >= comboData.ComboResetTime)
        {
            ResetCombo();
            return;
        }

        // 유효한 입력인지 체크
        bool isWithinLastInputTime = (currentTime - lastInputTime) <= comboData.LastInputCheckTime;
        bool isBuffered = (currentTime - lastInputTime) <= comboData.InputBufferTime;
        bool isFirstInput = lastInputTime < 0;

        // 지금 시간을 기록 
        lastInputTime = Time.time;

        if (isFirstInput || isWithinLastInputTime || isBuffered) 
        {
            inputQueue.Enqueue(newInput);

            if (newInput.InputType == InputCommandType.Action && CanExecuteNextAction())
            {
                ExecuteAttack(comboIndex);
                comboIndex++;
            }
        }
    }

    private void ExecuteAttack(int index)
    {
        if (currComboObj == null)
            return;

        ComboData data = currComboObj.GetComboData(index);
      
        comboResetTime = data.ComboResetTime;

        // Action 실행
#if UNITY_EDITOR
        if (bDebug)
            Debug.Log($"Execute Combodata {index}");
#endif
        weapon.DoAction(index);
    }


    // 입력 정보를 InputCommand로 변환 후 큐잉 
    public void InputQueue(InputCommandType commandType)
    {
        float currentTime = Time.time;
        var inputCommand = new InputCommand
        {
            InputType = commandType,
            TimeStamp = currentTime,
        };

        TryProcessInput(inputCommand);
    }


    // 입력 제한 시간 안에 입력 받았는지 검사한다. 
    private IEnumerator Rest_ComboTime()
    {
#if UNITY_EDITOR
        if (bDebug)
            Debug.Log($"Reset Start:  {comboIndex}");
#endif

        float currentRestTime = comboResetTime;
        while (currentRestTime > 0)
        {
            currentRestTime -= Time.deltaTime;
            comboInputHandler?.HandleComboRemainTime(currentRestTime);
            yield return null;
        }

        ResetCombo();
    }

    public void OnBeginDoAction()
    {
  
    }

    //TODO : Cancel 타이밍부터 리셋 카운트 내용 추가하기
    public void OnEndDoAction()
    {
        lastComboEnd = Time.time;

        // 진행했던 애니메이션이 끝나고 이곳을 호출하게 되면 종료자를 호출한다. 
        if (comboResetCoroutine != null)
            StopCoroutine(comboResetCoroutine);

        comboResetCoroutine = StartCoroutine(Rest_ComboTime());
    }


    private void ResetCombo()
    {
        comboIndex = 0;
#if UNITY_EDITOR
        if (bDebug)
            Debug.Log($"Reset Combo :  {comboIndex}");
#endif

        var data = currComboObj?.GetComboData(0);
        comboResetTime = data.ComboResetTime;

        lastComboEnd = Time.time;
        lastInputTime = -999;
        comboResetCoroutine = null;

        inputQueue.Clear();
        DestroyComboUIObjs();
    }

}
