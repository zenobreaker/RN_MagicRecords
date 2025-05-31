using UnityEngine;

public partial class ComboComponent: MonoBehaviour
{
    private void TryProcess_Action(InputCommand newInput)
    {
        if (bCanComboInput == false) return; 

        ComboData comboData = currComboObj.GetComboData(comboIndex);
        float currentTime = newInput.TimeStamp;

        bool isResetTimeExceeded = currentTime - lastInputTime >= comboData.ComboResetTime;
        bool isWithinLastInputTime = (currentTime - lastInputTime) <= comboData.LastInputCheckTime;
        bool isBuffered = (currentTime - lastInputTime) <= comboData.InputBufferTime;

        lastInputTime = Time.time;

        if (inputQueue.Count > 0 && isResetTimeExceeded && (isWithinLastInputTime || isBuffered) == false)
        {
#if UNITY_EDITOR
            if (bDebug)
                Debug.Log($"Invalid Time Reset:{currentTime} /  {currentTime - lastInputTime}");
#endif
            ResetCombo();
        }

        bool isFirstInput = lastInputTime < 0 || comboIndex == 0;

        comboInputHandler?.HandleInputEnabled(isFirstInput | isWithinLastInputTime);
        comboInputHandler?.HandleInputBuffered(isBuffered);
        comboInputHandler?.HandleInputEnableTime(comboData.LastInputCheckTime);
        comboInputHandler?.HandleInputBufferTime(comboData.InputBufferTime);

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
        comboInputHandler?.HandleComboIndex(index);
        comboResetTime = data.ComboResetTime;

        // Action ½ÇÇà
#if UNITY_EDITOR
        if (bDebug)
            Debug.Log($"Execute Combodata {index}");
#endif
        weapon.DoAction(index);
    }
}
