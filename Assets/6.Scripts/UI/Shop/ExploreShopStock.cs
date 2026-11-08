using System;
using System.Collections.Generic;
using System.Linq;

public enum ExploreShopKind
{
    Record, SkillSwap, Heal15, Heal60, CharacterLevelUp
}

[Serializable]
public sealed class ExploreShopOffer
{
    public ExploreShopKind kind;
    public int recordId;
    public int price = -1; // Unconfigured is never a free purchase.
    public bool sold;
    public int purchases;
    public ExploreShopOffer Copy()
    {
        return (ExploreShopOffer)MemberwiseClone();
    }
}

[Serializable]
public sealed class ExploreShopStock
{
    public List<ExploreShopOffer> offers = new();
    public int recordSlotCount;
    public int rerollCount;
    public int rarityMissCount;

    public int RerollPrice
    {
        get
        {
            long price = 15;
            for (int i = 0; i < rerollCount && price < int.MaxValue; i++)
            {
                price = System.Math.Min(int.MaxValue, price * 6 / 5);
            }
            return (int)price;
        }
    }

    public ExploreShopStock Copy()
    {
        return new ExploreShopStock
        {
            recordSlotCount = recordSlotCount,
            rerollCount = rerollCount,
            rarityMissCount = rarityMissCount,
            offers = offers?.Where(offer => offer != null).Select(offer => offer.Copy()).ToList() ?? new()
        };
    }
}

[Serializable]
public sealed class ExploreRecordPrice
{
    public RecordRarity rarity;
    public int price = -1;
    public int maximumPrice = -1;

    public int Roll(RecordType type)
    {
        int upper = System.Math.Max(price, maximumPrice);
        int first = UnityEngine.Random.Range(price, upper + 1);
        int second = UnityEngine.Random.Range(price, upper + 1);
        return type == RecordType.AUGMENT ? System.Math.Max(first, second) : System.Math.Min(first, second);
    }
}

[Serializable]
public sealed class ExploreHealthState
{
    public int characterId;
    public float current;
    public float maximum;
}
