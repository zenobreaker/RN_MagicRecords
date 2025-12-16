using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleManager
    : Singleton<BattleManager>
{
    public event Action<GameObject, GameObject, DamageEvent> OnAnyAttackHit;
    public event Action<GameObject, GameObject, float> OnAnyAttackHitFinish;

    private event Action OnFinishBeginBattle;

    private List<Character> players = new List<Character>();
    private List<Character> enemies = new List<Character>();
    private Dictionary<Character, List<Character>> battleTable;

    private bool bInBattle = false;

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    protected override void Start()
    {
        battleTable = new Dictionary<Character, List<Character>>();
        
        base.Start(); 
    }

    private void OnEnable()
    {
        GameManager.Instance.OnBattleStage += OnBattle;
        GameManager.Instance.OnFinishStage += OutBattle;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnBattleStage -= OnBattle;
            GameManager.Instance.OnFinishStage -= OutBattle;
        }
    }

    public void ResistEnemy(Character character)
    {
        enemies.Unique(character);

        // 이미 전투 중인 상태라면 등록하자마자 공격할 타겟을 지정한다. 
        if (bInBattle)
            TryAiToJoinBattle(character);
    }

    public void UnreistEnemy(Character character) => enemies.Remove(character);

    public void RegistPlayer(Character character) => players.Unique(character);

    public void UnreistPlayer(Character character) => players.Remove(character);

    public bool JoinBattle(Character player, Character enemy)
    {
        if (player == null || enemy == null)
        {
            Debug.LogError("JoinBattle 실패: player나 enemy가 null임");
            return false;
        }

        // 플레이어가 등록이 되지 않았다면 플레이어 등록
        if (battleTable.ContainsKey(player) == false)
        {
            battleTable.Add(player, new List<Character>());
        }

        // 등록된 플레이어에 적들 리스트에서 추가
        if (battleTable.TryGetValue(player, out List<Character> list))
        {
            list.Unique(enemy);
            return true;
        }

        return false;
    }

    public void OutBattle()
    {
        battleTable.Clear();
    }


    private Character GetPrioritizedPlayer()
    {
        //TODO: 최적의 플레이어를 계산해서 던져주기 
        foreach (var player in players)
        {
            return player;
        }

        return null;
    }


    public void OnBattle()
    {
        bInBattle = true;

        foreach (var enemy in enemies)
            TryAiToJoinBattle(enemy);

#if UNITY_EDITOR
        Debug.Log("Battle : OnBeginBattle");
#endif

        OnFinishBeginBattle?.Invoke();
    }


    private void TryAiToJoinBattle(Character target)
    {
        if (target.TryGetComponent<AIBehaviourComponent>(out var ai))
        {
            ai.SetCanMove(false);

            Character player = GetPrioritizedPlayer();
            if (player == null)
                return;

            ai.SetTarget(player.gameObject);
            // 성공적으로 등록했으면 처리
            bool bResult = JoinBattle(player, target);
            if (bResult)
                ai.SetCanMove(true);

        }
    }

    public void NotifyAttackHit(GameObject attacker, GameObject target, DamageEvent evt)
    {
        OnAnyAttackHit?.Invoke(attacker, target, evt);
    }

    public void NotifyAttackHitFinish(GameObject attacker, GameObject target, float damage)
    {
        OnAnyAttackHitFinish?.Invoke(attacker, target, damage);
    }
}
