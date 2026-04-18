using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MovableStopper : MonoBehaviour
{
    private static MovableStopper instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 💡 싱글톤 중복 생성 방지
            Destroy(gameObject);
        }
    }

    public static MovableStopper Instance { get => instance; }

    // /////////////////////////////////////////////////////////////////////////

    private List<IStoppable> stoppers = new List<IStoppable>();

    // 💡 1. 전체 Hit-Stop(역경직)을 통제할 중앙 취소 토큰!
    private CancellationTokenSource delayCts;

    public void Regist(IStoppable stopper)
    {
        // 💡 2. 추가 등록방지!
        stoppers.Unique(stopper);
    }

    public void Delete(IStoppable stopper)
    {
        stoppers.Remove(stopper);
    }

    public void Start_Delay(int frame)
    {
        if (frame < 1)
            return;

        // 💡 3. (주석 해결) 딜레이 중에 딜레이가 오면 기존 것을 취소!
        // 코루틴을 일일이 끌 필요 없이 토큰 하나만 캔슬하면 전원이 멈춥니다.
        if (delayCts != null)
        {
            delayCts.Cancel();
            delayCts.Dispose();
        }

        // 새로운 지시(토큰) 발급
        delayCts = new CancellationTokenSource();

        // 💡 4. 파괴된(Null) 오브젝트가 리스트에 남아있어 에러를 뿜는 것을 방지
        stoppers.RemoveAll(s => s == null || (s is MonoBehaviour mb && mb == null));

        // 모든 대상에게 새로운 역경직 명령 하달
        foreach (var stopper in stoppers)
        {
            // StartCoroutine 대신, 토큰을 넘겨주고 .Forget() 으로 던져버립니다!
            stopper.Start_FrameDelay(frame, delayCts.Token).Forget();
        }
    }

    // 💡 5. 씬이 넘어가거나 파괴될 때 메모리 누수 방지
    private void OnDestroy()
    {
        if (delayCts != null)
        {
            delayCts.Cancel();
            delayCts.Dispose();
            delayCts = null;
        }
    }
}