using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class ComboComponent : MonoBehaviour
{
    class InputElement
    {
        public string InputType; // �Է� Ÿ��
        public float TimeStamp;  // �Է��� �߻��� �ð�
        public int comboCount;
    }

    public bool bDebug;

    //[Header("Timer Settings")]
    private float comboCheckTime = 0.5f;         // ���� �� ���� ������ �ִ��� Ȯ���� �ð�
    private float lastInputCheckTime = 0.25f;    // ���� �޺��� �Է��� �ٶ�� ���� �ð�
    private float comboMaintainTime = 1.0f;        // �޺�(�Է� ť) ���� �ð� 
    private float curr_MaintainTime;

    private float lastInputTime = 0.0f;             // �������� �Է��� �޺� �Է� �ð�  
    private float lastComboEnd = 0.0f;              // ������ ���� ���� �ð�
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

        // �ð� �ʱ�ȭ 
        {
            ResetTimerValue(data);
        }

        // Action ����
        {
            if (bDebug)
            {
                Debug.Log($"Execute Combodata {data.ComboIndex} / {data.StateName} / Time stamp {inputElement.TimeStamp}");
            }
            weapon.DoAction(data.ComboIndex);

            if (comboMaintainCoroutine == null)
                comboMaintainCoroutine = StartCoroutine(ComboMaintainCoroutine());
        }

        //UI ó�� 
        {
            //  comboMaintainTimeGauge?.SetMaxValue(data.comboMaintainTime);
        }
    }



    public void InputCombo(KeyCode keycode)
    {
        // ������ �޺��� ���� �� �ش� �ð��� ����ߴ��� Ȯ��
        if (Time.time - lastComboEnd >= comboCheckTime)
        {
            // �޺� Ÿ�̸� üũ�� �ߴ�
            if (comboMaintainCoroutine != null)
            {
                StopCoroutine(comboMaintainCoroutine);
                comboMaintainCoroutine = null;
            }

            // ������ �Է� �� �ش� �ð� ��ŭ �������� 
            if (Time.time - lastInputTime >= lastInputCheckTime)
            {
                // ���� �޺� ���� 
                float currentTime = Time.time;
                var inputElement = new InputElement
                {
                    InputType = keycode.ToString(),
                    TimeStamp = currentTime,
                    comboCount = this.comboCount
                };

                ExecuteAttack(ref inputElement);
                comboCount++;

                lastInputTime = Time.time; // �� �ֽ�ȭ 
            }
        }
    }


    // �Է� ���� �ð� �ȿ� �Է� �޾Ҵ��� �˻��Ѵ�. 
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
        // �����ߴ� �ִϸ��̼��� ������ �̰��� ȣ���ϰ� �Ǹ� �����ڸ� ȣ���Ѵ�. 
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
