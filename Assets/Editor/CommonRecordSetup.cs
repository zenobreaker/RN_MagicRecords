#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// Explicit builder for game-owned common records. Does not create skill assets.
[InitializeOnLoad]
public static class CommonRecordSetup
{
    private const string Request = "Library/CommonRecordsBuild.request";
    public static readonly int[] RecordIds =
    {
        1,
        2,
        3,
        4,
        5,
        7,
        8,
        9,
        10,
        12,
        13,
        14,
        15,
        16,
        17,
        18,
        19,
        20,
        21,
        22
    };
    static CommonRecordSetup()
    {
        EditorApplication.update += Tick;
    }
    private static void Tick()
    {
        if (File.Exists(Request) == false || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        try
        {
            File.ReadAllText(Request);
            File.Delete(Request);
        }
        catch (IOException)
        {
            return;

        }
        try
        {
            Build();
        }
        catch (Exception e)
        {
            File.WriteAllText("Library/CommonRecordsBuild-error.txt", e.ToString());
            Debug.LogException(e);

        }
    }
    [MenuItem("Tools/Record/Build Common Record Buff Modules")]
    public static void Build()
    {
        var attack = Buff("SO_Buff_Attack", StatusType.ATTACK, "공격력 증가");
        var defense = Buff("SO_Buff_Defense", StatusType.DEFENSE, "방어력 증가");
        var speed = Buff("SO_Buff_MoveSpeed", StatusType.MOVESPEED, "이동속도 증가");
        var attackSpeed = Buff("SO_Buff_AttackSpeed", StatusType.ATTACKSPEED, "공격속도 증가");
        var crit = Buff("SO_Buff_CritRatio", StatusType.CRIT_RATIO, "치명타 확률 증가", ModifierValueType.FIXED);
        var critDamage = Buff("SO_Buff_CritDamage", StatusType.CRIT_DMG, "치명타 피해량 증가");
        var health = Buff("SO_Buff_Health", StatusType.HEALTH, "최대 체력 증가");
        var cooldown = Buff("SO_Buff_Cooldown", StatusType.NONE, "쿨다운 감소");
        cooldown.kind = RecordBuffEntry.BuffKind.CooldownReduction;
        var bleed = AssetDatabase.LoadAssetAtPath<SO_BaseEffect>("Assets/10.ScriptableObjects/Effects/SO_Bleed.asset");
        if (bleed == null)
        {
            throw new InvalidOperationException("SO_Bleed is required.");
        }

        var records = AssetDatabase.FindAssets("t:SO_RecordData", new[]
        {
            "Assets/10.ScriptableObjects/Records"
        })
                    .Select(g => AssetDatabase.LoadAssetAtPath<SO_RecordData>(AssetDatabase.GUIDToAssetPath(g))).ToDictionary(r => r.id);
        foreach (int id in RecordIds)
        {
            if (records.ContainsKey(id) == false)
            {
                throw new InvalidOperationException($"Missing common record {id}.");
            }
        }

        void Configure(int id, CommonRecordModule module)
        {
            var record = records[id];
            record.Modules = new List<CommonRecordModule>
            {
                module
            };
            record.Stats.Clear(); // Modules own the buffs; no duplicate stat application.
            record.type = id >= 18 && id <= 21 ? RecordType.AUGMENT : RecordType.PASSIVE;
            record.linkedPassiveSkillID = 0;
            record.isImplemented = true;
            record.importNote = "";
            EditorUtility.SetDirty(record);
        }
        RecordBuffEntry Entry(RecordBuffEntry template, float amount, float duration = 0)
        {
            return new()
            {
                buff = template.buff,
                kind = template.kind,
                stat = template.stat,
                valueType = template.valueType,
                displayName = template.displayName,
                amount = amount,
                duration = duration
            };
        }
        CommonRecordBuffModule Permanent(params RecordBuffEntry[] entries)
        {
            return new()
            {
                activation = CommonRecordBuffModule.Activation.Persistent,
                buffs = entries.ToList()
            };
        }
        Configure(1, Permanent(Entry(attack, 0.1f)));
        Configure(2, Permanent(Entry(defense, 0.1f)));
        Configure(3, Permanent(Entry(speed, 0.1f)));
        Configure(4, Permanent(Entry(attackSpeed, 0.1f)));
        Configure(5, Permanent(Entry(crit, 0.1f)));
        Configure(7, new CommonRecordBuffModule
        {
            activation = CommonRecordBuffModule.Activation.LowHealth,
            healthThreshold = .3f,
            buffs = new()
            {
                Entry(attackSpeed, .3f)
            }
        });
        Configure(8, Permanent(Entry(attack, 0.1f), Entry(attackSpeed, 0.1f)));
        Configure(9, Permanent(Entry(defense, 0.1f), Entry(health, .1f)));
        Configure(10, Permanent(Entry(attack, .1f)));
        Configure(12, Permanent(Entry(speed, .1f)));
        Configure(13, Permanent(Entry(crit, 0.1f)));
        Configure(14, Permanent(Entry(speed, .1f)));
        Configure(15, Permanent(Entry(attack, 0.1f), Entry(defense, 0.1f)));
        Configure(16, Permanent(Entry(defense, .1f)));
        Configure(17, Permanent(Entry(cooldown, .1f)));
        Configure(18, new CommonRecordBattleBleedModule
        {
            bleed = bleed,
            duration = 10,
            attackRatio = .1f
        });
        Configure(19, new CommonRecordBuffModule
        {
            activation = CommonRecordBuffModule.Activation.BattleStart,
            buffs = new()
            {
                Entry(critDamage, .2f, 30)
            }
        });
        Configure(20, new CommonRecordBuffModule
        {
            activation = CommonRecordBuffModule.Activation.Attack,
            probability = .05f,
            buffs = new()
            {
                Entry(speed, .12f, 10)
            }
        });
        Configure(21, new CommonRecordBuffModule
        {
            activation = CommonRecordBuffModule.Activation.BattleStart,
            buffs = new()
            {
                Entry(attack, .1f, 30)
            }
        });
        Configure(22, Permanent(Entry(attack, 0.1f), Entry(crit, 0.1f)));
        AssetDatabase.SaveAssets();
        // Keep reimport metadata in sync without discarding unrelated JSON fields.
        string path = "Assets/98.Datas/recordDataJson.json";
        var json = JObject.Parse(File.ReadAllText(path));
        foreach (var row in (JArray)json["recordDataJson"])
        {
            if (RecordIds.Contains((int)row["id"]))
            {
                row["isImplemented"] = true;
                row["importNote"] = "";
                row["recordType"] = (int)records[(int)row["id"]].type;
                row["linkedPassiveSkillID"] = 0;
                row["stats"] = new JArray();
            }
        }

        File.WriteAllText(path, json.ToString() + "\n", new UTF8Encoding(false));
        AssetDatabase.ImportAsset(path);
        File.WriteAllText("Library/CommonRecordsBuild-result.txt", $"PASS configured {RecordIds.Length} common records with SO_BaseEffect modules.\n");
        Debug.Log($"Common record buffs: {RecordIds.Length} configured.");
    }
    private static RecordBuffEntry Buff(string name, StatusType stat, string displayName, ModifierValueType valueType = ModifierValueType.PERCENT)
    {
        string path = $"Assets/10.ScriptableObjects/Effects/{name}.asset";
        var asset = AssetDatabase.LoadAssetAtPath<SO_BaseEffect>(path);
        if (asset == null)
        {
            if (File.Exists(path))
            {
                throw new InvalidOperationException($"Invalid effect asset: {path}");
            }

            asset = ScriptableObject.CreateInstance<SO_BaseEffect>();
            asset.id = name.Replace("SO_", "");
            AssetDatabase.CreateAsset(asset, path);
        }
        return new RecordBuffEntry
        {
            buff = asset,
            stat = stat,
            valueType = valueType,
            displayName = displayName
        };
    }
}
#endif
