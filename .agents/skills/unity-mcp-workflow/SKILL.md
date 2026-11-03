---
name: unity-mcp-workflow
description: >-
  Standard operating procedure and safety rules for Unity 6 Horror Game project.
  Use this skill whenever modifying scenes, prefabs, C# scripts, animation rigging,
  inventory slots (Flashlight, Radar), character arms IK, lighting, or UI via Unity MCP.
---

# Unity MCP Workflow & Safety Runbook

Quy chuẩn vận hành chuẩn (SOP) và quy tắc an toàn bắt buộc khi tương tác với dự án Unity 6 Horror Game thông qua Unity MCP.

---

## 1. Quy Tắc Play Mode An Toàn (Safe Play Mode Workflow)

### Nguyên tắc
Không bao giờ chỉnh sửa file Scene (`.unity`), Prefab (`.prefab`) hoặc ScriptableObject khi Unity Editor đang chạy Play Mode, vì các thay đổi trên Hierarchy/Inspector sẽ bị Unity hủy bỏ hoặc hoàn tác ngay khi thoát Play Mode.

### Quy trình chuẩn:
1. **Kiểm tra trạng thái:**
   ```csharp
   // Chạy qua unityMCP execute_code:
   return UnityEditor.EditorApplication.isPlaying ? "PLAYING" : "EDIT_MODE";
   ```
2. **Nếu đang `PLAYING` và cần sửa đổi Scene/Prefab:**
   - Gọi `manage_editor` với action `"stop"`.
   - Đợi Editor chuyển về Edit Mode hoàn toàn.
3. **Thực hiện chỉnh sửa và lưu:**
   - Thực hiện chỉnh sửa qua code/Inspector.
   - Luôn gọi `EditorUtility.SetDirty(target)` và `EditorSceneManager.MarkSceneDirty(scene)`.
   - Lưu Scene: `EditorSceneManager.SaveScene(scene)`.

---

## 2. Chống Bẫy SerializedField (Serialized Property Precedence)

### Nguyên tắc
Khi một thuộc tính trong file C# được khai báo là `[SerializeField]` hoặc `public`, giá trị được lưu trong Scene YAML / Prefab YAML trên Inspector **luôn được ưu tiên nạp đè** lên giá trị mặc định của C#.

### Quy trình chuẩn:
Khi cập nhật tham số (ví dụ: `radarCameraOffset`, `radarWristEulerOffset`, v.v.):
1. **Cập nhật mã nguồn C#**: Cập nhật giá trị khởi tạo mặc định trong file `.cs`.
2. **Đồng bộ hóa Inspector ở Edit Mode:**
   ```csharp
   var player = GameObject.Find("Player");
   var armIK = player.GetComponent<HorrorGame.Inventory.PlayerArmIK>();
   var so = new UnityEditor.SerializedObject(armIK);
   so.Update();
   var prop = so.FindProperty("radarCameraOffset");
   if (prop != null) prop.vector3Value = new Vector3(-0.045f, 0.025f, -0.055f);
   so.ApplyModifiedProperties();
   UnityEditor.EditorUtility.SetDirty(armIK);
   UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.scene);
   UnityEditor.SceneManagement.EditorSceneManager.SaveScene(player.scene);
   ```
3. **Hoặc khóa cứng ở Runtime:** Nếu giá trị đó phải là bất biến và không muốn bị phụ thuộc vào Inspector, gán trực tiếp tại dòng cuối cùng của `LateUpdate()` hoặc trong `Awake()`.

---

## 3. Cô Lập Tính Năng (Feature Isolation Rule)

### Nguyên tắc
Tách biệt hoàn toàn logic và cấu hình giữa các Slot vật phẩm và các phân hệ khác nhau.

### Quy tắc bất di bất dịch:
- Khi tinh chỉnh **Slot 2 (Radar)**:
  - **TUYỆT ĐỐI KHÔNG** thay đổi Transform, Offset, Pose ngón tay, SpotLight hay IK của **Slot 1 (Flashlight)**.
  - Tách riêng nhánh `if (currentItemData.ItemType == ItemType.Radar)` và `if (currentItemData.ItemType == ItemType.Flashlight)`.
  - Không sửa đổi chung vào biến dùng chung nếu chưa kiểm tra hồi quy trên toàn bộ các slot còn lại.

---

## 4. Quy Trình Tự Kiểm Tra Thị Giác (MCP Visual Verification Runbook)

### Nguyên tắc
AI không được kết luận "đã sửa xong" chỉ dựa trên việc code biên dịch thành công. Mọi thay đổi liên quan đến trực quan (vị trí tay, góc xoay vật thể, chùm sáng đèn pin, màn hình radar, UI) bắt buộc phải được AI tự nhìn thấy bằng mắt thông qua công cụ chụp ảnh của Unity MCP.

### Các bước thực hiện bắt buộc:
1. **Kiểm tra lỗi Console:**
   - Gọi tool `read_console`. Đảm bảo 0 lỗi đỏ (`Error`/`Exception`).
2. **Vào Play Mode:**
   - Gọi tool `manage_editor` (action: `"play"`).
3. **Kích hoạt trạng thái cần kiểm tra:**
   ```csharp
   // Chọn Slot 2 (Radar)
   var hotbar = HorrorGame.Inventory.HotbarInventory.Instance;
   hotbar.SelectSlot(1);

   // Đặt góc Pitch mong muốn (ví dụ: nhìn thẳng 0° hoặc nhìn sàn 60°)
   var pc = GameObject.FindWithTag("Player").GetComponent<PlayerController>();
   var xRotField = typeof(PlayerController).GetField("xRotation", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
   xRotField.SetValue(pc, 0f);
   pc.playerCamera.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
   ```
4. **Chụp ảnh Game View:**
   - Gọi `MCPVisionCapture.Capture("Temp/mcp_view_check.png")`.
   - *Lưu ý:* Luôn đợi 1 frame sau khi đổi trạng thái để `LateUpdate` và camera hoàn tất render.
5. **Tự đọc ảnh thẩm định:**
   - Gọi tool `view_file` mở trực tiếp file `Temp/mcp_view_check.png`.
   - Kiểm tra bằng mắt:
     - 4 ngón tay có bám sát vào thành vật thể không?
     - Ngón cái có đúng vị trí kẹp giữ không?
     - Màn hình/chùm sáng có hướng trực diện vào mắt người chơi không?
     - Có hiện tượng xuyên hình (mesh clipping) không?
6. **Thoát Play Mode:**
   - Gọi tool `manage_editor` (action: `"stop"`) để trả Editor về trạng thái nghỉ sạch sẽ.
