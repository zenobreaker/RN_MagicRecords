using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class BattleManager
    : Singleton<BattleManager>
{
    private Action OnFinishBeginBattle;
    private Action OnFinishInBattle;
    private Action OnFinishEndBattle;

    private List<Character> players = new List<Character>();
    private List<Character> enemies = new List<Character>();
    private Dictionary<Character, List<Character>> battleTable;

    protected override void Awake()
    {
        base.Awake();
        Instance = this;

        OnFinishBeginBattle += GameManager.Instance.OnFinishedBeginBattle;
        
        OnFinishEndBattle += GameManager.Instance.OnFinishedEndBattle;
    }

    private void Start()
    {
        battleTable = new Dictionary<Character, List<Character>>();
    }

    public void ResistEnemy(Character character) => enemies.Unique(character);

    public void UnreistEnemy(Character character) => enemies.Remove(character);

    public void ResistPlayer(Character character) => players.Unique(character);

    public void UnreistPlayer(Character character) => players.Remove(character);

    public void JoinBattle(Character player, Character enemy)
    {
        if (player == null || enemy == null)
        {
            Debug.LogError("JoinBattle 실패: player나 enemy가 null임");
            return;
        }

        if (battleTable.TryGetValue(player, out List<Character> list))
        {
            list.Unique(enemy);
            return;
        }

        if (battleTable.ContainsKey(player) == false)
        {
            battleTable.Add(player, new List<Character>());
            return;
        }

        battleTable[player].Add(enemy);
    }

    public void OutBattle(Character player, Character enemy)
    {
        if (battleTable.TryGetValue(player, out List<Character> list))
        {
            list.Remove(enemy);
        }
    }


    private Character GetPrioritizedPlayer()
    {
        //TODO: 최적의 플레이어를 계산해서 던져주기 
        foreach(var player in players)
        {
            return player; 
        }

        return null;
    }


    public void OnBeginBattle()
    {
        foreach(var enemy in enemies)
        {
            if(enemy.TryGetComponent<AIBehaviourComponent>(out var ai))
            {
                ai.SetCanMove(false);
                Character player = GetPrioritizedPlayer();
                ai.SetTarget(player.GameObject());
                JoinBattle(player, enemy);
            }
        }

#if UNITY_EDITOR
        Debug.Log("Battle : OnBeginBattle");
#endif

        OnFinishBeginBattle?.Invoke();
    }

    public void OnInBattle()
    {
        foreach (var enemy in enemies)
        {
            if (enemy.TryGetComponent<AIBehaviourComponent>(out var ai))
            {
                ai.SetCanMove(true);
            }
        }

#if UNITY_EDITOR
        Debug.Log("Battle : OnInBattle");
#endif
        OnFinishInBattle?.Invoke(); 
    }

    public void OnEndBattle()
    {
#if UNITY_EDITOR
        Debug.Log("Battle : OnFinishEndBattle");
#endif

        OnFinishEndBattle?.Invoke();
    }

    public void OnBeginBossBattle()
    {

    }

    public void OnInBossBattle()
    { }

    public void OnEndBossBattle()
    {

    }
}
