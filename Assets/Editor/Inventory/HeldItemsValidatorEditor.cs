using UnityEditor;
using UnityEngine;

namespace HorrorGame.Inventory.Editor
{
    /// <summary>
    /// Công cụ kiểm tra hợp lệ toàn bộ vật phẩm cầm tay trong dự án.
    /// 
    /// HƯỚNG DẪN CHO TÁC GIẢ THÊM VẬT PHẨM MỚI:
    /// =========================================================================================
    /// 1. Tạo Prefab mô hình 3D cho vật phẩm cầm tay (đặt trong Assets/Prefabs/Items/).
    /// 2. Tạo một Transform rỗng làm con của Prefab:
    ///    - Nếu cầm tay phải: đặt tên là "GripRight" tại vị trí lòng bàn tay nắm vào.
    ///    - Nếu cầm tay trái: đặt tên là "GripLeft" tại vị trí lòng bàn tay nắm vào.
    ///    - Nếu cầm hai tay: tạo cả "GripRight" và "GripLeft".
    /// 3. Tạo một ScriptableObject ItemData (Assets/Items/):
    ///    - Chọn GripType (OneHandRight / OneHandLeft / TwoHands).
    ///    - Chọn HandPose tương ứng (ví dụ: Pose_GripCylinder, Pose_HoldDevice, Pose_OpenRelaxed).
    ///    - Gán HeldPrefab trỏ tới Prefab vừa tạo.
    /// 4. KHÔNG CẦN VIẾT LẠI HOẶC SỬA BẤT KỲ DÒNG CODE NÀO CỦA HỆ THỐNG BÀN TAY!
    /// =========================================================================================
    /// </summary>
    public static class HeldItemsValidatorEditor
    {
        [MenuItem("Tools/Inventory/Validate Held Items")]
        public static void ValidateAllHeldItems()
        {
            Debug.Log("<color=cyan><b>[HeldItemsValidator] BẮT ĐẦU DUYỆT KIỂM TRA TOÀN BỘ ITEMDATA TRONG DỰ ÁN...</b></color>");

            string[] guids = AssetDatabase.FindAssets("t:ItemData");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[HeldItemsValidator] Không tìm thấy asset ItemData nào trong dự án!");
                return;
            }

            int validCount = 0;
            int warningCount = 0;

            // Nạp tư thế mặc định dự phòng GripCylinder
            HandPose defaultGripPose = AssetDatabase.LoadAssetAtPath<HandPose>("Assets/Items/Poses/Pose_GripCylinder.asset");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item == null) continue;

                Debug.Log($"<b>Đang kiểm tra: '{item.DisplayName}' ({item.name}) tại {path}</b>");

                if (item.HeldPrefab == null)
                {
                    Debug.LogWarning($"  [CẢNH BÁO] Item '{item.DisplayName}' chưa gán HeldPrefab!");
                    warningCount++;
                    continue;
                }

                GameObject prefab = item.HeldPrefab;
                Transform gripR = prefab.transform.Find("GripRight");
                Transform gripL = prefab.transform.Find("GripLeft");

                bool itemHasIssue = false;

                switch (item.GripType)
                {
                    case GripType.OneHandRight:
                        if (gripR == null)
                        {
                            Debug.LogError($"  [LỖI] Prefab '{prefab.name}' thiếu Transform con 'GripRight' cho kiểu cầm OneHandRight!");
                            itemHasIssue = true;
                        }
                        if (item.RightPose == null)
                        {
                            Debug.LogWarning($"  [CẢNH BÁO] Item '{item.DisplayName}' chưa gán RightPose! Đang tự động gán tư thế mặc định (Pose_GripCylinder).");
                            if (defaultGripPose != null)
                            {
                                SerializedObject so = new SerializedObject(item);
                                so.FindProperty("rightPose").objectReferenceValue = defaultGripPose;
                                so.ApplyModifiedProperties();
                            }
                            warningCount++;
                        }
                        break;

                    case GripType.OneHandLeft:
                        if (gripL == null)
                        {
                            Debug.LogError($"  [LỖI] Prefab '{prefab.name}' thiếu Transform con 'GripLeft' cho kiểu cầm OneHandLeft!");
                            itemHasIssue = true;
                        }
                        if (item.LeftPose == null)
                        {
                            Debug.LogWarning($"  [CẢNH BÁO] Item '{item.DisplayName}' chưa gán LeftPose! Đang tự động gán tư thế mặc định (Pose_GripCylinder).");
                            if (defaultGripPose != null)
                            {
                                SerializedObject so = new SerializedObject(item);
                                so.FindProperty("leftPose").objectReferenceValue = defaultGripPose;
                                so.ApplyModifiedProperties();
                            }
                            warningCount++;
                        }
                        break;

                    case GripType.TwoHands:
                        if (gripR == null)
                        {
                            Debug.LogError($"  [LỖI] Prefab '{prefab.name}' thiếu Transform con 'GripRight' cho kiểu cầm TwoHands!");
                            itemHasIssue = true;
                        }
                        if (gripL == null)
                        {
                            Debug.LogError($"  [LỖI] Prefab '{prefab.name}' thiếu Transform con 'GripLeft' cho kiểu cầm TwoHands!");
                            itemHasIssue = true;
                        }
                        break;

                    case GripType.None:
                        Debug.Log($"  [INFO] Item '{item.DisplayName}' có GripType = None (ẩn cả 2 tay).");
                        break;
                }

                if (itemHasIssue)
                {
                    warningCount++;
                }
                else
                {
                    validCount++;
                    Debug.Log($"  [HỢP LỆ] '{item.DisplayName}': Grip={item.GripType}, Điểm nắm & Tư thế hoàn chỉnh.");
                }
            }

            Debug.Log($"<color=green><b>[HeldItemsValidator] HOÀN TẤT KIỂM TRA: {validCount} vật phẩm hợp lệ, {warningCount} cảnh báo.</b></color>");
        }
    }
}
