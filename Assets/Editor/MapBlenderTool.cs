using UnityEngine;
using UnityEditor;
using System.IO;

public class MapBlenderTool : EditorWindow
{
    public enum GenerationMode { Patches, Paths }

    [Header("Target Settings")]
    public GameObject targetPlane;
    public int resolution = 1024;

    [Header("Texture Palette")]
    public Texture2D baseTexture;
    public Texture2D layer1Texture;
    public Texture2D layer2Texture;

    [Header("Generation Rules")]
    public GenerationMode mapMode = GenerationMode.Paths;
    public int seed = 12345;
    [Range(0.1f, 20f)] public float noiseScale = 5f;
    [Range(0.001f, 1f)] public float blendSoftness = 0.1f;
    [Range(0f, 1f)] public float coverage = 0.5f;

    [Header("Workflow")]
    public bool autoPreview = true;

    private Texture2D generatedSplatMap;

    [MenuItem("Tools/Map Blender Tool")]
    public static void ShowWindow() => GetWindow<MapBlenderTool>("Map Blender");

    private void OnGUI()
    {
        GUILayout.Label("개선된 절차적 지형 블렌더 (Auto Material)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();

        targetPlane = (GameObject)EditorGUILayout.ObjectField("Target Plane", targetPlane, typeof(GameObject), true);
        resolution = EditorGUILayout.IntSlider("Resolution", resolution, 64, 2048);

        baseTexture = (Texture2D)EditorGUILayout.ObjectField("Base (전체 배경)", baseTexture, typeof(Texture2D), false);
        layer1Texture = (Texture2D)EditorGUILayout.ObjectField("Layer 1 (주요 지형)", layer1Texture, typeof(Texture2D), false);
        layer2Texture = (Texture2D)EditorGUILayout.ObjectField("Layer 2 (세부 포인트)", layer2Texture, typeof(Texture2D), false);

        mapMode = (GenerationMode)EditorGUILayout.EnumPopup("Generation Mode", mapMode);

        GUILayout.BeginHorizontal();
        seed = EditorGUILayout.IntField("Seed", seed);
        if (GUILayout.Button("🎲 Random")) seed = Random.Range(0, 99999);
        GUILayout.EndHorizontal();

        noiseScale = EditorGUILayout.Slider("Noise Scale", noiseScale, 0.1f, 20f);
        blendSoftness = EditorGUILayout.Slider("Edge Softness", blendSoftness, 0.001f, 1f);
        coverage = EditorGUILayout.Slider("Coverage", coverage, 0f, 1f);

        autoPreview = EditorGUILayout.Toggle("Auto Preview", autoPreview);

        if (EditorGUI.EndChangeCheck() && autoPreview && targetPlane != null) GenerateSplatMap();

        EditorGUILayout.Space();

        // 💡 [수정됨] 버튼을 3개로 나누고 가로 폭을 맞췄습니다.
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("미리보기 (Apply)", GUILayout.Height(35))) GenerateSplatMap();
        if (GUILayout.Button("마스크만 저장 (Save PNG)", GUILayout.Height(35))) SaveSplatMapToPNG();
        GUILayout.EndHorizontal();

        // 💡 [추가됨] 대망의 자동 머티리얼 생성 버튼! (눈에 띄게 초록색으로)
        GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
        if (GUILayout.Button("✨ 머티리얼 자동 생성 & 적용 (Create Material) ✨", GUILayout.Height(40)))
        {
            CreateAndSaveMaterial();
        }
        GUI.backgroundColor = Color.white;
    }

    private void GenerateSplatMap()
    {
        if (targetPlane == null) return;
        if (generatedSplatMap != null) DestroyImmediate(generatedSplatMap);

        generatedSplatMap = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[resolution * resolution];

        float softRange = blendSoftness * 0.5f;
        float threshold = 1f - coverage;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float nx = (float)x / resolution * noiseScale + seed;
                float ny = (float)y / resolution * noiseScale + seed;

                float noise1 = Mathf.PerlinNoise(nx, ny);
                if (mapMode == GenerationMode.Paths) noise1 = 1f - Mathf.Abs(noise1 - 0.5f) * 2f;
                float rMask = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(threshold - softRange, threshold + softRange, noise1));

                float noise2 = Mathf.PerlinNoise(nx + 500.5f, ny + 500.5f);
                float gMask = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.7f - softRange, 0.7f + softRange, noise2));

                pixels[y * resolution + x] = new Color(rMask, gMask, 0, 1);
            }
        }

        generatedSplatMap.SetPixels(pixels);
        generatedSplatMap.Apply();
        ApplyToMaterial();
    }

    private void ApplyToMaterial()
    {
        var renderer = targetPlane.GetComponent<MeshRenderer>();
        if (renderer == null || renderer.sharedMaterial == null) return;

        renderer.sharedMaterial.SetTexture("_SplatMap", generatedSplatMap);
        if (baseTexture) renderer.sharedMaterial.SetTexture("_BaseTex", baseTexture);
        if (layer1Texture) renderer.sharedMaterial.SetTexture("_Layer1Tex", layer1Texture);
        if (layer2Texture) renderer.sharedMaterial.SetTexture("_Layer2Tex", layer2Texture);
    }

    // 💡 [수정됨] 머티리얼에서 이 경로를 가져다 써야 하므로 string으로 경로를 반환하도록 변경
    private string SaveSplatMapToPNG()
    {
        if (generatedSplatMap == null) GenerateSplatMap();
        if (generatedSplatMap == null) return null;

        string folderPath = "Assets/ProceduralMaps";
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        string timeStamp = System.DateTime.Now.ToString("MMdd_HHmm");
        string filePath = Path.Combine(folderPath, $"Splat_{seed}_{timeStamp}.png");

        File.WriteAllBytes(filePath, generatedSplatMap.EncodeToPNG());
        AssetDatabase.Refresh(); // 파일 시스템 갱신

        Debug.Log($"<color=white>스플랫 맵 저장 완료!</color> 경로: {filePath}");
        return filePath;
    }

    // 💡 [추가됨] 머티리얼을 세팅하고 저장하는 핵심 기능!
    private void CreateAndSaveMaterial()
    {
        // 1. 메모리에 있는 마스크 이미지를 파일(.png)로 먼저 구워냅니다.
        // (파일로 저장된 이미지 에셋이 아니면 머티리얼에 영구적으로 연결할 수 없기 때문입니다.)
        string splatPath = SaveSplatMapToPNG();
        if (string.IsNullOrEmpty(splatPath)) return;

        // 2. 방금 구운 PNG 파일을 유니티 에셋(Texture2D)으로 불러옵니다.
        Texture2D savedSplatTex = AssetDatabase.LoadAssetAtPath<Texture2D>(splatPath);

        // 3. 우리가 만든 셰이더를 찾습니다. 
        // 🚨 만약 셰이더 이름이 "Custom/MapBlender"가 아니라면 꼭 개발자님 셰이더 이름으로 바꿔주세요!
        Shader shader = Shader.Find("Custom/MapBlender");
        if (shader == null)
        {
            Debug.LogError("셰이더를 찾을 수 없습니다! 셰이더 이름이 'Custom/MapBlender'가 맞는지 확인해주세요.");
            return;
        }

        // 4. 새 머티리얼을 만들고 텍스처들을 몽땅 집어넣습니다.
        Material newMat = new Material(shader);
        newMat.SetTexture("_SplatMap", savedSplatTex);
        if (baseTexture) newMat.SetTexture("_BaseTex", baseTexture);
        if (layer1Texture) newMat.SetTexture("_Layer1Tex", layer1Texture);
        if (layer2Texture) newMat.SetTexture("_Layer2Tex", layer2Texture);

        // 보너스: 타일링 값도 20으로 기본 세팅해 줍니다.
        newMat.SetFloat("_Tiling", 20f);

        // 5. 머티리얼을 폴더에 저장합니다.
        string matFolder = "Assets/ProceduralMaps/Materials";
        if (!Directory.Exists(matFolder)) Directory.CreateDirectory(matFolder);

        string timeStamp = System.DateTime.Now.ToString("MMdd_HHmm");
        string matPath = $"{matFolder}/Mat_Terrain_{seed}_{timeStamp}.mat";

        AssetDatabase.CreateAsset(newMat, matPath);
        AssetDatabase.SaveAssets();

        // 6. 타겟 플레인에 완성된 머티리얼을 씌워줍니다.
        if (targetPlane != null)
        {
            targetPlane.GetComponent<MeshRenderer>().sharedMaterial = newMat;
        }

        // 유니티 프로젝트 창에서 방금 생성된 머티리얼 파일을 노란색으로 반짝거리게 강조해 줍니다.
        EditorGUIUtility.PingObject(newMat);

        Debug.Log($"<color=green>✨ 머티리얼 원스톱 생성 및 적용 완료!</color> 경로: {matPath}");
    }
}