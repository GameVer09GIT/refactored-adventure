using System.Collections;
using UnityEngine;

namespace HorrorGame.Inventory
{
    /// <summary>
    /// Điều khiển thiết bị Radar cầm tay (kích hoạt quét sóng, điều khiển RadarHUD, emission màn hình)
    /// </summary>
    public class RadarItem : HeldItem
    {
        [Header("Màn hình thiết bị")]
        [Tooltip("Renderer màn hình radar trên model cầm tay")]
        [SerializeField] private Renderer screenRenderer;

        [Tooltip("Chỉ số material màn hình")]
        [SerializeField] private int screenMaterialIndex = 0;

        [Tooltip("Màu emission màn hình ở trạng thái chờ (xanh dịu không lóa)")]
        [ColorUsage(true, true)]
        [SerializeField] private Color screenIdleEmission = new Color(0.01f, 0.22f, 0.04f, 1f);

        [Tooltip("Màu emission màn hình khi quét/phát hiện ma")]
        [ColorUsage(true, true)]
        [SerializeField] private Color screenActiveEmission = new Color(0.10f, 0.65f, 0.15f, 1f);

        [Header("Giao diện Radar 3D")]
        [Tooltip("Màn hình Radar 3D gắn trực tiếp trên thân máy")]
        [SerializeField] private Radar3DScreen radar3DScreen;

        [Tooltip("Hiển thị giao diện 2D HUD góc màn hình song song với màn hình 3D trên máy")]
        [SerializeField] private bool hide2DHUDWhenEquipped = true;

        [Header("Tham chiếu RadarHUD")]
        [Tooltip("HUD Radar hiển thị góc dưới phải màn hình")]
        [SerializeField] private RadarHUD radarHUD;

        private Material screenMaterialInstance;
        private Coroutine pulseCoroutine;

        protected override void Awake()
        {
            base.Awake();

            if (radar3DScreen == null)
            {
                radar3DScreen = GetComponent<Radar3DScreen>() ?? GetComponentInChildren<Radar3DScreen>();
                if (radar3DScreen == null)
                {
                    radar3DScreen = gameObject.AddComponent<Radar3DScreen>();
                }
            }

            if (screenRenderer != null && screenRenderer.materials != null && screenMaterialIndex < screenRenderer.materials.Length)
            {
                screenMaterialInstance = screenRenderer.materials[screenMaterialIndex];
            }

            SetScreenEmission(screenIdleEmission);

            if (radarHUD == null)
            {
                radarHUD = RadarHUD.Instance ?? Object.FindAnyObjectByType<RadarHUD>();
            }
            if (radarHUD != null && hide2DHUDWhenEquipped)
            {
                radarHUD.ShowHUD(false);
                radarHUD.gameObject.SetActive(false);
            }
        }

        public override void OnEquip(PlayerController playerController)
        {
            base.OnEquip(playerController);

            if (radarHUD == null)
            {
                radarHUD = RadarHUD.Instance ?? Object.FindAnyObjectByType<RadarHUD>();
            }

            if (radarHUD != null)
            {
                radarHUD.BindPlayer(playerController);
                if (hide2DHUDWhenEquipped)
                {
                    radarHUD.ShowHUD(false);
                    radarHUD.gameObject.SetActive(false);
                }
                else
                {
                    radarHUD.ShowHUD(true);
                }
            }

            SetScreenEmission(screenIdleEmission);
        }

        public override void OnUnequip()
        {
            if (radarHUD != null)
            {
                radarHUD.ShowHUD(false);
            }

            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }

            base.OnUnequip();
        }

        public override void OnPrimaryUse()
        {
            if (radarHUD == null)
            {
                radarHUD = RadarHUD.Instance ?? Object.FindAnyObjectByType<RadarHUD>();
            }

            Transform camTrans = player != null && player.playerCamera != null
                ? player.playerCamera
                : (Camera.main != null ? Camera.main.transform : transform);

            bool started3D = false;
            bool startedHUD = false;
            if (radar3DScreen != null && radar3DScreen.CanScan)
            {
                started3D = radar3DScreen.TriggerScan(camTrans, OnGhostDetected);
            }
            if (!hide2DHUDWhenEquipped && radarHUD != null && radarHUD.CanScan)
            {
                startedHUD = radarHUD.TriggerScan(OnGhostDetected);
            }

            bool started = started3D || startedHUD;

            if (started)
            {
                // Cử động ngón cái tay phải ấn nút quét và giật nhẹ
                if (ViewModelRoot.Instance != null)
                {
                    ViewModelRoot.Instance.TriggerItemAction(true, 0.08f);
                }
                else if (swayComponent != null)
                {
                    swayComponent.ApplyClickRecoil(0.008f, 1.0f);
                }
                StartScreenPulse(1.5f);
            }
        }

        private void OnGhostDetected(bool detected)
        {
            if (detected)
            {
                // Chớp sáng màn hình khi bắt được tín hiệu ma
                StartScreenPulse(0.35f);
            }
        }

        private void StartScreenPulse(float duration)
        {
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(ScreenPulseRoutine(duration));
        }

        private IEnumerator ScreenPulseRoutine(float duration)
        {
            SetScreenEmission(screenActiveEmission);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                Color c = Color.Lerp(screenActiveEmission, screenIdleEmission, t);
                SetScreenEmission(c);
                yield return null;
            }
            SetScreenEmission(screenIdleEmission);
            pulseCoroutine = null;
        }

        private void SetScreenEmission(Color color)
        {
            if (screenMaterialInstance != null && screenMaterialInstance.HasProperty("_EmissionColor"))
            {
                screenMaterialInstance.SetColor("_EmissionColor", color);
            }
        }

        public override void OnPlayerDied()
        {
            if (radarHUD != null)
            {
                radarHUD.ResetAll();
            }
            base.OnPlayerDied();
        }

        public override void OnPlayerRespawned()
        {
            base.OnPlayerRespawned();
            if (radarHUD != null)
            {
                radarHUD.ResetAll();
            }
        }
    }
}
