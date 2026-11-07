using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KhoaHon.Rendering
{
    /// <summary>
    /// Tham số sương thể tích (đặt trong Volume Profile). Renderer feature <see cref="KHVolumetricFogFeature"/>
    /// đọc component này mỗi frame; density = 0 thì tắt hẳn.
    /// </summary>
    [Serializable, VolumeComponentMenu("KhoaHon/Volumetric Fog")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class KHVolumetricFog : VolumeComponent
    {
        [Tooltip("Mật độ sương (hệ số tắt dần trên mét). 0 = tắt")]
        public ClampedFloatParameter density = new ClampedFloatParameter(0f, 0f, 0.5f);
        [Tooltip("Màu tán xạ của sương")]
        public ColorParameter tint = new ColorParameter(new Color(0.75f, 0.8f, 0.9f), false, false, true);
        [Tooltip("Độ cao gốc (m) — dưới mức này sương đặc nhất")]
        public FloatParameter baseHeight = new FloatParameter(10f);
        [Tooltip("Sương mỏng dần theo độ cao (càng lớn càng sát mặt đất)")]
        public ClampedFloatParameter heightFalloff = new ClampedFloatParameter(0.15f, 0f, 2f);
        [Tooltip("Tầm xa tối đa tính sương (m)")]
        public ClampedFloatParameter maxDistance = new ClampedFloatParameter(45f, 5f, 200f);
        [Tooltip("Bắt đầu tính sương cách camera (m) — tránh chói sát đèn pin")]
        public ClampedFloatParameter startDistance = new ClampedFloatParameter(0.8f, 0f, 5f);
        [Tooltip("Tán xạ về phía trước (0 đều mọi hướng, 0.9 tập trung theo hướng nhìn vào đèn)")]
        public ClampedFloatParameter anisotropy = new ClampedFloatParameter(0.55f, -0.9f, 0.95f);
        [Tooltip("Nhân cường độ ánh sáng chính (trăng)")]
        public ClampedFloatParameter mainLightIntensity = new ClampedFloatParameter(1f, 0f, 8f);
        [Tooltip("Nhân cường độ đèn phụ (đèn pin, nến, đèn treo)")]
        public ClampedFloatParameter additionalLightsIntensity = new ClampedFloatParameter(1f, 0f, 8f);
        [Tooltip("Ánh sáng môi trường trong sương")]
        public ColorParameter ambient = new ColorParameter(new Color(0.01f, 0.012f, 0.018f), false, false, true);
        [Tooltip("Tỷ lệ nhiễu 3D (cụm sương)")]
        public ClampedFloatParameter noiseScale = new ClampedFloatParameter(0.18f, 0.01f, 2f);
        [Tooltip("Độ mạnh nhiễu 3D")]
        public ClampedFloatParameter noiseIntensity = new ClampedFloatParameter(0.6f, 0f, 1f);
        [Tooltip("Gió làm sương trôi (m/s)")]
        public Vector3Parameter wind = new Vector3Parameter(new Vector3(0.25f, 0.05f, 0.1f));

        public bool IsActive() => active && density.value > 0f;
    }
}
