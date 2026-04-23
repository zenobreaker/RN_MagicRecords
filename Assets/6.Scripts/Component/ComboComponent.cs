using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ComboComponent : MonoBehaviour
{
    class InputCommand
    {
        public InputCommandType InputType;
        public int SkillSlotIndex = -1;
        public float TimeStamp;
    }

    public bool bDebug;

    private bool bCanComboInput = true;
    private float comboResetTime = 1.0f;
    private float lastInputTime = 0.0f;
    private int comboIndex = 0;

    private Queue<InputCommand> inputQueue;

    [SerializeField] private SO_Combo currComboObj;
    [SerializeField] private SO_ComboInputHanlder comboInputHandler;
    public SO_ComboInputHanlder ComboInputHanlder { get => comboInputHandler; }

    private WeaponComponent weapon;
    private SkillComponent skill;
    private DashComponent dash;
    private MovementComponent movement; 
    private Character ownerCharacter;
    

    private CancellationTokenSource comboCts;

    private void Awake()
    {
        ownerCharacter = GetComponent<Character>();
        if (ownerCharacter != null)
        {
            ownerCharacter.OnEndDoAction += OnEndDoAction;

            weapon = ownerCharacter.GetComponent<WeaponComponent>();
            if (weapon != null)
                weapon.OnWeaponTypeChanged_Combo += OnWeaponTypeChanged_Combo;

            skill = ownerCharacter.GetComponent<SkillComponent>();
            dash = ownerCharacter.GetComponent<DashComponent>();
            movement = ownerCharacter.GetComponent<MovementComponent>(); 
        }

        inputQueue = new Queue<InputCommand>();
    }

    private void OnDisable()
    {
        CancelComboTimer();
    }

    public void BreakCombo()
    {
        ResetCombo();
    }

    private void OnWeaponTypeChanged_Combo(SO_Combo comboData)
    {
        if (comboData == null) return;
        currComboObj = comboData;
        ResetCombo();
    }

    // 💡 [버그 픽스 1]: 새로운 아키텍처에 맞게 무기와 스킬이 바쁜지 "직접" 확인합니다!
    private bool CanExecuteNextAction()
    {
        if (weapon != null && weapon.InAction) return false;
        if (skill != null && skill.InAction) return false;

        return true; // 아무것도 안 하고 있을 때만 true!
    }

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

    private void TryProcessInput(InputCommand newInput)
    {
        if (newInput == null) return;

        // 평타가 아닌 다른 조작이 들어오면 즉시 콤보 초기화 (1타로 되돌림)
        if (newInput.InputType != InputCommandType.ACTION)
        {
            ResetCombo();
        }

        switch (newInput.InputType)
        {
            case InputCommandType.ACTION: TryProcess_Action(newInput); break;
            case InputCommandType.SKILL: TryProcess_Skill(newInput); break;
            case InputCommandType.MOVE: TryProcess_Move(newInput); break;
            case InputCommandType.DASH: TryProcess_Dash(newInput); break;
        }
    }

    private void TryProcess_Move(InputCommand newInput) { }
    private void TryProcess_Skill(InputCommand newInput) { skill?.UseSkill($"SLOT{newInput.SkillSlotIndex + 1}"); }
    private void TryProcess_Dash(InputCommand newInput) { dash?.TryDash(); }

    private void TryProcess_Action(InputCommand newInput)
    {
        if (bCanComboInput == false) return;

        // ================================================================
        // 💡 [핵심 버그 픽스]: 공격 버튼을 누른 순간, 조이스틱이나 방향키를 밀고 있다면 
        // 즉시 콤보를 끊고 1타부터 나가게 만듭니다!
        if (movement != null && movement.TargetDirection.magnitude > 0.05f)
        {
#if UNITY_EDITOR
            if (bDebug) Debug.Log("이동 중 공격 입력! 콤보를 1타로 초기화합니다.");
#endif
            ResetCombo();
        }
        // ================================================================

        ComboData comboData = currComboObj.GetComboData(comboIndex);
        float currentTime = newInput.TimeStamp;

        bool isResetTimeExceeded = currentTime - lastInputTime >= comboData.ComboResetTime;
        bool isWithinLastInputTime = (currentTime - lastInputTime) <= comboData.LastInputCheckTime;
        bool isBuffered = (currentTime - lastInputTime) <= comboData.InputBufferTime;

        lastInputTime = Time.time;

        if (inputQueue.Count > 0 && isResetTimeExceeded && (isWithinLastInputTime || isBuffered) == false)
        {
            ResetCombo();
        }

        bool isFirstInput = lastInputTime < 0 || comboIndex == 0;

        comboInputHandler?.HandleInputEnabled(isFirstInput | isWithinLastInputTime);
        comboInputHandler?.HandleInputBuffered(isBuffered);
        comboInputHandler?.HandleInputEnableTime(comboData.LastInputCheckTime);
        comboInputHandler?.HandleInputBufferTime(comboData.InputBufferTime);

        if (isFirstInput || isWithinLastInputTime || isBuffered)
        {
            // 💡 [버그 픽스 2]: 유저가 연타해서 큐가 수십 개씩 쌓이는 걸 막기 위해 항상 최신 1개만 유지합니다.
            inputQueue.Clear();
            inputQueue.Enqueue(newInput);

            // 캐릭터가 행동 가능한 상태면 즉시 실행! (애니메이션 재생 중이면 여기를 스킵하고 큐에만 보관됨)
            if (CanExecuteNextAction())
            {
                inputQueue.Dequeue(); // 실행할 거니까 큐에서 뺌
                ExecuteAttack(comboIndex);
                comboIndex++;
            }
        }
    }

    private void ExecuteAttack(int index)
    {
        if (currComboObj == null) return;

        ComboData data = currComboObj.GetComboData(index);
        comboInputHandler?.HandleComboIndex(index);
        comboResetTime = data.ComboResetTime;

        CancelComboTimer(); // 공격이 시작되었으므로 콤보 대기시간 캔슬
        weapon?.DoActionWithIndex(index);
    }

    // ComboComponent.cs 수정
    public void OnEndDoAction()
    {
        CancelComboTimer();

        // 💡 [수정] 방금 끝난 동작이 마지막 콤보 타수였다면?
        bool isLastCombo = (currComboObj != null && comboIndex >= currComboObj.MaxComboIndex());

        if (isLastCombo)
        {
            // 마지막 공격이 끝났으니, 사용자가 타다닥 눌러놨던 선입력을 무시(삭제)합니다.
            inputQueue.Clear();
            bCanComboInput = false;
        }
        else
        {
            // 1. 유저가 미리 눌러둔 평타(선입력)가 있다면 즉시 다음 콤보로 이어나감!
            if (inputQueue.Count > 0 && bCanComboInput)
            {
                var nextInput = inputQueue.Dequeue();
                if (nextInput.InputType == InputCommandType.ACTION)
                {
                    ExecuteAttack(comboIndex);
                    comboIndex++;
                    return; // 다음 공격을 시작했으므로 타이머를 돌리지 않고 종료
                }
            }
        }

        comboCts = new CancellationTokenSource();
        Rest_ComboTime(comboCts.Token).Forget();
    }

    private async UniTaskVoid Rest_ComboTime(CancellationToken token)
    {
        try
        {
            float currentResetTime = comboResetTime;
            while (currentResetTime > 0)
            {
                currentResetTime -= Time.deltaTime;
                comboInputHandler?.HandleInputResetTime(currentResetTime, comboResetTime);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
            }

            ResetCombo();
        }
        catch (OperationCanceledException) { /* 타이머 캔슬 시 정상 종료 */ }
    }

    private void ResetCombo()
    {
        CancelComboTimer();
        comboIndex = 0;

        var data = currComboObj?.GetComboData(0);
        if (data != null) comboResetTime = data.ComboResetTime;

        lastInputTime = Time.time;
        bCanComboInput = true;

        inputQueue.Clear();
        comboInputHandler?.HadleInputReset();
    }

    private void CancelComboTimer()
    {
        if (comboCts != null)
        {
            comboCts.Cancel();
            comboCts.Dispose();
            comboCts = null;
        }
    }
}