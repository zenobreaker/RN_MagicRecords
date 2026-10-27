using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;


public enum SoundCategory
{
    MASTER,
    BGM,
    SFX,
}


public class SoundSliderGroup : MonoBehaviour
{
    [SerializeField] private SoundCategory category;

    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private Button audioButton;
    [SerializeField] private Image audioButtonImage;
    [SerializeField] private Slider audioValueSlider;
    [SerializeField] private AudioMixer audioMixer;

    [Header("Sprite")]
    [SerializeField] private Sprite muteImage;
    [SerializeField] private Sprite unmuteImage;

    private bool isMute = false;
    private float volume = 0.0f;

    public float CurrentVolume
    {
        get
        {
            return this.volume;
        }
    }
    public bool CurrentMute
    {
        get
        {
            return this.isMute;
        }
    }

    private void Awake()
    {
        Debug.Assert(audioValueSlider != null);

        audioValueSlider.onValueChanged.AddListener(SetVolume);

        if (audioButton != null)
        {
            audioButton.onClick.AddListener(OnMuteButton);
        }
    }

    private void SetVolume(float sliderVolume)
    {
        volume = sliderVolume / 10.0f;

        if (this.isMute && sliderVolume > 0)
        {
            this.isMute = false;
        }
        // 슬라이더를 끝까지 내리면 자동으로 음소거 상태로 만듭니다
        else if (sliderVolume == 0)
        {
            this.isMute = true;
        }

        ApplyVolumeToMixer();
        DrawUI();
    }

    public void SetInitialize(SoundCategory category, bool mute, float volume)
    {
        this.category = category;
        isMute = mute;
        this.volume = volume;
        DrawUI();
    }

    private void OnMuteButton()
    {
        isMute = (isMute == false);

        if (isMute == false && this.volume <= 0.001f)
        {
            this.volume = 0.5f;
        }

        ApplyVolumeToMixer();
        DrawUI();
    }

    private void ApplyVolumeToMixer()
    {
        if (SoundManager.Instance == null)
        {
            return;
        }

        // 음소거 상태면 무조건 0 전달, 아니면 현재 들고 있는 볼륨(0~10 스케일) 전달
        float targetValue = isMute ? 0f : (this.volume);

        switch (category)
        {
            case SoundCategory.MASTER:
                SoundManager.Instance.SetMasterVolume(targetValue);
                break;
            case SoundCategory.BGM:
                SoundManager.Instance.SetBGMVolume(targetValue);
                break;
            case SoundCategory.SFX:
                SoundManager.Instance.SetSFXVolume(targetValue);
                break;
        }
    }

    private void DrawUI()
    {
        int stepValue = Mathf.RoundToInt(this.volume * 10.0f);
        DrawNameText();

        if (audioValueSlider != null)
        {
            audioValueSlider.SetValueWithoutNotify(stepValue);
        }

        if (audioButtonImage != null)
        {
            if (isMute)
            {
                audioButtonImage.sprite = muteImage;
            }
            else
            {
                audioButtonImage.sprite = unmuteImage;
            }
        }

        if (valueText != null)
        {
            valueText.text = (volume * 10.0f).ToString("0");
        }
    }

    private void DrawNameText()
    {
        if (nameText == null)
        {
            return;
        }

        switch (category)
        {
            case SoundCategory.MASTER:
                nameText.text = LocalizationManager.Instance.GetText("ui_sound_master");
                break;
            case SoundCategory.BGM:
                nameText.text = LocalizationManager.Instance.GetText("ui_sound_bgm");
                break;
            case SoundCategory.SFX:
                nameText.text = LocalizationManager.Instance.GetText("ui_sound_sfx");
                break;
        }
    }

}
