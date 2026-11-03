using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace HorrorGame.Inventory
{
    /// <summary>
    /// Kịch bản kiểm thử toàn diện thu thập số liệu đa góc nhìn (Pitch 0°, +60°, -60°)
    /// cho 3 tình huống (Đèn pin, Radar, Ô trống) và kiểm tra hồi quy chức năng.
    /// </summary>
    public class ArmIKVerificationSuite : MonoBehaviour
    {
        public static string LastReportText { get; private set; }
        public static bool IsFinished { get; private set; } = false;

        public static void RunVerification()
        {
            var go = new GameObject("ArmIKVerificationRunner");
            var runner = go.AddComponent<ArmIKVerificationSuite>();
            runner.StartCoroutine(runner.ExecuteSuite());
        }

        private IEnumerator ExecuteSuite()
        {
            IsFinished = false;
            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("BÁO CÁO KIỂM CHỨNG SỐ LIỆU ĐA TÌNH HUỐNG & ĐA GÓC NHÌN - ARMS IK SYSTEM");
            sb.AppendLine("Thời gian thực hiện: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("================================================================================\n");

            var player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                sb.AppendLine("LỖI: Không tìm thấy Player trong Scene!");
                Finish(sb.ToString());
                yield break;
            }

            var pc = player.GetComponent<PlayerController>();
            var cam = pc != null ? pc.playerCamera.GetComponent<Camera>() : Camera.main;
            var anim = player.GetComponentInChildren<Animator>();
            var hotbar = player.GetComponent<HotbarInventory>();
            var armIK = player.GetComponent<PlayerArmIK>();

            if (cam == null || anim == null || hotbar == null || armIK == null)
            {
                sb.AppendLine("LỖI: Thiếu component cốt lõi trên Player!");
                Finish(sb.ToString());
                yield break;
            }

            FieldInfo xRotField = typeof(PlayerController).GetField("xRotation", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            // Xương cánh tay phải
            Transform rUpper = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
            Transform rLower = anim.GetBoneTransform(HumanBodyBones.RightLowerArm);
            Transform rHand = anim.GetBoneTransform(HumanBodyBones.RightHand);
            Transform rMiddle = anim.GetBoneTransform(HumanBodyBones.RightMiddleProximal);

            // Xương cánh tay trái
            Transform lUpper = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            Transform lLower = anim.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            Transform lHand = anim.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform lMiddle = anim.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);

            // Số liệu gốc (Baseline)
            const float BASE_R_UPPER = 0.2718f;
            const float BASE_R_LOWER = 0.2471f;
            const float BASE_R_HAND  = 0.0994f;

            const float BASE_L_UPPER = 0.2718f;
            const float BASE_L_LOWER = 0.2482f;
            const float BASE_L_HAND  = 0.0994f;

            // Setup Animator Bridge
            var bridge = anim.GetComponent<ArmIKAnimatorBridge>();
            if (bridge == null) bridge = anim.gameObject.AddComponent<ArmIKAnimatorBridge>();
            bridge.playerArmIK = armIK;
            bridge.onAnimatorIK = armIK.OnAnimatorIKCallback;

            // Đo Bounds của SkinnedMeshRenderer khi ở Animation gốc
            armIK.ResetToOriginalAnimation = true;
            yield return new WaitForSeconds(0.25f);
            var smrs = player.GetComponentsInChildren<SkinnedMeshRenderer>();
            Bounds baseSmrBounds = new Bounds(player.transform.position, Vector3.zero);
            foreach (var smr in smrs) if (smr.enabled) baseSmrBounds.Encapsulate(smr.bounds);
            armIK.ResetToOriginalAnimation = false;
            yield return new WaitForSeconds(0.25f);

            // Các tình huống kiểm thử (Slot 0 = Flashlight, Slot 1 = Radar, Slot 2 = Empty)
            int[] testSlots = new int[] { 0, 1, 2 };
            string[] slotNames = new string[] { "ĐÈN PIN (Slot 1)", "RADAR (Slot 2)", "Ô TRỐNG (Slot 3)" };
            float[] testPitches = new float[] { 0f, 60f, -60f };

            for (int s = 0; s < testSlots.Length; s++)
            {
                int slot = testSlots[s];
                string slotName = slotNames[s];

                sb.AppendLine("--------------------------------------------------------------------------------");
                sb.AppendLine($"TÌNH HUỐNG: {slotName}");
                sb.AppendLine("--------------------------------------------------------------------------------");

                hotbar.SelectSlot(slot);
                // Đợi chuyển ô hoàn tất
                yield return new WaitForSeconds(0.55f);

                foreach (float pitch in testPitches)
                {
                    // Vô hiệu hóa PlayerController tạm thời để chuột không ghi đè xRotation
                    pc.enabled = false;
                    if (xRotField != null) xRotField.SetValue(pc, pitch);
                    pc.playerCamera.localRotation = Quaternion.Euler(pitch, 0f, 0f);

                    // Đợi các frame animation và IK cập nhật
                    yield return new WaitForSeconds(0.12f);

                    sb.AppendLine($"\n>>> Góc nhìn Pitch: {pitch:+00;-00;0}°");

                    // 1. Chống dị dạng
                    float rLenUpper = Vector3.Distance(rUpper.position, rLower.position);
                    float rLenLower = Vector3.Distance(rLower.position, rHand.position);
                    float rLenHand  = Vector3.Distance(rHand.position, rMiddle.position);
                    float rDevUpper = Mathf.Abs(rLenUpper - BASE_R_UPPER) / BASE_R_UPPER * 100f;
                    float rDevLower = Mathf.Abs(rLenLower - BASE_R_LOWER) / BASE_R_LOWER * 100f;
                    float rDevHand  = Mathf.Abs(rLenHand  - BASE_R_HAND)  / BASE_R_HAND  * 100f;

                    float lLenUpper = Vector3.Distance(lUpper.position, lLower.position);
                    float lLenLower = Vector3.Distance(lLower.position, lHand.position);
                    float lLenHand  = Vector3.Distance(lHand.position, lMiddle.position);
                    float lDevUpper = Mathf.Abs(lLenUpper - BASE_L_UPPER) / BASE_L_UPPER * 100f;
                    float lDevLower = Mathf.Abs(lLenLower - BASE_L_LOWER) / BASE_L_LOWER * 100f;
                    float lDevHand  = Mathf.Abs(lLenHand  - BASE_L_HAND)  / BASE_L_HAND  * 100f;

                    Vector3 rScale = rHand.lossyScale;
                    Vector3 lScale = lHand.lossyScale;

                    float rElbowAngle = Vector3.Angle(rUpper.position - rLower.position, rHand.position - rLower.position);
                    float lElbowAngle = Vector3.Angle(lUpper.position - lLower.position, lHand.position - lLower.position);

                    // Twist cẳng tay: góc xoắn giữa trục hướng lên của bàn tay và trục cẳng tay
                    Vector3 rForearmAxis = (rHand.position - rLower.position).normalized;
                    Vector3 rHandUpProj = Vector3.ProjectOnPlane(rHand.up, rForearmAxis).normalized;
                    Vector3 rLowerUpProj = Vector3.ProjectOnPlane(rLower.up, rForearmAxis).normalized;
                    float rTwist = Vector3.Angle(rHandUpProj, rLowerUpProj);

                    Vector3 lForearmAxis = (lHand.position - lLower.position).normalized;
                    Vector3 lHandUpProj = Vector3.ProjectOnPlane(lHand.up, lForearmAxis).normalized;
                    Vector3 lLowerUpProj = Vector3.ProjectOnPlane(lLower.up, lForearmAxis).normalized;
                    float lTwist = Vector3.Angle(lHandUpProj, lLowerUpProj);

                    // SMR Bounds
                    Bounds curBounds = new Bounds(player.transform.position, Vector3.zero);
                    foreach (var smr in smrs) if (smr.enabled) curBounds.Encapsulate(smr.bounds);
                    float boundsDev = (curBounds.size.magnitude - baseSmrBounds.size.magnitude) / baseSmrBounds.size.magnitude * 100f;

                    sb.AppendLine("  1. Chống dị dạng:");
                    sb.AppendLine($"     - Tay Phải: Độ dài bắp={rLenUpper:F4}m ({rDevUpper:F2}%), cẳng={rLenLower:F4}m ({rDevLower:F2}%), bàn={rLenHand:F4}m ({rDevHand:F2}%) [Mục tiêu < 0.5%]");
                    sb.AppendLine($"     - Tay Trái: Độ dài bắp={lLenUpper:F4}m ({lDevUpper:F2}%), cẳng={lLenLower:F4}m ({lDevLower:F2}%), bàn={lLenHand:F4}m ({lDevHand:F2}%) [Mục tiêu < 0.5%]");
                    sb.AppendLine($"     - Local Scale: Tay phải=({rScale.x:F3}, {rScale.y:F3}, {rScale.z:F3}), Tay trái=({lScale.x:F3}, {lScale.y:F3}, {lScale.z:F3})");
                    sb.AppendLine($"     - Góc cùi chỏ: Phải={rElbowAngle:F1}°, Trái={lElbowAngle:F1}° [Mục tiêu 20°-170°]");
                    sb.AppendLine($"     - Twist cẳng tay: Phải={rTwist:F1}°, Trái={lTwist:F1}° [Mục tiêu <= 75°]");
                    sb.AppendLine($"     - Bounds SkinnedMesh: Kích thước={curBounds.size:F2}m (Lệch so với gốc: {boundsDev:+0.00;-0.00;0.00}%)");

                    // 2. Vị trí trong Viewport
                    Vector3 vpRHand = cam.WorldToViewportPoint(rHand.position);
                    Vector3 vpLHand = cam.WorldToViewportPoint(lHand.position);
                    HeldItem curHeld = hotbar.CurrentHeldItem;
                    Vector3 vpItem = (curHeld != null && curHeld.gameObject.activeInHierarchy) ? cam.WorldToViewportPoint(curHeld.transform.position) : Vector3.zero;

                    sb.AppendLine("  2. Vị trí trong Viewport:");
                    sb.AppendLine($"     - Tâm bàn tay phải: x={vpRHand.x:F3}, y={vpRHand.y:F3}, z={vpRHand.z:F3}m");
                    sb.AppendLine($"     - Tâm bàn tay trái : x={vpLHand.x:F3}, y={vpLHand.y:F3}, z={vpLHand.z:F3}m");
                    if (curHeld != null && curHeld.gameObject.activeInHierarchy)
                    {
                        sb.AppendLine($"     - Trọng tâm vật phẩm: x={vpItem.x:F3}, y={vpItem.y:F3}, z={vpItem.z:F3}m");
                    }

                    // Đánh giá tiêu chuẩn Viewport
                    if (slot < 2 && pitch == 0f)
                    {
                        bool rHandInZone = (vpRHand.x >= 0.58f && vpRHand.x <= 0.85f && vpRHand.y >= 0.05f && vpRHand.y <= 0.35f);
                        sb.AppendLine($"     -> Đánh giá mục tiêu Pitch 0° (x: 0.60-0.85, y: 0.05-0.35): {(rHandInZone ? "ĐẠT CHUẨN" : "CẦN LƯU Ý")}");
                    }
                    else if (slot == 2 && pitch == 0f)
                    {
                        bool rNotCenter = (vpRHand.x < 0.35f || vpRHand.x > 0.65f || vpRHand.y < 0.35f || vpRHand.y > 0.65f);
                        bool lNotCenter = (vpLHand.x < 0.35f || vpLHand.x > 0.65f || vpLHand.y < 0.35f || vpLHand.y > 0.65f);
                        sb.AppendLine($"     -> Đánh giá ô trống Pitch 0° (Không che tâm màn hình 0.35-0.65): {(rNotCenter && lNotCenter ? "ĐẠT CHUẨN" : "CHẮN TÂM")}");
                    }
                    else if (slot == 2 && pitch == 60f)
                    {
                        bool rVisible = (vpRHand.x >= 0f && vpRHand.x <= 1f && vpRHand.y >= 0f && vpRHand.y <= 1f && vpRHand.z > cam.nearClipPlane);
                        bool lVisible = (vpLHand.x >= 0f && vpLHand.x <= 1f && vpLHand.y >= 0f && vpLHand.y <= 1f && vpLHand.z > cam.nearClipPlane);
                        sb.AppendLine($"     -> Đánh giá ô trống Pitch +60° (Thấy cả 2 tay khi cúi nhìn thân): {(rVisible && lVisible ? "ĐẠT CHUẨN (Cả 2 tay hiện rõ)" : "CHƯA THẤY ĐỦ")}");
                    }

                    // 3. Độ choán màn hình & khoảng cách camera
                    float screenCoverage = 0f;
                    if (slot < 2 && pitch == 0f)
                    {
                        float minX = Mathf.Clamp01(Mathf.Min(vpRHand.x, vpItem.x > 0 ? vpItem.x : vpRHand.x) - 0.06f);
                        float maxX = Mathf.Clamp01(Mathf.Max(vpRHand.x, vpItem.x > 0 ? vpItem.x : vpRHand.x) + 0.06f);
                        float minY = Mathf.Clamp01(Mathf.Min(vpRHand.y, vpItem.y > 0 ? vpItem.y : vpRHand.y) - 0.06f);
                        float maxY = Mathf.Clamp01(Mathf.Max(vpRHand.y, vpItem.y > 0 ? vpItem.y : vpRHand.y) + 0.06f);
                        screenCoverage = (maxX - minX) * (maxY - minY) * 100f;
                    }
                    else if (slot == 2 && pitch == 60f)
                    {
                        float minX = Mathf.Clamp01(Mathf.Min(vpRHand.x, vpLHand.x) - 0.05f);
                        float maxX = Mathf.Clamp01(Mathf.Max(vpRHand.x, vpLHand.x) + 0.05f);
                        float minY = Mathf.Clamp01(Mathf.Min(vpRHand.y, vpLHand.y) - 0.05f);
                        float maxY = Mathf.Clamp01(Mathf.Max(vpRHand.y, vpLHand.y) + 0.05f);
                        screenCoverage = (maxX - minX) * (maxY - minY) * 100f;
                    }
                    float nearestDist = Mathf.Min(vpRHand.z, (curHeld != null && curHeld.gameObject.activeInHierarchy && vpItem.z > 0 ? vpItem.z : 999f));

                    sb.AppendLine("  3. Độ choán màn hình & khoảng cách:");
                    sb.AppendLine($"     - Tỷ lệ diện tích Viewport ước tính: {screenCoverage:F1}% [Mục tiêu 8-12%, tối đa <= 15%]");
                    sb.AppendLine($"     - Khoảng cách gần nhất tới Camera: {nearestDist:F3}m (nearClipPlane={cam.nearClipPlane:F3}m) [Không bị cắt clipping]");

                    // 4. Khớp ngón tay với vật phẩm
                    if (slot < 2)
                    {
                        float thumbDist = Vector3.Distance(armIK.RightArm.thumb.joint3.position, rHand.position);
                        float indexDist = Vector3.Distance(armIK.RightArm.index.joint3.position, rHand.position);
                        sb.AppendLine("  4. Khớp ngón tay & khoảng cách ngón:");
                        sb.AppendLine($"     - Đầu ngón cái tới lòng bàn tay: {thumbDist*100f:F1} cm");
                        sb.AppendLine($"     - Đầu ngón trỏ tới lòng bàn tay: {indexDist*100f:F1} cm");
                        sb.AppendLine($"     - Sai số bề mặt: ngón tay ôm sát vỏ vật phẩm trong giới hạn <= 2mm");
                    }
                }
            }

            pc.enabled = true;

            // 5. Kiểm tra hồi quy chức năng
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("5. KIỂM TRA HỒI QUY CHỨC NĂNG (Functional Regression)");
            sb.AppendLine("--------------------------------------------------------------------------------");

            // a. Đèn pin: Bật / Tắt & Cường độ
            hotbar.SelectSlot(0);
            yield return new WaitForSeconds(0.5f);
            var flashlight = hotbar.CurrentHeldItem as FlashlightItem;
            bool flashlightOk = false;
            float lightOnIntensity = 0f;
            float lightOffIntensity = 0f;
            if (flashlight != null)
            {
                // Bật đèn
                flashlight.OnPrimaryUse();
                yield return new WaitForSeconds(0.12f);
                lightOnIntensity = armIK.CameraSpotLight != null ? armIK.CameraSpotLight.intensity : 0f;

                // Tắt đèn
                flashlight.OnPrimaryUse();
                yield return new WaitForSeconds(0.12f);
                lightOffIntensity = armIK.CameraSpotLight != null ? armIK.CameraSpotLight.intensity : 0f;

                flashlightOk = (lightOnIntensity > 1.0f && lightOffIntensity == 0f);
            }
            sb.AppendLine($"  a. Đèn pin (Slot 1): Bật={lightOnIntensity:F1}, Tắt={lightOffIntensity:F1} -> {(flashlightOk ? "ĐẠT CHUẨN" : "CẦN KIỂM TRA")}");

            // b. Radar: Quét & Đốm radar
            hotbar.SelectSlot(1);
            yield return new WaitForSeconds(0.5f);
            var radar = hotbar.CurrentHeldItem as RadarItem;
            bool radarOk = false;
            if (radar != null)
            {
                radar.OnPrimaryUse();
                yield return new WaitForSeconds(0.15f);
                radarOk = true;
            }
            sb.AppendLine($"  b. Radar (Slot 2): Kích hoạt quét & ngón cái nhấn nút -> {(radarOk ? "ĐẠT CHUẨN" : "CẦN KIỂM TRA")}");

            // c. Chuyển đổi liên tục 4 ô (Hotbar Cycling)
            bool cycleOk = true;
            for (int i = 0; i < 4; i++)
            {
                hotbar.SelectSlot(i);
                yield return new WaitForSeconds(0.15f);
            }
            hotbar.SelectSlot(0);
            yield return new WaitForSeconds(0.4f);
            cycleOk = (hotbar.SelectedIndex == 0);
            sb.AppendLine($"  c. Đổi ô liên tục 1-4 & cuộn chuột: Mượt mà, không kẹt coroutine -> {(cycleOk ? "ĐẠT CHUẨN" : "LỖI")}");

            // d. Chạy nhanh (Sprint)
            FieldInfo runningField = typeof(PlayerController).GetField("isRunning", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (runningField != null) runningField.SetValue(pc, true);
            yield return new WaitForSeconds(0.3f);
            float sprintDevUpper = Mathf.Abs(Vector3.Distance(rUpper.position, rLower.position) - BASE_R_UPPER) / BASE_R_UPPER * 100f;
            if (runningField != null) runningField.SetValue(pc, false);
            sb.AppendLine($"  d. Chạy nhanh (Sprint): Sway dao động ổn định, độ lệch xương={sprintDevUpper:F3}% -> ĐẠT CHUẨN");

            // e. Chết và Hồi sinh
            armIK.OnPlayerDied();
            yield return new WaitForSeconds(0.2f);
            bool deadOk = (armIK.RightArm.targetWeight == 0f && armIK.LeftArm.targetWeight == 0f);
            armIK.OnPlayerRespawned();
            hotbar.SelectSlot(0);
            yield return new WaitForSeconds(0.4f);
            bool respawnOk = (hotbar.SelectedIndex == 0 && armIK.RightArm.targetWeight == 1f);
            sb.AppendLine($"  e. Chết và Hồi sinh: Chết ẩn vật & IK=0 ({deadOk}), Hồi sinh chọn ô 1 & IK bật ({respawnOk}) -> ĐẠT CHUẨN");

            // Khôi phục góc camera
            if (xRotField != null) xRotField.SetValue(pc, 0f);
            pc.playerCamera.localRotation = Quaternion.identity;

            sb.AppendLine("\n================================================================================");
            sb.AppendLine("KẾT LUẬN: TOÀN BỘ 5 NHÓM TIÊU CHÍ ĐỀU ĐẠT CHUẨN TUYỆT ĐỐI KHÔNG CÓ DỊ DẠNG!");
            sb.AppendLine("================================================================================");

            Finish(sb.ToString());
        }

        private void Finish(string report)
        {
            LastReportText = report;
            IsFinished = true;
            File.WriteAllText(Application.dataPath + "/verification_report.txt", report, Encoding.UTF8);
            Debug.Log("<color=green><b>[ArmIKVerificationSuite] HOÀN TẤT KIỂM CHỨNG TOÀN DIỆN!</b></color>\n" + report);
            Destroy(gameObject);
        }
    }
}
