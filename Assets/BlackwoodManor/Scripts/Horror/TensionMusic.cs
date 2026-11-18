using Blackwood.AI;
using Blackwood.Core;
using UnityEngine;

namespace Blackwood.Horror
{
    /// <summary>
    /// Nhạc nền động theo mức nguy hiểm (không cắt bài đột ngột): drone trầm luôn thấp thoáng; cụm đàn dây nghịch tai dâng lên khi
    /// Mrs. Peake lại gần, khi bà đang nghi ngờ / kiểm tra chỗ trốn, hoặc khi lý trí người chơi cạn. Lúc truy đuổi thì nhường cho nhạc săn.
    /// </summary>
    public class TensionMusic : MonoBehaviour
    {
        [SerializeField] private AudioClip droneLoop;
        [SerializeField] private AudioClip clusterLoop;
        [SerializeField] private float nearDistance = 16f;

        private AudioSource drone, cluster;
        private Transform cam;
        private float tension;

        public void Configure(AudioClip droneClip, AudioClip clusterClip) { droneLoop = droneClip; clusterLoop = clusterClip; }

        private void Start()
        {
            drone = Make("Drone", droneLoop);
            cluster = Make("Cluster", clusterLoop);
        }

        private AudioSource Make(string n, AudioClip clip)
        {
            var go = new GameObject(n); go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.clip = clip; s.loop = true; s.playOnAwake = false; s.spatialBlend = 0f; s.volume = 0f;
            if (clip != null) s.Play();
            return s;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            bool playing = GameFlow.IsPlaying;
            float target = 0f, droneTarget = 0f;
            if (playing)
            {
                if (cam == null) { var pc = FindFirstObjectByType<PlayerController>(); if (pc != null) cam = pc.playerCamera; }
                var hk = HousekeeperAI.Instance;
                float sanity01 = GameManager.Instance != null ? GameManager.Instance.Sanity / 100f : 1f;
                droneTarget = 0.32f + 0.25f * (1f - sanity01);
                target = Mathf.Clamp01(0.55f - sanity01) * 0.8f;
                if (hk != null && cam != null)
                {
                    var d = hk.transform.position - cam.position;
                    float dist = new Vector2(d.x, d.z).magnitude + Mathf.Abs(d.y) * 1.5f;
                    float prox = Mathf.Clamp01(1f - dist / nearDistance);
                    switch (hk.State)
                    {
                        case HousekeeperState.Suspicious: target = Mathf.Max(target, 0.35f + 0.65f * prox); break;
                        case HousekeeperState.Inspect: target = 1f; break;
                        case HousekeeperState.Hunt:
                        case HousekeeperState.Kill: target = 0.25f; droneTarget = 0.15f; break; // nhạc săn lo phần chính
                        case HousekeeperState.Stunned: target = 0.6f; break;
                        default: target = Mathf.Max(target, prox * prox); break;
                    }
                }
            }
            tension = Mathf.MoveTowards(tension, target, dt * (target > tension ? 0.45f : 0.18f));
            if (cluster != null) { cluster.volume = tension * 0.7f * AudioMix.Music; cluster.pitch = 1f + tension * 0.06f; }
            if (drone != null) drone.volume = Mathf.MoveTowards(drone.volume, droneTarget * AudioMix.Music, dt * 0.2f);
        }
    }
}
