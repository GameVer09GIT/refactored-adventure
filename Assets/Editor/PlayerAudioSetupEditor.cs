using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HorrorAudio.Editor
{
    /// <summary>
    /// Menu công cụ tự động hóa 1-click thiết lập toàn bộ hệ thống âm thanh cho Player trong Scene.
    /// Hỗ trợ Undo, chạy được nhiều lần mà không nhân đôi component, nạp clip, cấu hình AudioSource,
    /// quét gán FootstepSurface cho sàn gỗ và kiểm tra AudioListener.
    /// Tự động chạy khi bấm Play nếu chưa thiết lập.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayerAudioSetupEditor
    {
        static PlayerAudioSetupEditor()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.delayCall += OnEditorLoaded;
        }

        private static void OnEditorLoaded()
        {
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            GhostAI ghost = Object.FindFirstObjectByType<GhostAI>();

            bool needsSetup = false;
            if (player != null && player.GetComponent<HorrorAudio.PlayerFootsteps>() == null)
            {
                needsSetup = true;
            }
            if (ghost != null && ghost.GetComponent<GhostAmbientSound>() == null)
            {
                needsSetup = true;
            }

            if (needsSetup)
            {
                Debug.Log("<color=cyan><b>[Horror Audio] Tự động thiết lập hệ thống âm thanh cho Player & Ghost trong Scene...</b></color>");
                AutoSetupPlayerAudio();
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                PlayerController player = Object.FindFirstObjectByType<PlayerController>();
                GhostAI ghost = Object.FindFirstObjectByType<GhostAI>();

                bool needsSetup = false;
                if (player != null && player.GetComponent<HorrorAudio.PlayerFootsteps>() == null)
                {
                    needsSetup = true;
                }
                if (ghost != null && ghost.GetComponent<GhostAmbientSound>() == null)
                {
                    needsSetup = true;
                }

                if (needsSetup)
                {
                    Debug.Log("<color=cyan><b>[Horror Audio] Tự động thiết lập âm thanh trước khi Play...</b></color>");
                    AutoSetupPlayerAudio();
                }
            }
        }

        [MenuItem("Tools/Audio/Auto Setup Player Audio", false, 0)]
        public static void AutoSetupPlayerAudioMenu()
        {
            AutoSetupPlayerAudio(true);
        }

        public static void AutoSetupPlayerAudio(bool showDialog = false)
        {
            Debug.Log("<color=cyan><b>[Horror Audio] Bắt đầu tự động thiết lập hệ thống âm thanh người chơi & con ma...</b></color>");

            // 1. Kiểm tra và tự động sinh file âm thanh thủ tục nếu chưa có
            EnsureAudioClipsExist();

            // 2. Tìm đối tượng Player trong Scene
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                GameObject playerGo = GameObject.FindWithTag("Player");
                if (playerGo != null) player = playerGo.GetComponent<PlayerController>();
            }

            if (player == null)
            {
                if (showDialog) EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy đối tượng Player có chứa script PlayerController trong scene hiện tại!", "OK");
                Debug.LogError("[Horror Audio] Không tìm thấy PlayerController trong Scene!");
                return;
            }

            GameObject playerObj = player.gameObject;
            Undo.RegisterCompleteObjectUndo(playerObj, "Setup Player Audio");

            // 3. Thêm hoặc lấy các component âm thanh
            PlayerFootsteps footsteps = GetOrAddComponent<PlayerFootsteps>(playerObj);
            PlayerBreathing breathing = GetOrAddComponent<PlayerBreathing>(playerObj);
            PlayerHeartbeat heartbeat = GetOrAddComponent<PlayerHeartbeat>(playerObj);

            // 4. Tạo các child AudioSource chuyên biệt
            AudioSource footstepSource = EnsureChildAudioSource(playerObj, "FootstepAudio", false, false, 128);
            AudioSource breathingSource = EnsureChildAudioSource(playerObj, "BreathingAudio", true, false, 128);
            AudioSource gaspSource = EnsureChildAudioSource(playerObj, "GaspAudio", false, false, 128);
            AudioSource heartbeatSource = EnsureChildAudioSource(playerObj, "HeartbeatAudio", false, false, 16);

            // 5. Nạp các clip và cấu hình SerializedObject
            ConfigureFootstepsComponent(footsteps, footstepSource);
            ConfigureBreathingComponent(breathing, breathingSource, gaspSource);
            ConfigureHeartbeatComponent(heartbeat, heartbeatSource);

            // 6. Dọn FootstepSurface sai và tự động phủ các vùng FootstepZone cho nhà sàn
            int removedSurfaces = FootstepZoneManagerEditor.RemoveMisassignedParentFootstepSurfaces(false);
            FootstepZoneManagerEditor.AutoCreateHouseWoodZones();

            // 7. Thiết lập âm thanh ghê rợn cho con ma
            int ghostSetupCount = SetupGhostAmbientSounds();

            // 8. Kiểm tra AudioListener
            CheckAudioListeners();

            // 9. Đánh dấu scene dirty và lưu Scene
            Scene scene = playerObj.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);

            string reportMsg = $"ĐÃ HOÀN TẤT THIẾT LẬP ÂM THANH PLAYER & MA!\n" +
                               $"- Player: {playerObj.name}\n" +
                               $"- Đã gắn: PlayerFootsteps, PlayerBreathing, PlayerHeartbeat\n" +
                               $"- Đã tạo 4 AudioSources 2D chuyên biệt (Footstep, Breathing, Gasp, Heartbeat)\n" +
                               $"- Đã gỡ {removedSurfaces} FootstepSurface gán sai trên object cha/phi-gỗ\n" +
                               $"- Đã tạo hệ thống FootstepZones phủ toàn bộ sàn gỗ nhà sàn\n" +
                               $"- Đã thiết lập {ghostSetupCount} con ma có âm thanh ghê rợn rên rỉ/hét\n" +
                               $"- Đã lưu Scene: {(saved ? "Thành công" : "Lưu thủ công")}";

            Debug.Log($"<color=green><b>[Horror Audio] {reportMsg}</b></color>");
            if (showDialog)
            {
                EditorUtility.DisplayDialog("Cài đặt Âm thanh Hoàn tất", reportMsg, "Tuyệt vời");
            }
        }

        #region Setup Steps

        private static void EnsureAudioClipsExist()
        {
            string heartClipPath = Path.Combine(Application.dataPath, "Audio/Heartbeat/heartbeat.wav");
            if (!File.Exists(heartClipPath))
            {
                Debug.Log("[Horror Audio] Chưa thấy file âm thanh, tiến hành tổng hợp thủ tục tự động...");
                AudioProceduralGenerator.GenerateAllSounds(false);
            }

            ConfigureAudioImporter("Assets/Audio/Breathing/tiengtho_loop.wav");
            ConfigureAudioImporter("Assets/Audio/Breathing/tiengtho.mp3");
        }

        public static void ConfigureAudioImporter(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer == null) return;

            bool changed = false;
            if (!importer.forceToMono)
            {
                importer.forceToMono = true;
                changed = true;
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            if (settings.loadType != AudioClipLoadType.CompressedInMemory)
            {
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                changed = true;
            }
            if (settings.compressionFormat != AudioCompressionFormat.Vorbis)
            {
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                changed = true;
            }

            if (changed)
            {
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp == null)
            {
                comp = Undo.AddComponent<T>(go);
            }
            return comp;
        }

        private static AudioSource EnsureChildAudioSource(GameObject parent, string childName, bool loop, bool playOnAwake, int priority)
        {
            Transform childT = parent.transform.Find(childName);
            GameObject childObj;

            if (childT == null)
            {
                childObj = new GameObject(childName);
                childObj.transform.SetParent(parent.transform, false);
                Undo.RegisterCreatedObjectUndo(childObj, $"Create {childName}");
            }
            else
            {
                childObj = childT.gameObject;
            }

            AudioSource src = childObj.GetComponent<AudioSource>();
            if (src == null)
            {
                src = Undo.AddComponent<AudioSource>(childObj);
            }

            src.spatialBlend = 0f; // 2D Sound
            src.loop = loop;
            src.playOnAwake = playOnAwake;
            src.priority = priority;

            EditorUtility.SetDirty(src);
            return src;
        }

        private static void ConfigureFootstepsComponent(PlayerFootsteps footsteps, AudioSource footstepSource)
        {
            var so = new SerializedObject(footsteps);
            so.Update();

            so.FindProperty("footstepSource").objectReferenceValue = footstepSource;

            // Load dirt walk clips (1-4) - chỉ nhận clip từ Footsteps/Dirt
            LoadClipsIntoArray(so.FindProperty("dirtWalkClips"), "Assets/Audio/Footsteps/Dirt/dirt_walk_", 4);
            // Load dirt run clips (1-4) - chỉ nhận clip từ Footsteps/Dirt
            LoadClipsIntoArray(so.FindProperty("dirtRunClips"), "Assets/Audio/Footsteps/Dirt/dirt_run_", 4);
            // Load wood walk & run clips từ amthanhsango - chỉ nhận clip từ Footsteps/Wood
            ConfigureWoodClips(so);

            // Load wood creak clips: chỉ nạp nếu file tồn tại
            LoadClipsIntoArray(so.FindProperty("woodCreakClips"), "Assets/Audio/Footsteps/Wood/wood_creak_", 3);

            // LayerMask: Bỏ qua Ignore Raycast
            int groundMask = ~LayerMask.GetMask("Ignore Raycast");
            so.FindProperty("groundLayerMask").intValue = groundMask;
            so.FindProperty("rayDistance").floatValue = 1.5f;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(footsteps);
        }

        private static void ConfigureBreathingComponent(PlayerBreathing breathing, AudioSource breathingSource, AudioSource gaspSource)
        {
            var so = new SerializedObject(breathing);
            so.Update();

            so.FindProperty("breathingSource").objectReferenceValue = breathingSource;
            so.FindProperty("gaspSource").objectReferenceValue = gaspSource;

            // Nạp clip thở thực tế tiengtho_loop.wav hoặc tiengtho.mp3
            AudioClip loopClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Breathing/tiengtho_loop.wav");
            if (loopClip == null) loopClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Breathing/tiengtho.mp3");
            so.FindProperty("breathingLoopClip").objectReferenceValue = loopClip;

            // Clip hớp hơi: mảng rỗng an toàn nếu clip cũ đã bị xóa
            LoadClipsIntoArray(so.FindProperty("gaspClips"), "Assets/Audio/Breathing/exhaustion_gasp_", 2);

            SerializedProperty gainProp = so.FindProperty("clipGain");
            if (gainProp != null) gainProp.floatValue = 0.95f;

            SerializedProperty crossfadeProp = so.FindProperty("useCrossfadeLoop");
            if (crossfadeProp != null) crossfadeProp.boolValue = true;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(breathing);
        }

        private static void ConfigureHeartbeatComponent(PlayerHeartbeat heartbeat, AudioSource heartbeatSource)
        {
            var so = new SerializedObject(heartbeat);
            so.Update();

            so.FindProperty("heartbeatSource").objectReferenceValue = heartbeatSource;

            AudioClip heartClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Heartbeat/heartbeat.wav");
            so.FindProperty("heartbeatClip").objectReferenceValue = heartClip;

            int obstacleMask = ~LayerMask.GetMask("Ignore Raycast");
            so.FindProperty("obstacleMask").intValue = obstacleMask;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(heartbeat);
        }

        private static void LoadClipsIntoArray(SerializedProperty arrayProp, string pathPrefix, int count)
        {
            if (arrayProp == null) return;
            var list = new List<AudioClip>();

            for (int i = 0; i < count; i++)
            {
                string assetPath = $"{pathPrefix}{i + 1:D2}.wav";
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                if (clip != null)
                {
                    list.Add(clip);
                }
            }

            arrayProp.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++)
            {
                arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            }
        }

        private static void ConfigureWoodClips(SerializedObject so)
        {
            string amthanhsangoDir = Path.Combine(Application.dataPath, "Audio/Footsteps/Wood/amthanhsango");
            if (Directory.Exists(amthanhsangoDir))
            {
                string[] files = Directory.GetFiles(amthanhsangoDir, "wood_step_*.wav");
                if (files != null && files.Length > 0)
                {
                    System.Array.Sort(files);

                    if (files.Length == 1)
                    {
                        string rel = "Assets/Audio/Footsteps/Wood/amthanhsango/" + Path.GetFileName(files[0]);
                        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(rel);
                        SetSingleClipToArray(so.FindProperty("woodWalkClips"), clip);
                        SetSingleClipToArray(so.FindProperty("woodRunClips"), clip);
                        return;
                    }

                    // Phân chia hợp lý giữa đi bộ và chạy
                    var runIndices = new HashSet<int> { 1, 5, 6, 7, 9, 14, 17, 21, 23, 28, 29, 30, 35, 38, 42 };
                    var quietIndices = new HashSet<int> { 11, 24, 25, 33, 37, 40 };

                    var walkClips = new List<AudioClip>();
                    var runClips = new List<AudioClip>();

                    for (int i = 0; i < files.Length; i++)
                    {
                        int stepNum = i + 1;
                        string rel = "Assets/Audio/Footsteps/Wood/amthanhsango/" + Path.GetFileName(files[i]);
                        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(rel);
                        if (clip == null) continue;

                        if (runIndices.Contains(stepNum))
                        {
                            runClips.Add(clip);
                        }
                        else if (!quietIndices.Contains(stepNum))
                        {
                            walkClips.Add(clip);
                        }
                    }

                    if (walkClips.Count == 0)
                    {
                        foreach (string f in files)
                        {
                            AudioClip c = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Footsteps/Wood/amthanhsango/" + Path.GetFileName(f));
                            if (c != null) walkClips.Add(c);
                        }
                    }
                    if (runClips.Count == 0) runClips.AddRange(walkClips);

                    SetClipsToArray(so.FindProperty("woodWalkClips"), walkClips);
                    SetClipsToArray(so.FindProperty("woodRunClips"), runClips);
                    return;
                }
            }

            // Fallback nếu chưa có thư mục amthanhsango
            LoadClipsIntoArray(so.FindProperty("woodWalkClips"), "Assets/Audio/Footsteps/Wood/wood_walk_", 4);
            LoadClipsIntoArray(so.FindProperty("woodRunClips"), "Assets/Audio/Footsteps/Wood/wood_run_", 4);
        }

        private static void SetSingleClipToArray(SerializedProperty prop, AudioClip clip)
        {
            if (prop == null) return;
            prop.arraySize = 1;
            prop.GetArrayElementAtIndex(0).objectReferenceValue = clip;
        }

        private static void SetClipsToArray(SerializedProperty prop, List<AudioClip> clips)
        {
            if (prop == null) return;
            prop.arraySize = clips.Count;
            for (int i = 0; i < clips.Count; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
            }
        }

        #endregion

        #region Ghost Sound Setup

        /// <summary>
        /// Thiết lập GhostAmbientSound cho tất cả con ma trong scene
        /// </summary>
        private static int SetupGhostAmbientSounds()
        {
            GhostAI[] ghosts = Object.FindObjectsByType<GhostAI>(FindObjectsSortMode.None);
            if (ghosts == null || ghosts.Length == 0) return 0;

            int count = 0;
            foreach (GhostAI ghost in ghosts)
            {
                if (ghost == null) continue;
                GameObject ghostObj = ghost.gameObject;

                // Thêm GhostAmbientSound nếu chưa có
                GhostAmbientSound ambientSound = ghostObj.GetComponent<GhostAmbientSound>();
                if (ambientSound == null)
                {
                    ambientSound = Undo.AddComponent<GhostAmbientSound>(ghostObj);
                }

                // Cấu hình qua SerializedObject
                ConfigureGhostAmbientSound(ambientSound, ghostObj);
                count++;
            }

            Debug.Log($"<color=magenta><b>[Horror Audio] Đã thiết lập GhostAmbientSound cho {count} con ma trong scene.</b></color>");
            return count;
        }

        private static void ConfigureGhostAmbientSound(GhostAmbientSound ambientSound, GameObject ghostObj)
        {
            var so = new SerializedObject(ambientSound);
            so.Update();

            // Tạo hoặc lấy AudioSource con cho ambient
            AudioSource ambientSrc = EnsureChildAudioSource(ghostObj, "GhostAmbientAudio", true, false, 64);
            ambientSrc.spatialBlend = 1.0f;
            ambientSrc.rolloffMode = AudioRolloffMode.Linear;
            ambientSrc.minDistance = 2.0f;
            ambientSrc.maxDistance = 25.0f;
            ambientSrc.dopplerLevel = 0f;
            EditorUtility.SetDirty(ambientSrc);

            AudioSource scareSrc = EnsureChildAudioSource(ghostObj, "GhostScareAudio", false, false, 32);
            scareSrc.spatialBlend = 1.0f;
            scareSrc.rolloffMode = AudioRolloffMode.Linear;
            scareSrc.minDistance = 1.5f;
            scareSrc.maxDistance = 20.0f;
            scareSrc.dopplerLevel = 0f;
            EditorUtility.SetDirty(scareSrc);

            so.FindProperty("ambientSource").objectReferenceValue = ambientSrc;
            so.FindProperty("scareSource").objectReferenceValue = scareSrc;

            // Nạp clips
            AudioClip moanClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Ghost/ghost_moan_loop.wav");
            AudioClip breathClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Ghost/ghost_breath_loop.wav");

            so.FindProperty("ghostMoanClip").objectReferenceValue = moanClip;
            so.FindProperty("ghostBreathClip").objectReferenceValue = breathClip;

            // Nạp scare clips (3)
            LoadClipsIntoArray(so.FindProperty("ghostScareClips"), "Assets/Audio/Ghost/ghost_scare_", 3);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(ambientSound);
        }

        #endregion

        #region Surface Scanning & Assignment

        /// <summary>
        /// Duyệt các Renderer trong Map, gán FootstepSurface = Wood cho object có tên/material chứa "wood", "plank", "go", "sàn", "floor" kèm "wood"
        /// </summary>
        public static int ScanAndAssignWoodSurfaces(GameObject playerObj)
        {
            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            var assignedList = new List<string>();

            foreach (Renderer rend in renderers)
            {
                if (rend == null) continue;
                GameObject go = rend.gameObject;

                // Bỏ qua Player và các đối tượng con của Player
                if (playerObj != null && (go == playerObj || go.transform.IsChildOf(playerObj.transform))) continue;

                // Bỏ qua con ma
                if (go.name.IndexOf("Ghost", System.StringComparison.OrdinalIgnoreCase) >= 0 || go.GetComponentInParent<GhostAI>() != null) continue;
                try
                {
                    if (go.CompareTag("Ghost")) continue;
                }
                catch { }

                if (IsWoodSurface(go, rend))
                {
                    FootstepSurface surfaceComp = go.GetComponent<FootstepSurface>();
                    if (surfaceComp == null)
                    {
                        surfaceComp = Undo.AddComponent<FootstepSurface>(go);
                    }
                    surfaceComp.SetSurface(SurfaceType.Wood);
                    EditorUtility.SetDirty(surfaceComp);

                    string path = GetHierarchyPath(go.transform);
                    assignedList.Add(path);
                }
            }

            // In danh sách chi tiết ra Console
            var sb = new StringBuilder();
            sb.AppendLine($"<color=yellow><b>[Horror Audio] DANH SÁCH {assignedList.Count} ĐỐI TƯỢNG ĐÃ ĐƯỢC GÁN BỀ MẶT GỖ (WOOD):</b></color>");
            sb.AppendLine("----------------------------------------------------------------------------------------------------------------");
            for (int i = 0; i < assignedList.Count; i++)
            {
                sb.AppendLine($"[{i + 1:D3}] {assignedList[i]}");
            }
            sb.AppendLine("----------------------------------------------------------------------------------------------------------------");
            Debug.Log(sb.ToString());

            return assignedList.Count;
        }

        private static bool IsWoodSurface(GameObject go, Renderer rend)
        {
            string goName = go.name.ToLowerInvariant();

            // Kiểm tra tên object
            if (goName.Contains("wood") || goName.Contains("plank") || goName.Contains("go") || goName.Contains("sàn") || goName.Contains("san"))
            {
                return true;
            }

            // Kiểm tra "floor" đi kèm "wood" hoặc cấu trúc sàn gỗ
            if (goName.Contains("floor") && (goName.Contains("wood") || goName.Contains("board") || goName.Contains("mid") || goName.Contains("path")))
            {
                return true;
            }

            // Kiểm tra tên các vật liệu gắn trên Renderer
            if (rend != null)
            {
                Material[] mats = rend.sharedMaterials;
                if (mats != null)
                {
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i] == null) continue;
                        string matName = mats[i].name.ToLowerInvariant();
                        if (matName.Contains("wood") || matName.Contains("plank") || matName.Contains("go") || matName.Contains("sàn") || matName.Contains("san"))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static string GetHierarchyPath(Transform t)
        {
            if (t == null) return "";
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }

        #endregion

        #region AudioListener Verification

        private static void CheckAudioListeners()
        {
            AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);

            if (listeners.Length == 1)
            {
                Debug.Log($"<color=green>[Audio Check] Scene có đúng 1 AudioListener duy nhất tại: '{GetHierarchyPath(listeners[0].transform)}' (Hợp lệ chuẩn).</color>");
            }
            else if (listeners.Length > 1)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"<color=orange>[Audio Check CẢNH BÁO] Phát hiện {listeners.Length} AudioListener trong scene (chỉ nên có 1 duy nhất):</color>");
                for (int i = 0; i < listeners.Length; i++)
                {
                    sb.AppendLine($"  - AudioListener #{i + 1}: {GetHierarchyPath(listeners[i].transform)}");
                }
                sb.AppendLine("Quy tắc an toàn: Script không tự ý xóa, vui lòng kiểm tra thủ công.");
                Debug.LogWarning(sb.ToString());
            }
            else
            {
                Debug.LogWarning("[Audio Check CẢNH BÁO] Không tìm thấy AudioListener nào trong Scene!");
            }
        }

        #endregion

        #region Helper Menus

        [MenuItem("Tools/Audio/Clear Footstep Surfaces", false, 20)]
        public static void ClearAllFootstepSurfaces()
        {
            FootstepSurface[] surfaces = Object.FindObjectsByType<FootstepSurface>(FindObjectsSortMode.None);
            int count = 0;

            foreach (var s in surfaces)
            {
                if (s == null) continue;
                // Không xóa trên Player nếu có
                if (s.GetComponent<PlayerController>() != null) continue;

                Undo.DestroyObjectImmediate(s);
                count++;
            }

            if (count > 0)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }

            Debug.Log($"<color=yellow>[Horror Audio] Đã gỡ bỏ thành công {count} component FootstepSurface khỏi các đối tượng trong scene.</color>");
            EditorUtility.DisplayDialog("Clear Footstep Surfaces", $"Đã gỡ bỏ {count} component FootstepSurface.", "OK");
        }

        [MenuItem("Tools/Footsteps/Set Selected As Wood", false, 30)]
        public static void SetSelectedAsWood()
        {
            GameObject[] selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                EditorUtility.DisplayDialog("Thông báo", "Vui lòng chọn ít nhất 1 GameObject trong Scene!", "OK");
                return;
            }

            int count = 0;
            foreach (var go in selected)
            {
                FootstepSurface s = go.GetComponent<FootstepSurface>();
                if (s == null) s = Undo.AddComponent<FootstepSurface>(go);
                s.SetSurface(SurfaceType.Wood);
                EditorUtility.SetDirty(s);
                count++;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"<color=green>[Footsteps] Đã gán bề mặt GỖ (Wood) cho {count} đối tượng đang chọn.</color>");
        }

        [MenuItem("Tools/Footsteps/Set Selected As Dirt", false, 31)]
        public static void SetSelectedAsDirt()
        {
            GameObject[] selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                EditorUtility.DisplayDialog("Thông báo", "Vui lòng chọn ít nhất 1 GameObject trong Scene!", "OK");
                return;
            }

            int count = 0;
            foreach (var go in selected)
            {
                FootstepSurface s = go.GetComponent<FootstepSurface>();
                if (s == null) s = Undo.AddComponent<FootstepSurface>(go);
                s.SetSurface(SurfaceType.Dirt);
                EditorUtility.SetDirty(s);
                count++;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"<color=green>[Footsteps] Đã gán bề mặt ĐẤT (Dirt) cho {count} đối tượng đang chọn.</color>");
        }

        #endregion
    }
}
