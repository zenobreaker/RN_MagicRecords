using UnityEngine;

enum PredefinedId : byte
{
    NoTeamId = 255,
}

public struct GenenricTeamId
{
    public byte teamId;
    public static readonly byte NoTeamId = (byte)PredefinedId.NoTeamId;
    public readonly bool IsValid
    {
        get
        {
            return teamId != NoTeamId;
        }
    }

    public GenenricTeamId(byte id)
    {
        teamId = id;
    }

    public static bool operator ==(GenenricTeamId a, GenenricTeamId b)
    {
        return a.teamId == b.teamId;
    }
    public static bool operator !=(GenenricTeamId a, GenenricTeamId b)
    {
        return a.teamId != b.teamId;
    }

    public override int GetHashCode()
    {
        return teamId.GetHashCode();
    }
    public override bool Equals(object obj)
    {
        return obj is GenenricTeamId other && this == other;
    }

    public static implicit operator GenenricTeamId(byte id)
    {
        return new GenenricTeamId(id);
    }
    public static implicit operator GenenricTeamId(int id)
    {
        return new GenenricTeamId((byte)id);
    } // º±≈√¿˚
}

public interface ITeamAgent
{
    void SetGenericTeamId(GenenricTeamId id);
    GenenricTeamId GetGeneriTeamId();
}

public static class TeamUtility
{
    public static GenenricTeamId GetTeamId(GameObject obj)
    {
        if (obj.TryGetComponent<ITeamAgent>(out var teamAgent))
        {
            return teamAgent.GetGeneriTeamId();
        }

        return new GenenricTeamId(GenenricTeamId.NoTeamId);
    }

    public static bool IsSameTeam(GameObject obj1, GameObject obj2)
    {
        var teamA = GetTeamId(obj1);
        var teamB = GetTeamId(obj2);

        return teamA.IsValid && teamB.IsValid && teamA == teamB;
    }


    public static bool IsEnemy(GameObject obj1, GameObject obj2)
    {
        var teamA = GetTeamId(obj1);
        var teamB = GetTeamId(obj2);

        return teamA.IsValid && teamB.IsValid && teamA != teamB;
    }
}
