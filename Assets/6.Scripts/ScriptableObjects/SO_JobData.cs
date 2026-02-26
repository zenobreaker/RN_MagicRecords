using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class JobInfo
{
    public int id;
    public Sprite jobSprite;
    public string jobName; 
}


[CreateAssetMenu(fileName = "SO_JobData", menuName = "Scriptable Objects/SO_JobData")]
public class SO_JobData : ScriptableObject
{
    public System.Collections.Generic.List<JobInfo> list = new();
    protected Dictionary<int, JobInfo> jobTable = new();

    public void Init()
    {
        foreach (JobInfo info in list)
        {
            jobTable[info.id] = info;
        }
    }

    public JobInfo GetJobInfo(int id)
    {
        if(jobTable.ContainsKey(id))
            return jobTable[id];
        return null;
    }
}
