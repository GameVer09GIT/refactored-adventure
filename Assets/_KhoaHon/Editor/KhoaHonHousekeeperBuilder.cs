using System.Collections.Generic;
using System.Linq;
using KhoaHon.AI;
using KhoaHon.Core;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

namespace KhoaHon.EditorTools
{
    /// <summary>
    /// Dựng "Chấp Pháp Quản Gia": model civilian_girl (rig Humanoid) mặc áo bà ba đen, da tái, tóc đen,
    /// đi/chạy bằng clip Mixamo có sẵn, chùm chìa khóa đeo hông; AI + âm thanh; waypoint tầng 2; chỗ trốn.
    /// </summary>
    public static class KhoaHonHousekeeperBuilder
    {
        const string Dir = "Assets/_KhoaHon/Prefabs/Characters";
        const string MatDir = "Assets/_KhoaHon/Materials/Housekeeper";
        const string AnimDir = "Assets/_KhoaHon/Animation";

        public static string Build(Transform root, float floorY)
        {
            KhoaHonDataBuilder.EnsureFolder(Dir); KhoaHonDataBuilder.EnsureFolder(MatDir); KhoaHonDataBuilder.EnsureFolder(AnimDir);
            KhoaHonAudioSynth.BuildAll();
            var cfg = KhoaHonDataBuilder.LoadOrCreate<HousekeeperConfig>("Assets/_KhoaHon/Data/Configs/KH_HousekeeperConfig.asset");

            // ---------- Vật liệu
            // "Banshee": giữ lưới skinned + normal map gốc của civilian_girl, thay albedo bằng bản xử lý
            // (da tử thi, hốc mắt thâm, máu trào miệng, tóc bạc, váy áo trắng ngà đẫm máu) — không sửa file asset pack
            var girlPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/civilian_girl/Prefabs/civilian_girl.prefab");
            var orig = new Dictionary<string, Material>();
            foreach (var r in girlPrefab.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials) if (m != null && !orig.ContainsKey(m.name)) orig[m.name] = m;
            Material Clone(string n, string origName, Texture2D baseMap, float smooth, Color emit)
            {
                if (!orig.TryGetValue(origName, out var o)) return null;
                string p = MatDir + "/" + n + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (m == null) { m = new Material(o); AssetDatabase.CreateAsset(m, p); }
                else { m.shader = o.shader; m.CopyPropertiesFromMaterial(o); }
                if (baseMap != null) { m.SetTexture("_BaseMap", baseMap); m.SetTexture("_MainTex", baseMap); }
                m.SetColor("_BaseColor", Color.white);
                if (smooth >= 0f) m.SetFloat("_Smoothness", smooth);
                if (emit.maxColorComponent > 0f)
                {
                    m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emit); m.SetTexture("_EmissionMap", baseMap);
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                }
                else m.DisableKeyword("_EMISSION");
                EditorUtility.SetDirty(m);
                return m;
            }
            var headTex = orig.ContainsKey("head") ? KhoaHonBansheeTextures.Head(orig["head"].GetTexture("_BaseMap")) : null;
            var bodyTex = orig.ContainsKey("body") ? KhoaHonBansheeTextures.Body(orig["body"].GetTexture("_BaseMap")) : null;
            var hairTex = orig.ContainsKey("hair") ? KhoaHonBansheeTextures.Hair(orig["hair"].GetTexture("_BaseMap")) : null;
            var remap = new Dictionary<string, Material>
            {
                { "head", Clone("M_HK_B_Head", "head", headTex, 0.5f, Color.black) },
                { "body", Clone("M_HK_B_Body", "body", bodyTex, -1f, Color.black) },
                { "body_skin", Clone("M_HK_B_Skin", "body_skin", bodyTex, 0.45f, Color.black) },
                { "hair", Clone("M_HK_B_Hair", "hair", hairTex, 0.3f, Color.black) },
                { "head_eyes", Clone("M_HK_B_Eyes", "head_eyes", headTex, 0.95f, new Color(0.55f, 0.6f, 0.66f)) },
                { "eyelahes", Clone("M_HK_B_Lashes", "eyelahes", null, -1f, Color.black) },
            };
            if (remap["eyelahes"] != null) remap["eyelahes"].SetColor("_BaseColor", new Color(0.55f, 0.55f, 0.52f));

            // ---------- Animator (Idle / Walk / Run theo Speed)
            string ctrlPath = AnimDir + "/KH_Housekeeper.controller";
            AssetDatabase.DeleteAsset(ctrlPath);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            var tree = new BlendTree { name = "Locomotion", blendParameter = "Speed", blendType = BlendTreeType.Simple1D, useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(tree, ctrl);
            tree.AddChild(Clip("Assets/Animation/X Bot@Idle.fbx"), 0f);
            tree.AddChild(Clip("Assets/Animation/X Bot@Walking.fbx"), 1.3f);
            tree.AddChild(Clip("Assets/Animation/Running.fbx"), 5.5f);
            var st = ctrl.layers[0].stateMachine.AddState("Locomotion");
            st.motion = tree;
            ctrl.layers[0].stateMachine.defaultState = st;

            // ---------- Cây đối tượng
            var hkRoot = new GameObject("KH_Housekeeper");
            hkRoot.transform.SetParent(root);
            hkRoot.transform.position = new Vector3(90.7f, floorY, 25.5f);
            var agent = hkRoot.AddComponent<NavMeshAgent>();
            agent.radius = 0.35f; agent.height = 1.8f; agent.baseOffset = 0f;
            hkRoot.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            var capsule = hkRoot.AddComponent<CapsuleCollider>();
            capsule.isTrigger = true; capsule.center = new Vector3(0, 0.9f, 0); capsule.height = 1.8f; capsule.radius = 0.3f;

            var visualRoot = new GameObject("VisualRoot").transform;
            visualRoot.SetParent(hkRoot.transform, false);
            var girl = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/civilian_girl/Prefabs/civilian_girl.prefab"), visualRoot);
            girl.name = "Body_QuanGia";
            PrefabUtility.UnpackPrefabInstance(girl, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            girl.transform.localPosition = Vector3.zero; girl.transform.localRotation = Quaternion.identity;
                        foreach (var mb in girl.GetComponentsInChildren<MonoBehaviour>(true)) if (mb == null || !(mb is Animator)) Object.DestroyImmediate(mb);
            foreach (var c in girl.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var r in girl.GetComponentsInChildren<Renderer>(true))
            {
                var arr = r.sharedMaterials;
                for (int i = 0; i < arr.Length; i++)
                    if (arr[i] != null && remap.TryGetValue(arr[i].name, out var nm) && nm != null) arr[i] = nm;
                r.sharedMaterials = arr;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            // Dáng cao gầy, lêu nghêu
            girl.transform.localScale = new Vector3(0.96f, 1.08f, 0.96f);
            var anim = girl.GetComponent<Animator>();
            if (anim == null) anim = girl.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            // Đầu (LOS + jumpscare) và hông (chùm chìa khóa)
            Transform head = anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Head) : null;
            Transform hips = anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Hips) : null;
            if (head == null) head = girl.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.ToLower().Contains("head"));
            if (hips == null) hips = girl.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.ToLower().Contains("pelvis"));
            var keysModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_KhoaHon/Models/Props/KH_ChumChiaKhoa.fbx");
            if (keysModel != null && hips != null)
            {
                var keys = (GameObject)PrefabUtility.InstantiatePrefab(keysModel, hips);
                keys.name = "ChumChiaKhoa";
                keys.transform.localPosition = new Vector3(0.16f, -0.08f, 0.02f);
                keys.transform.localRotation = Quaternion.identity;
                keys.transform.localScale = Vector3.one * 1.4f;
            }


            // Âm thanh
            AudioSource Src(string n, Transform parent)
            {
                var go = new GameObject(n); go.transform.SetParent(parent, false);
                if (n == "Voice") go.transform.localPosition = new Vector3(0, 1.6f, 0);
                var s = go.AddComponent<AudioSource>(); s.playOnAwake = false; return s;
            }
            var keysSrc = Src("Keys", hkRoot.transform);
            var voiceSrc = Src("Voice", hkRoot.transform);
            var music = Src("HuntMusic", hkRoot.transform);
            music.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(KhoaHonAudioSynth.Dir + "/hk_hunt_pulse_loop.wav");

            // Đèn tái mờ quanh bà (chỉ thấy khi gần, giúp đọc dáng trong bóng tối)
            var aura = new GameObject("PaleAura").AddComponent<Light>();
            aura.transform.SetParent(hkRoot.transform, false); aura.transform.localPosition = new Vector3(0, 1.5f, 0.35f);
            aura.type = LightType.Point; aura.range = 1.8f; aura.intensity = 0.16f; aura.color = new Color(0.6f, 0.75f, 0.7f); aura.shadows = LightShadows.None;

            var ai = hkRoot.AddComponent<HousekeeperAI>();
            var so = new SerializedObject(ai);
            so.FindProperty("config").objectReferenceValue = cfg;
            so.FindProperty("head").objectReferenceValue = head;
            so.FindProperty("animator").objectReferenceValue = anim;
            so.FindProperty("visualRoot").objectReferenceValue = visualRoot;
            so.FindProperty("keysSource").objectReferenceValue = keysSrc;
            so.FindProperty("voiceSource").objectReferenceValue = voiceSrc;
            so.FindProperty("huntMusic").objectReferenceValue = music;
            so.FindProperty("keysLoop").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(KhoaHonAudioSynth.Dir + "/hk_keys_drag_loop.wav");
            so.FindProperty("lullabyClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(KhoaHonAudioSynth.Dir + "/hk_lullaby_hum.wav");
            so.FindProperty("whisperRunClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(KhoaHonAudioSynth.Dir + "/hk_whisper_run.wav");
            so.FindProperty("mumbleLoop").objectReferenceValue = FindClip("ghost_moan_loop");
            so.FindProperty("screamClip").objectReferenceValue = FindClip("jumpscare_scream") ?? FindClip("ghost_scare_01");

            // Waypoint: hành lang vòng + ghé từng phòng (tầng 2, y=18)
            var wpRoot = new GameObject("HK_Waypoints").transform;
            wpRoot.SetParent(root);
            Vector3[] wps =
            {
                new Vector3(90.7f, floorY, 25.5f), new Vector3(80.7f, floorY, 25.5f), new Vector3(73.5f, floorY, 24.0f), new Vector3(80.7f, floorY, 21.0f),
                new Vector3(73.5f, floorY, 17.5f), new Vector3(80.7f, floorY, 14.0f), new Vector3(73.5f, floorY, 11.0f), new Vector3(80.7f, floorY, 9.5f),
                new Vector3(90.7f, floorY, 9.5f), new Vector3(98.7f, floorY, 9.5f), new Vector3(105.5f, floorY, 9.6f), new Vector3(108.6f, floorY, 14.0f),
                new Vector3(108.6f, floorY, 21.0f), new Vector3(108.7f, floorY, 25.6f), new Vector3(104.5f, floorY, 25.8f), new Vector3(98.7f, floorY, 17.5f),
            };
            var wpT = new Transform[wps.Length];
            for (int i = 0; i < wps.Length; i++)
            {
                var w = new GameObject("WP_" + i.ToString("00")).transform;
                w.SetParent(wpRoot); w.position = wps[i]; wpT[i] = w;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            SetArray(ai, "waypoints", wpT); // sau Apply của 'so' để không bị ghi đè

            // Chỗ trốn: mọi object HidingSpot_* đã đặt
            int spots = 0;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.name.StartsWith("HidingSpot_")).ToArray())
            {
                if (t.GetComponent<HidingSpot>() != null) continue;
                var rs = t.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0) continue;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                bool bed = t.name.Contains("_Bed");
                // Hướng ra phòng: từ tâm vật tới tâm phòng gần nhất, khớp trục gần nhất
                var room = Object.FindObjectsByType<RoomMarker>(FindObjectsSortMode.None).OrderBy(r => (r.transform.position - b.center).sqrMagnitude).First();
                var toRoom = room.transform.position - b.center; toRoom.y = 0;
                var axis = Mathf.Abs(toRoom.x) > Mathf.Abs(toRoom.z) ? new Vector3(Mathf.Sign(toRoom.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(toRoom.z));
                if (bed) axis = Mathf.Abs(toRoom.x) > Mathf.Abs(toRoom.z) ? new Vector3(Mathf.Sign(toRoom.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(toRoom.z));
                var hide = new GameObject("HidePoint").transform; hide.SetParent(t, true);
                hide.position = bed ? new Vector3(b.center.x, floorYFor(b) + 0.28f, b.center.z) : new Vector3(b.center.x, floorYFor(b) + 1.5f, b.center.z) + axis * 0.05f;
                hide.rotation = Quaternion.LookRotation(axis, Vector3.up);
                var outP = new GameObject("OutPoint").transform; outP.SetParent(t, true);
                float ext = Mathf.Abs(axis.x) > 0 ? b.extents.x : b.extents.z;
                outP.position = new Vector3(b.center.x, floorYFor(b), b.center.z) + axis * (ext + 0.75f);
                outP.rotation = Quaternion.LookRotation(axis, Vector3.up);
                var hs = t.gameObject.AddComponent<HidingSpot>();
                hs.Configure(bed ? HidingSpot.Kind.UnderBed : HidingSpot.Kind.Wardrobe, hide, outP);
                if (t.GetComponentInChildren<Collider>() == null)
                {
                    var bc = t.gameObject.AddComponent<BoxCollider>();
                    bc.center = t.InverseTransformPoint(b.center); var ls = t.lossyScale;
                    bc.size = new Vector3(b.size.x / ls.x, b.size.y / ls.y, b.size.z / ls.z);
                }
                spots++;
            }

            // Hệ thống phụ
            var sys = GameObject.Find("KH_GameSystems");
            if (sys != null && sys.GetComponent<BreathHold>() == null)
            {
                var bh = sys.AddComponent<BreathHold>();
                var bso = new SerializedObject(bh);
                bso.FindProperty("gaspClip").objectReferenceValue = FindClip("breath") ?? FindClip("gasp");
                bso.ApplyModifiedPropertiesWithoutUndo();
            }
            var player = GameObject.Find("Player");
            if (player != null && player.GetComponent<PlayerNoiseEmitter>() == null) player.AddComponent<PlayerNoiseEmitter>();

            if (NavMesh.SamplePosition(hkRoot.transform.position, out var hit, 2f, 1 << NavMesh.GetAreaFromName("Floor2"))) hkRoot.transform.position = hit.position;
            PrefabUtility.SaveAsPrefabAssetAndConnect(hkRoot, Dir + "/KH_Housekeeper.prefab", InteractionMode.AutomatedAction);
            return $"Housekeeper ok (humanoid={anim.isHuman}, head={(head ? head.name : "null")}, hips={(hips ? hips.name : "null")}, hidingSpots={spots})";

            float floorYFor(Bounds bb) => bb.min.y;
        }

        static GameObject M(string n) => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_KhoaHon/Models/Props/" + n + ".fbx");

        static GameObject Attach(GameObject model, Transform bone, Vector3 worldPos, Quaternion worldRot, Vector3 scale, Material mat, string name)
        {
            if (model == null || bone == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            go.name = name;
            go.transform.SetPositionAndRotation(worldPos, worldRot);
            go.transform.localScale = scale;
            go.transform.SetParent(bone, true);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) { var a = r.sharedMaterials; for (int i = 0; i < a.Length; i++) a[i] = mat; r.sharedMaterials = a; }
            return go;
        }

        static void AttachGarments(Animator anim, Material cloth, Material hairMat)
        {
            Transform B(HumanBodyBones b) => anim.GetBoneTransform(b);
            Transform First(params HumanBodyBones[] bs) { foreach (var x in bs) { var t = B(x); if (t != null) return t; } return null; }
            var hips = B(HumanBodyBones.Hips); var chest = First(HumanBodyBones.UpperChest, HumanBodyBones.Chest, HumanBodyBones.Spine);
            var neck = First(HumanBodyBones.Neck, HumanBodyBones.Head); var head = B(HumanBodyBones.Head);
            var root = anim.transform;
            Vector3 up = root.up, fwd = root.forward;
            // Thân áo: từ dưới hông 8 cm tới cổ
            float bottom = hips.position.y - 0.1f, top = neck.position.y + 0.02f;
            float shoulder = Vector3.Distance(B(HumanBodyBones.LeftUpperArm).position, B(HumanBodyBones.RightUpperArm).position);
            float w = Mathf.Clamp(shoulder / 0.33f, 0.8f, 1.3f);
            var basePos = new Vector3(hips.position.x, bottom, hips.position.z);
            Attach(M("KH_AoBaBa_Than"), chest, basePos, Quaternion.LookRotation(fwd, up), new Vector3(w, top - bottom, w), cloth, "AoBaBa_Than");
            Attach(M("KH_AoBaBa_Vat"), hips, basePos + up * 0.02f, Quaternion.LookRotation(fwd, up), new Vector3(w, 1f, w), cloth, "AoBaBa_Vat");
            // Ống tay: cánh tay trên + dưới mỗi bên
            void Sleeve(HumanBodyBones a, HumanBodyBones b, float thick, string n)
            {
                var ta = B(a); var tb = B(b); if (ta == null || tb == null) return;
                var d = tb.position - ta.position;
                Attach(M("KH_AoBaBa_Tay"), ta, ta.position, Quaternion.FromToRotation(Vector3.up, d.normalized), new Vector3(thick, d.magnitude * 1.02f, thick), cloth, n);
            }
            Sleeve(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, 1.15f, "Tay_TrenTrai");
            Sleeve(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, 1.0f, "Tay_DuoiTrai");
            Sleeve(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, 1.15f, "Tay_TrenPhai");
            Sleeve(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, 1.0f, "Tay_DuoiPhai");
            // Tóc dài xõa sau lưng: gốc trên đỉnh đầu, đảm bảo nằm phía sau mặt
            if (head != null)
            {
                var hair = Attach(M("KH_TocDai"), head, head.position + up * 0.16f, Quaternion.LookRotation(fwd, up), Vector3.one, hairMat, "TocDai");
                if (hair != null)
                {
                    var rs = hair.GetComponentsInChildren<Renderer>(); var hb = rs[0].bounds; foreach (var r in rs) hb.Encapsulate(r.bounds);
                    if (Vector3.Dot(hb.center - head.position, fwd) > 0f) hair.transform.RotateAround(head.position, up, 180f);
                }
            }
        }

        static Motion Clip(string fbx) =>
            AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));

        static AudioClip FindClip(string name)
        {
            var g = AssetDatabase.FindAssets(name + " t:AudioClip").FirstOrDefault();
            return g != null ? AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g)) : null;
        }

        static void SetArray(Object target, string prop, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(prop);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
