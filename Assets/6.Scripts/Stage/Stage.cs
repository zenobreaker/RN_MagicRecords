using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum StageType
{
    None, 
    Combat, 
    Event, 
    Shop,
    Boss_Combat, 
}

[System.Serializable]
public class Stage 
{
    public int id;

    public StageType type; 

    // ������ �� 
    public List<int> enemyIds = new List<int>();

    // ������ ����
    public List<int> rewardIds = new List<int>();
}


public class StageReplacer
{
    public Stage GetStage(int id)
    {
        Stage temp = new Stage();
        temp.id = id;
        //TODO : �������� Ÿ�� ���ϴ� ������ �ʿ���.

        // ���������� ���� ������ �ʿ��ѵ� 
        // ���� ���� ��츦 �����ϸ� ��� ���̺� ���Ͱ� ��ġ�� ���̺��� 
        // �̸� �������� �� ���̺��� id���� �����ٰ� �ٿ������� �� �� 

        CreateEnemyList(ref temp.enemyIds);
        CreateRewardList(ref temp.rewardIds);

        return temp; 
    }


    public void CreateEnemyList(ref List<int> enemyIds)
    {
        //TODO : �������� �׸��� ���� ���� ������ �ʿ���
        enemyIds.Clear();
        enemyIds.Add(0);
    }

    public void CreateRewardList(ref List<int> rewardIds)
    {
        rewardIds.Clear();
        rewardIds.Add(0);
    }
}
