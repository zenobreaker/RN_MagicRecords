#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
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
    private float previousTimeScale;

    [SetUp]
    public void SetUp()
    {
        previousTimeScale = Time.timeScale;
        Time.timeScale = 1f;
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
        Time.timeScale = previousTimeScale;
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
        AdvanceTime(5f);
        Assert.That(drone.Points, Is.EqualTo(25f));
        AdvanceTime(0f);
        Assert.That(drone.Points, Is.EqualTo(25f));
        AdvanceTime(30f);
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
        AdvanceTime(10f);
        drone.AddPoints(100f);
        Assert.That(drone.Points, Is.EqualTo(20f));
        battle.OutBattle();
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
        AdvanceTime(1f);
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

    [TestCase(RecordRarity.NORMAL, 0.45f)]
    [TestCase(RecordRarity.RARE, 0.51f)]
    [TestCase(RecordRarity.UNIQUE, 0.66f)]
    [TestCase(RecordRarity.LEGENDARY, 0.75f)]
    [TestCase(RecordRarity.MYTH, 0.9f)]
    public void MemoryChargeAddsRarityBonusToOnePercentOfCapacity(RecordRarity rarity, float expected)
    {
        data.initialPoints = 0f;
        drone.Initialize(data);
        drone.OnMemoryEffectActivated(rarity);
        Assert.That(drone.Points, Is.Zero);
        battle.OnBattle();
        drone.OnMemoryEffectActivated(rarity);
        Assert.That(drone.Points, Is.EqualTo(expected).Within(0.0001f));
        Assert.That(drone.IsFullyCharged, Is.False);
        Assert.That(drone.TryActivate(), Is.False);
        drone.AddPoints(30f);
        Assert.That(drone.Points, Is.EqualTo(30f));
        Assert.That(drone.TryActivate(), Is.True);
        drone.OnMemoryEffectActivated(rarity);
        Assert.That(drone.Points, Is.EqualTo(30f));
    }

    [Test]
    public void ActiveAppearsAsOneTimedBuffAndExpiresWithItsModifiers()
    {
        data.activeStats.Add(new RecordStatData
        {
            Status = StatusType.ATTACK,
            ValueType = ModifierValueType.FIXED,
            Value = 10f
        });
        battle.OnBattle();
        Assert.That(drone.TryActivate(), Is.True);
        var active = effects.ActiveEffects.OfType<GuideDroneActiveEffect>().Single();
        Assert.That(effects.ActiveEffects.Count, Is.EqualTo(1));
        Assert.That(active.Duration, Is.EqualTo(30f));
        Assert.That(active.FxIcon, Is.EqualTo(data.icon));
        Assert.That(active.ShowRemainingTime, Is.True);
        Assert.That(status.GetStatusValue(StatusType.ATTACK), Is.EqualTo(110f));
        AdvanceTime(7f);
        Assert.That(active.RemainingTime, Is.EqualTo(23f));
        effects.RemoveEffect(active, true);
        Assert.That(drone.IsActive, Is.False);
        Assert.That(drone.Points, Is.EqualTo(23f));
        Assert.That(status.GetStatusValue(StatusType.ATTACK), Is.EqualTo(100f));
    }

    [TestCase(RecordType.MEMORY, 0.51f)]
    [TestCase(RecordType.RELIC, 0f)]
    public void NaturalMemoryActivationChargesOnceForMultipleEffectsAndForcedReplayDoesNotCharge(RecordType type, float expected)
    {
        data.initialPoints = 0f;
        drone.Initialize(data);
        var template = ScriptableObject.CreateInstance<SO_RecordData>();
        created.Add(template);
        template.id = 17;
        template.type = type;
        template.rarity = RecordRarity.RARE;
        var module = TimedBuff();
        module.buffs.Add(new RecordBuffEntry
        {
            buff = module.buffs[0].buff,
            stat = StatusType.DEFENSE,
            amount = 0.2f,
            duration = 30f
        });
        template.Modules.Add(module);
        var passive = new RecordPassive(template);
        passive.OnAcquire(owner);
        battle.OnBattle();
        Assert.That(drone.Points, Is.EqualTo(expected).Within(0.0001f));
        passive.OnUpdate(1f);
        passive.ReactivateMemory(owner);
        Assert.That(drone.Points, Is.EqualTo(expected).Within(0.0001f));
        passive.OnLose();
    }

    /// <summary>런타임과 같이 기존 버프 시계가 진행된 후 드론 상태를 갱신합니다.</summary>
    private void AdvanceTime(float deltaTime)
    {
        var active = effects.ActiveEffects.OfType<GuideDroneActiveEffect>().FirstOrDefault();
        active?.Update(deltaTime);
        Invoke(drone, "Tick", deltaTime);
        if (active != null && active.IsExpired)
        {
            effects.RemoveEffect(active, true);
        }
    }

    [Test]
    public void DroneGaugeHidesDuringActivationAndRestoresDisabledAtZero()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/5.Prefabs/UI/HUD/GuideDroneGauge.prefab");
        var instance = Object.Instantiate(prefab);
        created.Add(instance);
        var gauge = instance.GetComponent<GuideDroneGaugeUI>();
        Invoke(gauge, "OnEnable");
        var serialized = new SerializedObject(gauge);
        var display = (GameObject)serialized.FindProperty("displayRoot").objectReferenceValue;
        var fill = (UnityEngine.UI.Image)serialized.FindProperty("chargeFill").objectReferenceValue;
        var button = (UnityEngine.UI.Button)serialized.FindProperty("activationButton").objectReferenceValue;
        Assert.That(display.activeSelf, Is.True);
        Assert.That(fill.fillAmount, Is.EqualTo(1f));
        Assert.That(button.interactable, Is.True);
        battle.OnBattle();
        drone.TryActivate();
        Assert.That(display.activeSelf, Is.False);
        AdvanceTime(30f);
        Assert.That(display.activeSelf, Is.True);
        Assert.That(fill.fillAmount, Is.Zero);
        Assert.That(button.interactable, Is.False);
        Invoke(gauge, "OnDisable");
    }

    [Test]
    public void BuffIconShowsDroneSecondsWithoutChangingOtherBuffLabels()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/5.Prefabs/UI/HUD/EffectImage.prefab");
        var instance = Object.Instantiate(prefab);
        created.Add(instance);
        var icon = instance.GetComponent<EffectIconUI>();
        var effect = new GuideDroneActiveEffect("DroneTest", data, 30f);
        effect.Update(6f);
        icon.OnApply(effect);
        var serialized = new SerializedObject(icon);
        var label = (TMPro.TextMeshProUGUI)serialized.FindProperty("remainingTime").objectReferenceValue;
        Assert.That(label.text, Is.EqualTo("24"));
        Assert.That(label.gameObject.activeSelf, Is.True);
        icon.OnApply(new StatBuffEffect("Other", 30f, StatusType.ATTACK, 0.1f));
        Assert.That(label.gameObject.activeSelf, Is.False);
    }

    [Test]
    public void NormalMemoryReachesFullChargeAfterSixtySevenActivations()
    {
        data.initialPoints = 0f;
        drone.Initialize(data);
        battle.OnBattle();
        for (int i = 0; i < 66; i++)
        {
            drone.OnMemoryEffectActivated(RecordRarity.NORMAL);
        }
        Assert.That(drone.IsFullyCharged, Is.False);
        drone.OnMemoryEffectActivated(RecordRarity.NORMAL);
        Assert.That(drone.IsFullyCharged, Is.True);
        Assert.That(drone.Points, Is.EqualTo(30f));
    }

    [Test]
    public void BattleInterruptionPreservesOnlyUnspentPoints()
    {
        battle.OnBattle();
        drone.TryActivate();
        AdvanceTime(5f);
        battle.OutBattle();
        Assert.That(drone.Points, Is.EqualTo(25f));
        battle.OnBattle();
        Assert.That(drone.TryActivate(), Is.False);
        drone.AddPoints(5f);
        Assert.That(drone.TryActivate(), Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AcceptedSkillCastChargesOnceAndRejectsBusyAndCooldownAttempts(bool concurrent)
    {
        data.initialPoints = 0f;
        drone.Initialize(data);
        battle.OnBattle();
        var skill = CreateActiveSkill(concurrent);
        try
        {
            skill.Cast();
            Assert.That(drone.Points, Is.EqualTo(1.5f));
            skill.Cast();
            skill.EndPhaseAndNext();
            Assert.That(drone.Points, Is.EqualTo(1.5f));
            skill.EndSkill(false);
            skill.Cast();
            Assert.That(drone.Points, Is.EqualTo(1.5f));
            skill.Update_Cooldown(10f);
            skill.Cast();
            Assert.That(drone.Points, Is.EqualTo(3f));
        }
        finally
        {
            skill.EndSkill(false);
        }
    }

    [TestCase("IsBasicAttack")]
    [TestCase("IsSubAction")]
    public void BasicAttacksAndSubActionsDoNotGrantSkillCharge(string flag)
    {
        data.initialPoints = 0f;
        drone.Initialize(data);
        battle.OnBattle();
        var skill = CreateActiveSkill();
        typeof(ActiveSkill).GetProperty(flag).SetValue(skill, true);
        try
        {
            skill.Cast();
            Assert.That(drone.Points, Is.Zero);
        }
        finally
        {
            skill.EndSkill(false);
        }
    }

    [Test]
    public void TwentySkillUsesFullyChargeAndMemoryChargeStillAddsSeparately()
    {
        data.initialPoints = 0f;
        drone.Initialize(data);
        battle.OnBattle();
        var skill = CreateActiveSkill();
        for (int i = 0; i < 20; i++)
        {
            skill.Update_Cooldown(10f);
            skill.Cast();
            skill.EndSkill(false);
            if (i == 18)
            {
                Assert.That(drone.Points, Is.EqualTo(28.5f));
                Assert.That(drone.IsFullyCharged, Is.False);
            }
        }
        Assert.That(drone.Points, Is.EqualTo(30f));
        Assert.That(drone.IsFullyCharged, Is.True);

        drone.Initialize(data);
        drone.OnMemoryEffectActivated(RecordRarity.NORMAL);
        skill.Update_Cooldown(10f);
        skill.Cast();
        skill.EndSkill(false);
        Assert.That(drone.Points, Is.EqualTo(1.95f).Within(0.0001f));
    }

    [Test]
    public void SkillChargeRespectsCombatPauseDeathAndDroneActivation()
    {
        data.initialPoints = 0f;
        drone.Initialize(data);
        var skill = CreateActiveSkill();
        drone.OnSkillUsed(skill);
        Assert.That(drone.Points, Is.Zero);
        battle.OnBattle();
        Time.timeScale = 0f;
        drone.OnSkillUsed(skill);
        Assert.That(drone.Points, Is.Zero);
        Time.timeScale = 1f;
        health.SetHealthPoint(0f);
        drone.OnSkillUsed(skill);
        Assert.That(drone.Points, Is.Zero);
        health.SetHealthPoint(100f);
        drone.AddPoints(30f);
        drone.TryActivate();
        AdvanceTime(5f);
        drone.OnSkillUsed(skill);
        Assert.That(drone.Points, Is.EqualTo(25f));
    }

    /// <summary>실제 Cast 경로의 사용 거부와 여러 페이즈를 검증할 최소 스킬을 구성합니다.</summary>
    private GenericActiveSkill CreateActiveSkill(bool concurrent = false)
    {
        status.SetStatusValue(StatusType.ATTACKSPEED, 1f);
        var template = ScriptableObject.CreateInstance<SO_ActiveSkillData>();
        created.Add(template);
        template.isConcurrentSkill = concurrent;
        template.levelDatas = new List<SkillLevelData>
        {
            new SkillLevelData
            {
                cooldown = 10f,
                castingTime = 0f,
                damageData = new DamageData()
            }
        };
        template.phaseList = new List<PhaseSkill>
        {
            new PhaseSkill(),
            new PhaseSkill()
        };
        var skill = new GenericActiveSkill(template);
        skill.SetLevel(1);
        skill.SetOwner(owner);
        return skill;
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
