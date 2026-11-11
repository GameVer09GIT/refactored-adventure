using System.Collections.Generic;
using System.IO;
using KhoaHon.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KhoaHon.EditorTools
{
    /// <summary>
    /// Nâng cấp đồ họa URP kiểu "UE5" cho Blackwood Manor mà KHÔNG sửa asset gốc trong Assets/Settings:
    /// - Renderer riêng KH_Renderer (Forward+): SSAO chất lượng cao, Decal, Sương thể tích.
    /// - 4 pipeline asset KH_URP_Low/Medium/High/Ultra (bóng đổ, STP, render scale theo mức).
    /// - Thêm 4 mức chất lượng Low/Medium/High/Ultra vào QualitySettings (giữ Mobile/PC cũ), mặc định PC = High.
    /// Chạy lại an toàn.
    /// </summary>
    public static class KhoaHonGraphicsBuilder
    {
        public const string Dir = "Assets/_KhoaHon/Settings";
        public const string RendererPath = Dir + "/KH_Renderer.asset";
        public static readonly string[] Levels = { "Low", "Medium", "High", "Ultra" };

        [MenuItem("KhoaHon/Build/4. Graphics (URP + Quality)")]
        public static void Menu() => Debug.Log(Build());

        public static string Build()
        {
            if (EditorApplication.isPlaying) return "Đang Play.";
            var log = new List<string>();
            KhoaHonDataBuilder.EnsureFolder(Dir);

            var renderer = BuildRenderer(log);
            var assets = new UniversalRenderPipelineAsset[Levels.Length];
            for (int i = 0; i < Levels.Length; i++) assets[i] = BuildPipelineAsset(i, renderer, log);
            BuildQualityLevels(assets, log);
            AssetDatabase.SaveAssets();
            return string.Join("\n", log);
        }

        // ------------------------------------------------------------------ Renderer
        static UniversalRendererData BuildRenderer(List<string> log)
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (data == null)
            {
                AssetDatabase.CopyAsset("Assets/Settings/PC_Renderer.asset", RendererPath);
                data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
                log.Add("Copied PC_Renderer → KH_Renderer");
            }
            var so = new SerializedObject(data);
            so.FindProperty("m_RenderingMode").intValue = 2; // Forward+
            so.ApplyModifiedPropertiesWithoutUndo();

            // SSAO chất lượng cao
            var ssao = GetOrAddFeature<ScreenSpaceAmbientOcclusion>(data, "SSAO");
            var sso = new SerializedObject(ssao);
            var st = sso.FindProperty("m_Settings");
            st.FindPropertyRelative("Downsample").boolValue = false;
            st.FindPropertyRelative("AfterOpaque").boolValue = false;
            st.FindPropertyRelative("Source").enumValueIndex = 1;          // DepthNormals
            st.FindPropertyRelative("NormalSamples").enumValueIndex = 2;   // High
            st.FindPropertyRelative("Intensity").floatValue = 0.9f;
            st.FindPropertyRelative("DirectLightingStrength").floatValue = 0.35f;
            st.FindPropertyRelative("Radius").floatValue = 0.45f;
            st.FindPropertyRelative("Samples").enumValueIndex = 0;         // High
            st.FindPropertyRelative("BlurQuality").enumValueIndex = 0;     // High
            st.FindPropertyRelative("Falloff").floatValue = 60f;
            sso.ApplyModifiedPropertiesWithoutUndo();

            // Decal (máu, vết cào, chữ máu)
            var decal = GetOrAddFeature<DecalRendererFeature>(data, "Decal");
            var dso = new SerializedObject(decal);
            var ds = dso.FindProperty("m_Settings");
            ds.FindPropertyRelative("technique").enumValueIndex = 0; // Automatic
            ds.FindPropertyRelative("maxDrawDistance").floatValue = 60f;
            dso.ApplyModifiedPropertiesWithoutUndo();

            // Sương thể tích
            var fog = GetOrAddFeature<KHVolumetricFogFeature>(data, "KH Volumetric Fog");
            var fso = new SerializedObject(fog);
            fso.FindProperty("shader").objectReferenceValue = Shader.Find("Hidden/KhoaHon/VolumetricFog");
            fso.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(data);
            log.Add("KH_Renderer features: " + string.Join(", ", data.rendererFeatures.ConvertAll(f => f != null ? f.name : "null")));
            return data;
        }

        static T GetOrAddFeature<T>(ScriptableRendererData data, string name) where T : ScriptableRendererFeature
        {
            foreach (var f in data.rendererFeatures) if (f is T t) { t.SetActive(true); return t; }
            var feat = ScriptableObject.CreateInstance<T>();
            feat.name = name;
            AssetDatabase.AddObjectToAsset(feat, data);
            data.rendererFeatures.Add(feat);
            // Cập nhật bảng localId để Unity nạp lại feature đúng thứ tự
            var so = new SerializedObject(data);
            var map = so.FindProperty("m_RendererFeatureMap");
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feat, out _, out long localId);
            map.arraySize = data.rendererFeatures.Count;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            so.ApplyModifiedPropertiesWithoutUndo();
            feat.Create();
            return feat;
        }

        // ------------------------------------------------------------------ Pipeline assets
        static UniversalRenderPipelineAsset BuildPipelineAsset(int level, UniversalRendererData renderer, List<string> log)
        {
            string path = Dir + "/KH_URP_" + Levels[level] + ".asset";
            var a = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (a == null)
            {
                AssetDatabase.CopyAsset("Assets/Settings/PC_RPAsset.asset", path);
                a = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            }
            var so = new SerializedObject(a);
            var list = so.FindProperty("m_RendererDataList");
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            so.FindProperty("m_DefaultRendererIndex").intValue = 0;

            //                             Low     Medium  High    Ultra
            int[] shadowRes = { 1024, 2048, 4096, 4096 };
            int[] addShadowRes = { 1024, 2048, 4096, 8192 };
            float[] shadowDist = { 25f, 35f, 50f, 70f };
            int[] cascades = { 2, 3, 4, 4 };
            int[] softQ = { 1, 2, 3, 3 };              // Low, Medium, High
            float[] scale = { 0.75f, 0.85f, 1f, 1f };
            int[] perObject = { 4, 6, 8, 8 };

            so.FindProperty("m_MainLightShadowmapResolution").intValue = shadowRes[level];
            so.FindProperty("m_AdditionalLightShadowsSupported").boolValue = true;
            so.FindProperty("m_AdditionalLightsShadowmapResolution").intValue = addShadowRes[level];
            so.FindProperty("m_ShadowDistance").floatValue = shadowDist[level];
            so.FindProperty("m_ShadowCascadeCount").intValue = cascades[level];
            so.FindProperty("m_SoftShadowQuality").intValue = softQ[level];
            so.FindProperty("m_SoftShadowsSupported").boolValue = true;
            so.FindProperty("m_RenderScale").floatValue = scale[level];
            // STP (khử răng cưa + upscale theo thời gian) cho High/Ultra; Low/Medium upscale song tuyến
            so.FindProperty("m_UpscalingFilter").intValue = level >= 2 ? 4 : 1;
            so.FindProperty("m_AdditionalLightsPerObjectLimit").intValue = perObject[level];
            so.FindProperty("m_SupportsHDR").boolValue = true;
            so.FindProperty("m_RequireDepthTexture").boolValue = true;
            var dec = so.FindProperty("m_SupportsLightCookies"); if (dec != null) dec.boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(a);
            log.Add($"KH_URP_{Levels[level]}: shadow {shadowRes[level]}/{addShadowRes[level]} dist {shadowDist[level]} scale {scale[level]} STP={(level >= 2)}");
            return a;
        }

        // ------------------------------------------------------------------ Quality levels
        static void BuildQualityLevels(UniversalRenderPipelineAsset[] assets, List<string> log)
        {
            var qs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0];
            var so = new SerializedObject(qs);
            var arr = so.FindProperty("m_QualitySettings");

            int FindLevel(string n)
            {
                for (int i = 0; i < arr.arraySize; i++)
                    if (arr.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == n) return i;
                return -1;
            }

            int template = FindLevel("PC");
            if (template < 0) template = arr.arraySize - 1;
            int[] lodBias = { 1, 1, 2, 2 };
            int[] aniso = { 1, 1, 2, 2 }; // 1 Enable, 2 ForceEnable
            for (int l = 0; l < Levels.Length; l++)
            {
                int idx = FindLevel(Levels[l]);
                if (idx < 0)
                {
                    arr.InsertArrayElementAtIndex(template);
                    arr.MoveArrayElement(template + 1, arr.arraySize - 1);
                    idx = arr.arraySize - 1;
                }
                var e = arr.GetArrayElementAtIndex(idx);
                e.FindPropertyRelative("name").stringValue = Levels[l];
                e.FindPropertyRelative("customRenderPipeline").objectReferenceValue = assets[l];
                e.FindPropertyRelative("lodBias").floatValue = lodBias[l];
                e.FindPropertyRelative("anisotropicTextures").intValue = aniso[l];
                e.FindPropertyRelative("antiAliasing").intValue = 0;
                var rp = e.FindPropertyRelative("realtimeReflectionProbes"); if (rp != null) rp.boolValue = l >= 2;
                var sp = e.FindPropertyRelative("softParticles"); if (sp != null) sp.boolValue = l >= 1;
                var tex = e.FindPropertyRelative("globalTextureMipmapLimit"); if (tex != null) tex.intValue = l == 0 ? 1 : 0;
            }

            // Mặc định cho PC (Standalone) = High
            int high = FindLevel("High");
            var def = so.FindProperty("m_PerPlatformDefaultQuality");
            for (int i = 0; i < def.arraySize; i++)
            {
                var p = def.GetArrayElementAtIndex(i);
                var k = p.FindPropertyRelative("first"); var v = p.FindPropertyRelative("second");
                if (k != null && v != null && k.stringValue == "Standalone") v.intValue = high;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            QualitySettings.SetQualityLevel(high, true);
            log.Add("Quality levels: " + string.Join(", ", QualitySettings.names) + " (Standalone default = High)");
        }
    }
}
