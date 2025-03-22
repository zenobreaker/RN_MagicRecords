using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.AI;

[System.Serializable]
public class Sound
{
    public string soundName;
    public AudioClip clip;
}

public class SoundManager : MonoBehaviour
{
    private  static SoundManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != null)
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);

        Awake_InitSFXTable();
    }

    public static SoundManager Instance { get { return instance; } }

    
    ///////////////////////////////////////////////////////////////////////////

    [Header("사운드 등록")]
    [SerializeField] Sound[] bgmSounds = null;
    [SerializeField] Sound[] sfxSounds = null;

    [Header("BGM 플레이어")]
    public AudioSource bgmPlayer = null;

    [Header("효과음 플레이어")]
    public AudioSource[] sfxPlayers = null;

    private Dictionary<string, AudioClip> sfxSoundTable = new Dictionary<string, AudioClip>();

    private void Awake_InitSFXTable()
    {
        foreach(Sound sound in sfxSounds )
        {
            sfxSoundTable.Add(sound.soundName, sound.clip);
        }
    }

    private void Start()
    {
        PlayRandomBGM();
    }

    private void Update()
    {
        if (!bgmPlayer.isPlaying && bgmSounds.Length > 0)
        {
            int random = Random.Range(0, bgmSounds.Length - 1);

            bgmPlayer.clip = bgmSounds[random].clip;
            bgmPlayer.Play();
        }
    }

    public void PlayRandomBGM()
    {
        if (bgmSounds.Length <= 0)
            return; 

        int random = Random.Range(0, bgmSounds.Length - 1); // 정수타입은 MAX값 미포함 실수 타입은 MAX값 포함
        bgmPlayer.clip = bgmSounds[random].clip;
        bgmPlayer.Play();
    }
    public void PlaySFX(string soundName)
    {
        if (string.IsNullOrEmpty(soundName))
            return;

        if (sfxSoundTable == null)
            return;


        if (sfxSoundTable.TryGetValue(soundName, out AudioClip clip))
        {
            AudioSource audioSource = GetNotPlayingAudioSource();
            if (audioSource == null)
            {
                Debug.Log($"Auido Source All Playing");
                FindExistingAudioSource(clip)?.Play();

                return;
            }
            audioSource.clip = clip;
            audioSource.Play();

            return;
        }

        Debug.Log($"Not Registered Sound");
    }

    private AudioSource GetNotPlayingAudioSource()
    {
        foreach(AudioSource audioSource in sfxPlayers)
        {
            if (audioSource.isPlaying)
                continue;

            return audioSource;
        }

        return null; 
    }

    private AudioSource FindExistingAudioSource(AudioClip clip)
    {
        foreach (AudioSource source in sfxPlayers)
        {
            if(/*source.isPlaying && */source.clip == clip)
            {
                return source;
            }
        }

        return null;
    }

    private void Old_Code(string _soundName)
    {
        // 효과음 플레이어에 여러개의 오디오소를 넣어주면 연속적인 재생이 가능해진다. (개수가 적을 수록 소리 딜레이가 크다)
        for (int i = 0; i < sfxSounds.Length; i++)
        {
            if (_soundName == sfxSounds[i].soundName)
            {
                for (int x = 0; x < sfxPlayers.Length; x++)
                {
                    if (!sfxPlayers[x].isPlaying)
                    {
                        sfxPlayers[x].clip = sfxSounds[i].clip;
                        sfxPlayers[x].Play();
                        //Debug.Log("플레이함" + _soundName);
                        return;
                    }
                }
                Debug.Log("모든 효과음 플레이어가 사용 중입니다!");
                return;
            }
        }
        Debug.Log("등록된 효과음이 없습니다.");
    }
}
