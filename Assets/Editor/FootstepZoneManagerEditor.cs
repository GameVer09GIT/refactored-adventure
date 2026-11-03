using System;
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
    /// Công cụ quản lý vùng bước chân FootstepZone và dọn dẹp các gán sai FootstepSurface.
    /// Cung cấp các chức năng Undo, tạo vùng theo Selection, dọn FootstepSurface trên object cha/phi-gỗ,
    /// tự động phủ toàn bộ sàn gỗ nhà sàn, và quét kiểm tra lưới Raycast 1m.
    /// </summary>
    public static class FootstepZoneManagerEditor
    {
        private const string ZonesParentName = "FootstepZones";

        [MenuItem("Tools/Footsteps/Create Wood Zone From Selection", false, 1)]
        public static void CreateWoodZoneFromSelection()
        {
            GameObject[] selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                EditorUtility.DisplayDialog("Thông báo", "Vui lòng chọn ít nhất một GameObject chứa sàn/bậc thang gỗ trong Scene!", "OK");
                return;
            }

            Bounds combinedBounds = new Bounds();
            bool hasBounds = false;

            foreach (var go in selected)
            {
                Renderer[] rends = go.GetComponentsInChildren<Renderer>();
                foreach (var r in rends)
                {
                    if (r == null || !r.enabled) continue;
                    if (!hasBounds)
                    {
                        combinedBounds = r.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(r.bounds);
                    }
                }

                Collider[] cols = go.GetComponentsInChildren<Collider>();
                foreach (var c in cols)
                {
                    if (c == null || c.isTrigger) continue;
                    if (!hasBounds)
                    {
                        combinedBounds = c.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(c.bounds);
                    }
                }
            }

            if (!hasBounds)
            {
                EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Renderer hoặc Collider nào trong các đối tượng đã chọn để tính Bounds!", "OK");
                return;
            }

            Transform zonesParent = EnsureZonesParent();
            string zoneName = $"Zone_Wood_{selected[0].name}";

            GameObject zoneGo = new GameObject(zoneName);
            Undo.RegisterCreatedObjectUndo(zoneGo, "Create Wood Zone From Selection");
            zoneGo.transform.SetParent(zonesParent, true);

            // Mở rộng 0.3m ngang, chiều cao từ -0.5m dưới sàn đến +2.5m trên sàn
            Vector3 center = combinedBounds.center;
            Vector3 size = combinedBounds.size;

            float bottomY = combinedBounds.min.y - 0.5f;
            float topY = combinedBounds.max.y + 2.5f;
            float height = Mathf.Max(1.5f, topY - bottomY);
            float centerY = bottomY + height * 0.5f;

            center.y = centerY;
            size.x += 0.6f; // +0.3m mỗi bên
            size.z += 0.6f;
            size.y = height;

            zoneGo.transform.position = center;

            BoxCollider box = zoneGo.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = Vector3.zero;
            box.size = size;

            FootstepZone zone = zoneGo.AddComponent<FootstepZone>();

            Selection.activeGameObject = zoneGo;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Debug.Log($"<color=green><b>[Footsteps] Đã tạo FootstepZone '{zoneName}' tại ({center.x:F1}, {center.y:F1}, {center.z:F1}) với kích thước ({size.x:F1}m x {size.y:F1}m x {size.z:F1}m).</b></color>");
        }

        [MenuItem("Tools/Footsteps/Clear All Footstep Zones", false, 2)]
        public static void ClearAllFootstepZones()
        {
            GameObject parentGo = GameObject.Find(ZonesParentName);
            int count = 0;

            if (parentGo != null)
            {
                count = parentGo.transform.childCount;
                Undo.DestroyObjectImmediate(parentGo);
            }
            else
            {
                FootstepZone[] zones = UnityEngine.Object.FindObjectsByType<FootstepZone>(FindObjectsSortMode.None);
                count = zones.Length;
                foreach (var z in zones)
                {
                    if (z != null) Undo.DestroyObjectImmediate(z.gameObject);
                }
            }

            FootstepZone.ClearRegistry();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"<color=yellow><b>[Footsteps] Đã xóa {count} FootstepZone khỏi Scene.</b></color>");
        }

        [MenuItem("Tools/Footsteps/Remove Misassigned Parent FootstepSurfaces", false, 3)]
        public static void RemoveMisassignedParentFootstepSurfacesMenu()
        {
            RemoveMisassignedParentFootstepSurfaces(true);
        }

        public static int RemoveMisassignedParentFootstepSurfaces(bool saveScene = true)
        {
            FootstepSurface[] surfaces = UnityEngine.Object.FindObjectsByType<FootstepSurface>(FindObjectsSortMode.None);
            var removedLog = new List<string>();
            int removedCount = 0;

            string[] invalidKeywords = {
                "pavement", "ground", "gravestone", "fence", "cobble", "rock", "roof",
                "wall", "column", "billboard", "pole", "blockouts", "sidewalk", "pavilion", "car", "gate"
            };

            foreach (var s in surfaces)
            {
                if (s == null) continue;
                Transform t = s.transform;
                string goName = t.name.ToLowerInvariant();
                string path = PlayerAudioSetupEditor.GetHierarchyPath(t).ToLowerInvariant();

                bool isInvalid = false;
                string reason = "";

                // 1. Kiểm tra từ khóa phi-gỗ rõ rệt
                foreach (var kw in invalidKeywords)
                {
                    if (goName.Contains(kw) || path.Contains(kw))
                    {
                        isInvalid = true;
                        reason = $"Tên hoặc đường dẫn chứa token phi-gỗ '{kw}'";
                        break;
                    }
                }

                // 2. Object cha lớn có nhiều con (khiến lan bề mặt xuống các con khác)
                if (!isInvalid && t.childCount >= 3)
                {
                    isInvalid = true;
                    reason = $"Object cha có {t.childCount} con, làm lan bề mặt";
                }

                // 3. Object là Root Map hoặc ground system
                if (!isInvalid && (path.Contains("horror_ground_system") || goName == "map" || goName.Contains("root")))
                {
                    isInvalid = true;
                    reason = "Object Root/Ground system";
                }

                if (isInvalid)
                {
                    string entry = $"{PlayerAudioSetupEditor.GetHierarchyPath(t)} (Lý do: {reason})";
                    removedLog.Add(entry);
                    Undo.DestroyObjectImmediate(s);
                    removedCount++;
                }
            }

            Debug.Log($"<color=orange><b>[Footsteps] Đã gỡ bỏ {removedCount} FootstepSurface bị gán sai khỏi các object cha/phi-gỗ:</b></color>");
            foreach (var r in removedLog)
            {
                Debug.Log($"  - Đã gỡ: {r}");
            }

            if (saveScene && removedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }

            return removedCount;
        }

        [MenuItem("Tools/Footsteps/Auto Create House Wood Zones", false, 4)]
        public static void AutoCreateHouseWoodZones()
        {
            Debug.Log("<color=cyan><b>[Footsteps] Bắt đầu tự động tạo các vùng FootstepZone phủ toàn bộ mặt sàn gỗ...</b></color>");

            // 1. Dọn vùng cũ
            ClearAllFootstepZones();

            // 2. Dọn các FootstepSurface sai
            RemoveMisassignedParentFootstepSurfaces(false);

            Transform parent = EnsureZonesParent();

            // Danh sách cụm sàn gỗ trong nhà và bến tàu cần phủ FootstepZone:
            // (A) Villa 1: Sàn chính nhà sàn
            CreateZoneForCluster(parent, "Zone_Villa1_MainFloor", new[] {
                "Villa1_Floor_Mid_A", "Villa1_Floor_Mid_B"
            }, 0.2f, 0.3f, 2.2f);

            // Bậc thang ngoài trời Villa 1: Tạo vùng riêng lẻ cho từng bậc thang, KHÔNG gộp 2 cầu thang xa nhau thành 1 box 38m
            CreateIndividualZones(parent, "Zone_Villa1_Stairs", "Villa1_ExtStairs_A", 0.15f, 0.2f, 1.8f);

            // (B) Villa 2: Nhà sàn chính (Pref_Villa2_C sàn gỗ)
            CreateZoneForCluster(parent, "Zone_Villa2_MainFloor", new[] {
                "Villa2_Floor_Mid_A_4x", "Villa2_Floor_Mid_A", "Villa2_Floor_Mid_B"
            }, 0.2f, 0.3f, 2.2f);

            // (C) Bến tàu / Cầu cảng gỗ (Struct_Docking_A, Struct_Docking_B)
            CreateZoneForCluster(parent, "Zone_Docking_A", new[] {
                "Struct_Docking_A"
            }, 0.2f, 0.3f, 2.2f);

            CreateZoneForCluster(parent, "Zone_Docking_B", new[] {
                "Struct_Docking_B"
            }, 0.2f, 0.3f, 2.2f);

            // (D) Sàn gỗ dưới lòng đất (MapDuoiLongDat)
            CreateZoneForCluster(parent, "Zone_Underground_Office", new[] {
                "Office_floor", "WoodUpstair"
            }, 0.2f, 0.3f, 2.2f);

            // LƯU Ý: Tuyệt đối KHÔNG tạo FootstepZone cho các tấm ván rời (Struct_WoodBoard_A) hay lối đi ván ngoài trời (Struct_WoodPath_A)
            // vì việc bao phủ các đối tượng này tạo ra Box Collider khổng lồ 165m x 160m che phủ toàn bộ đất ngoài trời!
            // Thay vào đó, PlayerFootsteps đã tự động nhận diện trực tiếp tên GameObject / Collider / Material của chúng khi dẫm lên.

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            Debug.Log("<color=green><b>[Footsteps] ĐÃ HOÀN TẤT TỰ ĐỘNG TẠO CÁC VÙNG FOOTSTEP ZONE CHUẨN XÁC VÀ LƯU SCENE!</b></color>");
        }

        private static void CreateIndividualZones(Transform parent, string baseZoneName, string filter, float horizontalPad, float bottomPad, float topPad)
        {
            Renderer[] allRends = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            int idx = 1;
            foreach (var r in allRends)
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (r.gameObject.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;

                Bounds bounds = r.bounds;
                string zoneName = $"{baseZoneName}_{idx++}";
                GameObject zoneGo = new GameObject(zoneName);
                Undo.RegisterCreatedObjectUndo(zoneGo, "Auto Create Individual Wood Zone");
                zoneGo.transform.SetParent(parent, true);

                Vector3 center = bounds.center;
                Vector3 size = bounds.size;
                float bottomY = bounds.min.y - bottomPad;
                float topY = bounds.max.y + topPad;
                float height = Mathf.Max(1.2f, topY - bottomY);
                float centerY = bottomY + height * 0.5f;

                center.y = centerY;
                size.x += horizontalPad * 2f;
                size.z += horizontalPad * 2f;
                size.y = height;

                zoneGo.transform.position = center;
                BoxCollider box = zoneGo.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.center = Vector3.zero;
                box.size = size;

                zoneGo.AddComponent<FootstepZone>();
            }
        }

        private static GameObject CreateZoneForCluster(Transform parent, string zoneName, string[] objectNameFilters, float horizontalPad, float bottomPad, float topPad)
        {
            Renderer[] allRends = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            Bounds bounds = new Bounds();
            bool hasBounds = false;
            int matchedCount = 0;

            foreach (var r in allRends)
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                string goName = r.gameObject.name;

                bool match = false;
                foreach (var filter in objectNameFilters)
                {
                    if (goName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        match = true;
                        break;
                    }
                }

                if (match)
                {
                    matchedCount++;
                    if (!hasBounds)
                    {
                        bounds = r.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(r.bounds);
                    }
                }
            }

            if (!hasBounds || matchedCount == 0)
            {
                Debug.LogWarning($"[Footsteps] Không tìm thấy object nào khớp filter cho vùng '{zoneName}'. Bỏ qua.");
                return null;
            }

            GameObject zoneGo = new GameObject(zoneName);
            Undo.RegisterCreatedObjectUndo(zoneGo, "Auto Create Wood Zone");
            zoneGo.transform.SetParent(parent, true);

            Vector3 center = bounds.center;
            Vector3 size = bounds.size;

            float bottomY = bounds.min.y - bottomPad;
            float topY = bounds.max.y + topPad;
            float height = Mathf.Max(1.5f, topY - bottomY);
            float centerY = bottomY + height * 0.5f;

            center.y = centerY;
            size.x += horizontalPad * 2f;
            size.z += horizontalPad * 2f;
            size.y = height;

            zoneGo.transform.position = center;

            BoxCollider box = zoneGo.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = Vector3.zero;
            box.size = size;

            FootstepZone zone = zoneGo.AddComponent<FootstepZone>();

            Debug.Log($"<color=cyan>[Footsteps] Đã tạo '{zoneName}' (khớp {matchedCount} renderers): Center=({center.x:F1}, {center.y:F1}, {center.z:F1}), Size=({size.x:F1}m, {size.y:F1}m, {size.z:F1}m)</color>");
            return zoneGo;
        }

        private static Transform EnsureZonesParent()
        {
            GameObject go = GameObject.Find(ZonesParentName);
            if (go == null)
            {
                go = new GameObject(ZonesParentName);
                Undo.RegisterCreatedObjectUndo(go, "Create FootstepZones Parent");
            }
            return go.transform;
        }

        [MenuItem("Tools/Footsteps/Run Full Raycast Verification", false, 10)]
        public static void RunVerification()
        {
            Debug.Log("<color=cyan><b>[Footsteps] Bắt đầu quét kiểm tra lưới Raycast 1m sau khi tối ưu...</b></color>");

            // Tìm PlayerFootsteps
            PlayerFootsteps footstepsScript = UnityEngine.Object.FindFirstObjectByType<PlayerFootsteps>();
            if (footstepsScript == null)
            {
                Debug.LogError("[Footsteps] Không tìm thấy PlayerFootsteps trong Scene!");
                return;
            }
            footstepsScript.ClearSurfaceCache();

            // Lấy danh sách vùng FootstepZone
            FootstepZone[] zones = UnityEngine.Object.FindObjectsByType<FootstepZone>(FindObjectsSortMode.None);
            Bounds houseBounds = new Bounds();
            bool hasHouseBounds = false;

            foreach (var z in zones)
            {
                if (z == null) continue;
                BoxCollider box = z.GetComponent<BoxCollider>();
                if (box == null) continue;

                if (!hasHouseBounds)
                {
                    houseBounds = box.bounds;
                    hasHouseBounds = true;
                }
                else
                {
                    houseBounds.Encapsulate(box.bounds);
                }
            }

            if (!hasHouseBounds)
            {
                Debug.LogError("[Footsteps] Không tìm thấy FootstepZone nào để kiểm tra!");
                return;
            }

            float minX = houseBounds.min.x - 15f;
            float maxX = houseBounds.max.x + 15f;
            float minZ = houseBounds.min.z - 15f;
            float maxZ = houseBounds.max.z + 15f;
            float rayY = houseBounds.max.y + 2.0f;
            float rayDist = houseBounds.size.y + 10.0f;

            int totalRaycasts = 0;
            int woodHits = 0;
            int dirtHits = 0;
            int wrongWoodOutside = 0;
            int insideHouseHits = 0;
            int insideWoodHits = 0;

            var wrongOutsideColliders = new Dictionary<string, int>();

            for (float x = minX; x <= maxX; x += 1.0f)
            {
                for (float z = minZ; z <= maxZ; z += 1.0f)
                {
                    totalRaycasts++;
                    Vector3 rayOrigin = new Vector3(x, rayY, z);

                    if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayDist, ~0, QueryTriggerInteraction.Ignore))
                    {
                        Collider col = hit.collider;
                        Vector3 hitPos = hit.point + Vector3.up * 0.1f;

                        // Kiểm tra bề mặt bằng logic FootstepZone và DetectSurface
                        SurfaceType surface = SurfaceType.Dirt;
                        if (FootstepZone.TryGetZoneSurface(hitPos, out SurfaceType zoneSurface, out _))
                        {
                            surface = zoneSurface;
                        }
                        else
                        {
                            surface = footstepsScript.DetectSurface(col);
                        }

                        bool isInsideZone = false;
                        foreach (var zZone in zones)
                        {
                            if (zZone != null && zZone.Contains(hitPos))
                            {
                                isInsideZone = true;
                                break;
                            }
                        }

                        if (surface == SurfaceType.Wood)
                        {
                            woodHits++;
                            if (!isInsideZone)
                            {
                                // Điểm ngoài vùng nhưng lại ra tiếng gỗ
                                wrongWoodOutside++;
                                string key = col.name;
                                if (!wrongOutsideColliders.ContainsKey(key)) wrongOutsideColliders[key] = 0;
                                wrongOutsideColliders[key]++;
                            }
                        }
                        else
                        {
                            dirtHits++;
                        }

                        if (isInsideZone)
                        {
                            insideHouseHits++;
                            if (surface == SurfaceType.Wood)
                            {
                                insideWoodHits++;
                            }
                        }
                    }
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("KẾT QUẢ QUÉT RAYCAST LƯỚI 1M SAU KHI SỬA FOOTSTEP ZONE & BỀ MẶT");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"Thời gian: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Tổng số điểm quét (bao gồm phạm vi mở rộng 15m): {totalRaycasts}");
            sb.AppendLine($"Tổng điểm nhận diện GỖ (Wood): {woodHits}");
            sb.AppendLine($"Tổng điểm nhận diện ĐẤT (Dirt): {dirtHits}");
            sb.AppendLine($"Số điểm TRONG VÙNG SÀN GỖ: {insideHouseHits} điểm (Ra Gỗ: {insideWoodHits} = {((float)insideWoodHits / Math.Max(1, insideHouseHits) * 100):F1}%)");
            sb.AppendLine($"Số điểm NGOÀI VÙNG SÀN GỖ bị ra Wood (Lỗi ngoài đất): {wrongWoodOutside} điểm");

            if (wrongWoodOutside > 0)
            {
                sb.AppendLine("\nDanh sách collider ngoài vùng đang bị nhận Wood:");
                foreach (var kv in wrongOutsideColliders)
                {
                    sb.AppendLine($"  - ({kv.Value} điểm) {kv.Key}");
                }
            }
            else
            {
                sb.AppendLine("\nTUYỆT VỜI: 0 điểm ngoài sàn gỗ bị nhận nhầm thành Wood! Toàn bộ nền đất ngoài trời ra ĐẤT (Dirt) 100%!");
            }

            string outPath = Path.Combine(Application.dataPath, "..", "Assets", "phase0_after_report.txt");
            File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);

            Debug.Log($"<color=green><b>[Footsteps] {sb}</b></color>");
            EditorUtility.DisplayDialog("Raycast Verification", $"Đã hoàn tất kiểm tra raycast!\n- Trong vùng sàn gỗ ra Wood: {insideWoodHits}/{insideHouseHits} ({((float)insideWoodHits / Math.Max(1, insideHouseHits) * 100):F1}%)\n- Ngoài vùng ra Wood (lỗi): {wrongWoodOutside} điểm\nXem chi tiết tại Assets/phase0_after_report.txt", "OK");
        }
    }
}
