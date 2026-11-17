using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Blackwood.EditorTools
{
    /// <summary>
    /// Trang phục 3D cho cánh tay người chơi, dựng THẲNG từ lưới tay gốc của civilian_girl (không sửa asset pack, không qua Blender):
    /// chép các tam giác theo trọng số xương rồi đẩy ra theo pháp tuyến — giữ nguyên UV, bindpose và trọng số nên khớp tuyệt đối
    /// với xương, IK cầm đồ và mọi animation.
    ///  • Tay áo (cẳng tay + bắp tay, dày ~5,5 mm, cổ tay áo phồng thêm ~4,5 mm): chỉ lấy vùng mà texture đã sơn là vải
    ///    (trọng số xương bàn tay &lt; 0,5 — cùng ngưỡng với <see cref="BlackwoodHousekeeperLook.ClothMask"/>), nên dùng luôn vật liệu
    ///    thân của nhân vật: Evelyn = áo khoác len, Alex = áo nỉ; viền cổ tay sẫm có sẵn trong texture.
    ///  • Găng tay (bàn tay + ngón, ôm sát 1,6 mm, che kín móng): dùng vùng găng da đã sơn trong texture của Alex; chỉ hiện khi chơi Alex
    ///    (<see cref="Blackwood.Characters.PlayerLook"/> bật/tắt).
    /// </summary>
    public static class BlackwoodPlayerGarments
    {
        const string MeshDir = "Assets/BlackwoodManor/Models/Player";
        const float Edge = 0.5f; // ngưỡng vải/da của ClothMask trên lưới tay

        static float Sm(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        static bool IsHandBone(string n) =>
            n.StartsWith("hand_") || n.StartsWith("thumb") || n.StartsWith("index") || n.StartsWith("middle") || n.StartsWith("ring") || n.StartsWith("pinky");
        static bool IsArmBone(string n) => n.StartsWith("lowerarm") || n.StartsWith("upperarm");

        /// <summary>Tổng trọng số của các xương thỏa điều kiện cho từng đỉnh.</summary>
        static float[] WeightOf(Mesh m, Transform[] bones, System.Func<string, bool> pick)
        {
            var bw = m.boneWeights;
            var hit = new bool[bones.Length];
            for (int b = 0; b < bones.Length; b++) hit[b] = bones[b] != null && pick(bones[b].name);
            var w = new float[bw.Length];
            for (int v = 0; v < bw.Length; v++)
            {
                var x = bw[v]; float s = 0f;
                if (hit[x.boneIndex0]) s += x.weight0; if (hit[x.boneIndex1]) s += x.weight1;
                if (hit[x.boneIndex2]) s += x.weight2; if (hit[x.boneIndex3]) s += x.weight3;
                w[v] = s;
            }
            return w;
        }

        /// <summary>Pháp tuyến trung bình theo vị trí (đỉnh bị tách ở đường may UV vẫn đẩy cùng hướng → vỏ không nứt).</summary>
        static Vector3[] WeldedNormals(Vector3[] verts, Vector3[] norms)
        {
            var sum = new Dictionary<Vector3Int, Vector3>();
            Vector3Int Key(Vector3 p) => new Vector3Int(Mathf.RoundToInt(p.x * 5000f), Mathf.RoundToInt(p.y * 5000f), Mathf.RoundToInt(p.z * 5000f));
            for (int i = 0; i < verts.Length; i++)
            {
                var k = Key(verts[i]);
                sum[k] = (sum.TryGetValue(k, out var s) ? s : Vector3.zero) + (i < norms.Length ? norms[i] : Vector3.up);
            }
            var r = new Vector3[verts.Length];
            for (int i = 0; i < verts.Length; i++) r[i] = sum[Key(verts[i])].normalized;
            return r;
        }

        /// <summary>Vỏ bọc: giữ các tam giác thỏa <paramref name="keep"/>, đỉnh đẩy ra theo pháp tuyến (không gian lưới gốc).</summary>
        static Mesh Shell(Mesh src, string name, System.Func<int, int, int, bool> keep, System.Func<int, float> offset)
        {
            var verts = src.vertices; var norms = src.normals; var uvs = src.uv; var bw = src.boneWeights; var tris = src.triangles;
            var push = WeldedNormals(verts, norms);
            var map = new Dictionary<int, int>();
            var nv = new List<Vector3>(); var nn = new List<Vector3>(); var nuv = new List<Vector2>(); var nbw = new List<BoneWeight>(); var nt = new List<int>();
            int Map(int i)
            {
                if (map.TryGetValue(i, out int j)) return j;
                j = nv.Count; map[i] = j;
                nv.Add(verts[i] + push[i] * offset(i)); nn.Add(i < norms.Length ? norms[i] : push[i]);
                nuv.Add(i < uvs.Length ? uvs[i] : Vector2.zero); nbw.Add(bw[i]);
                return j;
            }
            var kept = new List<int>();
            for (int t = 0; t < tris.Length; t += 3)
                if (keep(tris[t], tris[t + 1], tris[t + 2])) kept.Add(t);
            // Gọt "răng cưa" ở mép cắt: bỏ tam giác có ≥ 2 cạnh nằm trên mép (nhô ra như gai), lặp vài lượt
            System.Func<Vector3, Vector3Int> key = p => new Vector3Int(Mathf.RoundToInt(p.x * 5000f), Mathf.RoundToInt(p.y * 5000f), Mathf.RoundToInt(p.z * 5000f));
            var vk = new Vector3Int[verts.Length];
            for (int i = 0; i < verts.Length; i++) vk[i] = key(verts[i]);
            (Vector3Int, Vector3Int) E(int i, int j) { var x = vk[i]; var y = vk[j]; return (x.x < y.x || x.x == y.x && (x.y < y.y || x.y == y.y && x.z <= y.z)) ? (x, y) : (y, x); }
            for (int pass = 0; pass < 3; pass++)
            {
                var count = new Dictionary<(Vector3Int, Vector3Int), int>();
                foreach (int t in kept)
                    foreach (var e in new[] { E(tris[t], tris[t + 1]), E(tris[t + 1], tris[t + 2]), E(tris[t + 2], tris[t]) })
                        count[e] = (count.TryGetValue(e, out int q) ? q : 0) + 1;
                int before = kept.Count;
                kept.RemoveAll(t =>
                {
                    int open = 0;
                    if (count[E(tris[t], tris[t + 1])] == 1) open++;
                    if (count[E(tris[t + 1], tris[t + 2])] == 1) open++;
                    if (count[E(tris[t + 2], tris[t])] == 1) open++;
                    return open >= 2;
                });
                if (kept.Count == before) break;
            }
            foreach (int t in kept) { nt.Add(Map(tris[t])); nt.Add(Map(tris[t + 1])); nt.Add(Map(tris[t + 2])); }
            var m = new Mesh { name = name };
            m.SetVertices(nv); m.SetNormals(nn); m.SetUVs(0, nuv); m.boneWeights = nbw.ToArray(); m.SetTriangles(nt, 0);
            m.bindposes = src.bindposes;
            m.RecalculateTangents(); m.RecalculateBounds();
            return m;
        }

        static Mesh SaveMesh(Mesh m)
        {
            BlackwoodDataBuilder.EnsureFolder(MeshDir);
            string p = MeshDir + "/" + m.name + ".asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(p);
            if (old != null) { EditorUtility.CopySerialized(m, old); old.name = m.name; Object.DestroyImmediate(m); EditorUtility.SetDirty(old); return old; }
            AssetDatabase.CreateAsset(m, p);
            return m;
        }

        /// <summary>Dựng (hoặc nạp lại) tay áo + găng cho một lưới tay. Kết quả lưu ở Models/Player.</summary>
        static (Mesh sleeve, Mesh glove) Meshes(SkinnedMeshRenderer arm, Dictionary<Mesh, (Mesh, Mesh)> cache)
        {
            var src = arm.sharedMesh;
            if (cache.TryGetValue(src, out var pair)) return pair;
            var bones = arm.bones;
            var wHand = WeightOf(src, bones, IsHandBone);
            var wArm = WeightOf(src, bones, IsArmBone);
            float MaxH(int a, int b, int c) => Mathf.Max(wHand[a], Mathf.Max(wHand[b], wHand[c]));
            float MinA(int a, int b, int c) => Mathf.Min(wArm[a], Mathf.Min(wArm[b], wArm[c]));
            // Tay áo: cả 3 đỉnh nằm trong vùng vải của texture; phồng dần về phía cổ tay
            var sleeve = Shell(src, "PL_Sleeve_" + src.name, (a, b, c) => MaxH(a, b, c) < Edge && MinA(a, b, c) > 0.2f,
                i => 0.0055f + 0.0045f * Sm(0.08f, 0.45f, wHand[i]));
            // Găng: mọi tam giác chạm vùng bàn tay (hàng tam giác ở cổ tay nằm khuất dưới cổ tay áo)
            var glove = Shell(src, "PL_Glove_" + src.name, (a, b, c) => MaxH(a, b, c) >= Edge, i => 0.0016f);
            pair = (SaveMesh(sleeve), SaveMesh(glove));
            cache[src] = pair;
            return pair;
        }

        /// <summary>
        /// Gắn tay áo + găng vào mọi lưới tay đang hoạt động dưới <paramref name="root"/>. Trả về các renderer găng tay
        /// (để <see cref="Blackwood.Characters.PlayerLook"/> chỉ bật khi chơi Alex).
        /// </summary>
        public static List<Renderer> Dress(GameObject root, out string info)
        {
            var gloves = new List<Renderer>();
            var cache = new Dictionary<Mesh, (Mesh, Mesh)>();
            int n = 0;
            foreach (var arm in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                         .Where(r => (r.name == "hand_left" || r.name == "hand_right") && r.gameObject.activeInHierarchy && r.sharedMesh != null).ToArray())
            {
                var (sleeve, glove) = Meshes(arm, cache);
                string side = arm.name == "hand_left" ? "L" : "R";
                Attach(arm, "PL_Sleeve_" + side, sleeve);
                gloves.Add(Attach(arm, "PL_Glove_" + side, glove));
                n++;
            }
            var any = cache.Values.FirstOrDefault();
            info = "Garments: " + n + " cánh tay" + (any.Item1 != null ? $" (tay áo {any.Item1.vertexCount}v/{any.Item1.triangles.Length / 3}t, găng {any.Item2.vertexCount}v/{any.Item2.triangles.Length / 3}t)" : "");
            return gloves;
        }

        /// <summary>Dựng trang phục cho Player trong scene và khai báo găng tay cho PlayerLook.</summary>
        public static string Build(GameObject player)
        {
            if (player == null) return "Garments: không có Player";
            var gloves = Dress(player, out var info);
            AssetDatabase.SaveAssets();
            var look = player.GetComponent<Blackwood.Characters.PlayerLook>();
            if (look != null)
            {
                var so = new SerializedObject(look);
                var p = so.FindProperty("alexOnly");
                p.arraySize = gloves.Count;
                for (int i = 0; i < gloves.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = gloves[i];
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(look);
            }
            else info += " — WARN không có PlayerLook";
            return info;
        }

        static SkinnedMeshRenderer Attach(SkinnedMeshRenderer arm, string name, Mesh mesh)
        {
            var parent = arm.transform.parent;
            var old = parent.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = arm.transform.localPosition;
            go.transform.localRotation = arm.transform.localRotation;
            go.transform.localScale = arm.transform.localScale;
            go.layer = arm.gameObject.layer;
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;
            smr.bones = arm.bones;
            smr.rootBone = arm.rootBone;
            smr.sharedMaterial = arm.sharedMaterial; // body_skin → PlayerLook đổi sang vật liệu của nhân vật như lưới tay gốc
            smr.shadowCastingMode = arm.shadowCastingMode;
            smr.receiveShadows = arm.receiveShadows;
            smr.updateWhenOffscreen = arm.updateWhenOffscreen;
            smr.localBounds = arm.localBounds;
            smr.quality = arm.quality;
            smr.skinnedMotionVectors = arm.skinnedMotionVectors;
            return smr;
        }
    }
}
