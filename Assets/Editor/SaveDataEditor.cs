#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using UnityEngine;

public static class SaveDataEditor
{
    private const string MenuPath = "Tools/Save Data/";

    // 1. 전체 삭제
    [MenuItem(MenuPath + "Clear All Save Data", false, 1)]
    public static void ClearAllSaveData()
    {
        if (EditorUtility.DisplayDialog("전체 초기화", "모든 JSON 세이브 파일을 삭제하시겠습니까?", "삭제", "취소"))
        {
            DeleteFile("mapdata.json");
            DeleteFile("stagedata.json");
            DeleteFile("charinfo.json");
            DeleteFile("invetory.json");
            DeleteFile("learnskill.json");
            Debug.Log("<color=red><b>모든 세이브 데이터가 삭제되었습니다.</b></color>");
        }
    }

    [MenuItem(MenuPath + "--- Separator ---", false, 10)] private static void Separator() { }

    // 2. 맵 & 스테이지 데이터 삭제
    [MenuItem(MenuPath + "Delete Map & Stage Data", false, 11)]
    public static void DeleteMapStage()
    {
        DeleteFile("mapdata.json");
        DeleteFile("stagedata.json");
        Debug.Log("맵과 스테이지 데이터가 삭제되었습니다.");
    }

    // 3. 캐릭터 정보 삭제
    [MenuItem(MenuPath + "Delete Character Data", false, 12)]
    public static void DeleteCharacter()
    {
        DeleteFile("charinfo.json");
        Debug.Log("캐릭터 정보 데이터가 삭제되었습니다.");
    }

    // 4. 인벤토리(아이템) 삭제
    [MenuItem(MenuPath + "Delete Inventory Data", false, 13)]
    public static void DeleteInventory()
    {
        DeleteFile("invetory.json");
        Debug.Log("인벤토리 데이터가 삭제되었습니다.");
    }

    // 5. 스킬 데이터 삭제
    [MenuItem(MenuPath + "Delete Skill Data", false, 14)]
    public static void DeleteSkill()
    {
        DeleteFile("learnskill.json");
        Debug.Log("스킬 트리 데이터가 삭제되었습니다.");
    }

    [MenuItem(MenuPath + "--- Separator ---", false, 30)] private static void Separator2() { }

    // 폴더 열기 기능 (여전히 매우 유용함)
    [MenuItem(MenuPath + "Open Save Folder", false, 31)]
    public static void OpenSaveFolder()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }

    private static void DeleteFile(string fileName)
    {
        string fullPath = Path.Combine(Application.persistentDataPath, fileName);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
#endif