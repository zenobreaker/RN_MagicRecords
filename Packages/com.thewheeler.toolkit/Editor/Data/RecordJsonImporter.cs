using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheWheeler.Editor
{
    [Serializable]
    public sealed class RecordImportInfo
    {
        public int id;
        public string namekeycode, description, iconPath;
    }
    [Serializable]
    public sealed class RecordImportStat
    {
        public string stat;
        public int calcType;
        public float value;
    }
    [Serializable]
    public sealed class RecordImportSkill
    {
        public int skillID;
        public string modifier, operation;
        public float value;
    }
    [Serializable]
    public sealed class RecordImportData
    {
        public int id, recordType, rarity, linkedPassiveSkillID;
        public string targetFilter, importNote;
        public bool isImplemented = true;
        public RecordImportStat[] stats;
        public RecordImportSkill[] skills;
    }
    [Serializable]
    public sealed class RecordImportInfoRoot
    {
        public RecordImportInfo[] recordInfoJson;
    }
    [Serializable]
    public sealed class RecordImportDataRoot
    {
        public RecordImportData[] recordDataJson;
    }
    [Serializable]
    public sealed class RecordImportReport
    {
        public int created, updated, pending;
        public List<string> paths = new List<string>();
        public List<string> warnings = new List<string>();
    }

    // Serialized field contracts let the package import game-owned types without
    // an Assembly-CSharp reference. Existing paths, GUIDs, icons and modules survive.
    public static class RecordJsonImporter
    {
        public const string DefaultJsonFolder = "Assets/98.Datas";
        public const string DefaultAssetFolder = "Assets/10.ScriptableObjects/Records";
        public static RecordImportReport Import(string jsonFolder = DefaultJsonFolder,
            string assetFolder = DefaultAssetFolder, string recordTypeName = "SO_RecordData",
            string passiveTypeName = "SO_PassiveSkillData", bool preserveCommon = true)
        {
            assetFolder = ValidateAssetFolder(assetFolder);
            var info = Read<RecordImportInfoRoot>(Path.Combine(jsonFolder, "recordInfoJson.json")).recordInfoJson;
            var data = Read<RecordImportDataRoot>(Path.Combine(jsonFolder, "recordDataJson.json")).recordDataJson;
            if (info == null || data == null)
            {
                throw new InvalidDataException("Missing recordInfoJson/recordDataJson arrays.");
            }

            Validate(info, data);
            Type recordType = ResolveType(recordTypeName);
            if (recordType == null || typeof(ScriptableObject).IsAssignableFrom(recordType) == false)
            {
                throw new InvalidOperationException($"ScriptableObject type '{recordTypeName}' was not found.");
            }

            var byId = new Dictionary<int, ScriptableObject>();
            foreach (var asset in FindAssets(recordType, assetFolder))
            {
                int id = Required(new SerializedObject(asset), "id").intValue;
                if (byId.ContainsKey(id))
                {
                    throw new InvalidDataException($"Existing assets share record ID {id}.");
                }

                byId.Add(id, asset);
            }
            var passiveIds = new HashSet<int>();
            Type passiveType = ResolveType(passiveTypeName);
            if (passiveType != null)
            {
                foreach (var asset in FindAssets(passiveType, "Assets"))
                {
                    passiveIds.Add(Required(new SerializedObject(asset), "id").intValue);
                }
            }

            var probe = ScriptableObject.CreateInstance(recordType);
            try
            {
                var so = new SerializedObject(probe);
                foreach (string field in new[]
                {
                    "id",
                    "recordName",
                    "description",
                    "targetFilter",
                    "type",
                    "rarity",
                    "Stats",
                    "Skills",
                    "linkedPassiveSkillID",
                    "isImplemented"
                })
                {
                    Required(so, field);
                }
            }
            finally
            {
                Object.DestroyImmediate(probe);

            }
            // Detect every destination collision before writing any asset.
            foreach (var row in info)
            {
                if (byId.ContainsKey(row.id) == false && File.Exists($"{assetFolder}/{row.id}_{SafeFileName(row.namekeycode)}.asset"))
                {
                    throw new IOException($"Unrelated asset already occupies the destination for ID {row.id}.");
                }
            }

            EnsureFolder(assetFolder);
            var dataById = data.ToDictionary(x => x.id);
            var report = new RecordImportReport();
            foreach (var row in info.OrderBy(x => x.id))
            {
                var payload = dataById[row.id];
                bool found = byId.TryGetValue(row.id, out var asset);
                if (found == false)
                {
                    asset = ScriptableObject.CreateInstance(recordType);
                }

                var so = new SerializedObject(asset);
                bool protectedCommon = found && preserveCommon && row.id >= 1 && row.id <= 7;
                bool ready = payload.isImplemented;
                if (payload.linkedPassiveSkillID > 0 && passiveIds.Contains(payload.linkedPassiveSkillID) == false)
                {
                    ready = false;
                    report.warnings.Add($"Record {row.id}: passive {payload.linkedPassiveSkillID} is missing; asset remains pending.");
                }
                if (protectedCommon == false)
                {
                    Required(so, "id").intValue = row.id;
                    Required(so, "recordName").stringValue = row.namekeycode;
                    Required(so, "description").stringValue = row.description;
                    Required(so, "type").intValue = payload.recordType;
                    Required(so, "rarity").intValue = payload.rarity;
                    SetEnum(Required(so, "targetFilter"), payload.targetFilter);
                    if (string.IsNullOrEmpty(row.iconPath) == false)
                    {
                        var icon = AssetDatabase.LoadAssetAtPath<Sprite>(row.iconPath);
                        if (icon == null)
                        {
                            report.warnings.Add($"Record {row.id}: icon not found at {row.iconPath}.");
                        }
                        else
                        {
                            Required(so, "icon").objectReferenceValue = icon;
                        }
                    }
                }
                Required(so, "linkedPassiveSkillID").intValue = payload.linkedPassiveSkillID;
                Required(so, "isImplemented").boolValue = ready;
                var note = so.FindProperty("importNote");
                if (note != null)
                {
                    note.stringValue = ready ? "" : payload.importNote ?? "Effect implementation pending";
                }

                if (payload.stats != null)
                {
                    WriteStats(Required(so, "Stats"), payload.stats);
                }

                if (payload.skills != null)
                {
                    WriteSkills(Required(so, "Skills"), payload.skills);
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                if (found)
                {
                    EditorUtility.SetDirty(asset);
                    report.updated++;
                }
                else
                {
                    AssetDatabase.CreateAsset(asset, $"{assetFolder}/{row.id}_{SafeFileName(row.namekeycode)}.asset");
                    report.created++;
                }
                if (ready == false)
                {
                    report.pending++;
                }

                report.paths.Add(AssetDatabase.GetAssetPath(asset));
            }
            AssetDatabase.SaveAssets();
            return report;
        }
        public static void Validate(RecordImportInfo[] info, RecordImportData[] data)
        {
            if (info.Any(x => x == null) || data.Any(x => x == null))
            {
                throw new InvalidDataException("Null record row.");
            }

            if (info.Select(x => x.id).Distinct().Count() != info.Length || data.Select(x => x.id).Distinct().Count() != data.Length)
            {
                throw new InvalidDataException("Duplicate record ID in JSON.");
            }

            if (new HashSet<int>(info.Select(x => x.id)).SetEquals(data.Select(x => x.id)) == false)
            {
                throw new InvalidDataException("Info/data record IDs do not match.");
            }

            foreach (var row in info)
            {
                if (string.IsNullOrWhiteSpace(row.namekeycode) || string.IsNullOrWhiteSpace(row.description))
                {
                    throw new InvalidDataException($"Record {row.id} has an empty localization key.");
                }
            }

            foreach (var row in data)
            {
                if (row.recordType < 0 || row.recordType > 4 || row.rarity < 0 || row.rarity > 4)
                {
                    throw new InvalidDataException($"Record {row.id} has an invalid type/rarity.");
                }

                if (row.targetFilter != "ALL" && row.targetFilter != "Shooter")
                {
                    throw new InvalidDataException($"Record {row.id} has an invalid target filter.");
                }

                foreach (var stat in row.stats ?? Array.Empty<RecordImportStat>())
                {
                    if (stat == null || StatIndex(stat.stat) < 1 || stat.calcType < 0 || stat.calcType > 1 || float.IsNaN(stat.value) || float.IsInfinity(stat.value))
                    {
                        throw new InvalidDataException($"Record {row.id} has an invalid stat payload.");
                    }
                }

                foreach (var skill in row.skills ?? Array.Empty<RecordImportSkill>())
                {
                    if (skill == null || skill.skillID <= 0 || Array.IndexOf(ModifierNames, skill.modifier) < 0 || Array.IndexOf(OperationNames, skill.operation) < 0 || float.IsNaN(skill.value) || float.IsInfinity(skill.value))
                    {
                        throw new InvalidDataException($"Record {row.id} has an invalid skill payload.");
                    }
                }
            }
        }
        public static int StatIndex(string stat)
        {
            switch (stat)
            {
                case "ATK":
                case "ATTACK":
                    return 1;
                case "DEF":
                case "DEFENSE":
                    return 2;
                case "ASPD":
                case "ATTACKSPEED":
                    return 3;
                case "SPD":
                case "MOVESPEED":
                    return 4;
                case "CRIT_RATIO":
                    return 5;
                case "CRIT_DMG":
                    return 6;
                case "HP":
                case "HEALTH":
                    return 7;
                case "HP_REGEN":
                case "HEALTH_REGEN":
                    return 8;
                default:
                    return -1;
            }
        }
        static readonly string[] ModifierNames =
        {
            "Damage",
            "Cooldown",
            "Range",
            "ProjectileCount",
            "ProjectileSpeed",
            "AttackCount",
            "Duration",
            "ExplosionRadius",
            "CriticalChance",
            "CriticalDamage",
            "ManaCost"
        };
        static readonly string[] OperationNames =
        {
            "Add",
            "Multiply",
            "Override"
        };
        static void WriteStats(SerializedProperty list, RecordImportStat[] rows)
        {
            list.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++)
            {
                var item = list.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("Status").intValue = StatIndex(rows[i].stat);
                item.FindPropertyRelative("ValueType").intValue = rows[i].calcType;
                item.FindPropertyRelative("Value").floatValue = rows[i].value;
            }
        }
        static void WriteSkills(SerializedProperty list, RecordImportSkill[] rows)
        {
            list.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++)
            {
                var item = list.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("SkillID").intValue = rows[i].skillID;
                item.FindPropertyRelative("Modifier").intValue = Array.IndexOf(ModifierNames, rows[i].modifier);
                item.FindPropertyRelative("Operation").intValue = Array.IndexOf(OperationNames, rows[i].operation);
                item.FindPropertyRelative("Value").floatValue = rows[i].value;
            }
        }
        static void SetEnum(SerializedProperty field, string value)
        {
            int index = Array.IndexOf(field.enumNames, value);
            if (index < 0)
            {
                throw new InvalidDataException($"Unknown enum value {value} for {field.propertyPath}.");
            }

            field.enumValueIndex = index;
        }
        static SerializedProperty Required(SerializedObject so, string name)
        {
            return so.FindProperty(name) ?? throw new InvalidOperationException($"{so.targetObject.GetType().FullName} requires serialized field '{name}'.");
        }
        static T Read<T>(string path) where T : class
        {
            return JsonUtility.FromJson<T>(File.ReadAllText(path)) ?? throw new InvalidDataException(path);
        }
        public static Type ResolveType(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).FirstOrDefault(t => t != null);
        }
        static ScriptableObject[] FindAssets(Type type, string folder)
        {
            return AssetDatabase.FindAssets("t:" + type.Name, new[]
            {
                folder
            }).Select(g => AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(g), type) as ScriptableObject).Where(a => a != null).ToArray();
        }
        static string SafeFileName(string value)
        {
            return string.Concat(value.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        }
        static string ValidateAssetFolder(string folder)
        {
            folder = folder.Replace('\\', '/').TrimEnd('/');
            string full = Path.GetFullPath(folder), assets = Path.GetFullPath("Assets") + Path.DirectorySeparatorChar;
            if (full.StartsWith(assets, StringComparison.OrdinalIgnoreCase) == false)
            {
                throw new ArgumentException("Destination must be inside Assets.");
            }

            return folder;
        }
        static void EnsureFolder(string folder)
        {
            var parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (AssetDatabase.IsValidFolder(next) == false)
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
        public static int RegisterAddressables(IEnumerable<string> paths, string label = "RecordData")
        {
            var settings = ResolveType("UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject")?.GetProperty("Settings", BindingFlags.Static | BindingFlags.Public)?.GetValue(null);
            if (settings == null)
            {
                return 0;
            }

            var group = settings.GetType().GetProperty("DefaultGroup")?.GetValue(settings);
            var create = settings.GetType().GetMethods().First(m => m.Name == "CreateOrMoveEntry" && m.GetParameters().Length == 4);
            int count = 0;
            foreach (string path in paths)
            {
                var find = settings.GetType().GetMethods().First(m => m.Name == "FindAssetEntry" && m.GetParameters().Length == 2);
                string guid = AssetDatabase.AssetPathToGUID(path);
                var entry = find.Invoke(settings, new object[]
                {
                    guid,
                    false
                }) ?? create.Invoke(settings, new[]
                {
                    (object)guid,
                    group,
                    false,
                    false
                });
                entry.GetType().GetMethods().First(m => m.Name == "SetLabel" && m.GetParameters().Length == 4).Invoke(entry, new object[]
                {
                    label,
                    true,
                    true,
                    false
                });
                count++;
            }
            EditorUtility.SetDirty((Object)settings);
            AssetDatabase.SaveAssets();
            return count;
        }
        public static int BindPrefab(string prefabPath, IEnumerable<string> paths, string componentTypeName = "RecordManager", string listName = "records")
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (root == null)
            {
                return 0;
            }

            int added = 0;
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null || component.GetType().FullName != componentTypeName)
                {
                    continue;
                }

                var so = new SerializedObject(component);
                var list = Required(so, listName);
                var known = new HashSet<Object>(Enumerable.Range(0, list.arraySize).Select(i => list.GetArrayElementAtIndex(i).objectReferenceValue));
                foreach (string path in paths)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                    if (asset == null || known.Add(asset) == false)
                    {
                        continue;
                    }

                    int index = list.arraySize++;
                    list.GetArrayElementAtIndex(index).objectReferenceValue = asset;
                    added++;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(component);
            }
            if (added > 0)
            {
                PrefabUtility.SavePrefabAsset(root);
            }

            return added;
        }
    }
}
