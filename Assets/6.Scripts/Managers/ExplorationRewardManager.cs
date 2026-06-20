using System.Collections.Generic;

public sealed class ExplorationRewardManager 
{
    private List<IReward> pendingRewards = new();

    public void AddReward(IReward reward)
    {
        pendingRewards.Add(reward);
    }

    public void ReceiveReward(IReward reward)
    {
        reward.Receive();

        pendingRewards.Remove(reward);
    }
}
