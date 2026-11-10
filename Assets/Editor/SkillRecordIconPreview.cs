using System;
using UnityEditor;
using UnityEngine;

internal static class SkillRecordIconPreview
{
    /// <summary>원본 텍스처 대신 선택된 스프라이트의 Unity 미리보기를 가져옵니다.</summary>
    public static Texture2D GetTexture(Sprite sprite, Action repaint)
    {
        if (sprite == null)
        {
            return null;
        }

        Texture2D preview = AssetPreview.GetAssetPreview(sprite);
        if (preview == null && AssetPreview.IsLoadingAssetPreview(sprite.GetInstanceID()))
        {
            repaint?.Invoke();
        }

        return preview;
    }

    /// <summary>레코드와 스킬의 선택된 아이콘을 이름 및 설명과 함께 표시합니다.</summary>
    public static void DrawHeader(SerializedObject asset, Action repaint)
    {
        SerializedProperty property = asset.FindProperty("skillImage") ?? asset.FindProperty("icon");
        Sprite sprite = property?.objectReferenceValue as Sprite;
        if (sprite == null)
        {
            TheWheeler.Editor.EditorLocalization.DrawPreview(asset);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(GetTexture(sprite, repaint), GUILayout.Width(64f), GUILayout.Height(64f));
            using (new EditorGUILayout.VerticalScope())
            {
                TheWheeler.Editor.EditorLocalization.DrawPreview(asset);
            }
        }
    }
}
