using System.Collections;
using KhoaHon.Core;
using KhoaHon.Notebook;
using UnityEngine;

namespace KhoaHon.Narrative
{
    /// <summary>
    /// Phát băng cassette của Julian (qua máy ghi âm của Evelyn hoặc điện thoại của Alex): tiếng rè + clip lồng tiếng
    /// (nếu có) + phụ đề theo thời gian. Vẫn đi lại được khi nghe. Nghe hết thì hồi lý trí một lần.
    /// </summary>
    public class TapePlayer : MonoBehaviour
    {
        public static TapePlayer Instance { get; private set; }

        [SerializeField] private AudioClip hissLoop, clickClip;

        private AudioSource hiss, voice;
        private Coroutine running;
        public TapeData Current { get; private set; }
        public bool IsPlaying => running != null;

        public void Configure(AudioClip hissClip, AudioClip click) { hissLoop = hissClip; clickClip = click; }

        private void Awake()
        {
            Instance = this;
            hiss = gameObject.AddComponent<AudioSource>(); hiss.loop = true; hiss.spatialBlend = 0f; hiss.playOnAwake = false;
            voice = gameObject.AddComponent<AudioSource>(); voice.loop = false; voice.spatialBlend = 0f; voice.playOnAwake = false;
            DeathScreenManager.OnPlayerDied += Stop;
        }

        private void OnDestroy()
        {
            DeathScreenManager.OnPlayerDied -= Stop;
            if (Instance == this) Instance = null;
        }

        public void Play(TapeData tape)
        {
            if (tape == null) return;
            Stop();
            Current = tape;
            running = StartCoroutine(Run(tape));
        }

        public void Stop()
        {
            if (running != null) StopCoroutine(running);
            running = null;
            if (hiss != null) hiss.Stop();
            if (voice != null) voice.Stop();
            if (Current != null) KhoaHonHUD.Instance?.ClearSubtitle();
            Current = null;
        }

        private IEnumerator Run(TapeData tape)
        {
            if (clickClip != null) Horror.AmbientScares.Play2D(clickClip, 0.8f);
            KhoaHonHUD.Toast(T.Format("tape.playing", tape.Title));
            if (hissLoop != null) { hiss.clip = hissLoop; hiss.volume = 0.25f * AudioMix.Voice; hiss.Play(); }
            if (tape.clip != null) { voice.clip = tape.clip; voice.volume = AudioMix.Voice; voice.Play(); }

            float t = 0f, end = 0f;
            foreach (var l in tape.lines) end = Mathf.Max(end, l.time + l.duration);
            if (tape.clip != null) end = Mathf.Max(end, tape.clip.length);
            int next = 0;
            while (t < end + 0.5f)
            {
                // Tạm dừng game thì băng cũng dừng
                if (GameFlow.State == FlowState.Paused) { yield return null; continue; }
                t += Time.deltaTime;
                while (next < tape.lines.Count && t >= tape.lines[next].time)
                {
                    var l = tape.lines[next];
                    KhoaHonHUD.Subtitle("<color=#B9A36A>" + T.Get("tape.speaker") + ":</color> " + l.Text, l.duration);
                    next++;
                }
                yield return null;
            }
            if (clickClip != null) Horror.AmbientScares.Play2D(clickClip, 0.8f);
            hiss.Stop();
            if (GameManager.Instance != null) GameManager.Instance.Sanity += tape.sanityRestore;
            running = null;
            Current = null;
        }
    }
}
