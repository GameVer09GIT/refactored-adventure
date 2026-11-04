# ASSETS_NEEDED — Khóa Hồn

Danh sách asset còn thiếu. Trong lúc chờ, dự án dùng placeholder (primitive, X Bot, material tối màu).
Mức ưu tiên: **P1** = ảnh hưởng hiểu cốt truyện / gameplay · **P2** = ảnh hưởng không khí · **P3** = trang trí.

| Tên | Mô tả | Nơi dùng | Ưu tiên | Placeholder hiện tại |
|---|---|---|---|---|
| Bài vị ×13 | 5 bài vị Mắt Trận khắc tên hành, 8 bài vị không tên | Phòng Thờ | P1 | Khối hộp gỗ + TMP serif |
| Đài sen ×5 | Đài sen gỗ/đồng có chỗ đặt bùa | Phòng Thờ | P1 | Cylinder dẹt |
| Trang bùa ×5 | Giấy vàng, chữ đỏ, ký hiệu từng hành | Phòng Thờ, Sổ tay | P1 | Quad + texture màu |
| Ấn Đồng | Ấn đồng cổ, có thể nứt theo 3 mức | Sảnh Tế | P1 | Cube vàng đồng |
| Vạc máu đen | Vạc lớn, mặt chất lỏng đen bóng, ánh đỏ | Sảnh Tế | P1 | Cylinder + material đen bóng |
| Búp bê vải | Búp bê bé An | Phòng Trẻ Em | P1 | Capsule nhỏ |
| Huyết thư | Thư cầu cứu nhòe máu | Phòng Khách Trọ | P1 | Quad giấy |
| Lược sừng gãy | Lược của bà Quản Gia | Phòng Gia Nhân | P1 | Khối mỏng |
| Búa đóng đinh | Búa đẫm máu của Thợ Cả | Thư Viện Cổ, Kết 1 | P1 | Cylinder + cube |
| Đôi hài thêu | Hài của Nhị nương | Phòng Ngủ Chủ Nhân | P1 | 2 khối nhỏ đỏ |
| Còi nhựa | Còi kỷ niệm của Quân | Xà lim, Kết 1 | P1 | Capsule nhỏ |
| Máy ghi âm cassette | Model cầm tay: hộp, 2 cuộn băng, LED | Hotbar ô 2 | P1 | Hộp + 2 cylinder + LED emissive |
| Máy ảnh film có flash | Model cầm tay | Hotbar ô 3 | P1 | Hộp + cylinder ống kính |
| Xà beng | Model cầm tay | Hotbar ô 4 | P1 | Cylinder cong |
| Balo của Quân | Balo phóng viên | Sảnh Chính | P2 | Khối hộp |
| Nến | Nến leo lét | Phòng Thờ | P2 | Cylinder + point light nhỏ |
| Xích sắt ×5 | Xích nối nắp hầm vào tường, đứt được | Nhà Bếp | P1 | Chuỗi cylinder |
| Nắp hầm sắt | Nắp hầm kiên cố | Nhà Bếp | P1 | Plane + cube |
| Két sắt | Két có bàn phím 4 số | Phòng Làm Việc | P1 | Cube xám |
| Bảng đồng | Bảng khắc "Khởi công Thanh Vân — 1921" | Phòng Khách | P1 | Quad đồng + TMP serif |
| Ảnh gia tộc Trịnh | Ảnh khổ lớn | Phòng Khách | P2 | `painting_*` (ZNS3D) / Quad |
| Xương phu hồ, vách vữa | Mesh/decal xương lộ trong vách | Cầu thang hầm | P2 | Decal tối |
| Thi thể Quân | Nhân vật nam nằm nghiêng, áo phóng viên | Xà lim | P1 | X Bot, material tối |
| Hồn Quân | Bản bán trong suốt | Kết 1, Kết 2 | P1 | X Bot + material phát sáng |
| Trịnh Đại Quan | Quý ông trung niên, áo the, khói đen | Sảnh Tế | P1 | X Bot + material khói bán trong suốt |
| Ghế gỗ mun | Ghế cổ | Sảnh Tế | P3 | `ChairLeather` + material gỗ đen |
| Bà Quản Gia (áo bà ba đen) | Biến thể visual của Ghost, chùm chìa khóa ở hông | Tầng 2, Kết ẩn | P1 | `Ghost.prefab` variant + material đen + mesh chìa khóa |
| Xe tải | Xe tải đêm | Kết ẩn | P2 | `Prop_Car_A` phóng to, đổi màu |
| Lồng tiếng băng #1–#3 | Giọng Quân | Băng ghi âm | P1 | Chỉ phụ đề |
| Lồng tiếng Trịnh Đại Quan | Giọng trầm, chậm | Sảnh Tế | P1 | Chỉ phụ đề |
| Giọng bà Quản Gia, hát ru | Lời tự sáng tác | Tầng 2 | P2 | `ghost_moan_loop` |
| Âm thanh chùm chìa khóa kéo lê | Loop 3D | Bà Quản Gia | P1 | Tạm `3D_Taps` / procedural |
| Âm thanh mưa, sấm | Loop ngoài trời | Sân, cổng | P2 | — |
| Âm thanh màn trập + flash | One-shot | Máy ảnh | P2 | — |
| Tiếng rè cassette | Loop | Máy ghi âm | P2 | — |

| Icon hotbar ×3 | Sprite cho Máy ghi âm, Máy ảnh film, Xà beng | HotbarUI | P2 | Ô trống (icon null) |
| Pose cầm tay ×3 | Chỉnh IK/offset cầm máy ghi âm, máy ảnh, xà beng (hiện mượn Item_Radar) | Hotbar ô 2–4 | P1 (Phase 3) | Cầm được nhưng xuyên tay |

## TODO nội dung
- **Bài chú trên 5 trang bùa**: GDD và PROMPT chưa có lời. Hiện để `[Câu chú n — chờ nội dung]` trong `Data/Talismans/TAL_*.asset`. Cần 4 câu/trang trước Phase 4 (Kết 2 đọc ngược bài chú trang Hỏa).
- Thẻ phóng viên của Quân: GDD chưa có tên tòa soạn. Hiện để "THẺ PHÓNG VIÊN — Hạ Quân — Phóng viên ảnh".
