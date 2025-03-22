using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioMixerUI : UiBase
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider slider_Master;
    [SerializeField] private Slider slider_BGM;
    [SerializeField] private Slider slider_SFX;


    private float masterVolume;
    private float bgmVolume;
    private float sfxVolume;

    private void Awake()
    {
        slider_Master.onValueChanged.AddListener(SetMaterVolume);
        slider_BGM.onValueChanged.AddListener(SetBGMVolume);
        slider_SFX.onValueChanged.AddListener(SetSFXVolume);

        UIOpend += OnUIOpend;
        UIClosed += OnUIClosed; 
    }

    private void Start ()
    {
        foreach (var group in audioMixer.FindMatchingGroups("Master"))
            Debug.Log($"{group.name}");
    }

    private void OnUIOpend ()
    {
        slider_Master.value  = PlayerPrefs.GetFloat("Master_Volume");
        slider_BGM.value  = PlayerPrefs.GetFloat("BGM_Volume");
        slider_SFX.value  = PlayerPrefs.GetFloat("SFX_Volume");
    }

    private void OnUIClosed()
    {
        PlayerPrefs.SetFloat("Master_Volume", masterVolume);
        PlayerPrefs.SetFloat("BGM_Volume", bgmVolume);
        PlayerPrefs.SetFloat("SFX_Volume", sfxVolume);
    }

    private void SetMaterVolume(float volume)
    {
        //audioMixer.FindMatchingGroups("Master")[0].audioMixer.SetFloat("Master",MathF.Log10(volume) * 20);
        //audioMixer?.SetFloat("Master", MathF.Log10(volume) * 20);
        masterVolume = volume;
    }

    private void SetBGMVolume(float volume)
    {
        audioMixer?.SetFloat("BGM", MathF.Log10(volume) * 20);
        bgmVolume = volume;
    }

    private void SetSFXVolume(float volume)
    {
        audioMixer?.SetFloat("SFX", MathF.Log10(volume) * 20);
        sfxVolume = volume;
    }

}
