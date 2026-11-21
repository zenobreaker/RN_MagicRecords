using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>로비 드론 정보와 탐사 준비의 메모리 드론 선택을 표시합니다.</summary>
public sealed class UIGuideDronePopup : SelectBaseUI, IExplorationSetupPage
{
    [SerializeField] private SO_GuideDroneData droneData;
    [SerializeField] private Sprite profileImage;
    [SerializeField] private Image droneImage;
    [SerializeField] private LocalizeText[] localizedTexts;
    [SerializeField] private ScrollRect descriptionScroll;

    [Header("드론 목록")]
    [SerializeField] private List<SO_GuideDroneData> droneDefinitions = new();
    [SerializeField] private RectTransform droneListContent;
    [SerializeField] private UIGuideDroneSelectButton droneButtonPrefab;
    [SerializeField] private ScrollRect droneListScroll;

    [Header("선택한 드론 정보")]
    [SerializeField] private LocalizeText droneNameText;
    [SerializeField] private LocalizeText profileText;
    [SerializeField] private LocalizeText passiveText;
    [SerializeField] private LocalizeText activeText;

    private readonly List<SO_GuideDroneData> availableDrones = new();
    private SO_GuideDroneData selectedDrone;
    private ExplorationSetupData setupContext;
    private ExploreManager setupManager;

    /// <summary>정보창을 다시 열면 목록의 첫 드론을 표시합니다.</summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        selectedDrone = null;
        setupContext = null;
        setupManager = null;
    }

    /// <summary>이번 탐사 선택, 이전 선택, 목록 첫 항목 순으로 선택을 복원합니다.</summary>
    public void OnShowPage(ExplorationSetupData setupData)
    {
        setupContext = setupData;
        setupManager = AppManager.Instance.SafeInvoke(app => app.GetExploreManager());
        string preferredId = setupData?.SelectedMemoryDroneId;
        if (string.IsNullOrEmpty(preferredId))
        {
            preferredId = PlayerManager.Instance.SafeInvoke(players => players.Data.lastSelectedMemoryDroneId);
        }

        selectedDrone = setupManager.SafeInvoke(explore => explore.FindMemoryDrone(preferredId));
        RefreshUI();
        StoreSetupSelection();
    }

    /// <summary>선택한 드론이 현재 탐사 목록에 있을 때만 탐사를 시작할 수 있습니다.</summary>
    public bool IsReadyToProceed()
    {
        return setupContext != null && selectedDrone != null &&
            availableDrones.Contains(selectedDrone) &&
            setupContext.SelectedMemoryDroneId == selectedDrone.droneId;
    }

    /// <summary>탐사에 포함된 화면은 준비창 전체를 닫고 로비에서는 기존 닫기를 사용합니다.</summary>
    public override void CloseUI()
    {
        if (setupContext != null)
        {
            GetComponentInParent<UIExplorationSetup>().SafeInvoke(setup => setup.CloseUI());
            return;
        }

        base.CloseUI();
    }

    /// <summary>기존 팝업 표시 절차를 통해 이미지와 설명을 갱신합니다.</summary>
    public override void RefreshUI()
    {
        ShowPopUp();
        Canvas.ForceUpdateCanvases();
        descriptionScroll.SafeInvoke(scroll => scroll.verticalNormalizedPosition = 1f);
        droneListScroll.SafeInvoke(scroll =>
        {
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
        });
    }

    /// <summary>등록된 드론 아이콘과 선택한 드론의 이미지 및 설명을 표시합니다.</summary>
    protected override void DrawPopUp()
    {
        CollectDrones();
        if (availableDrones.Contains(selectedDrone) == false)
        {
            selectedDrone = availableDrones.Count > 0 ? availableDrones[0] : null;
        }

        DrawDroneButtons();
        Sprite image = selectedDrone.SafeInvoke(data => data.icon);
        if (selectedDrone != null && selectedDrone == droneData && profileImage != null)
        {
            image = profileImage;
        }
        droneImage.SafeInvoke(target =>
        {
            target.sprite = image;
            target.enabled = image != null;
        });

        if (localizedTexts != null)
        {
            foreach (var text in localizedTexts)
            {
                text.SafeInvoke(value => value.ApplyLocalization());
            }
        }

        SetLocalizedText(droneNameText, selectedDrone.SafeInvoke(data => data.displayNameKey),
            selectedDrone.SafeInvoke(data => data.droneName, string.Empty));
        SetLocalizedText(profileText, selectedDrone.SafeInvoke(data => data.profileKey));
        SetLocalizedText(passiveText, selectedDrone.SafeInvoke(data => data.passiveDescriptionKey));
        SetLocalizedText(activeText, selectedDrone.SafeInvoke(data => data.activeDescriptionKey));
    }

    /// <summary>빈 항목과 중복을 제외하며 기존 단일 드론 설정도 지원합니다.</summary>
    private void CollectDrones()
    {
        availableDrones.Clear();
        if (setupContext != null)
        {
            setupManager.SafeInvoke(explore => availableDrones.AddRange(explore.GetMemoryDroneOptions()));
            return;
        }

        if (droneDefinitions != null)
        {
            foreach (var definition in droneDefinitions)
            {
                if (definition != null && availableDrones.Contains(definition) == false)
                {
                    availableDrones.Add(definition);
                }
            }
        }

        if (availableDrones.Count == 0 && droneData != null)
        {
            availableDrones.Add(droneData);
        }
    }

    /// <summary>기존 UI 목록 도우미로 독립 프리팹의 아이콘 슬롯을 재사용합니다.</summary>
    private void DrawDroneButtons()
    {
        if (droneListContent == null || droneButtonPrefab == null)
        {
            return;
        }

        UIListDrawer.DrawListToTarget<UIGuideDroneSelectButton, SO_GuideDroneData>(
            droneListContent, droneButtonPrefab.gameObject, availableDrones,
            (slot, definition, index) => slot.Setup(definition, definition == selectedDrone, SelectDrone));
    }

    /// <summary>아이콘을 선택하면 드론 정보와 설명 스크롤만 갱신합니다.</summary>
    private void SelectDrone(SO_GuideDroneData definition)
    {
        if (definition == null || availableDrones.Contains(definition) == false)
        {
            return;
        }

        selectedDrone = definition;
        DrawPopUp();
        StoreSetupSelection();
        if (setupContext != null)
        {
            setupManager.SafeInvoke(explore => explore.SaveSetupProgress(setupContext));
        }
        Canvas.ForceUpdateCanvases();
        descriptionScroll.SafeInvoke(scroll =>
        {
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
        });
    }

    /// <summary>정보 열람은 저장하지 않고 탐사 선택만 준비 데이터와 이전 선택 기록에 반영합니다.</summary>
    private void StoreSetupSelection()
    {
        if (setupContext == null)
        {
            return;
        }

        setupContext.SelectedMemoryDroneId = selectedDrone.SafeInvoke(data => data.droneId, string.Empty);
        PlayerManager.Instance.SafeInvoke(players => players.RememberMemoryDrone(setupContext.SelectedMemoryDroneId));
        SelectionChanged();
    }

    /// <summary>드론별 지역화 키를 적용하고 키가 없으면 이전 설명을 지웁니다.</summary>
    private static void SetLocalizedText(LocalizeText target, string key, string fallback = "")
    {
        target.SafeInvoke(value =>
        {
            value.textKey = key;
            if (string.IsNullOrEmpty(key))
            {
                value.GetComponent<TMP_Text>().SafeInvoke(text => text.text = fallback);
            }
            else
            {
                value.ApplyLocalization();
            }
        });
    }
}
