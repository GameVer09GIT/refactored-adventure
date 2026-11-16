using KhoaHon.AI;
using KhoaHon.Core;
using UnityEngine;
using UnityEngine.AI;

namespace KhoaHon.Horror
{
    /// <summary>
    /// Tiếng bước chân nặng của Mrs. Peake trên sàn gỗ: phát theo quãng đường đi được (khớp sải chân), 3D + bị tường che thì đục.
    /// Bước nhanh dần = bà đang lao tới — người chơi nghe được trước khi nhìn thấy.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class HousekeeperFootsteps : MonoBehaviour
    {
        [SerializeField] private AudioClip[] steps = new AudioClip[0];
        [Tooltip("Sải chân khi đi / khi chạy (m mỗi bước)")]
        [SerializeField] private Vector2 stride = new Vector2(0.72f, 1.2f);
        [SerializeField] private float maxDistance = 24f;

        private NavMeshAgent agent;
        private AudioSource src;
        private float travelled;
        private int last = -1;

        public void Configure(AudioClip[] clips) => steps = clips;

        private void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            var go = new GameObject("Footsteps");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            src = go.AddComponent<AudioSource>();
            src.playOnAwake = false; src.spatialBlend = 1f; src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = 1.6f; src.maxDistance = maxDistance; src.dopplerLevel = 0f;
            go.AddComponent<AudioOcclusion>().SetIgnoreRoot(transform);
        }

        private void Update()
        {
            if (!GameFlow.IsPlaying || steps == null || steps.Length == 0) return;
            float v = agent.velocity.magnitude;
            if (v < 0.25f) { travelled = Mathf.Min(travelled, stride.x * 0.5f); return; }
            travelled += v * Time.deltaTime;
            float run = Mathf.InverseLerp(1.6f, 5.5f, v);
            float need = Mathf.Lerp(stride.x, stride.y, run);
            if (travelled < need) return;
            travelled = 0f;
            int i = Random.Range(0, steps.Length);
            if (i == last) i = (i + 1) % steps.Length;
            last = i;
            src.pitch = Random.Range(0.9f, 1.08f) + run * 0.08f;
            src.PlayOneShot(steps[i], Mathf.Lerp(0.5f, 1f, run) * AudioMix.Sfx);
        }
    }
}
