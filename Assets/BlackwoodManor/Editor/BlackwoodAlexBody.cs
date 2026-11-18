using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Blackwood.EditorTools
{
    /// <summary>
    /// Thân 3D riêng của Alex (áo nỉ có mũ, ba lô, quần jean, giày) — model AI (TRELLIS) đã gắn xương civilian_girl trong Blender
    /// (<c>Tools/Blender/alex_body.py</c> → <c>Tools/Blender/out/characters/BW_Alex_Body.fbx</c>).
    /// Ở đây nhập FBX + texture, gắn lưới vào đúng xương của Player (bindpose tính từ bindpose gốc của civilian_girl nên
    /// không phụ thuộc tư thế đang lưu trong scene), rồi khai báo cho <see cref="Blackwood.Characters.PlayerLook"/>:
    /// chơi Alex thì hiện thân mới, ẩn thân/đầu gốc; Evelyn giữ nguyên. Tay góc nhìn thứ nhất (tay gốc + tay áo/găng) không đổi.
    /// </summary>
    public static class BlackwoodAlexBody
    {
        const string SrcDir = "../Tools/Blender/out/characters/";
        const string FbxName = "BW_Alex_Body.fbx";
        const string TexName = "T_PL_Alex_Body_A.png";
        const string MeshDir = "Assets/BlackwoodManor/Models/Player";
        const string TexDir = "Assets/BlackwoodManor/Textures/Player";
        const string MatDir = "Assets/BlackwoodManor/Materials/Player";

        /// <summary>Lưới gốc của civilian_girl bị thay khi chơi Alex.</summary>
        static readonly string[] GirlParts =
        {
            "shirt_t", "pant", "chest", "boot_left", "boot_right", "head_01", "hair",
            "eye_left", "eye_right", "eyelashes_left", "eyelashes_right", "teeth_up", "teeth_down", "tongue"
        };

        [MenuItem("Blackwood/Build/Alex Body (3D)")]
        public static void Menu() => Debug.Log(Build(GameObject.Find("Player")));

        static string Copy(string name, string dstDir)
        {
            string src = Path.GetFullPath(Path.Combine(Application.dataPath, SrcDir + name));
            if (!File.Exists(src)) return null;
            BlackwoodDataBuilder.EnsureFolder(dstDir);
            string dst = dstDir + "/" + name;
            BlackwoodDataBuilder.WriteBytesIfChanged(dst, File.ReadAllBytes(src));
            AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
            return dst;
        }

        static Material BuildMaterial()
        {
            string tp = Copy(TexName, TexDir);
            Texture2D tex = null;
            if (tp != null)
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(tp);
                ti.sRGBTexture = true; ti.mipmapEnabled = true; ti.maxTextureSize = 2048;
                ti.anisoLevel = 4; ti.textureCompression = TextureImporterCompression.CompressedHQ;
                ti.SaveAndReimport();
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(tp);
            }
            BlackwoodDataBuilder.EnsureFolder(MatDir);
            // Tên không kết thúc bằng _Body/_Skin để PlayerLook không đổi vật liệu này
            string mp = MatDir + "/M_PL_Alex_Outfit3D.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, mp); }
            else m.shader = sh;
            m.SetTexture("_BaseMap", tex); m.SetTexture("_MainTex", tex);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", 0.16f); m.SetFloat("_Metallic", 0f);
            var weave = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/T_PL_FabricDetail_N.png");
            if (weave != null)
            {
                m.SetTexture("_DetailNormalMap", weave); m.SetFloat("_DetailNormalMapScale", 0.35f);
                m.SetTextureScale("_DetailAlbedoMap", new Vector2(18f, 18f));
            }
            BlackwoodMaterialUtil.Validate(m);
            EditorUtility.SetDirty(m);
            return m;
        }

        static GameObject ImportFbx()
        {
            string dst = Copy(FbxName, MeshDir);
            if (dst == null) return null;
            var imp = (ModelImporter)AssetImporter.GetAtPath(dst);
            imp.animationType = ModelImporterAnimationType.Generic;
            imp.importAnimation = false; imp.materialImportMode = ModelImporterMaterialImportMode.None;
            imp.isReadable = true; imp.importBlendShapes = false; imp.optimizeMeshVertices = false;
            imp.importNormals = ModelImporterNormals.Import;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<GameObject>(dst);
        }

        public static string Build(GameObject player)
        {
            if (EditorApplication.isPlaying) return "AlexBody: đang Play.";
            if (player == null) return "AlexBody: không có Player";
            var look = player.GetComponent<Blackwood.Characters.PlayerLook>();
            var girlSmr = player.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(r => r.name == "shirt_t" && r.gameObject.activeInHierarchy);
            if (look == null || girlSmr == null) return "AlexBody: WARN thiếu PlayerLook hoặc civilian_girl";
            var girl = girlSmr.transform.parent;
            var fbx = ImportFbx();
            if (fbx == null) return "AlexBody: WARN chưa có " + FbxName + " (chạy Tools/Blender/alex_body.py)";
            var mat = BuildMaterial();

            // Xương theo tên + ma trận bind của từng xương trong hệ của girl (lấy từ bindpose các lưới gốc)
            var girlInv = girl.worldToLocalMatrix;
            var bone = new Dictionary<string, Transform>();
            var bindInGirl = new Dictionary<string, Matrix4x4>();
            var parts = new List<Renderer>();
            foreach (var r in girl.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!GirlParts.Contains(r.name) && r.name != "hand_left" && r.name != "hand_right") continue;
                if (GirlParts.Contains(r.name)) parts.Add(r);
                var bp = r.sharedMesh.bindposes; var bs = r.bones;
                var toGirl = girlInv * r.transform.localToWorldMatrix;
                for (int i = 0; i < bs.Length && i < bp.Length; i++)
                {
                    if (bs[i] == null || bindInGirl.ContainsKey(bs[i].name)) continue;
                    bone[bs[i].name] = bs[i];
                    bindInGirl[bs[i].name] = toGirl * bp[i].inverse;
                }
            }
            foreach (var t in girl.GetComponentsInChildren<Transform>(true)) if (!bone.ContainsKey(t.name)) bone[t.name] = t;
            Transform pelvis = bone.TryGetValue("pelvis", out var pv) ? pv : girl;

            var inst = (GameObject)Object.Instantiate(fbx);
            inst.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            inst.transform.localScale = Vector3.one;
            var report = new List<string>();
            var made = new List<Renderer>();
            try
            {
                foreach (var src in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var sm = src.sharedMesh; var sb = src.bones; var sbind = sm.bindposes;
                    if (sb.Length == 0) continue;
                    Matrix4x4 Ld = inst.transform.worldToLocalMatrix * sb[0].localToWorldMatrix * sbind[0];
                    var nb = new Transform[sb.Length]; var nbind = new Matrix4x4[sb.Length];
                    int missing = 0;
                    for (int i = 0; i < sb.Length; i++)
                    {
                        string n = sb[i] != null ? sb[i].name : "";
                        if (!bone.TryGetValue(n, out var ob)) { missing++; ob = pelvis; n = "pelvis"; }
                        nb[i] = ob;
                        var bm = bindInGirl.TryGetValue(n, out var g) ? g : girlInv * ob.localToWorldMatrix;
                        nbind[i] = bm.inverse * Ld;
                    }
                    var mesh = Object.Instantiate(sm);
                    mesh.name = src.name;
                    mesh.bindposes = nbind;
                    mesh.RecalculateBounds();
                    BlackwoodDataBuilder.EnsureFolder(MeshDir);
                    string mp = MeshDir + "/" + src.name + ".asset";
                    AssetDatabase.DeleteAsset(mp);
                    AssetDatabase.CreateAsset(mesh, mp);

                    var old = girl.Find(src.name);
                    if (old != null) Object.DestroyImmediate(old.gameObject);
                    var go = new GameObject(src.name);
                    go.transform.SetParent(girl, false);
                    go.layer = girlSmr.gameObject.layer;
                    var smr = go.AddComponent<SkinnedMeshRenderer>();
                    smr.sharedMesh = mesh; smr.bones = nb; smr.rootBone = pelvis;
                    smr.sharedMaterial = mat;
                    // Đầu chỉ đổ bóng (camera góc nhìn thứ nhất nằm trong đầu) — PlayerArmIK cũng đặt theo tên "head"
                    smr.shadowCastingMode = src.name.Contains("Head") ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : UnityEngine.Rendering.ShadowCastingMode.On;
                    smr.updateWhenOffscreen = true;
                    smr.localBounds = new Bounds(Vector3.zero, new Vector3(4f, 5f, 4f));
                    smr.quality = SkinQuality.Bone4;
                    made.Add(smr);
                    report.Add($"{src.name}:{mesh.vertexCount}v/{sb.Length}b" + (missing > 0 ? $"(thiếu {missing} xương)" : ""));
                }
                var dHead = inst.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "head");
                if (dHead != null && bindInGirl.TryGetValue("head", out var hb))
                {
                    var a = inst.transform.InverseTransformPoint(dHead.position);
                    report.Add($"lệch xương đầu {(a - (Vector3)hb.GetColumn(3)).magnitude * 100f:F1} cm");
                }
            }
            finally { Object.DestroyImmediate(inst); }
            if (made.Count == 0) return "AlexBody: WARN FBX không có lưới skin";

            // PlayerLook: thân mới chỉ hiện khi Alex; thân/đầu gốc chỉ hiện khi Evelyn
            var so = new SerializedObject(look);
            var ao = so.FindProperty("alexOnly");
            var keep = new List<Object>();
            for (int i = 0; i < ao.arraySize; i++)
            {
                var o = ao.GetArrayElementAtIndex(i).objectReferenceValue;
                if (o != null && !o.name.StartsWith("PL_Alex_")) keep.Add(o);
            }
            keep.AddRange(made);
            ao.arraySize = keep.Count;
            for (int i = 0; i < keep.Count; i++) ao.GetArrayElementAtIndex(i).objectReferenceValue = keep[i];
            var eo = so.FindProperty("evelynOnly");
            eo.arraySize = parts.Count;
            for (int i = 0; i < parts.Count; i++) eo.GetArrayElementAtIndex(i).objectReferenceValue = parts[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(look);
            AssetDatabase.SaveAssets();
            return "AlexBody: " + string.Join(", ", report) + $"; ẩn {parts.Count} lưới gốc khi chơi Alex";
        }
    }
}
