using KhoaHon.AI;
using KhoaHon.Core;
using UnityEngine;

namespace KhoaHon.Horror
{
    /// <summary>
    /// Nền âm thanh nhà + hù dọa ngẫu nhiên quanh Vy (tiếng gõ, cót két, đập rầm, thì thầm sau lưng).
    /// Lý trí càng thấp hù càng dày. Stinger khi bà Quản Gia bắt đầu truy đuổi.
    /// </summary>
    public class AmbientScares : MonoBehaviour
    {
        [SerializeField] private AudioSource ambience;
        [SerializeField] private AudioClip[] scares = new AudioClip[0];
        [SerializeField] private AudioClip whispers;
        [SerializeField] private AudioClip stinger;
        [SerializeField] private Vector2 interval = new Vector2(22f, 55f);

        private float next;
        private Transform player;
        private HousekeeperState lastState;

        private void Start()
        {
            next = Time.time + Random.Range(interval.x, interval.y) * 0.5f;
            if (ambience != null) { ambience.loop = true; ambience.spatialBlend = 0f; ambience.Play(); }
        }

        private void Update()
        {
            if (ambience != null) ambience.volume = Mathf.MoveTowards(ambience.volume, (GameFlow.State == FlowState.Menu ? 0.35f : 0.75f) * AudioMix.Ambience, Time.unscaledDeltaTime * 0.5f);
            if (!GameFlow.IsPlaying) return;
            if (player == null) { var pc = FindFirstObjectByType<PlayerController>(); if (pc != null) player = pc.transform; else return; }

            var hk = HousekeeperAI.Instance;
            if (hk != null)
            {
                if (hk.State == HousekeeperState.Hunt && lastState != HousekeeperState.Hunt && lastState != HousekeeperState.Stunned)
                {
                    Play2D(stinger, 0.9f);
                    HorrorPostFX.Instance?.Flash(0.6f);
                }
                lastState = hk.State;
            }

            float sanity01 = GameManager.Instance != null ? GameManager.Instance.Sanity / 100f : 1f;
            if (Time.time < next) return;
            next = Time.time + Random.Range(interval.x, interval.y) * Mathf.Lerp(0.45f, 1f, sanity01);
            bool behindWhisper = whispers != null && (sanity01 < 0.6f || Random.value < 0.25f);
            var clip = behindWhisper ? whispers : (scares.Length > 0 ? scares[Random.Range(0, scares.Length)] : null);
            if (clip == null) return;
            var back = -player.forward; back.y = 0;
            var dir = Quaternion.Euler(0, Random.Range(-70f, 70f), 0) * back.normalized;
            var pos = player.position + dir * (behindWhisper ? 1.2f : Random.Range(4f, 9f)) + Vector3.up * Random.Range(0.5f, 3f);
            var go = new GameObject("Scare_" + clip.name);
            go.transform.position = pos;
            var s = go.AddComponent<AudioSource>();
            s.clip = clip; s.spatialBlend = 1f; s.rolloffMode = AudioRolloffMode.Linear; s.minDistance = 1f; s.maxDistance = 25f;
            s.volume = (behindWhisper ? 0.55f : 0.9f) * AudioMix.Sfx; s.pitch = Random.Range(0.85f, 1.1f);
            s.Play();
            Destroy(go, clip.length + 0.2f);
        }

        public static void Play2D(AudioClip clip, float vol)
        {
            if (clip == null) return;
            var go = new GameObject("2D_" + clip.name);
            var s = go.AddComponent<AudioSource>(); s.clip = clip; s.spatialBlend = 0f; s.volume = vol * AudioMix.Sfx; s.ignoreListenerPause = true; s.Play();
            Object.Destroy(go, clip.length + 0.2f);
        }
    }
}
