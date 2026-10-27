using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TheWheeler.Editor
{
    // Reads JSON directly in Edit Mode; never needs a scene or LocalizationManager.
    [InitializeOnLoad]
    public static class EditorLocalization
    {
        public const string DefaultPath = "Assets/98.Datas/stringData.json";
        [Serializable]
        private class Row
        {
            public string key, kr, en;
        }
        [Serializable]
        private class Root
        {
            public Row[] stringData;
        }
        private static Dictionary<string, string> table = new Dictionary<string, string>();
        private static DateTime stamp = DateTime.MinValue;
        private static long length = -1;
        static EditorLocalization()
        {
            EditorApplication.projectChanged += Invalidate;
        }
        public static void Invalidate()
        {
            stamp = DateTime.MinValue;
            length = -1;
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                window.Repaint();
            }
        }
        public static string Resolve(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return "";
            }

            try
            {
                var file = new FileInfo(DefaultPath);
                if (file.Exists == false)
                {
                    return key;
                }

                if (file.LastWriteTimeUtc != stamp || file.Length != length)
                {
                    var next = Parse(File.ReadAllText(DefaultPath));
                    table = next;
                    stamp = file.LastWriteTimeUtc;
                    length = file.Length;
                }
                return table.TryGetValue(key, out var value) && string.IsNullOrEmpty(value) == false ? value : key;
            }
            catch (IOException)
            {
                return key;

            }
            catch (UnauthorizedAccessException)
            {
                return key;

            }
            catch (ArgumentException)
            {
                return key;

            }
        }
        public static Dictionary<string, string> Parse(string json)
        {
            var result = new Dictionary<string, string>();
            foreach (var row in JsonUtility.FromJson<Root>(json)?.stringData ?? Array.Empty<Row>())
            {
                if (row != null && string.IsNullOrEmpty(row.key) == false && result.ContainsKey(row.key) == false)
                {
                    result.Add(row.key, row.kr);
                }
            }

            return result;
        }
        public static bool Matches(string query, int id, string key, string assetName = "")
        {
            return string.IsNullOrEmpty(query) || Contains(Resolve(key), query) || Contains(key, query) || Contains(assetName, query) || Contains(id.ToString(), query);
        }
        static bool Contains(string value, string query)
        {
            return (value ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        public static void DrawPreview(SerializedObject so)
        {
            var name = so.FindProperty("recordName") ?? so.FindProperty("skillName");
            if (name == null)
            {
                return;
            }

            var id = so.FindProperty("id");
            EditorGUILayout.LabelField(new GUIContent($"[{id?.intValue ?? 0}] {Resolve(name.stringValue)}", name.stringValue), EditorStyles.boldLabel);
            var description = so.FindProperty("description") ?? so.FindProperty("skillDescription");
            if (description != null && string.IsNullOrEmpty(description.stringValue) == false)
            {
                EditorGUILayout.LabelField(new GUIContent(Resolve(description.stringValue), description.stringValue), EditorStyles.wordWrappedLabel);
            }

            EditorGUILayout.Space(4);
        }
    }
}
