using KhoaHon.Core;
using UnityEngine;

namespace KhoaHon.Interaction
{
    /// <summary>
    /// Đồ vật vật lý (chai, cốc, ấm, chậu…): va chạm đủ mạnh thì phát tiếng và gây tiếng động —
    /// Mrs. Peake nghe được và chuyển sang Nghi ngờ (Suspicious).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class NoisyProp : MonoBehaviour
    {
        [SerializeField] private float minImpulse = 1.2f;
        [SerializeField] private float noiseRadius = 11f;
        [SerializeField] private AudioClip[] hitClips = new AudioClip[0];
        private float nextNoise;

        public void Configure(AudioClip[] clips, float radius) { hitClips = clips; noiseRadius = radius; }

        private void OnCollisionEnter(Collision c)
        {
            if (Time.time < nextNoise || c.impulse.magnitude < minImpulse) return;
            nextNoise = Time.time + 0.6f;
            if (hitClips != null && hitClips.Length > 0)
            {
                var clip = hitClips[Random.Range(0, hitClips.Length)];
                if (clip != null) AudioSource.PlayClipAtPoint(clip, transform.position, Mathf.Clamp01(c.impulse.magnitude / 5f));
            }
            if (GameFlow.IsPlaying) GameEvents.RaiseNoise(transform.position, noiseRadius, NoiseType.PropFall);
        }
    }
}
