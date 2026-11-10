using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>씬에 배치한 대화 UI와 기존 탐사 전투를 연결하는 첫 플레이 시퀀스입니다.</summary>
public sealed class TutorialSequence : MonoBehaviour
{
    [Header("Tutorial setup")]
    [SerializeField] private int characterId = 1;
    [SerializeField] private int jobId = 1;
    [SerializeField] private int stageId = 1;
    [SerializeField] private int rescueGroupId = 1;
    [SerializeField] private SO_ActiveSkillData initialSkill;
    [SerializeField] private SO_RecordData startingRecord;
    [SerializeField, Min(1f)] private float movementDistance = 6f;
    [SerializeField, Min(.1f)] private float arrivalRadius = 1.2f;
    [SerializeField] private GameObject destinationPrefab;
    [SerializeField] private GameObject butterflyPrefab;

    [Header("Scene UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private Button continueButton;

    [Header("Dialogue (temporary story text)")]
    [SerializeField, TextArea(2, 5)] private string[] storyLines;
    [SerializeField, TextArea(2, 5)] private string[] wakeLines;
    [SerializeField, TextArea(2, 5)] private string[] rescueLines;
    [SerializeField, TextArea(2, 5)] private string[] rescuedLines;

    private bool advance;
    private bool dialoguePause;
    private bool usedSkill;
    private bool recordEffectHit;
    private GameObject observedPlayer;
    private BattleManager observedBattle;
    private bool consumedBullet;
    private bool reloadedByCombo;
    private int previousBullets;
    private SkillComponent skills;
    private SO_SkillEventHandler skillEvents;
    private IMagicBulletProvider bulletProvider;
    private GameObject butterfly;

    public SO_RecordData StartingRecord => startingRecord;
    public bool HasRecordEffectHit => recordEffectHit;

    public static bool CapturesInput
    {
        get;
        private set;
    }

    private void Awake()
    {
        continueButton.SafeInvoke(button => button.onClick.AddListener(Advance));
        dialoguePanel.SafeInvoke(panel => panel.SetActive(false));
        SetObjective(string.Empty);
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == "Story")
        {
            RunStoryAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }
    }

    private void Update()
    {
        if (dialoguePause && Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            Advance();
        }
    }

    /// <summary>배경 설명 이후 저장된 튜토리얼을 복구하거나 첫 탐사를 준비합니다.</summary>
    private async UniTaskVoid RunStoryAsync(CancellationToken token)
    {
        await UniTask.WaitUntil(() => SceneLoadingController.IsLoading == false, cancellationToken: token);
        await ShowDialogueAsync(storyLines, token);

        var app = AppManager.Instance;
        var explore = app.SafeInvoke(manager => manager.GetExploreManager());
        if (app == null || explore == null)
        {
            SetObjective("게임 매니저 설정을 확인해주세요.");
            return;
        }

        if (PlayerManager.Instance.SafeInvoke(players => players.Data.hasCompletedTutorial))
        {
            SceneLoadingController.LoadScene("Lobby");
            return;
        }

        if (SaveManager.HasSavedExploreRun())
        {
            explore.Init(false);
            if (explore.IsTutorialStage)
            {
                EnterTutorialStage();
                return;
            }
            if (explore.TutorialStep == TutorialStep.Record || explore.TutorialStep == TutorialStep.RecordEffect)
            {
                SceneLoadingController.LoadScene("StageSelectScene");
                return;
            }
        }
        if (SaveManager.HasSavedExploreRun())
        {
            var confirmation = UIManager.Instance.SafeInvoke(ui => ui.OpenUI<UISkillEventConfirmation>(true));
            if (confirmation == null)
            {
                SetObjective("튜토리얼 시작 확인 화면을 열지 못했습니다.");
                return;
            }

            confirmation.SetData(
                "튜토리얼 시작",
                "진행 중인 탐사가 있습니다. 확인하면 현재 탐사 진행과 탐사 중 획득한 레코드를 초기화하고 튜토리얼을 시작합니다. 취소하면 기존 탐사를 유지하고 로비로 이동합니다.",
                false,
                _ => this.SafeInvoke(sequence => sequence.StartTutorial(true)),
                () => SceneLoadingController.LoadScene("Lobby"));
            return;
        }

        StartTutorial(false);
    }

    /// <summary>기존 탐사를 교체하는 경우에는 확인 후 튜토리얼을 준비합니다.</summary>
    private void StartTutorial(bool replaceExistingRun)
    {
        var app = AppManager.Instance;
        if (app.SafeInvoke(manager => manager.BeginTutorial(characterId, jobId, initialSkill, replaceExistingRun)))
        {
            EnterTutorialStage();
            return;
        }

        SetObjective("튜토리얼 캐릭터, 직업, 스킬 설정을 확인해주세요.");
    }

    /// <summary>튜토리얼용 맵은 Inspector에 지정한 기존 스테이지를 재사용합니다.</summary>
    private void EnterTutorialStage()
    {
        StageInfo stage = AppManager.Instance.SafeInvoke(app => app.GetStageInfo(stageId));
        if (stage == null)
        {
            SetObjective("튜토리얼 스테이지 설정을 확인해주세요.");
            return;
        }

        AppManager.Instance.SafeInvoke(app => app.GetExploreManager())
            .SafeInvoke(explore => explore.ResetTutorialHealth());
        GameManager.Instance.SafeInvoke(game => game.EnterStage(stage));
    }

    /// <summary>적 없는 이동 씬과 나비를 구출하는 전투 씬을 순서대로 진행합니다.</summary>
    public async UniTask<StageResult> RunStageAsync(SpawnManager spawns, RoomData room, CancellationToken token)
    {
        var player = PlayerManager.Instance.SafeInvoke(players => players.GetCurrentPlayer());
        if (player == null || room.EnemySpawnPoints.Count == 0)
        {
            throw new InvalidOperationException("Tutorial player or room spawn points are missing.");
        }

        var explore = AppManager.Instance.SafeInvoke(app => app.GetExploreManager());
        UIManager.Instance.SafeInvoke(ui => ui.SetGameplayHUDVisible(false));
        if (explore.TutorialStep == TutorialStep.Movement)
        {
            await ShowDialogueAsync(wakeLines, token);
            Vector3 destination = FindDestination(player.transform.position, room);
            GameObject marker = Instantiate(destinationPrefab, destination, Quaternion.identity);
            try
            {
                while (Vector3.Distance(player.transform.position, destination) > arrivalRadius)
                {
                    SetObjective($"움직이기: 빛나는 목적지로 이동하세요. ({Vector3.Distance(player.transform.position, destination):0}m)");
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            finally
            {
                marker.SafeInvoke(value => Destroy(value));
            }
        }
        else
        {
            await RunRescueAsync(player, spawns, room, token);
        }

        bool success = spawns.ActivePlayerCount > 0;
        if (success == false)
        {
            await ShowDialogueAsync(new[]
            {
                "다시 도전해 보자. 나비가 기다리고 있어!"
            }, token);
        }
        SetObjective(string.Empty);
        return new StageResult
        {
            IsSuccess = success,
            IsPlayerDead = success == false
        };
    }

    /// <summary>실제 이동 가능한 위치를 목적지로 사용합니다.</summary>
    private Vector3 FindDestination(Vector3 origin, RoomData room)
    {
        var path = new NavMeshPath();
        foreach (var point in room.EnemySpawnPoints)
        {
            if (point != null && Vector3.Distance(origin, point.position) >= movementDistance &&
                NavMesh.CalculatePath(origin, point.position, NavMesh.AllAreas, path) &&
                path.status == NavMeshPathStatus.PathComplete)
            {
                return point.position;
            }
        }

        if (NavMesh.SamplePosition(origin + Vector3.forward * movementDistance, out var hit, movementDistance, NavMesh.AllAreas) &&
            Vector3.Distance(origin, hit.position) > arrivalRadius &&
            NavMesh.CalculatePath(origin, hit.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete)
        {
            return hit.position;
        }

        throw new InvalidOperationException("Tutorial room has no reachable movement destination.");
    }

    /// <summary>한 방향에서 등장한 적을 처치하며 스킬 소비와 마지막 평타 장전을 경험합니다.</summary>
    private async UniTask RunRescueAsync(Player player, SpawnManager spawns, RoomData room, CancellationToken token)
    {
        var point = room.EnemySpawnPoints[0];
        butterfly = Instantiate(butterflyPrefab, point.position + Vector3.forward * 3f, Quaternion.identity);
        await spawns.SpawnNPCAsync(rescueGroupId, new List<Transform>
        {
            point
        }, true, token);

        foreach (var enemy in BattleManager.Instance.CurrentEnemies)
        {
            enemy.GetComponent<AIController>().SafeInvoke(ai => ai.SetForcedTarget(butterfly));
        }

        await ShowDialogueAsync(rescueLines, token);
        var weapons = player.GetComponent<WeaponComponent>();
        weapons.SetGunMode();
        player.SetCombatInputEnabled(true);
        UIManager.Instance.SafeInvoke(ui => ui.SetGameplayHUDVisible(true));
        BeginObserving(player);
        try
        {
            // 마지막 평타 장전은 전투 중 안내하며, 자동 장전으로 가득 찬 뒤의 전멸 진행을 막지 않습니다.
            while (spawns.ActivePlayerCount > 0 &&
                (spawns.ActiveEnemyCount > 0 || usedSkill == false || consumedBullet == false))
            {
                if (usedSkill == false || consumedBullet == false)
                {
                    SetObjective("나비 구하기: 슬롯 A의 매그넘샷을 사용하세요. 스킬이 마법탄을 소비합니다.");
                }
                else if (reloadedByCombo == false && bulletProvider is Passive_MagicBulletLoad load &&
                    load.CurrentBulletCount >= load.MaxBulletCount)
                {
                    SetObjective("탄환이 가득 찼습니다. 매그넘샷으로 다시 소비한 뒤 기본 공격을 끝까지 이어가세요.");
                }
                else if (reloadedByCombo == false)
                {
                    SetObjective("마법탄 보충: 기본 공격을 끝까지 이어서 마지막 타격으로 탄환을 보충하세요.");
                }
                else
                {
                    SetObjective("나비 구하기: 남은 적을 모두 처치하세요. 위험한 공격은 이동과 회피로 피하세요.");
                }

                // 적은 나비를 향해 공격합니다. 나비는 튜토리얼 연출용 보호 대상입니다.
                foreach (var enemy in BattleManager.Instance.CurrentEnemies)
                {
                    enemy.GetComponent<AIController>().SafeInvoke(ai => ai.SetForcedTarget(butterfly));
                }
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        finally
        {
            EndObserving();
            butterfly.SafeInvoke(value => Destroy(value));
        }

        if (spawns.ActivePlayerCount > 0)
        {
            await ShowDialogueAsync(rescuedLines, token);
        }
    }

    /// <summary>일반 탐사 첫 전투에서는 할로포인트를 장착한 매그넘샷 사용을 확인합니다.</summary>
    public void ObserveRecordEffect()
    {
        BeginObserving(PlayerManager.Instance.SafeInvoke(players => players.GetCurrentPlayer()));
        SetObjective("레코드 효과 확인: 매그넘샷을 사용해 할로포인트의 갈라지는 탄환을 확인하세요.");
    }

    public async UniTask WaitForRecordEffectAsync(CancellationToken token)
    {
        try
        {
            await UniTask.WaitUntil(() => recordEffectHit, cancellationToken: token);
            await ShowDialogueAsync(new[]
            {
                "레코드가 스킬을 바꾸는군. 이제 원하는 길을 골라 탐사를 이어가자!"
            }, token);
        }
        finally
        {
            EndObserving();
            SetObjective(string.Empty);
        }
    }

    /// <summary>스킬 슬롯과 마법탄 공급자의 실제 전투 이벤트를 관찰합니다.</summary>
    private void BeginObserving(Player player)
    {
        EndObserving();
        usedSkill = false;
        recordEffectHit = false;
        consumedBullet = false;
        reloadedByCombo = false;
        skills = player.SafeInvoke(value => value.GetComponent<SkillComponent>());
        observedPlayer = player.SafeInvoke(value => value.gameObject);
        observedBattle = BattleManager.Instance;
        if (observedBattle != null)
        {
            observedBattle.OnAttackResolved += OnAttackResolved;
        }
        bulletProvider = skills.SafeInvoke(value => value.GetCapability<IMagicBulletProvider>());
        previousBullets = bulletProvider?.CurrentBulletCount ?? 0;
        skillEvents = SkillManager.Instance.SafeInvoke(manager => manager.SkillEventHandler);
        if (skills != null)
        {
            skills.OnActiveSkillCooldownChanged += OnSkillCooldown;
            skills.OnBasicAttackBulletReloaded += OnLastAttack;
        }
        if (skillEvents != null)
        {
            skillEvents.OnChangeBullets += OnBulletsChanged;
        }
    }

    private void OnSkillCooldown(SkillSlot slot, bool inCooldown)
    {
        if (slot == SkillSlot.SLOT1 && inCooldown)
        {
            usedSkill = true;
        }
    }

    /// <summary>기본 공격만으로 전투가 끝나도 레코드 효과 체험을 건너뛰지 않습니다.</summary>
    private void OnAttackResolved(GameObject attacker, GameObject target, DamageEvent hit, float damage)
    {
        if (attacker == observedPlayer && usedSkill && hit != null &&
            hit.IsBasicAttack == false && hit.IsSubAction == false && hit.IsStatusEffectDamage == false &&
            target != null && target.TryGetComponent<Enemy>(out _) && damage > 0f)
        {
            recordEffectHit = true;
        }
    }

    private void OnBulletsChanged(Queue<BulletData> bullets)
    {
        int current = bullets?.Count ?? 0;
        if (current < previousBullets)
        {
            consumedBullet = true;
        }
        previousBullets = current;
    }

    private void OnLastAttack()
    {
        if (consumedBullet)
        {
            reloadedByCombo = true;
        }
    }

    private void EndObserving()
    {
        if (observedBattle != null)
        {
            observedBattle.OnAttackResolved -= OnAttackResolved;
        }
        observedBattle = null;
        observedPlayer = null;
        if (skills != null)
        {
            skills.OnActiveSkillCooldownChanged -= OnSkillCooldown;
            skills.OnBasicAttackBulletReloaded -= OnLastAttack;
        }
        if (skillEvents != null)
        {
            skillEvents.OnChangeBullets -= OnBulletsChanged;
        }
        skills = null;
        skillEvents = null;
        bulletProvider = null;
    }

    private void Advance()
    {
        advance = true;
    }

    /// <summary>대화 동안 전투를 정지하고 자신이 요청한 정지만 해제합니다.</summary>
    private async UniTask ShowDialogueAsync(string[] lines, CancellationToken token)
    {
        if (lines == null || lines.Length == 0)
        {
            return;
        }

        PauseManager.RequestPause();
        var player = PlayerManager.Instance.SafeInvoke(players => players.GetCurrentPlayer());
        bool restoreCombatInput = player.SafeInvoke(value => value.CombatInputEnabled);
        player.SafeInvoke(value => value.SetCombatInputEnabled(false));
        dialoguePause = true;
        CapturesInput = true;
        dialoguePanel.SafeInvoke(panel => panel.SetActive(true));
        try
        {
            foreach (string line in lines)
            {
                dialogueText.SafeInvoke(label => label.text = line);
                advance = false;
                await UniTask.NextFrame(cancellationToken: token);
                await UniTask.WaitUntil(() => advance, cancellationToken: token);
            }
        }
        finally
        {
            dialoguePanel.SafeInvoke(panel => panel.SetActive(false));
            player.SafeInvoke(value => value.SetCombatInputEnabled(restoreCombatInput));
            ReleaseDialoguePause();
        }
    }

    private void SetObjective(string text)
    {
        objectiveText.SafeInvoke(label => label.text = text);
    }

    private void ReleaseDialoguePause()
    {
        if (dialoguePause)
        {
            dialoguePause = false;
            CapturesInput = false;
            PauseManager.RequestResume();
        }
    }

    private void OnDestroy()
    {
        continueButton.SafeInvoke(button => button.onClick.RemoveListener(Advance));
        EndObserving();
        ReleaseDialoguePause();
    }
}
