using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


public interface IExplorationSetupPage
{
    // 해당 페이지가 열릴 때 호출됨 (이전 단계의 데이터를 기반으로 UI 구성)
    void OnShowPage(ExplorationSetupData setupData);

    // 유저가 필수 항목을 선택했는지 여부 (선택해야 '다음' 버튼 활성화)
    bool IsReadyToProceed();
}

[Serializable]
public class JobButton
{
    public int jobID;
    public GameObject jobButtonObj;
    public Button jobButton;
    public event Action<int> OnJobButtonClicked;

    public void SetButtonEvent()
    {
        if(jobButton != null)
        {
            jobButton.onClick.RemoveAllListeners();
            jobButton.onClick.AddListener(() => OnJobButtonClicked.Invoke(jobID));
        }
    }
}

public class CharacterSelectUI 
    : SelectBaseUI
    , IExplorationSetupPage
{
    [Header("References")]
    [SerializeField] private CharacterInfoController infoController; // 우측 정보창

    [Header("Components")]
    [SerializeField] private Transform classButtonContainer; // 하단 직업 버튼들이 모여있는 부모 객체
    [SerializeField] private List<JobButton> jobButtons;
    [SerializeField] private Image selectJobImage;

    private ExplorationSetupData currentContext;
    private int selectedCharacter = -1;
    private int selectedClass = -1;

    private List<int> characterList = new();


    protected override void Awake()
    {
        base.Awake();

        characterList = PlayerManager.Instance.SafeInvoke(v => v.CharIds);
    }


    protected override void OnEnable()
    {
        base.OnEnable();


    }

    public void OnShowPage(ExplorationSetupData setupData)
    {

        currentContext = setupData;
        selectedCharacter = setupData.SelectedCharacterId; // 뒤로가기 해서 돌아왔을 때 기존 선택 유지용

        DrawCharacterList();

        // 뒤로가기로 돌아왔을 때 기존 선택 복구
        if (selectedCharacter != -1)
        {
            SelectCharacter(selectedCharacter);
            if (selectedClass != -1)
                SelectClass(selectedClass);
        }
        else if (characterList.Count > 0)
        {
            // 아무것도 안 골라져 있다면 맨 첫 번째 캐릭터를 기본으로 선택
            SelectCharacter(characterList[0]);
        }
        
        ShowPopUp(); 
    }

    private void DrawCharacterList()
    {
        InitReplaceContentObject(characterList.Count);

        for (int i = 0; i < characterList.Count; i++)
        {
            int charId = characterList[i];
            var slotObj = content.transform.GetChild(i);

            if (slotObj.TryGetComponent<CharSelectCharSlot>(out var slot))
            {
                var charInfo = PlayerManager.Instance.GetCharacterInfo(charId);

                // CharData 조립
                CharData data = new CharData
                {
                    charId = charId,
                    charName = charInfo != null ? charInfo.name : "Unknown",
                    charIcon = charInfo != null ? charInfo.charSprite : null
                };

                slot.SetCharData(data);

                // 이벤트 연결
                slot.OnClickedSlot -= OnCharacterSlotClicked;
                slot.OnClickedSlot += OnCharacterSlotClicked;
                slot.gameObject.SetActive(true);
                slot.SetSelected(charId == selectedCharacter);
            }
        }
    }

    // 좌측 캐릭터 슬롯을 눌렀을 때
    private void OnCharacterSlotClicked(CharData data)
    {
        // 이미 선택된 데이터라면 무시 
        if (selectedCharacter == data.charId) return; 

        SelectCharacter(data.charId);
    }

    public void SelectCharacter(int charId)
    {
        selectedCharacter = charId;
        currentContext.SelectedCharacterId = charId;

        RefreshCharacterSlotSelectionUI();

        // 1. 우측 캐릭터 정보(스탯/장비) 갱신
        if (infoController != null)
            infoController.UpdateCharacterInfo(charId);

        // 2. 하단 직업 목록 갱신 및 초기화
        UpdateClassList(charId);

        SelectionChanged();
    }

    //모든 슬롯을 순회하며 선택된 ID만 테두리를 켜고, 나머지는 끕니다.
    private void RefreshCharacterSlotSelectionUI()
    {
        // content 하위에 생성된 모든 자식(슬롯)을 순회
        for (int i = 0; i < content.transform.childCount; i++)
        {
            var slotObj = content.transform.GetChild(i);

            // 활성화된 슬롯에 대해서만 검사
            if (slotObj.gameObject.activeSelf && slotObj.TryGetComponent<CharSelectCharSlot>(out var slot))
            {
                // 이 슬롯의 아이디가 지금 선택된 아이디와 같으면 true, 아니면 false
                bool isTargetSelected = (slot.CurrentCharId == selectedCharacter);
                slot.SetSelected(isTargetSelected);
            }
        }
    }

    private void UpdateClassList(int charId)
    {
        List<JobInfo> availableJobs = PlayerManager.Instance.GetAvailableJobs(charId);

        foreach (var btn in jobButtons)
        {
            if (btn.jobButtonObj!= null)
                btn.jobButtonObj.SetActive(false);
        }

        // 가져온 직업 데이터를 바탕으로 하단의 UI 버튼들의 아이콘과 이벤트를 세팅합니다.
        if (jobButtons.Count > 0 && availableJobs.Count > 0)
        {
            foreach(var job in availableJobs)
            {
                var targetButton = jobButtons.FirstOrDefault(b => b.jobID == job.id);

                if (targetButton != null && targetButton.jobButtonObj!= null)
                {
                    // 3. 컨테이너(classButtonContainer)의 자식으로 붙여줍니다. (false는 로컬 스케일 유지용)
                    targetButton.jobButtonObj.transform.SetParent(classButtonContainer, false);

                    // 4. 컨테이너 내에서 리스트 순서대로 예쁘게 정렬되도록 하이라키 맨 아래로 보냅니다.
                    targetButton.jobButtonObj.transform.SetAsLastSibling();

                    // 5. 버튼 활성화
                    targetButton.jobButtonObj.SetActive(true);

                    targetButton.OnJobButtonClicked -= SelectClass;
                    targetButton.OnJobButtonClicked += SelectClass;
                    targetButton.SetButtonEvent(); 
                }
            }
        }


        // 캐릭터가 바뀌었으니 직업 선택 상태는 강제로 초기화
        selectedClass = -1;
        currentContext.SelectedClassId = -1;

        RefreshJobSlotSelectionUI();
    }

    // 하단 직업 버튼을 눌렀을 때
    public void SelectClass(int classId)
    {
        if (selectedClass != -1)
            selectedClass  = selectedClass == classId ? -1 : classId;
        else 
            selectedClass = classId;
        
        currentContext.SelectedClassId = selectedClass;

        RefreshJobSlotSelectionUI();

        SelectionChanged();
    }

    // 하단 '선택(다음)' 버튼이 활성화될 수 있는 조건
    public bool IsReadyToProceed()
    {
        return selectedCharacter != -1 && selectedClass != -1;
    }

    // 선택된 직업 버튼으로 selectJobImage를 이동시키는 함수
    private void RefreshJobSlotSelectionUI()
    {
        if (selectJobImage == null) return;

        // 선택된 직업이 없다면 프레임을 숨깁니다 (캐릭터를 막 바꿨을 때)
        if (selectedClass == -1)
        {
            selectJobImage.gameObject.SetActive(false);
            return;
        }

        // 현재 선택된 직업 버튼 정보를 찾습니다.
        var targetButton = jobButtons.FirstOrDefault(b => b.jobID == selectedClass);

        if (targetButton != null && targetButton.jobButtonObj != null)
        {
            // 1. 프레임 이미지를 선택된 버튼의 자식으로 옮깁니다. (false로 스케일 찌그러짐 방지)
            selectJobImage.transform.SetParent(targetButton.jobButtonObj.transform, false);

            // 2. 부모(버튼)의 정중앙에 위치하도록 좌표를 0으로 초기화합니다.
            if (selectJobImage.TryGetComponent<RectTransform>(out var rt))
            {
                rt.anchoredPosition = Vector2.zero;
            }

            // 3. 버튼의 아이콘보다 위에(앞에) 그려지도록 하이라키 맨 아래로 내립니다.
            selectJobImage.transform.SetAsLastSibling();

            // 4. 프레임을 켭니다.
            selectJobImage.gameObject.SetActive(true);
        }
    }


}
