# Unity 6 Horror Game - Quy Tắc Dự Án & Antigravity Workflow Rules

Mọi phiên làm việc và tác vụ của AI trong workspace này bắt buộc phải tuân thủ nghiêm ngặt 4 nguyên tắc sau:

---

## 1. QUY TẮC PLAY MODE AN TOÀN (SAFE PLAY MODE EDITING)
- **Kiểm tra trạng thái trước khi sửa Scene/Prefab:** Trước khi dùng Unity MCP hoặc C# reflection để chỉnh sửa bất kỳ Scene (`.unity`), Prefab (`.prefab`), ScriptableObject (`.asset`) hay thuộc tính Inspector nào, **BẮT BUỘC** kiểm tra Editor có đang ở chế độ Play Mode (`EditorApplication.isPlaying`) hay không.
- **Dừng Play Mode trước khi lưu:** Nếu Editor đang ở Play Mode, phải gọi lệnh `manage_editor` (action: `stop`) để thoát Play Mode về Edit Mode trước khi ghi đè hoặc lưu dữ liệu (`EditorSceneManager.SaveScene`, `AssetDatabase.SaveAssets`) nhằm tránh bị mất dữ liệu hoặc reset ngầm khi thoát Play Mode.

---

## 2. CHỐNG BẪY SERIALIZED FIELD (SERIALIZED PROPERTY PRECEDENCE)
- **Hiểu rõ thứ tự ưu tiên serialize:** Khi thay đổi các giá trị cấu hình mặc định (vị trí `Vector3`, góc xoay `Quaternion`/`Vector3`, tốc độ `float`, tầm nhìn, v.v.) trong file `.cs`, giá trị đã được Unity serialize và lưu trong file YAML của Scene/Prefab trên Inspector sẽ **LUÔN GHI ĐÈ** giá trị mặc định trong mã nguồn C#.
- **Biện pháp bắt buộc (chọn 1 trong 2 hoặc kết hợp cả 2):**
  1. *Đồng bộ Inspector qua MCP:* Sử dụng `execute_code` ở Edit Mode để cập nhật trực tiếp `SerializedProperty` trên GameObject trong Scene/Prefab, gọi `EditorUtility.SetDirty()`, đánh dấu `MarkSceneDirty()` và lưu scene.
  2. *Khóa cứng ở Runtime:* Nếu biến cần giữ giá trị tuyệt đối không phụ thuộc vào Inspector, ghi đè cưỡng bức giá trị ở cuối hàm `LateUpdate()` hoặc trong `Awake()`/`Start()`.

---

## 3. CÔ LẬP TÍNH NĂNG (FEATURE ISOLATION RULE)
- **Tuyệt đối không can thiệp chéo:** Khi người dùng yêu cầu sửa một Slot đồ (ví dụ: Slot 2 - Radar) hoặc một tính năng cụ thể:
  - **TUYỆT ĐỐI KHÔNG** sửa đổi bất kỳ logic, tham số Transform, Pose IK, cường độ ánh sáng, góc xoay hay mã nguồn của các Slot/tính năng khác đã hoạt động ổn định (đặc biệt là Slot 1 - Flashlight).
  - Tách bạch logic xử lý theo từng `ItemType` hoặc component riêng biệt. Bất kỳ thay đổi dùng chung nào đều phải kiểm tra hồi quy trên các slot khác.

---

## 4. QUY TRÌNH TỰ KIỂM TRA THỊ GIÁC (MCP VISUAL VERIFICATION WORKFLOW)
Với **MỌI** tác vụ liên quan đến UI/HUD, Camera, Góc nhìn, Ánh sáng, Vị trí vật thể 3D, Cầm nắm đồ vật hoặc Animation trên nhân vật `civilian_girl`, sau khi sửa đổi code **BẮT BUỘC** thực hiện quy trình 3 bước trước khi báo cáo hoàn thành:
1. **Kiểm tra Console:** Đọc Unity Console (`read_console`) đảm bảo 0 lỗi biên dịch (Compilation Errors) và 0 ngoại lệ lúc chạy (Runtime Exceptions).
2. **Chụp ảnh thực tế Game View:** Vào Play Mode (`manage_editor play`), chọn đúng slot/góc nhìn cần test, gọi `MCPVisionCapture.Capture("Temp/mcp_view_check.png")`. Chú ý đợi tối thiểu 1 frame sau khi chuyển trạng thái để `LateUpdate` và camera render ổn định.
3. **Tự đọc ảnh thẩm định:** Dùng công cụ `view_file` mở trực tiếp ảnh PNG vừa chụp để kiểm tra bằng mắt (mép ngón tay, hướng vật phẩm, tỷ lệ, không bị lún/xuyên hình). Chỉ báo cáo hoàn thành khi chính mắt AI đã xác nhận hình ảnh đạt yêu cầu.
