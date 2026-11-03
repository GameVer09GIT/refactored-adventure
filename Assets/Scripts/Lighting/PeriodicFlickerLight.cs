using UnityEngine;

namespace HorrorGame.Lighting
{
    /// <summary>
    /// Periodic flicker effect for horror ambient lights (candles, old lamps).
    /// Every <see cref="flickerInterval"/> seconds, performs a short burst of
    /// rapid intensity fluctuations simulating electrical fault or wind-blown candle.
    /// Optionally syncs emission color on an attached Renderer material.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class PeriodicFlickerLight : MonoBehaviour
    {
        [Header("Flicker Timing")]
        [Tooltip("Seconds between each flicker burst.")]
        [SerializeField] private float flickerInterval = 5.0f;

        [Tooltip("Duration of the flicker burst in seconds.")]
        [SerializeField] private float flickerDuration = 0.5f;

        [Header("Intensity Multipliers")]
        [Tooltip("Minimum intensity multiplier during flicker (0 = fully off).")]
        [SerializeField, Range(0f, 1f)] private float minIntensityMult = 0f;

        [Tooltip("Maximum intensity multiplier during flicker (1 = base intensity).")]
        [SerializeField, Range(0f, 1.5f)] private float maxIntensityMult = 1.2f;

        [Header("Emission Sync")]
        [Tooltip("Optional: Renderer whose material emission syncs with flicker.")]
        [SerializeField] private Renderer emissiveRenderer;

        [Tooltip("Emission color when light is at base intensity.")]
        [SerializeField] private Color baseEmissionColor = new Color(2.5f, 1.4f, 0.4f);

        [Header("Organic Idle Flutter")]
        [Tooltip("Subtle breathing flutter during the 5s interval so candle/lamp feels alive.")]
        [SerializeField] private bool enableIdleFlutter = true;

        // --- Private state ---
        private Light _light;
        private float _baseIntensity;
        private float _timer;
        private float _flickerTimer;
        private bool _isFlickering;
        private MaterialPropertyBlock _mpb;
        private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            _light = GetComponent<Light>();
            _baseIntensity = _light.intensity;
            // First flicker burst happens quickly (after 1.2s - 2.0s) so the player immediately sees it
            _timer = flickerInterval - Random.Range(1.2f, 2.0f);
            _mpb = new MaterialPropertyBlock();

            if (emissiveRenderer == null)
            {
                emissiveRenderer = GetComponentInChildren<Renderer>();
            }
            SyncEmission(1f);
        }

        private void Update()
        {
            if (_isFlickering)
            {
                UpdateFlickerBurst();
            }
            else
            {
                _timer += Time.deltaTime;

                // Subtle organic candle breathing during idle
                if (enableIdleFlutter)
                {
                    float subtleNoise = Mathf.PerlinNoise(Time.time * 3f + (float)GetInstanceID() * 0.1f, 0f);
                    float subtleMult = Mathf.Lerp(0.92f, 1.05f, subtleNoise);
                    _light.intensity = _baseIntensity * subtleMult;
                    SyncEmission(subtleMult);
                }

                if (_timer >= flickerInterval)
                {
                    _timer = 0f;
                    _flickerTimer = 0f;
                    _isFlickering = true;
                }
            }
        }

        private void UpdateFlickerBurst()
        {
            _flickerTimer += Time.deltaTime;

            if (_flickerTimer >= flickerDuration)
            {
                // End burst: restore base intensity
                _isFlickering = false;
                _light.intensity = _baseIntensity;
                SyncEmission(1f);
                return;
            }

            // Dramatic, unmistakable flicker cuts simulating electrical short or strong wind burst
            float mult;
            float roll = Random.value;
            if (roll < 0.4f)
            {
                mult = 0f; // Complete blackout cut
            }
            else if (roll < 0.65f)
            {
                mult = Random.Range(0.1f, 0.35f); // Dim spark
            }
            else
            {
                mult = Random.Range(0.8f, 1.2f); // Sharp flare
            }

            _light.intensity = _baseIntensity * mult;
            SyncEmission(mult);
        }

        private void SyncEmission(float multiplier)
        {
            if (emissiveRenderer == null) return;

            emissiveRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(EmissionColorID, baseEmissionColor * multiplier);
            emissiveRenderer.SetPropertyBlock(_mpb);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (flickerInterval < 0.5f) flickerInterval = 0.5f;
            if (flickerDuration < 0.1f) flickerDuration = 0.1f;
            if (flickerDuration > flickerInterval * 0.5f) flickerDuration = flickerInterval * 0.5f;
        }
#endif
    }
}