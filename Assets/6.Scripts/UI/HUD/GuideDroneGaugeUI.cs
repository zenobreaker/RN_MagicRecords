using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>충전 중에는 노란 게이지를 표시하고 드론 활성화 중에는 표시 영역만 숨깁니다.</summary>
public sealed class GuideDroneGaugeUI : MonoBehaviour
{
    [SerializeField] private SO_HUDHandler handler;
    [SerializeField] private GameObject displayRoot;
    [SerializeField] private Image chargeFill;
    [SerializeField] private Image droneIcon;
    [SerializeField] private Button activationButton;
    [SerializeField] private TextMeshProUGUI chargeText;
    [SerializeField] private Color readyColor = Color.white;
    [SerializeField] private Color chargingColor = Color.gray;
    private GuideDrone currentDrone;

    private void OnEnable()
    {
        if (handler == null)
        {
            handler = Resources.Load<SO_HUDHandler>("SO_HUDHandler");
        }
        if (handler != null)
        {
            handler.OnGuideDroneChanged += Refresh;
            Refresh(handler.CurrentGuideDrone);
        }
        else
        {
            Refresh(null);
        }
        activationButton.SafeInvoke(button => button.onClick.AddListener(OnActivate));
    }

    private void OnDisable()
    {
        if (handler != null)
        {
            handler.OnGuideDroneChanged -= Refresh;
        }
        activationButton.SafeInvoke(button => button.onClick.RemoveListener(OnActivate));
        currentDrone = null;
    }

    /// <summary>프리팹에 연결된 표시 요소만 갱신하며 UI 구조를 생성하지 않습니다.</summary>
    private void Refresh(GuideDrone drone)
    {
        currentDrone = drone;
        bool available = drone != null && drone.IsInitialized;
        displayRoot.SafeInvoke(root => root.SetActive(available && drone.IsActive == false));
        chargeFill.SafeInvoke(fill => fill.fillAmount = available ? drone.ChargeRatio : 0f);
        droneIcon.SafeInvoke(icon =>
        {
            icon.sprite = available ? drone.Data.icon : null;
            icon.enabled = icon.sprite != null;
            icon.color = available && drone.IsFullyCharged ? readyColor : chargingColor;
        });
        activationButton.SafeInvoke(button => button.interactable = available && drone.IsFullyCharged && drone.IsActive == false);
        chargeText.SafeInvoke(text => text.text = available ? $"{Mathf.FloorToInt(drone.ChargeRatio * 100f)}%" : string.Empty);
    }

    private void OnActivate()
    {
        if (CheatConsoleUI.CapturesInput == false && TutorialSequence.CapturesInput == false)
        {
            currentDrone.SafeInvoke(drone => drone.TryActivate());
        }
    }
}
