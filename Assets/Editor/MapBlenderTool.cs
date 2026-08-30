using UnityEngine;
using UnityEditor;
using System;
using System.IO;

public class MapBlenderTool : EditorWindow
{
    public enum GenerationMode
    {
        Patches,
        Paths
    }
    [Header("Shader Settings")]
    public Shader mapBlenderShader;

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

    [Range(0.1f, 20f)]
    public float noiseScale = 5f;

    [Range(0.001f, 1f)]
    public float blendSoftness = 0.1f;

    [Range(0f, 1f)]
    public float coverage = 0.5f;

    [Header("Material Settings")]
    [Tooltip("비워두면 기본 이름으로 생성됩니다.")]
    public string materialName = "";

    [Tooltip("머티리얼에 적용할 텍스처 타일링")]
    public float materialBaseTiling = 0f;
    public float materialLayer1Tiling = 0f;
    public float materialLayer2Tiling = 0f;

    [Header("Workflow")]
    public bool autoPreview = true;

    private Texture2D generatedSplatMap;

    private const string BaseFolder = "Assets/ProceduralMaps";
    private const string MaterialFolder = "Assets/ProceduralMaps/Materials";

    [MenuItem("Tools/Map Blender Tool")]
    public static void ShowWindow()
    {
        GetWindow<MapBlenderTool>("Map Blender");
    }

    private void OnGUI()
    {
        GUILayout.Label(
            "개선된 절차적 지형 블렌더 (Auto Material)",
            EditorStyles.boldLabel);

        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();

        // --------------------------------------------------
        // Shader
        // --------------------------------------------------

        GUILayout.Label("Shader Settings", EditorStyles.boldLabel);

        mapBlenderShader = (Shader)EditorGUILayout.ObjectField(
            "Map Blender Shader",
            mapBlenderShader,
            typeof(Shader),
            false
        );

        EditorGUILayout.Space();


        // --------------------------------------------------
        // Target
        // --------------------------------------------------

        targetPlane = (GameObject)EditorGUILayout.ObjectField(
            "Target Plane",
            targetPlane,
            typeof(GameObject),
            true);

        resolution = EditorGUILayout.IntSlider(
            "Resolution",
            resolution,
            64,
            2048);

        EditorGUILayout.Space();

        // --------------------------------------------------
        // Texture
        // --------------------------------------------------

        baseTexture = (Texture2D)EditorGUILayout.ObjectField(
            "Base (전체 배경)",
            baseTexture,
            typeof(Texture2D),
            false);
        
        EditorGUILayout.Space();
        
        materialBaseTiling = EditorGUILayout.FloatField(
        "BaseTiling",
        materialBaseTiling);
        
        EditorGUILayout.Space();
        
        layer1Texture = (Texture2D)EditorGUILayout.ObjectField(
            "Layer 1 (주요 지형)",
            layer1Texture,
            typeof(Texture2D),
            false);
        
        EditorGUILayout.Space();
        
        materialLayer1Tiling = EditorGUILayout.FloatField(
         "Layer1Tiling",
         materialLayer1Tiling);
        
        EditorGUILayout.Space();

        layer2Texture = (Texture2D)EditorGUILayout.ObjectField(
            "Layer 2 (세부 포인트)",
            layer2Texture,
            typeof(Texture2D),
            false);

        EditorGUILayout.Space();
        
        materialLayer2Tiling = EditorGUILayout.FloatField(
         "Layer2Tiling",
         materialLayer2Tiling);

        EditorGUILayout.Space();

        // --------------------------------------------------
        // Generation
        // --------------------------------------------------

        mapMode = (GenerationMode)EditorGUILayout.EnumPopup(
            "Generation Mode",
            mapMode);

        GUILayout.BeginHorizontal();

        seed = EditorGUILayout.IntField(
            "Seed",
            seed);

        if (GUILayout.Button("🎲 Random", GUILayout.Width(100)))
        {
            seed = UnityEngine.Random.Range(0, 99999);
        }

        GUILayout.EndHorizontal();

        noiseScale = EditorGUILayout.Slider(
            "Noise Scale",
            noiseScale,
            0.1f,
            20f);

        blendSoftness = EditorGUILayout.Slider(
            "Edge Softness",
            blendSoftness,
            0.001f,
            1f);

        coverage = EditorGUILayout.Slider(
            "Coverage",
            coverage,
            0f,
            1f);

        EditorGUILayout.Space();

        // --------------------------------------------------
        // Material
        // --------------------------------------------------

        GUILayout.Label("Material Settings", EditorStyles.boldLabel);

        materialName = EditorGUILayout.TextField(
            new GUIContent(
                "Material Name",
                "비워두면 기본 이름으로 생성됩니다."),
            materialName);



        if (string.IsNullOrWhiteSpace(materialName))
        {
            EditorGUILayout.HelpBox(
                "Material Name을 비워두면 기본 이름으로 생성됩니다.",
                MessageType.Info);
        }

        EditorGUILayout.Space();

        // --------------------------------------------------
        // Workflow
        // --------------------------------------------------

        autoPreview = EditorGUILayout.Toggle(
            "Auto Preview",
            autoPreview);

        if (EditorGUI.EndChangeCheck() &&
            autoPreview &&
            targetPlane != null)
        {
            GenerateSplatMap();

            // 기존 Material이 있다면 실시간 미리보기
            ApplyPreviewToMaterial();
        }

        EditorGUILayout.Space();

        // --------------------------------------------------
        // Preview / Save
        // --------------------------------------------------

        GUILayout.BeginHorizontal();

        if (GUILayout.Button(
                "미리보기 (Apply)",
                GUILayout.Height(35)))
        {
            GenerateSplatMap();
            ApplyPreviewToMaterial();
        }

        if (GUILayout.Button(
                "마스크만 저장 (Save PNG)",
                GUILayout.Height(35)))
        {
            GenerateSplatMap();
            SaveSplatMapToPNG();
        }

        GUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // --------------------------------------------------
        // Create Material
        // --------------------------------------------------

        GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);

        if (GUILayout.Button(
                "✨ 머티리얼 생성 & Target Plane 적용 ✨",
                GUILayout.Height(40)))
        {
            CreateAndApplyMaterial();
        }

        GUI.backgroundColor = Color.white;
    }

    // ======================================================
    // Splat Map
    // ======================================================

    private void GenerateSplatMap()
    {
        if (targetPlane == null)
            return;

        if (generatedSplatMap != null)
        {
            DestroyImmediate(generatedSplatMap);
        }

        generatedSplatMap = new Texture2D(
            resolution,
            resolution,
            TextureFormat.RGBA32,
            false);

        generatedSplatMap.name = "Generated_SplatMap";

        Color[] pixels =
            new Color[resolution * resolution];

        float softRange = blendSoftness * 0.5f;
        float threshold = 1f - coverage;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float nx =
                    (float)x / resolution *
                    noiseScale +
                    seed;

                float ny =
                    (float)y / resolution *
                    noiseScale +
                    seed;

                // ------------------------------------------
                // Layer 1
                // ------------------------------------------

                float noise1 =
                    Mathf.PerlinNoise(nx, ny);

                if (mapMode == GenerationMode.Paths)
                {
                    noise1 =
                        1f -
                        Mathf.Abs(noise1 - 0.5f) * 2f;
                }

                float rMask =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.InverseLerp(
                            threshold - softRange,
                            threshold + softRange,
                            noise1));

                // ------------------------------------------
                // Layer 2
                // ------------------------------------------

                float noise2 =
                    Mathf.PerlinNoise(
                        nx + 500.5f,
                        ny + 500.5f);

                float gMask =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.InverseLerp(
                            0.7f - softRange,
                            0.7f + softRange,
                            noise2));

                pixels[y * resolution + x] =
                    new Color(
                        rMask,
                        gMask,
                        0f,
                        1f);
            }
        }

        generatedSplatMap.SetPixels(pixels);
        generatedSplatMap.Apply();
    }

    // ======================================================
    // Preview
    // ======================================================

    private void ApplyPreviewToMaterial()
    {
        if (targetPlane == null)
            return;

        MeshRenderer renderer =
            targetPlane.GetComponent<MeshRenderer>();

        if (renderer == null)
            return;

        // 기존 Material이 없으면 Preview하지 않음
        if (renderer.sharedMaterial == null)
            return;

        Material material =
            renderer.sharedMaterial;

        Undo.RecordObject(
            material,
            "Map Blender Preview");

        material.SetTexture(
            "_SplatMap",
            generatedSplatMap);

        if (baseTexture != null)
        {
            material.SetTexture(
                "_BaseTex",
                baseTexture);
        }

        if (layer1Texture != null)
        {
            material.SetTexture(
                "_Layer1Tex",
                layer1Texture);
        }

        if (layer2Texture != null)
        {
            material.SetTexture(
                "_Layer2Tex",
                layer2Texture);
        }

        material.SetFloat(
            "_BaseTiling",
            materialBaseTiling);

        material.SetFloat(
          "_Layer1Tiling",
          materialLayer1Tiling);

        material.SetFloat(
          "_Layer2Tiling",
          materialLayer2Tiling);

        EditorUtility.SetDirty(material);

        SceneView.RepaintAll();
    }

    // ======================================================
    // Save Splat Map
    // ======================================================

    private string SaveSplatMapToPNG()
    {
        if (generatedSplatMap == null)
        {
            GenerateSplatMap();
        }

        if (generatedSplatMap == null)
            return null;

        if (!Directory.Exists(BaseFolder))
        {
            Directory.CreateDirectory(BaseFolder);
        }

        string timeStamp =
            DateTime.Now.ToString("MMdd_HHmmss");

        string fileName =
            $"Splat_{seed}_{timeStamp}.png";

        string filePath =
            Path.Combine(
                BaseFolder,
                fileName);

        File.WriteAllBytes(
            filePath,
            generatedSplatMap.EncodeToPNG());

        AssetDatabase.Refresh();

        Debug.Log(
            $"<color=white>스플랫 맵 저장 완료!</color> " +
            $"{filePath}");

        return filePath;
    }

    // ======================================================
    // Material Creation
    // ======================================================

    private void CreateAndApplyMaterial()
    {
        if (targetPlane == null)
        {
            Debug.LogError(
                "Target Plane이 지정되지 않았습니다.");
            return;
        }

        // ------------------------------------------
        // 1. Splat 생성
        // ------------------------------------------

        GenerateSplatMap();

        if (generatedSplatMap == null)
        {
            Debug.LogError(
                "Splat Map 생성에 실패했습니다.");
            return;
        }

        // ------------------------------------------
        // 2. Splat 저장
        // ------------------------------------------

        string splatPath =
            SaveSplatMapToPNG();

        if (string.IsNullOrEmpty(splatPath))
            return;

        Texture2D savedSplatTex =
            AssetDatabase.LoadAssetAtPath<Texture2D>(
                splatPath);

        if (savedSplatTex == null)
        {
            Debug.LogError(
                $"Splat Texture를 불러올 수 없습니다. " +
                $"Path: {splatPath}");

            return;
        }

        // ------------------------------------------
        // 3. Shader
        // ------------------------------------------

        Shader shader = mapBlenderShader;

        if (shader == null)
        {
            Debug.LogError(
         "Map Blender Shader가 지정되지 않았습니다.");

            return;
        }

        // ------------------------------------------
        // 4. Material 이름
        // ------------------------------------------

        string finalMaterialName =
            GetMaterialName();

        string materialPath =
            Path.Combine(
                MaterialFolder,
                finalMaterialName + ".mat");

        // Unity Asset 경로 형식
        materialPath =
            materialPath.Replace("\\", "/");

        // ------------------------------------------
        // 5. 폴더 생성
        // ------------------------------------------

        if (!Directory.Exists(MaterialFolder))
        {
            Directory.CreateDirectory(
                MaterialFolder);
        }

        AssetDatabase.Refresh();

        // ------------------------------------------
        // 6. 동일 이름 방지
        // ------------------------------------------

        materialPath =
            AssetDatabase.GenerateUniqueAssetPath(
                materialPath);

        // ------------------------------------------
        // 7. Material 생성
        // ------------------------------------------

        Material newMaterial =
            new Material(shader);

        newMaterial.name =
            Path.GetFileNameWithoutExtension(
                materialPath);

        // ------------------------------------------
        // 8. Texture 적용
        // ------------------------------------------

        newMaterial.SetTexture(
            "_SplatMap",
            savedSplatTex);

        if (baseTexture != null)
        {
            newMaterial.SetTexture(
                "_BaseTex",
                baseTexture);
        }

        if (layer1Texture != null)
        {
            newMaterial.SetTexture(
                "_Layer1Tex",
                layer1Texture);
        }

        if (layer2Texture != null)
        {
            newMaterial.SetTexture(
                "_Layer2Tex",
                layer2Texture);
        }

        newMaterial.SetFloat(
            "_BaseTiling",
            materialBaseTiling);

        newMaterial.SetFloat(
            "_Layer1Tiling",
            materialLayer1Tiling);

        newMaterial.SetFloat(
            "_Layer2Tiling",
            materialLayer2Tiling);

        // ------------------------------------------
        // 9. Asset 저장
        // ------------------------------------------

        AssetDatabase.CreateAsset(
            newMaterial,
            materialPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ------------------------------------------
        // 10. Target Plane에 즉시 적용
        // ------------------------------------------

        MeshRenderer renderer =
            targetPlane.GetComponent<MeshRenderer>();

        if (renderer != null)
        {
            Undo.RecordObject(
                renderer,
                "Apply Generated Map Material");

            renderer.sharedMaterial =
                newMaterial;

            EditorUtility.SetDirty(renderer);
        }

        // ------------------------------------------
        // 11. 선택
        // ------------------------------------------

        Selection.activeObject =
            newMaterial;

        EditorGUIUtility.PingObject(
            newMaterial);

        SceneView.RepaintAll();

        Debug.Log(
            $"<color=green>" +
            $"✨ 머티리얼 생성 및 Target Plane 적용 완료!" +
            $"</color>\n" +
            $"Material: {materialPath}\n" +
            $"Target: {targetPlane.name}");
    }

    // ======================================================
    // Material Name
    // ======================================================

    private string GetMaterialName()
    {
        if (!string.IsNullOrWhiteSpace(materialName))
        {
            return SanitizeFileName(
                materialName.Trim());
        }

        // 기본 이름
        string timeStamp =
            DateTime.Now.ToString("MMdd_HHmmss");

        return $"Mat_Terrain_{seed}_{timeStamp}";
    }

    private string SanitizeFileName(string value)
    {
        foreach (char invalidChar
                 in Path.GetInvalidFileNameChars())
        {
            value =
                value.Replace(
                    invalidChar.ToString(),
                    "_");
        }

        return string.IsNullOrWhiteSpace(value)
            ? $"Mat_Terrain_{seed}"
            : value;
    }
}