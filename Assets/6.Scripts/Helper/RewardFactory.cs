using UnityEngine;

public static class RewardFactory
{
    public static IReward Create(RewardData rewardData)
    {
        switch (rewardData.rewardType)
        {
            case RewardType.ITEM:
                {
                    ItemData item =
                        AppManager.Instance.GetItemData(
                            rewardData.itemId,
                            rewardData.itemCategory);

                    return new ItemReward(item);
                }
            case RewardType.RECORD:
                {
                    var mode = (RecordRewardMode)rewardData.param1;

                    switch (mode)
                    {
                        case RecordRewardMode.RandomAll:
                            return new RecordReward(mode);

                        case RecordRewardMode.RandomByRarity:
                            return new RecordReward(
                                mode
                                , rarity: (RecordRarity)rewardData.param2 // CS1061 관련 부분은 별도 처리 필요
                            );

                        case RecordRewardMode.FixedRecord:
                            return new RecordReward(
                                mode
                                , recordId: rewardData.param2 // CS1061 관련 부분은 별도 처리 필요
                            );
                    }
                    break; // CS8070: switch 블록 내에서 break 추가
                }
        }

        return null;
    }
}
