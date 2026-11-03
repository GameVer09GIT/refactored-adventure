using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HorrorAudio.Editor
{
    [InitializeOnLoad]
    public static class AudioDiagnosticPhase0
    {
        static AudioDiagnosticPhase0()
        {
            EditorApplication.delayCall += RunDiagnosticsOnce;
        }

        [MenuItem("Tools/Audio/Run Phase 0 Diagnostics", false, 10)]
        public static void RunMenuDiagnostics()
        {
            ExecuteDiagnostics(true);
        }

        private static void RunDiagnosticsOnce()
        {
            ExecuteDiagnostics(false);
        }

        public static void ExecuteDiagnostics(bool showDialog)
        {
            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("PHASE 0 DIAGNOSTIC REPORT: HIỆN TRẠNG BỀ MẶT BƯỚC CHÂN VÀ HỆ THỐNG ÂM THANH");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"Thời gian: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Scene hiện tại: {SceneManager.GetActiveScene().name} ({SceneManager.GetActiveScene().path})");

            // 1. Kiểm tra FootstepSurface hiện có trong Scene
            FootstepSurface[] surfaces = UnityEngine.Object.FindObjectsByType<FootstepSurface>(FindObjectsSortMode.None);
            sb.AppendLine($"\n--- 1. TỔNG SỐ COMPONENT FootstepSurface: {surfaces.Length} ---");
            
            var parentSuspects = new List<string>();
            foreach (var s in surfaces)
            {
                if (s == null) continue;
                Transform t = s.transform;
                int childCount = t.childCount;
                string path = PlayerAudioSetupEditor.GetHierarchyPath(t);

                // Kiểm tra xem có phải parent lớn không (nhiều con hoặc tên chứa root, map, house, etc)
                if (childCount >= 3 || path.Contains("Map") || t.name.ToLowerInvariant().Contains("house") || t.name.ToLowerInvariant().Contains("nha"))
                {
                    parentSuspects.Add($"[Parent có {childCount} con] {path} (Loại: {s.Surface})");
                }
            }

            sb.AppendLine($"Số lượng FootstepSurface gắn trên object cha/cụm lớn nghi ngờ làm lan bề mặt: {parentSuspects.Count}");
            foreach (var p in parentSuspects)
            {
                sb.AppendLine($"  - {p}");
            }

            // 2. Gom cụm sàn gỗ dựa trên Renderer có material/texture chứa token gỗ hoặc object tên nhà sàn
            Renderer[] allRends = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            string[] woodTokens = { "wood", "wooden", "plank", "planks", "parquet", "timber", "lumber", "board", "boards", "deck", "floorboard" };
            string[] houseNames = { "nhà sàn", "nhasan", "house", "hut", "porch", "stairs", "sàn", "san" };

            var woodRends = new List<Renderer>();
            Bounds totalWoodBounds = new Bounds();
            bool hasBounds = false;

            foreach (var r in allRends)
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (r is ParticleSystemRenderer || r is TrailRenderer) continue;

                string goName = r.gameObject.name.ToLowerInvariant();
                bool isHouseOrWood = false;

                foreach (var ht in houseNames)
                {
                    if (goName.Contains(ht)) { isHouseOrWood = true; break; }
                }

                if (!isHouseOrWood && r.sharedMaterials != null)
                {
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m == null) continue;
                        string mName = m.name.ToLowerInvariant();
                        foreach (var wt in woodTokens)
                        {
                            if (mName.Contains(wt)) { isHouseOrWood = true; break; }
                        }
                        if (isHouseOrWood) break;

                        // Kiểm tra tên main texture
                        if (m.mainTexture != null)
                        {
                            string tName = m.mainTexture.name.ToLowerInvariant();
                            foreach (var wt in woodTokens)
                            {
                                if (tName.Contains(wt)) { isHouseOrWood = true; break; }
                            }
                        }
                        if (isHouseOrWood) break;
                    }
                }

                if (isHouseOrWood)
                {
                    woodRends.Add(r);
                    if (!hasBounds)
                    {
                        totalWoodBounds = r.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        totalWoodBounds.Encapsulate(r.bounds);
                    }
                }
            }

            sb.AppendLine($"\n--- 2. TÌM THẤY {woodRends.Count} RENDERER LIÊN QUAN ĐẾN NHÀ SÀN / GỖ ---");
            if (hasBounds)
            {
                sb.AppendLine($"Bounds tổng thể khu vực nhà sàn: Min=({totalWoodBounds.min.x:F2}, {totalWoodBounds.min.y:F2}, {totalWoodBounds.min.z:F2}), Max=({totalWoodBounds.max.x:F2}, {totalWoodBounds.max.y:F2}, {totalWoodBounds.max.z:F2})");
                sb.AppendLine($"Kích thước Bounds (Size): {totalWoodBounds.size.x:F2}m x {totalWoodBounds.size.y:F2}m x {totalWoodBounds.size.z:F2}m, Center=({totalWoodBounds.center.x:F2}, {totalWoodBounds.center.y:F2}, {totalWoodBounds.center.z:F2})");
            }

            // 3. Quét lưới Raycast 1m trên bounds nhà sàn và vòng 15m xung quanh
            sb.AppendLine("\n--- 3. KẾT QUẢ QUÉT LƯỚI RAYCAST (BƯỚC 1M) ---");
            int totalRaycasts = 0;
            int woodHits = 0;
            int dirtHits = 0;
            int noHits = 0;

            var missingWoodColliders = new Dictionary<string, int>();
            var wrongWoodColliders = new Dictionary<string, int>();

            if (hasBounds)
            {
                float minX = totalWoodBounds.min.x - 15f;
                float maxX = totalWoodBounds.max.x + 15f;
                float minZ = totalWoodBounds.min.z - 15f;
                float maxZ = totalWoodBounds.max.z + 15f;
                float rayY = totalWoodBounds.max.y + 2.0f;
                float rayDist = totalWoodBounds.size.y + 10.0f;

                PlayerFootsteps footstepsScript = UnityEngine.Object.FindFirstObjectByType<PlayerFootsteps>();

                for (float x = minX; x <= maxX; x += 1.0f)
                {
                    for (float z = minZ; z <= maxZ; z += 1.0f)
                    {
                        totalRaycasts++;
                        Vector3 rayOrigin = new Vector3(x, rayY, z);
                        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayDist, ~0, QueryTriggerInteraction.Ignore))
                        {
                            Collider c = hit.collider;
                            SurfaceType currentDecision = footstepsScript != null ? footstepsScript.DetectSurface(c) : SurfaceType.Dirt;

                            bool isInsideHouseBounds = (x >= totalWoodBounds.min.x && x <= totalWoodBounds.max.x && z >= totalWoodBounds.min.z && z <= totalWoodBounds.max.z);

                            if (currentDecision == SurfaceType.Wood)
                            {
                                woodHits++;
                                if (!isInsideHouseBounds)
                                {
                                    // Ngoài nhà nhưng lại ra tiếng gỗ!
                                    string key = $"{c.gameObject.name} [Path: {PlayerAudioSetupEditor.GetHierarchyPath(c.transform)}]";
                                    if (!wrongWoodColliders.ContainsKey(key)) wrongWoodColliders[key] = 0;
                                    wrongWoodColliders[key]++;
                                }
                            }
                            else
                            {
                                dirtHits++;
                                if (isInsideHouseBounds)
                                {
                                    // Trong nhà nhưng lại ra tiếng đất!
                                    string key = $"{c.gameObject.name} [Path: {PlayerAudioSetupEditor.GetHierarchyPath(c.transform)}]";
                                    if (!missingWoodColliders.ContainsKey(key)) missingWoodColliders[key] = 0;
                                    missingWoodColliders[key]++;
                                }
                            }
                        }
                        else
                        {
                            noHits++;
                        }
                    }
                }
            }

            sb.AppendLine($"Tổng số điểm raycast lưới 1m: {totalRaycasts}");
            sb.AppendLine($"Số điểm nhận diện GỖ (Wood): {woodHits} ({((float)woodHits / Math.Max(1, totalRaycasts) * 100):F1}%)");
            sb.AppendLine($"Số điểm nhận diện ĐẤT (Dirt): {dirtHits} ({((float)dirtHits / Math.Max(1, totalRaycasts) * 100):F1}%)");
            sb.AppendLine($"Số điểm không chạm collider: {noHits}");

            sb.AppendLine($"\nDanh sách collider NGOÀI NHÀ đang bị nhận nhầm thành GỖ (gây sai tiếng gỗ ngoài đất): {wrongWoodColliders.Count}");
            foreach (var kv in wrongWoodColliders)
            {
                sb.AppendLine($"  - ({kv.Value} điểm) {kv.Key}");
            }

            sb.AppendLine($"\nDanh sách collider TRONG KHU VỰC NHÀ đang bị nhận nhầm thành ĐẤT (gây thiếu tiếng gỗ): {missingWoodColliders.Count}");
            foreach (var kv in missingWoodColliders)
            {
                sb.AppendLine($"  - ({kv.Value} điểm) {kv.Key}");
            }

            string outPath = Path.Combine(Application.dataPath, "..", "Assets", "phase0_report.txt");
            File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"<color=cyan><b>[Horror Audio] Đã lưu báo cáo chẩn đoán Phase 0 tại: Assets/phase0_report.txt</b></color>");

            if (showDialog)
            {
                EditorUtility.DisplayDialog("Phase 0 Diagnostic", $"Đã hoàn tất quét chẩn đoán hiện trạng!\n- FootstepSurface: {surfaces.Length}\n- Điểm Wood: {woodHits}, Dirt: {dirtHits}\n- Sai ngoài nhà: {wrongWoodColliders.Count} collider\n- Thiếu trong nhà: {missingWoodColliders.Count} collider\nXem chi tiết tại Assets/phase0_report.txt", "OK");
            }
        }
    }
}
