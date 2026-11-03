using System.Collections;
using UnityEngine;

namespace HorrorGame.Inventory
{
    /// <summary>
    /// Điều khiển Đèn pin cầm tay (Bật/Tắt, SpotLight thật, Emission kính, tiếng click, giật model)
    /// </summary>
    public class FlashlightItem : HeldItem
    {
        [Header("Nguồn sáng thật (Spot Light)")]
        [Tooltip("Spot Light chiếu sáng của đèn pin")]
        [SerializeField] private Light spotLight;

        [Tooltip("Cường độ ánh sáng tối đa (URP Lumen, 25 để cắt xuyên bóng tối đặc quánh)")]
        [SerializeField] private float maxIntensity = 25f;

        [Tooltip("Màu ánh sáng đèn pin (trắng hơi ngả vàng nhạt #FFF9E6)")]
        [SerializeField] private Color lightColor = new Color(1.0f, 0.976f, 0.902f);

        [Tooltip("Tầm chiếu sáng (45 mét - đủ soi rõ hành lang dài và không gian ngoài trời)")]
        [SerializeField] private float lightRange = 45.0f;

        [Tooltip("Góc chiếu sáng ngoài (60 độ - rộng vừa phải)")]
        [SerializeField] private float spotAngle = 60.0f;

        [Tooltip("Góc chiếu sáng trong (30 độ - vùng tâm sáng gắt)")]
        [SerializeField] private float innerSpotAngle = 30.0f;

        [Tooltip("Bật đổ bóng mềm (Soft Shadows)")]
        [SerializeField] private bool enableShadows = true;

        [Tooltip("Thời gian chuyển đổi độ sáng khi bật/tắt (ramp 0.08s)")]
        [SerializeField] private float toggleRampDuration = 0.08f;

        [Header("Mặt kính phát sáng (Emission)")]
        [Tooltip("Renderer chứa mặt kính phát sáng")]
        [SerializeField] private Renderer glassRenderer;

        [Tooltip("Chỉ số material mặt kính trong renderer")]
        [SerializeField] private int glassMaterialIndex = 0;

        [Tooltip("Màu emission khi bật đèn")]
        [ColorUsage(true, true)]
        [SerializeField] private Color glassEmissionOn = new Color(1.5f, 1.45f, 1.3f);

        [Header("Âm thanh & Hiệu ứng")]
        [Tooltip("Clip âm thanh bật/tắt đèn pin (flashlight_click)")]
        [SerializeField] private AudioClip toggleClickClip;

        [Tooltip("Âm lượng tiếng click")]
        [Range(0f, 1f)][SerializeField] private float clickVolume = 0.85f;

        [Header("Hành vi")]
        [Tooltip("Tự động tắt đèn khi cất đèn pin vào túi (chuyển sang ô khác)")]
        [SerializeField] private bool keepOnWhenUnequipped = false;

        [Tooltip("Tùy chọn nhấp nháy khi ma ở gần (mặc định TẮT)")]
        [SerializeField] private bool flickerNearGhost = false;

        [Tooltip("Khoảng cách bắt đầu nhấp nháy nếu bật tùy chọn")]
        [SerializeField] private float ghostFlickerDistance = 10f;

        private bool isOn = false;
        private Coroutine rampCoroutine;
        private Material glassMaterialInstance;

        public bool IsOn => isOn;

        protected override void Awake()
        {
            base.Awake();

            // Tắt các light con gắn trực tiếp trong prefab đèn pin để tránh chùm sáng bị xoay ngược theo xương tay và tốn shadow atlas
            var childLights = GetComponentsInChildren<Light>(true);
            for (int i = 0; i < childLights.Length; i++)
            {
                childLights[i].enabled = false;
                childLights[i].shadows = LightShadows.None;
                childLights[i].intensity = 0f;
            }

            // Lấy material instance của mặt kính để đổi emission runtime
            if (glassRenderer != null && glassRenderer.materials != null && glassMaterialIndex < glassRenderer.materials.Length)
            {
                glassMaterialInstance = glassRenderer.materials[glassMaterialIndex];
            }

            // Cấu hình CameraSpotLight theo các tham số của đèn pin
            if (PlayerArmIK.Instance != null)
            {
                PlayerArmIK.Instance.ConfigureCameraSpotLight(lightRange, spotAngle, innerSpotAngle, lightColor, enableShadows);
            }

            SetLightStateImmediate(false);
        }

        public override void OnEquip(PlayerController playerController)
        {
            base.OnEquip(playerController);
            // Khi lấy ra cầm trên tay, nếu trước đó tắt thì vẫn tắt
            if (!keepOnWhenUnequipped && isOn)
            {
                SetLightStateImmediate(false);
            }
        }

        public override void OnUnequip()
        {
            SetLightStateImmediate(false);
            base.OnUnequip();
        }

        public override void OnPrimaryUse()
        {
            // Bật/tắt đèn
            ToggleFlashlight();
        }

        public void ToggleFlashlight()
        {
            isOn = !isOn;

            // Phát âm thanh click
            PlayOneShotSound(toggleClickClip, clickVolume, Random.Range(0.96f, 1.04f));

            // Cử động ngón cái ấn nút 0.08s
            if (PlayerArmIK.Instance != null)
            {
                PlayerArmIK.Instance.TriggerThumbPress(true, 0.08f, 25f);
            }
            if (ViewModelRoot.Instance != null)
            {
                ViewModelRoot.Instance.TriggerItemAction(true, 0.08f);
            }
            else if (swayComponent != null)
            {
                swayComponent.ApplyClickRecoil(0.012f, 1.5f);
            }

            // Chuyển mượt độ sáng và emission
            if (rampCoroutine != null) StopCoroutine(rampCoroutine);
            rampCoroutine = StartCoroutine(RampLightIntensity(isOn ? maxIntensity : 0f, toggleRampDuration));
        }

        private IEnumerator RampLightIntensity(float targetIntensity, float duration)
        {
            float startIntensity = 0f;
            if (PlayerArmIK.Instance != null && PlayerArmIK.Instance.CameraSpotLight != null)
            {
                startIntensity = PlayerArmIK.Instance.CameraSpotLight.intensity;
            }

            Color startEmission = glassMaterialInstance != null && glassMaterialInstance.HasProperty("_EmissionColor")
                ? glassMaterialInstance.GetColor("_EmissionColor")
                : Color.black;
            Color targetEmission = isOn ? glassEmissionOn : Color.black;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float currentI = Mathf.Lerp(startIntensity, targetIntensity, t);
                if (PlayerArmIK.Instance != null)
                {
                    PlayerArmIK.Instance.SetFlashlightIntensity(currentI);
                }

                if (glassMaterialInstance != null && glassMaterialInstance.HasProperty("_EmissionColor"))
                {
                    glassMaterialInstance.SetColor("_EmissionColor", Color.Lerp(startEmission, targetEmission, t));
                }
                yield return null;
            }

            if (PlayerArmIK.Instance != null)
            {
                PlayerArmIK.Instance.SetFlashlightIntensity(targetIntensity);
            }
            if (glassMaterialInstance != null && glassMaterialInstance.HasProperty("_EmissionColor"))
            {
                glassMaterialInstance.SetColor("_EmissionColor", targetEmission);
            }

            rampCoroutine = null;
        }

        public void SetLightStateImmediate(bool enabledState)
        {
            isOn = enabledState;
            if (PlayerArmIK.Instance != null)
            {
                PlayerArmIK.Instance.SetFlashlightIntensity(isOn ? maxIntensity : 0f);
            }
            if (glassMaterialInstance != null && glassMaterialInstance.HasProperty("_EmissionColor"))
            {
                glassMaterialInstance.SetColor("_EmissionColor", isOn ? glassEmissionOn : Color.black);
            }
        }

        public override void OnPlayerDied()
        {
            SetLightStateImmediate(false);
            base.OnPlayerDied();
        }

        public override void OnPlayerRespawned()
        {
            base.OnPlayerRespawned();
            SetLightStateImmediate(false);
        }

        void Update()
        {
            if (!isEquipped || !isOn || !flickerNearGhost) return;

            // Kiểm tra ma gần nếu bật cờ nhấp nháy
            GhostAI ghost = GhostAI.Instance;
            if (ghost != null && spotLight != null)
            {
                float dist = Vector3.Distance(transform.position, ghost.transform.position);
                if (dist <= ghostFlickerDistance)
                {
                    float flickerFactor = Mathf.PerlinNoise(Time.time * 25f, 0f);
                    if (flickerFactor < 0.25f)
                    {
                        spotLight.intensity = maxIntensity * 0.2f;
                    }
                    else
                    {
                        spotLight.intensity = maxIntensity;
                    }
                }
            }
        }
    }
}
