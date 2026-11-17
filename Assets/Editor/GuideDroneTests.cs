#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class GuideDroneTests
{
    private readonly List<Object> created = new();
    private GameObject owner;
    private StatusComponent status;
    private EffectComponent effects;
    private HealthPointComponent health;
    private BattleManager battle;
    private GuideDrone drone;
    private SO_GuideDroneData data;

    [SetUp]
    public void SetUp()
    {
        CreateObject("GuideDroneTestGame").AddComponent<GameManager>();
        CreateObject("GuideDroneTestLocalization").AddComponent<LocalizationManager>();
        battle = CreateObject("GuideDroneTestBattle").AddComponent<BattleManager>();
        owner = CreateObject("GuideDroneTestOwner");
        owner.AddComponent<Character>();
        health = owner.AddComponent<HealthPointComponent>();
        health.SetHealthPoint(100f);
        status = owner.AddComponent<StatusComponent>();
        effects = owner.AddComponent<EffectComponent>();
        Invoke(status, "Awake");
        Invoke(effects, "Awake");
        status.SetStatusValue(StatusType.CRIT_RATIO, 0.2f);
        status.SetStatusValue(StatusType.ATTACK, 100f);
        drone = owner.AddComponent<GuideDrone>();
        Invoke(drone, "Awake");
        data = Object.Instantiate(AssetDatabase.LoadAssetAtPath<SO_GuideDroneData>(
            "Assets/10.ScriptableObjects/GuideDrones/SO_GuideDrone_Narae.asset"));
        created.Add(data);
        drone.Initialize(data);
    }

    [TearDown]
    public void TearDown()
    {
        Invoke(drone, "OnDisable");
        effects.ClearEffects();
        for (int i = created.Count - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(created[i]);
        }
        created.Clear();
        Time.timeScale = 1f;
    }

    [Test]
    public void NaraeAddsPercentagePointsAndCleansUpOnlyHerEffects()
    {
        Assert.That(drone.Points, Is.EqualTo(30f));
        Assert.That(status.GetStatusValue(StatusType.CRIT_RATIO), Is.EqualTo(0.25f).Within(0.0001f));
        battle.OnBattle();
        Assert.That(drone.TryActivate(), Is.True);
        Assert.That(status.GetStatusValue(StatusType.CRIT_RATIO), Is.EqualTo(0.35f).Within(0.0001f));
        Assert.That(drone.TryActivate(), Is.False);
        battle.OutBattle();
        Assert.That(drone.IsActive, Is.False);
        Assert.That(status.GetStatusValue(StatusType.CRIT_RATIO), Is.EqualTo(0.25f).Within(0.0001f));
        Invoke(drone, "OnDisable");
        Assert.That(status.GetStatusValue(StatusType.CRIT_RATIO), Is.EqualTo(0.2f).Within(0.0001f));
    }

    [Test]
    public void PointsDrainWithCombatTimeAndExpireWithoutRemovingPassive()
    {
        battle.OnBattle();
        Assert.That(drone.TryActivate(), Is.True);
        Invoke(drone, "Tick", 5f);
        Assert.That(drone.Points, Is.EqualTo(25f));
        Invoke(drone, "Tick", 0f);
        Assert.That(drone.Points, Is.EqualTo(25f));
        Invoke(drone, "Tick", 30f);
        Assert.That(drone.Points, Is.Zero);
        Assert.That(drone.IsActive, Is.False);
        Assert.That(drone.TryActivate(), Is.False);
        Assert.That(status.GetStatusValue(StatusType.CRIT_RATIO), Is.EqualTo(0.25f).Within(0.0001f));
    }

    [Test]
    public void ActivationRejectsOutsideBattlePauseDeathAndInvalidSettings()
    {
        Assert.That(drone.TryActivate(), Is.False);
        battle.OnBattle();
        Time.timeScale = 0f;
        Assert.That(drone.TryActivate(), Is.False);
        Time.timeScale = 1f;
        health.SetHealthPoint(0f);
        Assert.That(drone.TryActivate(), Is.False);
        health.SetHealthPoint(100f);
        data.pointsPerSecond = 0f;
        Assert.That(drone.TryActivate(), Is.False);
        data.pointsPerSecond = float.NaN;
        Assert.That(data.HasValidPointSettings(), Is.False);
    }

    [Test]
    public void PointAwardsClampAndReinitializationDoesNotStackPassive()
    {
        drone.AddPoints(float.NaN);
        drone.AddPoints(float.PositiveInfinity);
        drone.AddPoints(-1f);
        Assert.That(drone.Points, Is.EqualTo(30f));
        battle.OnBattle();
        drone.TryActivate();
        Invoke(drone, "Tick", 10f);
        drone.AddPoints(100f);
        Assert.That(drone.Points, Is.EqualTo(30f));
        drone.Initialize(data);
        Assert.That(drone.IsActive, Is.False);
        Assert.That(status.GetStatusValue(StatusType.CRIT_RATIO), Is.EqualTo(0.25f).Within(0.0001f));
    }

    [Test]
    public void DeathAndDisableRemoveActiveBonuses()
    {
        battle.OnBattle();
        drone.TryActivate();
        health.SetHealthPoint(0f);
        Invoke(drone, "Tick", 1f);
        Assert.That(drone.IsActive, Is.False);
        health.SetHealthPoint(100f);
        drone.TryActivate();
        Invoke(drone, "OnDisable");
        Assert.That(drone.Points, Is.Zero);
        Assert.That(status.GetStatusValue(StatusType.CRIT_RATIO), Is.EqualTo(0.2f).Within(0.0001f));
    }

    [Test]
    public void MemoryBypassesAttackChanceAndRefreshesWithoutStacking()
    {
        var module = TimedBuff();
        module.activation = CommonRecordBuffModule.Activation.Attack;
        module.probability = 0f;
        module.OnAcquire(owner, 1);
        Assert.That(module.TryApplyAttack(0f), Is.False);
        module.OnMemoryReactivated();
        var effect = effects.HasEffect("Record:17:GuideDroneTestBuff:0");
        Assert.That(effect, Is.Not.Null);
        Assert.That(status.GetStatusValue(StatusType.ATTACK), Is.EqualTo(110f).Within(0.001f));
        effect.Update(7f);
        module.OnMemoryReactivated();
        Assert.That(effect.RemainingTime, Is.EqualTo(30f));
        Assert.That(status.GetStatusValue(StatusType.ATTACK), Is.EqualTo(110f).Within(0.001f));
        effects.RemoveEffect(effect, true);
        module.OnMemoryReactivated();
        Assert.That(effects.HasEffect(effect.ID), Is.Not.SameAs(effect));
        module.OnLose();
        Assert.That(status.GetStatusValue(StatusType.ATTACK), Is.EqualTo(100f));
    }

    [Test]
    public void MemoryOnlyTouchesOwnedMemoryAndPreservesPermanentEntries()
    {
        var records = CreateObject("GuideDroneTestRecords").AddComponent<RecordManager>();
        var inventory = Field<RecordInventory>(records, "recordInventory");
        var passives = Field<Dictionary<string, PassiveSkill>>(records, "recordPassives");
        foreach (var type in new[]
        {
            RecordType.MEMORY, RecordType.RELIC
        })
        {
            var template = ScriptableObject.CreateInstance<SO_RecordData>();
            created.Add(template);
            template.type = type;
            template.id = type == RecordType.MEMORY ? 17 : 19;
            template.Modules.Add(TimedBuff());
            var record = inventory.AddRecord(template.GetRecordData());
            var passive = new RecordPassive(template);
            passive.OnAcquire(owner);
            passives.Add(record.uniqueID, passive);
        }
        var stranger = CreateObject("GuideDroneTestStranger");
        records.ReactivateMemoryRecords(stranger);
        Assert.That(effects.ActiveEffects.Count, Is.Zero);
        records.ReactivateMemoryRecords(owner);
        Assert.That(effects.ActiveEffects.Count, Is.EqualTo(1));
        Assert.That(effects.HasEffect("Record:17:GuideDroneTestBuff:0"), Is.Not.Null);
        foreach (var passive in passives.Values)
        {
            passive.OnLose();
        }

        var permanent = TimedBuff();
        permanent.buffs[0].duration = 0f;
        permanent.OnAcquire(owner, 1);
        permanent.OnMemoryReactivated();
        Assert.That(effects.ActiveEffects.Count, Is.Zero);
        permanent.OnLose();
    }

    [Test]
    public void SoroRestartsDurationWithoutAwardingInitialEnergy()
    {
        var soro = new RecordSoroModule();
        soro.SetRecordId(39);
        soro.OnAcquire(owner, 1);
        effects.AddLaserEnergy(3, null, false);
        soro.OnMemoryReactivated();
        Assert.That(status.GetStatusValue(StatusType.ENERGY_GAIN_BONUS), Is.EqualTo(1f));
        Assert.That(effects.HasEffect("LaserEnergy").StackCount, Is.EqualTo(3));
        soro.OnUpdate(29f);
        soro.OnMemoryReactivated();
        soro.OnUpdate(29f);
        Assert.That(status.GetStatusValue(StatusType.ENERGY_GAIN_BONUS), Is.EqualTo(1f));
        soro.OnUpdate(1f);
        Assert.That(status.GetStatusValue(StatusType.ENERGY_GAIN_BONUS), Is.Zero);
        Assert.That(effects.HasEffect("LaserEnergy").StackCount, Is.EqualTo(3));
        soro.OnLose();
    }

    private CommonRecordBuffModule TimedBuff()
    {
        var definition = ScriptableObject.CreateInstance<SO_BaseEffect>();
        created.Add(definition);
        definition.id = "GuideDroneTestBuff";
        var module = new CommonRecordBuffModule
        {
            activation = CommonRecordBuffModule.Activation.BattleStart,
            buffs = new List<RecordBuffEntry>
            {
                new RecordBuffEntry
                {
                    buff = definition,
                    stat = StatusType.ATTACK,
                    amount = 0.1f,
                    duration = 30f
                }
            }
        };
        module.SetRecordId(17);
        return module;
    }

    private GameObject CreateObject(string name)
    {
        var value = new GameObject(name);
        created.Add(value);
        return value;
    }

    private static T Field<T>(object target, string name)
    {
        return (T)target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    }

    private static void Invoke(object target, string method, params object[] arguments)
    {
        target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, arguments);
    }
}
#endif
