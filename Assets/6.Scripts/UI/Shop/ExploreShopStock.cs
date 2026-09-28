using System;
using System.Collections.Generic;
using System.Linq;

public enum ExploreShopKind { Record, SkillSwap, Heal15, Heal60, CharacterLevelUp }

[Serializable]
public sealed class ExploreShopOffer
{
    public ExploreShopKind kind;
    public int recordId;
    public int price = -1; // Unconfigured is never a free purchase.
    public bool sold;
    public int purchases;
    public ExploreShopOffer Copy() => (ExploreShopOffer)MemberwiseClone();
}

[Serializable]
public sealed class ExploreShopStock
{
    public List<ExploreShopOffer> offers = new();
    public ExploreShopStock Copy() => new ExploreShopStock { offers = offers.Select(o => o.Copy()).ToList() };
}

[Serializable]
public sealed class ExploreRecordPrice
{
    public RecordRarity rarity;
    public int price = -1;
}

[Serializable]
public sealed class ExploreHealthState
{
    public int characterId;
    public float current;
    public float maximum;
}
