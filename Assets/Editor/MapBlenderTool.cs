using UnityEngine;
using UnityEditor;
using System.IO;

public class MapBlenderTool : EditorWindow
{
    [Header("Target Settings")]
    public GameObject targetPlane;
    public int resolution = 512; // 스플랫 맵 텍스처 해상도

    [Header("Texture Palette")]
    public Texture2D baseTexture;   // 흙
    public Texture2D layer1Texture; // 풀 1
    public Texture2D layer2Texture; // 풀 2 (옵션)

    [Header("Generation Rules")]
    public int seed = 12345;
    public float noiseScale = 5f;
    [Range(0.01f, 1f)] public float blendSoftness = 0.5f;
    [Range(0f, 1f)] public float coverage = 0.5f;

    [Header("Workflow")]
    public bool autoPreview = true;

    private Texture2D generatedSplatMap;

    [MenuItem("Tools/Map Blender Tool")]
    public static void ShowWindow()
    {
        GetWindow<MapBlenderTool>("Map Blender");
    }

    private void OnGUI()
    {
        GUILayout.Label("절차적 지형 블렌더 (Procedural Terrain Blender)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();

        // 1. 타겟 설정
        EditorGUILayout.LabelField("1. Target Settings", EditorStyles.boldLabel);
        targetPlane = (GameObject)EditorGUILayout.ObjectField("Target Plane", targetPlane, typeof(GameObject), true);
        resolution = EditorGUILayout.IntSlider("Splat Map Resolution", resolution, 64, 2048);
        EditorGUILayout.Space();

        // 2. 텍스처 팔레트
        EditorGUILayout.LabelField("2. Texture Palette", EditorStyles.boldLabel);
        baseTexture = (Texture2D)EditorGUILayout.ObjectField("Base (Dirt)", baseTexture, typeof(Texture2D), false);
        layer1Texture = (Texture2D)EditorGUILayout.ObjectField("Layer 1 (Grass)", layer1Texture, typeof(Texture2D), false);
        layer2Texture = (Texture2D)EditorGUILayout.ObjectField("Layer 2 (Detail)", layer2Texture, typeof(Texture2D), false);
        EditorGUILayout.Space();

        // 3. 생성 규칙
        EditorGUILayout.LabelField("3. Generation Rules", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        seed = EditorGUILayout.IntField("Seed", seed);
        if (GUILayout.Button("🎲 Random", GUILayout.Width(80)))
        {
            seed = Random.Range(0, 99999);
            GUI.FocusControl(null); // 포커스 해제
        }
        GUILayout.EndHorizontal();

        noiseScale = EditorGUILayout.Slider("Noise Scale", noiseScale, 0.1f, 20f);
        blendSoftness = EditorGUILayout.Slider("Blend Softness", blendSoftness, 0.01f, 1f);
        coverage = EditorGUILayout.Slider("Coverage", coverage, 0f, 1f);
        EditorGUILayout.Space();

        // 4. 워크플로우 (버튼 및 옵션)
        EditorGUILayout.LabelField("4. Workflow", EditorStyles.boldLabel);
        autoPreview = EditorGUILayout.Toggle("Auto Preview", autoPreview);

        // 변경사항이 감지되었고, Auto Preview가 켜져있다면 즉시 갱신
        bool isChanged = EditorGUI.EndChangeCheck();
        if (isChanged && autoPreview && targetPlane != null)
        {
            GenerateSplatMap();
        }

        EditorGUILayout.Space();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("적용 (Apply / Redo)", GUILayout.Height(40)))
        {
            GenerateSplatMap();
        }
        if (GUILayout.Button("스플랫 맵 저장 (Save to PNG)", GUILayout.Height(40)))
        {
            SaveSplatMapToPNG();
        }
        GUILayout.EndHorizontal();
    }

    private void GenerateSplatMap()
    {
        if (targetPlane == null)
        {
            Debug.LogWarning("타겟 플레인을 먼저 등록해주세요!");
            return;
        }

        // 기존 텍스처 메모리 해제
        if (generatedSplatMap != null) DestroyImmediate(generatedSplatMap);
        
        generatedSplatMap = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[resolution * resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float xCoord = (float)x / resolution * noiseScale + seed;
                float yCoord = (float)y / resolution * noiseScale + seed;

                // 펄린 노이즈 생성
                float noise = Mathf.PerlinNoise(xCoord, yCoord);

                // Coverage와 Softness를 이용해 마스크 값 계산 (Smoothstep 활용)
                float lowerBound = 1f - coverage - (blendSoftness / 2f);
                float upperBound = 1f - coverage + (blendSoftness / 2f);
                float rChannel = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(lowerBound, upperBound, noise));

                // Layer 2용 노이즈 (약간 다른 오프셋으로 생성)
                float noise2 = Mathf.PerlinNoise(xCoord + 100, yCoord + 100);
                float gChannel = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 1.0f, noise2)) * rChannel; // 풀 위에만 피어나도록

                pixels[y * resolution + x] = new Color(rChannel, gChannel, 0, 1);
            }
        }

        generatedSplatMap.SetPixels(pixels);
        generatedSplatMap.Apply();

        ApplyToMaterial();
    }

    private void ApplyToMaterial()
    {
        MeshRenderer renderer = targetPlane.GetComponent<MeshRenderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            // 💡 셰이더의 프로퍼티 이름과 일치해야 합니다!
            renderer.sharedMaterial.SetTexture("_SplatMap", generatedSplatMap);
            if (baseTexture) renderer.sharedMaterial.SetTexture("_BaseTex", baseTexture);
            if (layer1Texture) renderer.sharedMaterial.SetTexture("_Layer1Tex", layer1Texture);
            if (layer2Texture) renderer.sharedMaterial.SetTexture("_Layer2Tex", layer2Texture);
        }
    }

    private void SaveSplatMapToPNG()
    {
        if (generatedSplatMap == null) GenerateSplatMap();
        if (generatedSplatMap == null) return;

        string folderPath = "Assets/ProceduralMaps";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string fileName = $"SplatMap_{seed}_{System.DateTime.Now:MMdd_HHmm}.png";
        string filePath = Path.Combine(folderPath, fileName);

        byte[] bytes = generatedSplatMap.EncodeToPNG();
        File.WriteAllBytes(filePath, bytes);

        AssetDatabase.Refresh(); // 프로젝트 창 새로고침
        Debug.Log($"<color=green>스플랫 맵 저장 완료!</color> 경로: {filePath}");
    }
}