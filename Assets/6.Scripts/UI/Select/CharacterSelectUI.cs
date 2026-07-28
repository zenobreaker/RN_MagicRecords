using System;
using System.Collections.Generic;
using UnityEngine;

public class ExplorationSetupData
{
    public int SelectedCharacterId { get; set; } = -1;
    public int SelectedClassId { get; set; } = -1;


    public void Reset()
    {
        SelectedCharacterId = -1;
        SelectedClassId = -1;
    }
}

public interface IExplorationSetupPage
{
    // 해당 페이지가 열릴 때 호출됨 (이전 단계의 데이터를 기반으로 UI 구성)
    void OnShowPage(ExplorationSetupData setupData);

    // 유저가 필수 항목을 선택했는지 여부 (선택해야 '다음' 버튼 활성화)
    bool IsReadyToProceed();
}

public class CharacterSelectUI 
    : SelectBaseUI
    , IExplorationSetupPage
{
    [Header("References")]
    [SerializeField] private CharacterInfoController infoController; // 우측 정보창
    [SerializeField] private Transform classButtonContainer; // 하단 직업 버튼들이 모여있는 부모 객체

    private ExplorationSetupData currentContext;
    private int selectedCharacter = -1;
    private int selectedClass = -1;

    private List<int> characterList = new();

    public event Action OnSelectionChanged;

    protected override void Awake()
    {
        base.Awake();

        characterList = PlayerManager.Instance.SafeInvoke(v => v.CharIds);
    }


    protected override void OnEnable()
    {
        base.OnEnable();


    }
 
    protected override void OnCompleteSelect()
    {

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

        OnSelectionChanged?.Invoke();
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
        // TODO: PlayerManager나 AppManager를 통해 해당 캐릭터가 가질 수 있는 직업 리스트 로드
        // List<JobInfo> availableJobs = PlayerManager.Instance.GetAvailableJobs(charId);

        // 가져온 직업 데이터를 바탕으로 하단의 UI 버튼들의 아이콘과 이벤트를 세팅합니다.
        // 세팅된 하단 직업 버튼이 클릭되면 SelectClass(jobId) 함수가 호출되도록 연결하세요.

        // 캐릭터가 바뀌었으니 직업 선택 상태는 강제로 초기화
        selectedClass = -1;
        currentContext.SelectedClassId = -1;
    }

    // 하단 직업 버튼을 눌렀을 때
    public void SelectClass(int classId)
    {
        selectedClass = classId;
        currentContext.SelectedClassId = classId;

        // TODO: 클릭된 직업 버튼 시각적 강조 (외곽선 켜기 등)

        OnSelectionChanged?.Invoke();
    }

    // 하단 '선택(다음)' 버튼이 활성화될 수 있는 조건
    public bool IsReadyToProceed()
    {
        // 💡 캐릭터와 직업을 모두 골라야만 다음 페이지(스킬 선택)로 넘어갈 수 있습니다.
        return selectedCharacter != -1 && selectedClass != -1;
    }
}
