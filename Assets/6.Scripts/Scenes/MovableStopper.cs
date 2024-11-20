using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovableStopper : MonoBehaviour
{
    private static MovableStopper instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;

        DontDestroyOnLoad(gameObject);
    }

    public static MovableStopper Instance { get => instance; }

    ///////////////////////////////////////////////////////////////////////////

    private List<IStoppable> stoppers = new List<IStoppable>();

    public void Regist(IStoppable stopper)
    {
        // 추가 등록해버리나?
        stoppers.Add(stopper);
    }

    public void Delete(IStoppable stopper)
    {
        stoppers.Remove(stopper);
    }

    public void Start_Delay(int frame)
    {
        if (frame < 1)
            return;

        stoppers.ForEach(stopper =>
        {
            // 딜레이 중에 딜레이 오는 것도 잇으니 취소 하고 다시 콜해야한다.
            //stopper.gameObject 에서 스탑 코루틴하던가...

            StartCoroutine(stopper.Start_FrameDelay(frame));
        });
    }

}
