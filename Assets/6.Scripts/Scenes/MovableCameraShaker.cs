using Unity.Cinemachine;
using UnityEngine;

public class MovableCameraShaker : MonoBehaviour
{
    private static MovableCameraShaker instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;

        DontDestroyOnLoad(gameObject);
    }

    public static MovableCameraShaker Instance { get => instance;  }

    private CinemachineImpulseSource impulse;
    private CinemachineImpulseListener listener;
    private CinemachineBrain brain;

    private void Start()
    {
        brain = Camera.main.GetComponent<CinemachineBrain>();
        impulse = GetComponent<CinemachineImpulseSource>();
        if (brain != null)
        {
            listener = brain.GetComponent<CinemachineImpulseListener>();
        }
    }

    public virtual void Play_Impulse(ActionData data)
    {
        if (impulse == null || data == null)
            return;

        Play_Impulse(data.settings);
    }

    public void Play_Impulse(NoiseSettings settings)
    {
        if (settings == null) return;

#if UNITY_EDITOR
        Debug.Log("Shake!");
#endif
        listener.ReactionSettings.m_SecondaryNoise = settings;
        impulse.GenerateImpulse();
    }
}
