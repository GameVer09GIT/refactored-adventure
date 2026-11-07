using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace KhoaHon.Rendering
{
    /// <summary>
    /// Sương thể tích cho URP (Render Graph): raymarch nửa độ phân giải trên depth, tán xạ ánh trăng có bóng đổ
    /// và đèn phụ (đèn pin, nến), làm mờ giữ cạnh theo depth, rồi trộn lên ảnh camera.
    /// Tham số lấy từ <see cref="KHVolumetricFog"/> trong Volume; chất lượng do cài đặt người chơi.
    /// </summary>
    public class KHVolumetricFogFeature : ScriptableRendererFeature
    {
        public enum Quality { Off = 0, Low = 1, Medium = 2, High = 3 }

        /// <summary>Chất lượng do Cài đặt chọn (Off tắt hẳn pass).</summary>
        public static Quality QualityOverride = Quality.High;

        [SerializeField] private Shader shader;
        [Tooltip("Thời điểm chèn pass")]
        [SerializeField] private RenderPassEvent passEvent = RenderPassEvent.AfterRenderingSkybox;

        private Material material;
        private FogPass pass;

        public override void Create()
        {
            pass = new FogPass { renderPassEvent = passEvent };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (QualityOverride == Quality.Off) return;
            var camType = renderingData.cameraData.cameraType;
            if (camType == CameraType.Preview || camType == CameraType.Reflection) return;
            var fog = VolumeManager.instance.stack.GetComponent<KHVolumetricFog>();
            if (fog == null || !fog.IsActive()) return;
            if (material == null)
            {
                if (shader == null) shader = Shader.Find("Hidden/KhoaHon/VolumetricFog");
                if (shader == null) return;
                material = CoreUtils.CreateEngineMaterial(shader);
            }
            pass.Setup(material, fog, QualityOverride);
            pass.ConfigureInput(ScriptableRenderPassInput.Depth);
            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
            material = null;
        }

        private class FogPass : ScriptableRenderPass
        {
            private static readonly int P0 = Shader.PropertyToID("_FogParams0");
            private static readonly int P1 = Shader.PropertyToID("_FogParams1");
            private static readonly int P2 = Shader.PropertyToID("_FogParams2");
            private static readonly int TintId = Shader.PropertyToID("_FogTint");
            private static readonly int AmbientId = Shader.PropertyToID("_FogAmbient");
            private static readonly int WindId = Shader.PropertyToID("_FogWind");
            private static readonly int BlurDirId = Shader.PropertyToID("_FogBlurDir");

            private Material mat;
            private Quality quality;

            private class PassData
            {
                public Material mat;
                public TextureHandle src;
                public int pass;
                public Vector4 blurDir;
            }

            public void Setup(Material m, KHVolumetricFog f, Quality q)
            {
                mat = m; quality = q;
                int steps = q == Quality.High ? 40 : q == Quality.Medium ? 28 : 18;
                mat.SetVector(P0, new Vector4(f.density.value, f.heightFalloff.value, f.baseHeight.value, f.maxDistance.value));
                mat.SetVector(P1, new Vector4(f.anisotropy.value, f.mainLightIntensity.value, f.additionalLightsIntensity.value, f.startDistance.value));
                mat.SetVector(P2, new Vector4(f.noiseScale.value, f.noiseIntensity.value, steps, Time.frameCount % 64));
                mat.SetColor(TintId, f.tint.value);
                mat.SetColor(AmbientId, f.ambient.value);
                var w = f.wind.value;
                mat.SetVector(WindId, new Vector4(w.x, w.y, w.z, Time.time));
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var res = frameData.Get<UniversalResourceData>();
                var cam = frameData.Get<UniversalCameraData>();
                if (res.isActiveTargetBackBuffer || mat == null) return;

                var desc = cam.cameraTargetDescriptor;
                int div = quality == Quality.Low ? 4 : 2;
                desc.width = Mathf.Max(1, desc.width / div);
                desc.height = Mathf.Max(1, desc.height / div);
                desc.depthBufferBits = 0;
                desc.depthStencilFormat = GraphicsFormat.None;
                desc.msaaSamples = 1;
                desc.graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;
                var fogA = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_KHFogA", false, FilterMode.Bilinear);
                var fogB = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_KHFogB", false, FilterMode.Bilinear);

                // 1) Raymarch
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("KH Volumetric Fog · Raymarch", out var d))
                {
                    d.mat = mat; d.pass = 0;
                    builder.UseAllGlobalTextures(true);
                    if (res.cameraDepthTexture.IsValid()) builder.UseTexture(res.cameraDepthTexture);
                    builder.SetRenderAttachment(fogA, 0);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc((PassData p, RasterGraphContext ctx) =>
                        Blitter.BlitTexture(ctx.cmd, new Vector4(1, 1, 0, 0), p.mat, p.pass));
                }

                // 2) Làm mờ giữ cạnh: ngang rồi dọc
                Blur(renderGraph, res, fogA, fogB, new Vector4(1f / desc.width, 0, 0, 0));
                Blur(renderGraph, res, fogB, fogA, new Vector4(0, 1f / desc.height, 0, 0));

                // 3) Trộn lên ảnh camera: color * T + inScatter
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("KH Volumetric Fog · Composite", out var d))
                {
                    d.mat = mat; d.pass = 2; d.src = fogA;
                    builder.UseTexture(fogA);
                    builder.SetRenderAttachment(res.activeColorTexture, 0);
                    builder.SetRenderFunc((PassData p, RasterGraphContext ctx) =>
                        Blitter.BlitTexture(ctx.cmd, p.src, new Vector4(1, 1, 0, 0), p.mat, p.pass));
                }
            }

            private void Blur(RenderGraph rg, UniversalResourceData res, TextureHandle src, TextureHandle dst, Vector4 dir)
            {
                using (var builder = rg.AddRasterRenderPass<PassData>("KH Volumetric Fog · Blur", out var d))
                {
                    d.mat = mat; d.pass = 1; d.src = src; d.blurDir = dir;
                    builder.UseTexture(src);
                    builder.UseAllGlobalTextures(true);
                    if (res.cameraDepthTexture.IsValid()) builder.UseTexture(res.cameraDepthTexture);
                    builder.SetRenderAttachment(dst, 0);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc((PassData p, RasterGraphContext ctx) =>
                    {
                        ctx.cmd.SetGlobalVector(BlurDirId, p.blurDir);
                        Blitter.BlitTexture(ctx.cmd, p.src, new Vector4(1, 1, 0, 0), p.mat, p.pass);
                    });
                }
            }
        }
    }
}
