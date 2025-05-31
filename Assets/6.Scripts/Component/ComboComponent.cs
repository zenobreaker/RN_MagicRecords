using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum InputCommandType
{
    None = 0,   
    Action = 1,
    Skill,
    Move,
    Dash,
    Max,
}


public partial class ComboComponent : MonoBehaviour
{
    class InputCommand
    {
        public InputCommandType InputType;
        public int SkillSlotIndex = -1; // 스킬 SkillSlot 1~4 대응
        public float TimeStamp;
    }

    public bool bDebug;

    private float comboResetTime = 1.0f;        // 콤보(입력 큐) 유지 시간 
    private float lastInputTime = 0.0f;         // 마지막에 입력한 콤보 입력 시간  
    private int comboIndex = 0;

    private Queue<InputCommand> inputQueue;

    [SerializeField] private SO_Combo currComboObj;
    [SerializeField] private SO_ComboInputHanlder comboInputHandler;
    public SO_ComboInputHanlder ComboInputHanlder {get => comboInputHandler;  }

    private WeaponComponent weapon;
    private SkillComponent skill; 
    private DashComponent dash;
    private Character ownerCharacter; 

    private Coroutine comboResetCoroutine;

    private void Awake()
    {
        ownerCharacter = GetComponent<Character>();
        if (ownerCharacter != null)
        {
            ownerCharacter.OnBeginDoAction += OnBeginDoAction;
            ownerCharacter.OnEndDoAction += OnEndDoAction;

            weapon = ownerCharacter.GetComponent<WeaponComponent>();
            if(weapon != null)
                weapon.OnWeaponTypeChanged_Combo += OnWeaponTypeChanged_Combo;

            skill = ownerCharacter.GetComponent<SkillComponent>();
            dash = ownerCharacter.GetComponent<DashComponent>();
        }

        inputQueue = new Queue<InputCommand>();
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
        if (ownerCharacter == null) return false; 
        return ownerCharacter.InAction == false;
    }

    private void TryProcessInput(InputCommand newInput)
    {
        if (newInput == null) return;

        //if(inputQueue.Count > 0 && inputQueue.Peek().InputType != newInput.InputType)
        //{
        //    ResetCombo();
        //    return; 
        //}

        switch(newInput.InputType)
        {
            case InputCommandType.Action:   TryProcess_Action(newInput); break;
            case InputCommandType.Skill:    TryProcess_Skill(newInput);  break;
            case InputCommandType.Move:     TryProcess_Move(newInput);   break; 
            case InputCommandType.Dash:     TryProcess_Dash(newInput);   break;
        }
    }

    // 입력 정보를 InputCommand로 변환 후 큐잉 
    public void InputQueue(InputCommandType commandType, int skillIndex = -1)
    {
        float currentTime = Time.time;
        var inputCommand = new InputCommand
        {
            InputType = commandType,
            SkillSlotIndex = skillIndex,
            TimeStamp = currentTime,
        };

        comboInputHandler?.HandleInputCommandType(commandType);
        TryProcessInput(inputCommand);
    }


    // 입력 제한 시간 안에 입력 받았는지 검사한다. 
    private IEnumerator Rest_ComboTime()
    {
#if UNITY_EDITOR
        if (bDebug)
            Debug.Log($"Reset Start:  {comboIndex}");
#endif

        float currentResetTime = comboResetTime;
        while (currentResetTime > 0)
        {
            currentResetTime -= Time.deltaTime;
            comboInputHandler?.HandleInputResetTime(currentResetTime, comboResetTime);
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

        // 진행했던 애니메이션이 끝나고 이곳을 호출하게 되면 종료자를 호출한다. 
        if (comboResetCoroutine != null)
        {
            StopCoroutine(comboResetCoroutine);
            comboResetCoroutine = null;
        }

        if (comboIndex >= currComboObj.MaxComboIndex())
        {
            ResetCombo();
            return;
        }

        comboResetCoroutine = StartCoroutine(Rest_ComboTime());
    }


    private void ResetCombo()
    {
        if (comboResetCoroutine != null)
        {
            StopCoroutine(comboResetCoroutine);
            comboResetCoroutine = null;
        }

        comboIndex = 0;
#if UNITY_EDITOR
        if (bDebug)
            Debug.Log($"Reset Combo :  {comboIndex}");
#endif
        var data = currComboObj?.GetComboData(0);
        
        lastInputTime = Time.time;
        comboResetTime = data.ComboResetTime;
        comboResetCoroutine = null;

        inputQueue.Clear();
        comboInputHandler?.HadleInputReset();
    }
}
