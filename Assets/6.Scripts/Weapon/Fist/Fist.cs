using Unity.VisualScripting;
using UnityEngine;



public class Fist : Weapon_Combo
{
    private enum PartType
    {
        LeftHand, RightHand, LeftFoot, RightFoot, Max
    }

    bool isPlayer = true;

    protected override void Awake()
    {
        base.Awake();

        Debug.Assert(colliders.Length > 0);

        for (int i = 0; i < (int)PartType.Max; i++)
        {
            if (i >= colliders.Length) continue; 

            Transform t = colliders[i].transform;
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity; 

            if(t.TryGetComponent<Fist_Trigger>(out Fist_Trigger trigger))
            {
                trigger.OnTrigger += OnTriggerEnter;

                string partName = ((PartType)i).ToString();
                Transform parent = rootObject.transform.FindChildByName(partName);
                if(parent != null)
                    t.SetParent(parent, false);
            }
        }
    }
    protected override void Start()
    {
        base.Start();

        if (rootObject.TryGetComponent<Enemy>(out Enemy enemy))
        {
            isPlayer = false; 
        }
    }

    public override void Begin_Equip()
    {
        base.Begin_Equip();

        if (rootObject == null)
            return;



        End_Equip(); 
    }

    public override void Begin_DoAction()
    {
        base.Begin_DoAction();
        
        hitList.Clear();
    }

    public override void End_DoAction()
    {
        base.End_DoAction();

        hitList.Clear();
    }

    public override void Begin_JudgeAttack(AnimationEvent e)
    {
        base.Begin_JudgeAttack(e);

        string estring = e.stringParameter;
        if (estring != "")
        {
            string[] strings = estring.Split(',');
            foreach (string s in strings)
            {
                if (int.TryParse(s, out int result))
                    colliders[result].enabled = true;
            }
            return;
        }

        colliders[e.intParameter].enabled = true;
    }

    public override void End_JudgeAttack(AnimationEvent e)
    {
        base.End_JudgeAttack(e);

        colliders[e.intParameter].enabled = false;
    }

    public override void Play_PlaySound()
    {
        base.Play_PlaySound();

        actionDatas[index].Play_Sound();
    }

    public override void Play_CameraShake()
    {
        base.Play_CameraShake();

        if (isPlayer)
            actionDatas[index].Play_CameraShake();
    }
}
