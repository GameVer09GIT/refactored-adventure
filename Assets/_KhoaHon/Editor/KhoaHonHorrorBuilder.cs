using System.IO;
using System.Linq;
using KhoaHon.AI;
using KhoaHon.Core;
using KhoaHon.Horror;
using KhoaHon.Interaction;
using KhoaHon.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KhoaHon.EditorTools
{
    /// <summary>
    /// Lớp "kinh dị": texture máu sinh bằng thuật toán, rải máu/xương/xác theo cốt truyện từng phòng, máu nhỏ giọt,
    /// bà Quản Gia đẫm máu, hậu kỳ, âm thanh, menu/tạm dừng/cài đặt/HUD. Gọi cuối KhoaHonVillaConverter.
    /// </summary>
    public static class KhoaHonHorrorBuilder
    {
        const string TexDir = "Assets/_KhoaHon/Textures/Gore";
        const string MatDir = "Assets/_KhoaHon/Materials/Gore";
        const string Models = "Assets/_KhoaHon/Models/Props/";
        // Mỗi loại máu có vài biến thể (seed khác nhau) để không lặp hình
        static Material[] mSplat, mPool, mDrag, mHand, mHandSmall, mHandSmear, mScorch, mDrip;
        static Material mCorpse, mParticle;
        static Transform root;

        public static string Build(Transform parent, Vector3 cellarOrigin)
        {
            KhoaHonDataBuilder.EnsureFolder(TexDir); KhoaHonDataBuilder.EnsureFolder(MatDir);
            root = new GameObject("KH_Horror").transform; root.SetParent(parent);
            BuildTexturesAndMaterials();
            int decals = PlaceGore(cellarOrigin);
            PlaceBonesAndCorpse(cellarOrigin);
            BuildDrips(cellarOrigin);
            BloodyHousekeeper();
            BuildPostFx();
            WireSystems();
            return $"Horror layer ok (decals={decals})";
        }

        // ================================================================== Máu: URP Decal chiếu lên bề mặt
        static void BuildTexturesAndMaterials()
        {
            Material[] Set(string name, KhoaHonBloodTextures.Kind kind, bool wet, params int[] seeds)
            {
                var arr = new Material[seeds.Length];
                for (int i = 0; i < seeds.Length; i++) arr[i] = DMat(name + "_" + seeds[i], KhoaHonBloodTextures.Build(kind, seeds[i]), wet);
                return arr;
            }
            mSplat = Set("MD_Blood_Splat", KhoaHonBloodTextures.Kind.Splat, true, 11, 21, 31).Concat(Set("MD_Blood_SplatWide", KhoaHonBloodTextures.Kind.SplatWide, true, 41)).ToArray();
            mPool = Set("MD_Blood_Pool", KhoaHonBloodTextures.Kind.Pool, true, 12, 22);
            mDrag = Set("MD_Blood_Drag", KhoaHonBloodTextures.Kind.Drag, true, 13, 23);
            mHand = Set("MD_Blood_Hand", KhoaHonBloodTextures.Kind.Hand, true, 14, 24);
            mHandSmear = Set("MD_Blood_HandSmear", KhoaHonBloodTextures.Kind.HandSmear, true, 34);
            mHand = mHand.Concat(mHandSmear).ToArray();
            mHandSmall = Set("MD_Blood_HandChild", KhoaHonBloodTextures.Kind.HandChild, true, 15, 25);
            mDrip = Set("MD_Blood_Drips", KhoaHonBloodTextures.Kind.Drips, true, 17, 27);
            mScorch = Set("MD_Scorch", KhoaHonBloodTextures.Kind.Scorch, false, 16);
            mCorpse = PlainMat("M_Corpse_Skin", new Color(0.28f, 0.27f, 0.25f), 0.25f);
            mParticle = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_BloodDrop.mat");
            if (mParticle == null)
            {
                mParticle = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mParticle, MatDir + "/M_BloodDrop.mat");
            }
            mParticle.SetColor("_BaseColor", new Color(0.22f, 0.006f, 0.005f, 1f));
        }

        /// <summary>Vật liệu decal URP chuẩn (albedo + normal map độ dày giúp máu nổi khối, bắt sáng theo bề mặt bên dưới).</summary>
        static Material DMat(string name, KhoaHonBloodTextures.Pair p, bool wet)
        {
            string path = MatDir + "/" + name + ".mat";
            var sh = Shader.Find("Shader Graphs/Decal");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            else m.shader = sh;
            m.SetTexture("Base_Map", p.baseMap);
            m.SetTexture("Normal_Map", p.normalMap);
            m.SetFloat("Normal_Blend", 0.9f);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Hình loang mềm dùng cho texture "thấm máu" của bà Quản Gia.</summary>
        static float Blob(float u, float v, float cx, float cy, float rad, float wobble, int k)
        {
            float dx = u - cx, dy = v - cy, d = Mathf.Sqrt(dx * dx + dy * dy);
            float ang = Mathf.Atan2(dy, dx);
            float edge = rad * (1f + wobble * (Mathf.PerlinNoise(Mathf.Cos(ang) * 2f + k, Mathf.Sin(ang) * 2f + k * 1.7f) - 0.5f) * 2f);
            return Mathf.Clamp01((edge - d) / (rad * 0.18f + 0.002f));
        }

        static Material PlainMat(string name, Color c, float smooth)
        {
            string p = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, p); }
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth);
            EditorUtility.SetDirty(m);
            return m;
        }

        // ================================================================== Rải máu
        static int count;

        /// <summary>
        /// Decal chiếu từ trên xuống (sàn) hoặc vào tường. Hộp chiếu dày nên ôm theo mọi gờ ván, bậc thang,
        /// chân bàn — không còn tấm phẳng nổi lơ lửng hay lộ khung chữ nhật.
        /// </summary>
        static DecalProjector Proj(Material[] set, Vector3 pos, Quaternion rot, float w, float h, float depth)
        {
            var m = set[count % set.Length];
            var go = new GameObject("Gore_" + m.name);
            go.transform.SetParent(root);
            go.transform.SetPositionAndRotation(pos, rot);
            var dp = go.AddComponent<DecalProjector>();
            dp.material = m;
            dp.size = new Vector3(w, h, depth);
            dp.pivot = Vector3.zero;
            dp.drawDistance = 45f;
            dp.fadeScale = 0.85f;
            go.isStatic = true;
            count++;
            return dp;
        }

        static void Floor(Material[] m, float x, float y, float z, float size, float yaw, float stretch = 1f)
            => Proj(m, new Vector3(x, y, z), Quaternion.Euler(90f, yaw, 0f), size, size * stretch, 0.5f);

        static void Wall(Material[] m, Vector3 pos, Vector3 normal, float size, float roll = 0f, float stretch = 1f)
            => Proj(m, pos, Quaternion.LookRotation(-normal, Vector3.up) * Quaternion.Euler(0f, 0f, roll), size, size * stretch, 0.4f);

        static int PlaceGore(Vector3 O)
        {
            count = 0;
            const float GF = 11f, F2 = 18f;
            var rnd = new System.Random(99);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            // --- Sảnh: vệt kéo lê từ cửa chính tới chân cầu thang, dấu tay trên cột
            Floor(mDrag, 81.2f, GF, 12.0f, 1.3f, 8f, 3.2f);
            Floor(mDrag, 88.2f, GF, 20.6f, 1.1f, 60f, 3.6f);
            Floor(mSplat, 92.5f, GF, 23.0f, 1.4f, R(0, 360));
            Wall(mHand, new Vector3(94.45f, GF + 1.35f, 20.6f), Vector3.left, 0.32f, -12f);
            Wall(mHand, new Vector3(94.45f, GF + 1.1f, 21.2f), Vector3.left, 0.3f, 18f);
            Wall(mDrip, new Vector3(82.95f, GF + 2.4f, 17.5f), Vector3.right, 1.6f, 0f, 1.4f);
            // --- Bếp (thảm sát 1978): vũng máu dưới bàn tiệc, máu bắn trên bàn, vệt kéo về nắp hầm, dấu tay quanh nắp
            Floor(mPool, 71.2f, GF, 19.8f, 2.4f, 0f);
            Floor(mSplat, 69.6f, GF, 21.4f, 1.3f, R(0, 360));
            Floor(mSplat, 73.0f, GF, 18.3f, 1.1f, R(0, 360));
            Floor(mDrag, 72.0f, GF, 23.1f, 1.0f, 95f, 3.0f);
            Wall(mDrip, new Vector3(70.0f, GF + 2.2f, 27.3f), Vector3.back, 2.2f, 0f, 1.5f);
            Wall(mHand, new Vector3(74.0f, GF + 0.9f, 27.3f), Vector3.back, 0.3f, 25f);
            Wall(mHand, new Vector3(74.6f, GF + 0.5f, 27.3f), Vector3.back, 0.28f, -8f);
            Wall(mSplat, new Vector3(66.95f, GF + 1.6f, 20.0f), Vector3.right, 1.5f, R(0, 360));
            // --- Phòng khách: máu loang dưới ảnh gia tộc
            Wall(mDrip, new Vector3(66.95f, GF + 3.6f, 11.5f), Vector3.right, 1.6f, 0f, 1.6f);
            Floor(mSplat, 68.5f, GF, 12.2f, 1.2f, R(0, 360));
            // --- Phòng làm việc (Lâm Gia Huy treo cổ 1996): vết máu khô dưới xà
            Floor(mPool, 103.5f, GF, 12.0f, 1.3f, 30f);
            // --- Cầu thang: dấu tay trượt dọc tường
            for (int i = 0; i < 6; i++) Wall(mHand, new Vector3(102.75f, GF + 1.2f + i * 0.55f, 23.0f - i * 0.9f), Vector3.right, 0.3f, R(-30, 30));
            // --- Tầng 2 hành lang: vệt máu quanh giếng trời
            Floor(mDrag, 80.7f, F2, 16.0f, 1.0f, 0f, 3.6f);
            Floor(mSplat, 98.0f, F2, 24.8f, 1.2f, R(0, 360));
            Floor(mSplat, 90.0f, F2, 9.0f, 0.9f, R(0, 360));
            // --- Trẻ Em (bé An bị nhốt trong tủ áo): dấu tay trẻ con chi chít trên tủ và tường
            for (int i = 0; i < 12; i++) Wall(mHandSmall, new Vector3(66.98f, F2 + R(0.3f, 2.3f), R(22.3f, 24.4f)), Vector3.right, R(0.14f, 0.2f), R(-40, 40)); // mảng tường trống giữa cửa sổ và góc phòng (tủ che phần còn lại)
            Floor(mSplat, 68.6f, F2, 25.0f, 0.8f, R(0, 360));
            // --- Phòng Thờ: máu loang trên sàn trước bàn thờ, chảy từ tường xuống
            Floor(mPool, 68.6f, F2, 17.5f, 1.6f, 90f);
            Wall(mDrip, new Vector3(66.95f, F2 + 2.6f, 17.5f), Vector3.right, 2.8f, 0f, 1.4f);
            // --- Gia Nhân: vệt máu trên sàn tới giường
            Floor(mDrag, 72.0f, F2, 10.5f, 0.9f, 80f, 3.0f);
            Floor(mSplat, 69.6f, F2, 9.4f, 1.0f, R(0, 360));
            // --- Ngủ Chủ Nhân (Nhị nương bị thiêu): vết cháy sém sàn + tường
            Floor(mScorch, 106.0f, F2, 9.7f, 2.4f, R(0, 360));
            Wall(mScorch, new Vector3(106.0f, F2 + 1.2f, 7.85f), Vector3.forward, 2.6f, 0f, 1.2f);
            Floor(mSplat, 104.2f, F2, 10.6f, 0.8f, R(0, 360));
            // --- Thư Viện (Thợ Cả): máu bắn trên kệ, vũng quanh búa
            Floor(mPool, 108.0f, F2, 17.8f, 1.4f, 0f);
            Wall(mSplat, new Vector3(106.95f, F2 + 1.5f, 18.5f), Vector3.right, 1.6f, R(0, 360));
            // --- Khách Trọ (bị chuốc độc): máu nôn quanh bàn
            Floor(mSplat, 107.6f, F2, 24.5f, 1.1f, R(0, 360));
            Floor(mPool, 108.3f, F2, 25.8f, 0.9f, 0f);
            // --- Thêm máu dọc lối đi chính (sảnh -> cầu thang -> hành lang tầng 2), tường tìm bằng raycast
            Physics.SyncTransforms();
            void WallRay(Material[] m, Vector3 from, Vector3 dir, float size, float roll = 0f, float stretch = 1f)
            {
                if (Physics.Raycast(from, dir, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore) && Mathf.Abs(hit.normal.y) < 0.3f)
                    Wall(m, hit.point, hit.normal, size, roll, stretch);
            }
            Floor(mSplat, 83.4f, GF, 10.2f, 1.5f, R(0, 360));
            Floor(mPool, 82.3f, GF, 13.8f, 1.2f, 30f);
            Floor(mDrag, 84.0f, GF, 16.5f, 1.2f, 75f, 3.0f);
            Floor(mSplat, 88.5f, GF, 14.0f, 1.3f, R(0, 360));
            Floor(mSplat, 90.6f, GF, 21.5f, 1.0f, R(0, 360));
            Floor(mDrag, 96.5f, GF, 22.0f, 1.1f, 100f, 3.2f);
            Floor(mPool, 99.8f, GF, 21.0f, 1.0f, 0f);
            WallRay(mSplat, new Vector3(84f, GF + 1.6f, 11f), Vector3.back, 1.6f, R(0, 360));
            WallRay(mDrip, new Vector3(88f, GF + 2.6f, 11f), Vector3.back, 2.0f, 0f, 1.5f);
            WallRay(mHand, new Vector3(86f, GF + 1.2f, 11f), Vector3.back, 0.32f, R(-25, 25));
            WallRay(mHand, new Vector3(86.4f, GF + 1.0f, 11f), Vector3.back, 0.3f, R(-25, 25));
            Floor(mSplat, 80.7f, F2, 21.0f, 1.2f, R(0, 360));
            Floor(mPool, 85.5f, F2, 25.4f, 1.3f, 0f);
            Floor(mDrag, 92.0f, F2, 25.5f, 1.1f, 90f, 3.4f);
            Floor(mSplat, 98.7f, F2, 14.0f, 1.0f, R(0, 360));
            Floor(mDrag, 98.7f, F2, 19.0f, 1.0f, 0f, 3.0f);
            Floor(mSplat, 104.0f, F2, 25.8f, 1.0f, R(0, 360));
            WallRay(mHand, new Vector3(80.7f, F2 + 1.2f, 12f), Vector3.left, 0.32f, R(-25, 25));
            WallRay(mHand, new Vector3(80.7f, F2 + 1.0f, 12.8f), Vector3.left, 0.3f, R(-25, 25));
            WallRay(mDrip, new Vector3(80.7f, F2 + 2.2f, 18f), Vector3.right, 1.8f, 0f, 1.5f);
            WallRay(mSplat, new Vector3(98.7f, F2 + 1.5f, 12f), Vector3.right, 1.4f, R(0, 360));
            WallRay(mHand, new Vector3(98.7f, F2 + 1.1f, 22f), Vector3.right, 0.3f, R(-25, 25));
            WallRay(mDrip, new Vector3(90.7f, F2 + 2.2f, 25.5f), Vector3.forward, 1.8f, 0f, 1.5f);
            // --- Hầm: máu khắp nơi
            float yb = O.y - 10f;
            for (int i = 0; i < 6; i++) Floor(mDrag, O.x + 7f - i * 2.3f, O.y - 1.6f - i * 1.65f, O.z + 5.2f, 0.9f, 90f, 2.4f);
            Floor(mPool, O.x - 6.5f, yb, O.z + 5.0f, 1.8f, 0f);
            Floor(mDrag, O.x - 6.5f, yb, O.z + 1.5f, 1.0f, 0f, 3.6f);
            Floor(mPool, O.x - 11.6f, yb, O.z + 1.6f, 2.2f, 40f); // dưới xác Quân
            Floor(mSplat, O.x - 9.5f, yb, O.z - 1.5f, 1.4f, R(0, 360));
            for (int i = 0; i < 5; i++) Wall(mHand, new Vector3(O.x - 13.0f, yb + 0.4f + i * 0.22f, O.z + 1.2f + i * 0.35f), Vector3.right, 0.28f, R(-25, 25));
            var c = O + new Vector3(-6.5f, -10f, -12f);
            Floor(mPool, c.x, yb, c.z + 1.8f, 4.2f, 0f);
            for (int i = 0; i < 8; i++) Floor(mSplat, c.x + R(-4.5f, 4.5f), yb, c.z + R(-4.5f, 4.5f), R(0.9f, 1.8f), R(0, 360));
            for (int i = 0; i < 8; i++)
            {
                var dir = Quaternion.Euler(0, i * 45f, 0) * Vector3.forward;
                if (i == 0) continue;
                Wall(mDrip, c + dir * 5.82f + Vector3.up * 2.6f, -dir, 2.6f, 0f, 1.6f);
            }
            return count;
        }

        // ================================================================== Xương, xác
        static GameObject Place(string model, Vector3 pos, Vector3 euler, float scale = 1f)
        {
            var m = AssetDatabase.LoadAssetAtPath<GameObject>(Models + model + ".fbx");
            if (m == null) return null;
            var g = (GameObject)PrefabUtility.InstantiatePrefab(m, root);
            g.transform.SetPositionAndRotation(pos, Quaternion.Euler(euler));
            g.transform.localScale = Vector3.one * scale;
            return g;
        }

        static void PlaceBonesAndCorpse(Vector3 O)
        {
            var rnd = new System.Random(7);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            float yb = O.y - 10f;
            // 8 phu hồ chôn trong vách đường hầm: sọ + xương lòi ra khỏi tường
            for (int i = 0; i < 8; i++)
            {
                float x = O.x + 7.5f - i * 1.7f, y = O.y - 1.5f - i * 1.2f + R(0.3f, 1.4f);
                bool north = i % 2 == 0;
                float z = O.z + (north ? 6.05f : 4.35f);
                Place("KH_SoNguoi", new Vector3(x, y, z), new Vector3(R(-20, 20), north ? 180f : 0f, R(-25, 25)), 1.15f);
                Place("KH_XuongDui", new Vector3(x + 0.35f, y - 0.4f, z + (north ? 0.05f : -0.05f)), new Vector3(R(50, 110), R(0, 360), 0), 1.1f);
                if (i % 3 == 0) Place("KH_LongNguc", new Vector3(x - 0.4f, y - 0.3f, z), new Vector3(0, north ? 180f : 0f, 90f), 1.0f);
            }
            // Chiếu nghỉ hầm + hành lang
            Place("KH_DongXuong", new Vector3(O.x - 7.4f, yb, O.z + 6.5f), new Vector3(0, R(0, 360), 0), 1.4f);
            Place("KH_DongXuong", new Vector3(O.x - 5.9f, yb, O.z - 4.5f), new Vector3(0, R(0, 360), 0), 1.2f);
            // Sảnh tế: xương quanh vạc, móc treo trần
            var c = O + new Vector3(-6.5f, -10f, -12f);
            for (int i = 0; i < 6; i++) Place("KH_DongXuong", c + new Vector3(Mathf.Cos(i) * 3.6f, 0f, Mathf.Sin(i) * 3.6f), new Vector3(0, R(0, 360), 0), R(1.0f, 1.6f));
            for (int i = 0; i < 4; i++) Place("KH_SoNguoi", c + new Vector3(R(-2.5f, 2.5f), 0.08f, R(-1.5f, 3.5f)), new Vector3(R(-30, 30), R(0, 360), R(-40, 40)), 1.1f);
            for (int i = 0; i < 7; i++) Place("KH_MocTreo", c + new Vector3(Mathf.Cos(i * 0.9f) * 4.2f, 5.0f, Mathf.Sin(i * 0.9f) * 4.2f), new Vector3(0, R(0, 360), 0), 1.3f);
            Place("KH_MocTreo", O + new Vector3(-6.5f, -7.0f, -2.5f), Vector3.zero, 1.3f);
            // Giẻ máu
            Place("KH_GieMau", new Vector3(71.8f, 11.01f, 21.1f), new Vector3(0, 30, 0));
            Place("KH_GieMau", new Vector3(108.5f, 18.01f, 25.0f), new Vector3(0, 120, 0));
            Place("KH_GieMau", O + new Vector3(-10.5f, -9.99f, 0.6f), new Vector3(0, 200, 0));

            // Xác Quân trong xà lim: thi thể bọc vải liệm cột dây (model Blender KH_XacQuan), máu thấm loang
            var ph = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).FirstOrDefault(t => t.name == "Body_Quan_Placeholder");
            if (ph != null)
            {
                var body = Place("KH_XacQuan", new Vector3(ph.position.x, O.y - 10f + 0.005f, ph.position.z), new Vector3(0, 70, 0), 1.05f);
                if (body != null)
                {
                    body.name = "XacQuan";
                    var col = body.AddComponent<BoxCollider>(); col.isTrigger = true; col.center = new Vector3(0, 0.2f, 0); col.size = new Vector3(0.9f, 0.5f, 1.4f);
                    body.AddComponent<InfoInteractable>().Configure("prompt.body", "");
                    Object.DestroyImmediate(ph.gameObject);
                }
            }
        }

        // ================================================================== Máu nhỏ giọt
        static void BuildDrips(Vector3 O)
        {
            void Drip(Vector3 pos, float rate)
            {
                var go = new GameObject("BloodDrip"); go.transform.SetParent(root); go.transform.position = pos;
                var ps = go.AddComponent<ParticleSystem>();
                var main = ps.main; main.startLifetime = 1.4f; main.startSpeed = 0f; main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.035f);
                main.gravityModifier = 1.0f; main.startColor = new Color(0.35f, 0.01f, 0.01f); main.maxParticles = 40; main.simulationSpace = ParticleSystemSimulationSpace.World;
                var em = ps.emission; em.rateOverTime = rate;
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(0.4f, 0.01f, 0.4f);
                var col = ps.collision; col.enabled = true; col.type = ParticleSystemCollisionType.World; col.bounce = 0f; col.lifetimeLoss = 1f;
                var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mParticle; r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.05f; r.lengthScale = 2f;
            }
            Drip(new Vector3(68.2f, 24.6f, 17.5f), 2.2f);                  // Phòng Thờ
            Drip(new Vector3(71.2f, 17.6f, 19.8f), 1.6f);                  // Bếp, trên bàn tiệc
            Drip(new Vector3(88.0f, 17.4f, 20.2f), 1.2f);                  // Sảnh, từ lan can tầng 2
            Drip(O + new Vector3(-11.6f, -7.1f, 1.6f), 2.5f);              // Xà lim
            Drip(O + new Vector3(-6.5f, -5.1f, -10.2f), 4f);               // Sảnh tế, trên vạc
        }

        // ================================================================== Bà Quản Gia đẫm máu
        /// <summary>Gán texture "thấm máu" cho vật liệu: màu nền gốc + vết loang/vệt chảy; alpha = độ bóng (máu ướt bóng hơn vải).</summary>
        static void Soak(Material m, string texName, int size, Color baseCol, float amount, int seed, float baseGloss)
        {
            var tex = SoakTex(texName, size, baseCol, amount, seed, baseGloss);
            m.SetTexture("_BaseMap", tex); m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_SmoothnessTextureChannel", 1f); m.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            m.SetFloat("_Smoothness", 0.85f);
            EditorUtility.SetDirty(m);
        }

        static Texture2D SoakTex(string name, int size, Color baseCol, float amount, int seed, float baseGloss)
        {
            string path = TexDir + "/" + name + ".png";
            var r = new System.Random(seed);
            int nb = Mathf.RoundToInt(10 + amount * 30);
            var blobs = new Vector4[nb];
            for (int i = 0; i < nb; i++) blobs[i] = new Vector4((float)r.NextDouble(), (float)r.NextDouble(), 0.02f + (float)r.NextDouble() * 0.09f * (0.5f + amount), (float)r.NextDouble());
            var px = new Color32[size * size];
            var bloodWet = new Color(0.42f, 0.025f, 0.02f); var bloodDry = new Color(0.17f, 0.012f, 0.01f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size, v = (y + 0.5f) / size;
                    float mask = 0f;
                    for (int i = 0; i < nb; i++)
                    {
                        var bl = blobs[i];
                        if (Mathf.Abs(u - bl.x) < bl.z * 1.8f && Mathf.Abs(v - bl.y) < bl.z * 1.8f) mask = Mathf.Max(mask, Blob(u, v, bl.x, bl.y, bl.z, 0.6f, i + seed));
                        // Vệt máu chảy xuống từ vết loang
                        if (bl.w > 0.35f)
                        {
                            float sx = bl.x + (bl.w - 0.5f) * bl.z, len = bl.z * (2f + bl.w * 4f);
                            float t = (bl.y - v) / len;
                            if (t > 0f && t < 1f)
                            {
                                float w = 0.006f * (1f - t * 0.7f) + 0.004f * bl.w;
                                float wob = Mathf.PerlinNoise(v * 40f, i) * 0.006f;
                                mask = Mathf.Max(mask, Mathf.Clamp01((w - Mathf.Abs(u - sx - wob)) / 0.0025f));
                            }
                        }
                    }
                    float n = Mathf.PerlinNoise(u * 30f + seed, v * 30f) * 0.6f + Mathf.PerlinNoise(u * 120f, v * 120f + seed) * 0.4f;
                    mask = Mathf.Clamp01(mask * (0.75f + n * 0.5f));
                    var cb = baseCol * (0.85f + n * 0.3f); cb.a = 1f;
                    var cbl = Color.Lerp(bloodWet, bloodDry, n);
                    var c = Color.Lerp(cb, cbl, mask);
                    c.a = Mathf.Lerp(baseGloss, 0.95f, mask);
                    px[y * size + x] = c;
                }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels32(px); tex.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath, "../" + path), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.alphaIsTransparency = false; ti.mipmapEnabled = true; ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void BloodyHousekeeper()
        {
            var hk = Object.FindFirstObjectByType<HousekeeperAI>();
            if (hk == null) return;
            // Máu, da tử thi, mắt trắng đục đã nằm sẵn trong texture Banshee (KhoaHonBansheeTextures)
            // Dáng đi + âm thanh mới
            if (hk.gameObject.GetComponent<HousekeeperCreep>() == null) hk.gameObject.AddComponent<HousekeeperCreep>();
            var so = new SerializedObject(hk);
            var breath = new GameObject("Breath"); breath.transform.SetParent(hk.transform, false); breath.transform.localPosition = new Vector3(0, 1.55f, 0.1f);
            so.FindProperty("breathSource").objectReferenceValue = breath.AddComponent<AudioSource>();
            so.FindProperty("breathLoop").objectReferenceValue = Clip("hk_breath_loop");
            so.FindProperty("screamClip").objectReferenceValue = Clip("hk_scream");
            so.ApplyModifiedPropertiesWithoutUndo();
            var music = hk.transform.Find("HuntMusic")?.GetComponent<AudioSource>();
            if (music != null) music.clip = Clip("hk_hunt_v2_loop");
        }

        static AudioClip Clip(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>(KhoaHonAudioSynth.Dir + "/" + n + ".wav");

        // ================================================================== Hậu kỳ
        static void BuildPostFx()
        {
            string p = "Assets/_KhoaHon/Data/Configs/KH_HorrorVolume.asset";
            var prof = AssetDatabase.LoadAssetAtPath<VolumeProfile>(p);
            if (prof == null) { prof = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(prof, p); }
            T Add<T>() where T : VolumeComponent
            {
                if (!prof.TryGet(out T c)) { c = prof.Add<T>(true); AssetDatabase.AddObjectToAsset(c, prof); }
                c.active = true; c.SetAllOverridesTo(true);
                return c;
            }
            // Tông màu điện ảnh: ACES, bóng lạnh xanh, vùng sáng ấm (nến/đèn dầu), tương phản vừa
            var tm = Add<Tonemapping>(); tm.mode.value = TonemappingMode.Neutral; // ACES làm cảnh tối & ám màu (đã thử)
            var ca = Add<ColorAdjustments>(); ca.postExposure.value = 0.9f; ca.contrast.value = 8f; ca.saturation.value = -10f; ca.colorFilter.value = new Color(0.88f, 0.93f, 0.96f);
            var wb = Add<WhiteBalance>(); wb.temperature.value = -4f; wb.tint.value = 0f;
            var lgg = Add<LiftGammaGain>(); lgg.lift.value = new Vector4(0.97f, 0.99f, 1.04f, 0f); lgg.gamma.value = new Vector4(1f, 1f, 1.02f, 0f); lgg.gain.value = new Vector4(1.04f, 1.0f, 0.95f, 0.02f); lgg.active = false;
            var spl = Add<SplitToning>(); spl.shadows.value = new Color(0.33f, 0.42f, 0.55f); spl.highlights.value = new Color(0.62f, 0.52f, 0.38f); spl.balance.value = -15f; spl.active = false;
            var vg = Add<Vignette>(); vg.intensity.value = 0.32f; vg.smoothness.value = 0.45f; vg.color.value = Color.black;
            var chr = Add<ChromaticAberration>(); chr.intensity.value = 0.12f;
            var fg = Add<FilmGrain>(); fg.type.value = FilmGrainLookup.Medium4; fg.intensity.value = 0.28f; fg.response.value = 0.7f;
            var ld = Add<LensDistortion>(); ld.intensity.value = -0.06f;
            var bl = Add<Bloom>(); bl.intensity.value = 0.7f; bl.threshold.value = 0.9f; bl.scatter.value = 0.7f; bl.highQualityFiltering.value = true;
            bl.dirtTexture.value = LensDirtTex(); bl.dirtIntensity.value = 2.2f;
            var mb = Add<MotionBlur>(); mb.intensity.value = 0.22f; mb.quality.value = MotionBlurQuality.Medium;
            var sm = Add<ShadowsMidtonesHighlights>(); sm.shadows.value = new Vector4(0.95f, 0.98f, 1.05f, 0f); sm.active = false;
            // DoF: tắt mặc định, HorrorPostFX bật khi đọc tài liệu/cutscene
            var dof = Add<DepthOfField>(); dof.mode.value = DepthOfFieldMode.Bokeh; dof.focusDistance.value = 0.6f; dof.focalLength.value = 60f; dof.aperture.value = 2.8f; dof.active = false;
            // Sương thể tích (KHVolumetricFogFeature)
            var vf = Add<KhoaHon.Rendering.KHVolumetricFog>();
            vf.density.value = 0.08f; vf.startDistance.value = 0.8f; vf.baseHeight.value = 11f; vf.heightFalloff.value = 0.12f; vf.maxDistance.value = 40f;
            vf.anisotropy.value = 0.6f; vf.mainLightIntensity.value = 2.5f; vf.additionalLightsIntensity.value = 0.9f;
            vf.tint.value = new Color(0.78f, 0.84f, 0.95f); vf.ambient.value = new Color(0.006f, 0.008f, 0.012f);
            vf.noiseScale.value = 0.2f; vf.noiseIntensity.value = 0.55f; vf.wind.value = new Vector3(0.2f, 0.03f, 0.12f);
            EditorUtility.SetDirty(prof);

            // Gộp Volume: tắt các Volume toàn cục cũ (của MapKinhDi/greybox) để chỉ còn 1 profile điện ảnh
            foreach (var old in Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (old.isGlobal && old.sharedProfile != prof) { old.gameObject.SetActive(false); }

            var go = new GameObject("KH_HorrorVolume"); go.transform.SetParent(root);
            var vol = go.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 50; vol.sharedProfile = prof;
            var hb = new GameObject("Heartbeat").AddComponent<AudioSource>(); hb.transform.SetParent(go.transform);
            hb.clip = Clip("sfx_heartbeat_fast"); hb.loop = true; hb.spatialBlend = 0f; hb.volume = 0f; hb.playOnAwake = false;
            var fx = go.AddComponent<HorrorPostFX>();
            var so = new SerializedObject(fx); so.FindProperty("heartbeat").objectReferenceValue = hb; so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Vết bẩn ống kính cho Bloom: đốm mờ + vệt nước, sinh thủ tục.</summary>
        static Texture2D LensDirtTex()
        {
            string path = TexDir + "/T_LensDirt.png";
            const int size = 512;
            var r = new System.Random(4242);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            var blobs = new Vector4[70];
            for (int i = 0; i < blobs.Length; i++)
                blobs[i] = new Vector4((float)r.NextDouble(), (float)r.NextDouble(), 0.01f + (float)r.NextDouble() * 0.07f, 0.15f + (float)r.NextDouble() * 0.6f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size, v = (y + 0.5f) / size, a = 0f;
                    foreach (var b in blobs)
                    {
                        float d = Vector2.Distance(new Vector2(u, v), new Vector2(b.x, b.y)) / b.z;
                        if (d < 1f) a += (1f - d * d) * b.w;
                    }
                    // vệt nước chảy dọc
                    a += Mathf.Pow(Mathf.PerlinNoise(u * 40f, v * 3f), 6f) * 1.5f;
                    float edge = Mathf.Clamp01(Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 1.6f);
                    a = Mathf.Clamp01(a * (0.35f + edge * 0.65f));
                    px[y * size + x] = new Color(a, a * 0.97f, a * 0.92f, 1f);
                }
            tex.SetPixels(px); tex.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath, "../" + path), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ================================================================== Menu, tạm dừng, cài đặt, HUD, hù dọa
        static void WireSystems()
        {
            var sys = GameObject.Find("KH_GameSystems");
            if (sys == null) return;
            var mm = sys.AddComponent<MainMenuController>();
            var so = new SerializedObject(mm);
            so.FindProperty("menuMusic").objectReferenceValue = Clip("mus_menu_musicbox_loop");
            so.FindProperty("uiHover").objectReferenceValue = Clip("ui_hover");
            so.FindProperty("uiClick").objectReferenceValue = Clip("ui_click");
            so.FindProperty("uiBack").objectReferenceValue = Clip("ui_back");
            so.ApplyModifiedPropertiesWithoutUndo();
            sys.AddComponent<PauseMenu>();
            sys.AddComponent<SettingsMenuUI>();
            sys.AddComponent<HudExtras>();
            var js = sys.AddComponent<JumpscareFX>();
            var jso = new SerializedObject(js); jso.FindProperty("scareClip").objectReferenceValue = Clip("sfx_jumpscare"); jso.ApplyModifiedPropertiesWithoutUndo();
            var amb = new GameObject("HouseAmbience").AddComponent<AudioSource>(); amb.transform.SetParent(sys.transform);
            amb.clip = Clip("amb_house_loop"); amb.loop = true; amb.playOnAwake = false; amb.volume = 0f;
            var sc = sys.AddComponent<AmbientScares>();
            var sso = new SerializedObject(sc);
            sso.FindProperty("ambience").objectReferenceValue = amb;
            var arr = sso.FindProperty("scares");
            var clips = new[] { "sfx_scare_knock", "sfx_scare_creak", "sfx_scare_bang", "sfx_scare_creak" }.Select(Clip).ToArray();
            arr.arraySize = clips.Length; for (int i = 0; i < clips.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
            sso.FindProperty("whispers").objectReferenceValue = Clip("sfx_scare_whispers");
            sso.FindProperty("stinger").objectReferenceValue = Clip("sfx_stinger_detect");
            sso.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
