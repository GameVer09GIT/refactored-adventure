using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menu công cụ tự động hóa thiết lập Model con ma, Sửa màu hồng URP, Prefab, Jumpscare 3s và UI Màn hình chết trong Unity Editor.
/// </summary>
[InitializeOnLoad]
public class GhostSetupUtility : EditorWindow
{
    static GhostSetupUtility()
    {
        EditorApplication.delayCall += AutoInit;
    }

    private static void AutoInit()
    {
        ConfigureTextures();
        FixAllGhostsInActiveScene();
    }

    [MenuItem("Ghost AI/Cai Dat Nhanh (Setup All In 1 Click)", false, 1)]
    public static void SetupAll()
    {
        ConfigureTextures();
        CreateMaterials();
        CreateGhostPrefab();
        FixAllGhostsInActiveScene();
        SetupSceneObjects();
        Debug.Log("<color=green>[Ghost AI] ĐÃ HOÀN TẤT CÀI ĐẶT TOÀN BỘ: Đã sửa màu hồng, thiết lập Jumpscare 3 giây, âm thanh tiếng thét, UI DeathScreen và NavMeshSurface!</color>");
    }

    [MenuItem("Ghost AI/1. Sua Mau Hong Con Ma (Fix Pink Material)", false, 10)]
    public static void FixPinkGhostMaterialsMenu()
    {
        CreateMaterials();
        FixAllGhostsInActiveScene();
        Debug.Log("<color=green>[Ghost AI] Đã sửa triệt để màu hồng cho tất cả con ma trong Scene và Prefab!</color>");
    }

    /// <summary>
    /// Cấu hình ảnh ma kinh dị và ảnh YOU DIED thành Sprite (2D and UI)
    /// </summary>
    public static void ConfigureTextures()
    {
        string path1 = "Assets/Ghost/Textures/anh-ma-kinh-di-30.jpg";
        TextureImporter ti1 = AssetImporter.GetAtPath(path1) as TextureImporter;
        if (ti1 != null && ti1.textureType != TextureImporterType.Sprite)
        {
            ti1.textureType = TextureImporterType.Sprite;
            ti1.spriteImportMode = SpriteImportMode.Single;
            ti1.alphaIsTransparency = false;
            ti1.SaveAndReimport();
        }

        string path2 = "Assets/Ghost/Textures/you-died.jpg";
        TextureImporter ti2 = AssetImporter.GetAtPath(path2) as TextureImporter;
        if (ti2 != null && ti2.textureType != TextureImporterType.Sprite)
        {
            ti2.textureType = TextureImporterType.Sprite;
            ti2.spriteImportMode = SpriteImportMode.Single;
            ti2.alphaIsTransparency = false;
            ti2.SaveAndReimport();
        }
    }

    [MenuItem("Ghost AI/2. Tao Material URP Con Ma", false, 11)]
    public static void CreateMaterials()
    {
        Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLitShader == null)
        {
            urpLitShader = Shader.Find("Standard");
        }

        // 1. Tạo Material vải bao tải (M_GhostCloth)
        Material clothMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Ghost/Materials/M_GhostCloth.mat");
        if (clothMat == null)
        {
            clothMat = new Material(urpLitShader);
            clothMat.name = "M_GhostCloth";
            AssetDatabase.CreateAsset(clothMat, "Assets/Ghost/Materials/M_GhostCloth.mat");
        }
        else
        {
            clothMat.shader = urpLitShader;
        }

        Texture2D burlapTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Ghost/Textures/burlap_cloth.jpg");
        if (burlapTex != null)
        {
            clothMat.SetTexture("_BaseMap", burlapTex);
        }
        clothMat.SetColor("_BaseColor", new Color(0.85f, 0.83f, 0.80f, 1f));
        clothMat.SetFloat("_Smoothness", 0.08f);
        clothMat.SetFloat("_Cull", 0f); // Double-sided rendering cho vải
        EditorUtility.SetDirty(clothMat);

        // 2. Tạo Material hốc mắt đen sâu (M_GhostEyes)
        Material eyesMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Ghost/Materials/M_GhostEyes.mat");
        if (eyesMat == null)
        {
            eyesMat = new Material(urpLitShader);
            eyesMat.name = "M_GhostEyes";
            AssetDatabase.CreateAsset(eyesMat, "Assets/Ghost/Materials/M_GhostEyes.mat");
        }
        else
        {
            eyesMat.shader = urpLitShader;
        }

        eyesMat.SetColor("_BaseColor", new Color(0.02f, 0.02f, 0.02f, 1f));
        eyesMat.SetFloat("_Smoothness", 0.0f);
        EditorUtility.SetDirty(eyesMat);

        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Quét và sửa vật liệu cho tất cả con ma trong scene đang mở
    /// </summary>
    public static void FixAllGhostsInActiveScene()
    {
        Material clothMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Ghost/Materials/M_GhostCloth.mat");
        Material eyesMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Ghost/Materials/M_GhostEyes.mat");

        if (clothMat == null || eyesMat == null) return;

        // Quét các GameObject có tên chứa Ghost hoặc có component GhostAI
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjects)
        {
            if (obj == null) continue;

            bool isGhost = obj.name.ToLower().Contains("ghost") || obj.GetComponent<GhostAI>() != null;
            if (isGhost)
            {
                // Thêm component GhostMaterialFixer nếu thiếu
                if (obj.GetComponent<GhostMaterialFixer>() == null)
                {
                    obj.AddComponent<GhostMaterialFixer>();
                }

                // Gán vật liệu chuẩn vào tất cả MeshRenderer con
                MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var r in renderers)
                {
                    r.sharedMaterials = new Material[] { clothMat, eyesMat };
                    EditorUtility.SetDirty(r);
                }

                EditorUtility.SetDirty(obj);
            }
        }
    }

    [MenuItem("Ghost AI/3. Tao Prefab Con Ma Hoan Chinh", false, 12)]
    public static void CreateGhostPrefab()
    {
        GameObject ghostMeshObj = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Ghost/ghost.obj");
        if (ghostMeshObj == null)
        {
            Debug.LogError("[Ghost AI] Không tìm thấy file Assets/Ghost/ghost.obj!");
            return;
        }

        GameObject ghostRoot = new GameObject("Ghost");

        // 1. NavMeshAgent
        NavMeshAgent agent = ghostRoot.AddComponent<NavMeshAgent>();
        agent.acceleration = 12f;
        agent.angularSpeed = 240f;
        agent.stoppingDistance = 1.0f;
        agent.radius = 0.4f;
        agent.height = 2.0f;
        agent.speed = 1.5f;

        // 2. Collider
        CapsuleCollider col = ghostRoot.AddComponent<CapsuleCollider>();
        col.radius = 0.4f;
        col.height = 2.0f;
        col.center = new Vector3(0f, 1.0f, 0f);
        col.isTrigger = true;

        // 3. AudioSource 3D
        AudioSource audio = ghostRoot.AddComponent<AudioSource>();
        audio.spatialBlend = 1.0f;
        audio.playOnAwake = false;
        audio.rolloffMode = AudioRolloffMode.Logarithmic;
        audio.minDistance = 2.0f;
        audio.maxDistance = 25.0f;

        // 4. GhostMaterialFixer (Đảm bảo không bao giờ bị hồng)
        ghostRoot.AddComponent<GhostMaterialFixer>();

        // 5. Visual Child
        GameObject visualObj = Object.Instantiate(ghostMeshObj, ghostRoot.transform);
        visualObj.name = "GhostVisual";
        visualObj.transform.localPosition = Vector3.zero;
        visualObj.transform.localRotation = Quaternion.identity;

        Material clothMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Ghost/Materials/M_GhostCloth.mat");
        Material eyesMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Ghost/Materials/M_GhostEyes.mat");

        MeshRenderer[] renderers = visualObj.GetComponentsInChildren<MeshRenderer>();
        foreach (var r in renderers)
        {
            if (clothMat != null && eyesMat != null)
            {
                r.sharedMaterials = new Material[] { clothMat, eyesMat };
            }
        }

        // 6. GhostFloat
        ghostRoot.AddComponent<GhostFloat>();

        // 7. GhostAI
        ghostRoot.AddComponent<GhostAI>();

        // Lưu Prefab
        string prefabPath = "Assets/Ghost/Prefabs/Ghost.prefab";
        PrefabUtility.SaveAsPrefabAsset(ghostRoot, prefabPath);
        Object.DestroyImmediate(ghostRoot);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Ghost AI] Đã tạo thành công Prefab tại: " + prefabPath);
    }

    [MenuItem("Ghost AI/4. Cai Dat UI Jumpscare & DeathScreen Vao Scene", false, 13)]
    public static void SetupSceneObjects()
    {
        // 1. Đảm bảo cấu hình Texture & Audio
        ConfigureTextures();
        AssetDatabase.ImportAsset("Assets/Ghost/Audio/jumpscare_scream.wav", ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/Ghost/Resources/jumpscare_scream.wav", ImportAssetOptions.ForceUpdate);

        // 2. Tạo hoặc tìm DeathScreenManager trong Scene
        DeathScreenManager manager = Object.FindFirstObjectByType<DeathScreenManager>();
        if (manager == null)
        {
            GameObject mgrObj = new GameObject("DeathScreenManager");
            manager = mgrObj.AddComponent<DeathScreenManager>();
            Debug.Log("[Ghost AI] Đã tạo DeathScreenManager trong Scene!");
        }

        // 3. Xây dựng mới hoàn toàn UI ngay chính giữa màn hình
        manager.LoadTexturesAndAudio();
        manager.BuildCanvasAndUI();
        EditorUtility.SetDirty(manager);

        // 4. Thêm NavMeshSurface vào scene nếu chưa có
        NavMeshSurface existingSurface = Object.FindFirstObjectByType<NavMeshSurface>();
        if (existingSurface == null)
        {
            GameObject surfaceObj = new GameObject("NavMesh_Manager");
            NavMeshSurface surface = surfaceObj.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            Debug.Log("[Ghost AI] Đã thêm NavMeshSurface vào scene!");
        }

        // 5. Sửa màu hồng và cài đặt cho con ma trong scene
        FixAllGhostsInActiveScene();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    [MenuItem("Ghost AI/5. Dat Lai Nut Bam Giua Man Hinh (Reset UI to Center)", false, 14)]
    public static void ResetUIToCenter()
    {
        DeathScreenManager manager = Object.FindFirstObjectByType<DeathScreenManager>();
        if (manager != null)
        {
            manager.LoadTexturesAndAudio();
            manager.BuildCanvasAndUI();
            EditorUtility.SetDirty(manager);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("<color=green>[Ghost AI] Đã đặt lại 2 nút HỒI SINH và THOÁT GAME vào ngay chính giữa màn hình với bố cục vàng, không còn che chữ YOU DIED!</color>");
        }
    }
}

