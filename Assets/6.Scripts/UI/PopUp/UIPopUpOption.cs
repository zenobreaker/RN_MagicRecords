using UnityEngine;

public class UIPopUpOption
    : UIPopUp
{
    [SerializeField] private SoundSliderGroup masterGroup;
    [SerializeField] private SoundSliderGroup bgmGroup;
    [SerializeField] private SoundSliderGroup sfxGroup;

    protected override void OnEnable()
    {
        base.OnEnable();

        //slider_BGM.value = PlayerPrefs.GetFloat("BGM_Volume");
        //slider_SFX.value = PlayerPrefs.GetFloat("SFX_Volume");

        // 1. 값 불러오기 (저장된 값이 없으면 쓸 기본값 지정: 볼륨 0.5, 음소거 해제)
        float loadedMasterVolume = PlayerPrefs.GetFloat("Master_Volume", 0.5f);
        bool loadedMasterMute = PlayerPrefs.GetInt("Master_Mute", 0) == 1; // 1이면 true, 0이면 false

        float loadedBGMVolume = PlayerPrefs.GetFloat("BGM_Volume", 0.5f);
        bool loadedBGMMute = PlayerPrefs.GetInt("BGM_Mute", 0) == 1;

        float loadedSFXVolume = PlayerPrefs.GetFloat("SFX_Volume", 0.5f);
        bool loadedSFXMute = PlayerPrefs.GetInt("SFX_Mute", 0) == 1;


        // 2. 불러온 값을 UI에 주입 (out 키워드 제거)
        if (masterGroup != null)
            masterGroup.SetInitialize(SoundCategory.MASTER, loadedMasterMute, loadedMasterVolume);

        if (bgmGroup != null)
            bgmGroup.SetInitialize(SoundCategory.BGM, loadedBGMMute, loadedBGMVolume);

        if (sfxGroup != null)
            sfxGroup.SetInitialize(SoundCategory.SFX, loadedSFXMute, loadedSFXVolume);


        ShowPopUp();
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        // 3. UI가 꺼질 때, SoundSliderGroup이 가지고 있는 '최신 값'을 달라고 해서 저장!
        if (masterGroup != null)
        {
            PlayerPrefs.SetFloat("Master_Volume", masterGroup.CurrentVolume);
            // bool(True/False)을 Int(1/0)로 변환해서 저장
            PlayerPrefs.SetInt("Master_Mute", masterGroup.CurrentMute ? 1 : 0);

        }

        if (bgmGroup != null)
        {
            PlayerPrefs.SetInt("BGM_Mute", bgmGroup.CurrentMute ? 1 : 0);
            PlayerPrefs.SetFloat("BGM_Volume", bgmGroup.CurrentVolume);
        }

        if (sfxGroup != null)
        {
            PlayerPrefs.SetFloat("SFX_Volume", sfxGroup.CurrentVolume);
            PlayerPrefs.SetInt("SFX_Mute", sfxGroup.CurrentMute ? 1 : 0);
        }
            PlayerPrefs.Save(); // 💡 디스크에 즉시 저장 확정 (안전장치)
    }

    protected override void DrawPopUp()
    {

    }
}
