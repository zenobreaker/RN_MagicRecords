using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio; // 💡 [추가] AudioMixer를 사용하기 위한 네임스페이스

[System.Serializable]
public class Sound
{
    public string soundName;
    public AudioClip clip;
}

public class SoundManager : MonoBehaviour
{
    private static SoundManager instance;

    public static SoundManager Instance { get { return instance; } }

    [Header("사운드 등록")]
    [SerializeField] Sound[] bgmSounds = null;
    [SerializeField] Sound[] sfxSounds = null;

    [Header("오디오 믹서 (Audio Mixer)")]
    [Tooltip("최상위 Audio Mixer 에셋을 연결하세요.")]
    public AudioMixer masterMixer;
    [Tooltip("BGM용 Audio Mixer Group을 연결하세요.")]
    public AudioMixerGroup bgmGroup;
    [Tooltip("SFX용 Audio Mixer Group을 연결하세요.")]
    public AudioMixerGroup sfxGroup;

    [Header("플레이어 할당")]
    public AudioSource bgmPlayer = null;
    public AudioSource[] sfxPlayers = null;

    private Dictionary<string, AudioClip> bgmSoundTable = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> sfxSoundTable = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        Awake_InitSFXTable();
        Awake_InitBGMTable();
        Awake_InitMixerGroups(); // 💡 믹서 그룹 자동 할당
    }

    private void Awake_InitSFXTable()
    {
        foreach (Sound sound in sfxSounds)
        {
            if (!sfxSoundTable.ContainsKey(sound.soundName))
            {
                sfxSoundTable.Add(sound.soundName, sound.clip);
            }
        }
    }

    private void Awake_InitBGMTable()
    {
        foreach (Sound sound in bgmSounds)
        {
            if (!bgmSoundTable.ContainsKey(sound.soundName))
            {
                bgmSoundTable.Add(sound.soundName, sound.clip);
            }
        }
    }

    // 💡 [추가] 인스펙터에서 믹서 그룹을 넣었으면, 오디오 소스들에 자동으로 연결해줍니다.
    private void Awake_InitMixerGroups()
    {
        if (bgmPlayer != null && bgmGroup != null)
        {
            bgmPlayer.outputAudioMixerGroup = bgmGroup;
        }

        if (sfxPlayers != null && sfxGroup != null)
        {
            foreach (AudioSource source in sfxPlayers)
            {
                source.outputAudioMixerGroup = sfxGroup;
            }
        }
    }

    public void PlayRandomBGM()
    {
        if (bgmSounds.Length <= 0) return;

        // 💡 [수정] Random.Range(int min, int max)는 max를 포함하지 않습니다!
        // bgmSounds.Length - 1 을 넣으면 마지막 요소가 절대 나오지 않으므로 Length를 그대로 넣어야 합니다.
        int random = Random.Range(0, bgmSounds.Length);

        bgmPlayer.clip = bgmSounds[random].clip;
        bgmPlayer.Play();
    }

    public void PlayBGM(string soundName)
    {
        if (string.IsNullOrEmpty(soundName) || bgmSoundTable == null) return;

        if(bgmSoundTable.ContainsKey(soundName))
        {
            bgmPlayer.clip = bgmSoundTable[soundName];
            bgmPlayer.Play(); 
        }
    }

    public void PlaySFX(string soundName)
    {
        if (string.IsNullOrEmpty(soundName) || sfxSoundTable == null) return;

        if (sfxSoundTable.TryGetValue(soundName, out AudioClip clip))
        {
            AudioSource audioSource = GetNotPlayingAudioSource();
            if (audioSource == null)
            {
                // 빈 플레이어가 없다면, 같은 소리를 내고 있는 녀석을 찾아서 처음부터 다시 재생시킵니다.
                FindExistingAudioSource(clip)?.Play();
                return;
            }

            audioSource.clip = clip;
            audioSource.Play();
            return;
        }

        Debug.LogWarning($"[SoundManager] 등록되지 않은 효과음입니다: {soundName}");
    }

    private AudioSource GetNotPlayingAudioSource()
    {
        foreach (AudioSource audioSource in sfxPlayers)
        {
            if (!audioSource.isPlaying)
                return audioSource;
        }
        return null;
    }

    private AudioSource FindExistingAudioSource(AudioClip clip)
    {
        foreach (AudioSource source in sfxPlayers)
        {
            if (source.clip == clip)
            {
                return source;
            }
        }
        return null;
    }

    // =========================================================================
    // 💡 [추가] UI 슬라이더에서 호출할 볼륨 조절 함수들 (0.0 ~ 1.0 값을 받음)
    // =========================================================================

    public void SetMasterVolume(float sliderValue)
    {
        if (masterMixer == null) return;

        // 💡 [핵심] 들어온 값(0~10)을 최대값(10)으로 나누어 0.0 ~ 1.0 비율(%)로 만듭니다.
        // 만약 슬라이더의 Max Value를 100으로 쓰신다면 100f로 나누시면 됩니다!
        float normalizedValue = sliderValue / 10f;

        if (normalizedValue <= 0.001f)
        {
            masterMixer.SetFloat("Master", -80f);
        }
        else
        {
            // 이제 0.0 ~ 1.0 사이의 값이 들어가므로 부드럽게 깎입니다!
            Debug.Log($"Sound matster : {normalizedValue}");
            masterMixer.SetFloat("Master", Mathf.Lerp(-40f, 0f, normalizedValue));
        }
    }

    public void SetBGMVolume(float sliderValue)
    {
        if (masterMixer == null) return;

        float normalizedValue = sliderValue / 10f;

        if (normalizedValue <= 0.001f)
            masterMixer.SetFloat("BGM", -80f);
        else
            masterMixer.SetFloat("BGM", Mathf.Lerp(-40f, 0f, normalizedValue));
    }

    public void SetSFXVolume(float sliderValue)
    {
        if (masterMixer == null) return;

        float normalizedValue = sliderValue / 10f;

        if (normalizedValue <= 0.001f)
            masterMixer.SetFloat("SFX", -80f);
        else
            masterMixer.SetFloat("SFX", Mathf.Lerp(-40f, 0f, normalizedValue));
    }
}