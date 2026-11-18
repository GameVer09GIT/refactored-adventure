using System.Collections;
using Blackwood.Core;
using Blackwood.Horror;
using Blackwood.Items;
using Blackwood.Narrative;
using Blackwood.UI;
using TMPro;
using UnityEngine;

namespace Blackwood.Sequences
{
    /// <summary>Điều kiện mở khóa lựa chọn ở Sảnh Tế (logic thuần, có test).</summary>
    public static class EndingRules
    {
        /// <summary>Kết 1 cần đủ 5 kỷ vật (để ném vào vạc) và búa của Thomas (Relic_Kim) để đập Cổ Vật.</summary>
        public static bool CanRelease(System.Func<StoryFlag, bool> has)
        {
            for (int i = 0; i < 5; i++) if (!has(HanhUtil.RelicFlag((Hanh)i))) return false;
            return has(StoryFlag.Relic_Kim);
        }

        /// <summary>Kết 2 cần bài chú thứ năm (trang Quý Bà — Relic_Hoa) để đọc ngược.</summary>
        public static bool CanClaim(System.Func<StoryFlag, bool> has) => has(StoryFlag.Relic_Hoa);
    }

    /// <summary>
    /// Sảnh Tế Khóa Hồn: bước vào là Lord Arthur Blackwood (bóng ma trên ghế mun) bắt đầu đàm phán.
    /// Hết lời dụ dỗ → người chơi chọn: thiêu kỷ vật + đập Cổ Vật (Kết 1) hoặc nhỏ máu + đọc ngược bài chú (Kết 2).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class RitualSequence : MonoBehaviour
    {
        [SerializeField] private Transform lord;
        [SerializeField] private Transform relic;
        [SerializeField] private AudioClip lordVoice, ambience;
        [Tooltip("Giọng Lord Blackwood (ElevenLabs): 0–3 = lord.1–4, 4–6 = lord.2–4 bản Alex. Thiếu thì chỉ hiện phụ đề.")]
        [SerializeField] private AudioClip[] voiceVi = new AudioClip[7];
        [SerializeField] private AudioClip[] voiceEn = new AudioClip[7];

        private bool running;
        private GameObject choiceUI;

        public Transform Relic => relic;
        public Transform Lord => lord;

        public void Configure(Transform lordGhost, Transform relicObj, AudioClip voice, AudioClip amb)
        {
            lord = lordGhost; relic = relicObj; lordVoice = voice; ambience = amb;
        }

        public void ConfigureVoice(AudioClip[] vi, AudioClip[] en) { voiceVi = vi; voiceEn = en; }

        /// <summary>Clip giọng của câu thứ i (1–4) theo ngôn ngữ đang chọn; bản Alex dùng ô 4–6.</summary>
        private AudioClip VoiceFor(int i, bool alex)
        {
            var set = Loc.IsEnglish ? voiceEn : voiceVi;
            if (set == null) return null;
            int k = alex && i >= 2 ? 4 + (i - 2) : i - 1;
            // Không lùi về câu của Evelyn: lời thoại bản Alex khác xưng hô — thiếu giọng thì chỉ hiện phụ đề
            return k < set.Length ? set[k] : null;
        }

        private void Update()
        {
            // Cổ Vật lơ lửng, xoay chậm; Lord thở nhẹ
            if (relic != null) relic.localRotation = Quaternion.Euler(0f, Time.time * 12f, 0f) * Quaternion.Euler(8f * Mathf.Sin(Time.time * 0.7f), 0f, 0f);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (running || other.GetComponentInParent<PlayerController>() == null) return;
            var gm = GameManager.Instance;
            if (gm == null || !GameFlow.IsPlaying || gm.Has(StoryFlag.Ending1) || gm.Has(StoryFlag.Ending2)) return;
            StartCoroutine(Negotiate());
        }

        private IEnumerator Negotiate()
        {
            running = true;
            var gm = GameManager.Instance;
            GameFlow.Set(FlowState.Intro);
            var pc = FindFirstObjectByType<PlayerController>();
            var from = pc.playerCamera;
            var cam = CutsceneKit.Begin(from.position, from.rotation, 55f, true);
            var target = lord != null ? lord.position + Vector3.up * 1.55f : relic.position;
            float t = 0f;
            var startRot = cam.transform.rotation;
            while (t < 2f) { t += Time.deltaTime; cam.transform.rotation = Quaternion.Slerp(startRot, Quaternion.LookRotation(target - cam.transform.position), Mathf.SmoothStep(0, 1, t / 2f)); yield return null; }
            HorrorPostFX.CinematicFocus = true;
            if (!gm.Has(StoryFlag.MetDaiQuan)) gm.SetFlag(StoryFlag.MetDaiQuan);
            gm.SetStage(GameStage.Stage4_Ritual);

            // Góc máy: nghiêng sang bên để thấy Lord ngồi sau vạc; câu 3 (lời dụ dỗ) cận cảnh Cổ Vật
            var lordHead = target;
            var relicPos = relic != null ? relic.position : lordHead;
            var side = Vector3.Cross(Vector3.up, (lordHead - relicPos).normalized);
            var sideShot = Vector3.Lerp(relicPos, lordHead, 0.45f) + side * 2.4f + Vector3.up * 0.3f;
            bool alex = gm.Character == PlayerCharacter.Alex;
            for (int i = 1; i <= 4; i++)
            {
                var spoken = VoiceFor(i, alex);
                if (lordVoice != null) AmbientScares.Play2D(lordVoice, spoken != null ? 0.12f : 0.35f); // thì thầm nền nhỏ lại khi có giọng
                AudioSource voiceSrc = null;
                if (spoken != null)
                {
                    voiceSrc = new GameObject("LordVoice").AddComponent<AudioSource>();
                    voiceSrc.clip = spoken; voiceSrc.spatialBlend = 0f; voiceSrc.volume = AudioMix.Voice; voiceSrc.Play();
                    Destroy(voiceSrc.gameObject, spoken.length + 0.3f);
                }
                Vector3 pos, look;
                if (i == 3) { pos = relicPos + side * 1.1f + (lordHead - relicPos).normalized * -0.4f + Vector3.up * 0.15f; look = relicPos; }
                else if (i == 4) { pos = lordHead + (relicPos - lordHead).normalized * 1.7f + side * 0.5f; look = lordHead; }
                else { pos = sideShot; look = lordHead; }
                cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(look - pos));
                HorrorPostFX.FocusDistance = Vector3.Distance(pos, look);
                yield return SlowPush(cam.transform, look, 0.3f, 0.25f);
                string key = "lord." + i;
                if (alex && T.Table != null && T.Table.Has(key + ".alex")) key += ".alex";
                yield return CutsceneKit.Line("<color=#B8A07A>Lord Blackwood:</color> " + T.Get(key), spoken != null ? Mathf.Max(4f, spoken.length + 0.7f) : 5.2f);
            }
            HorrorPostFX.CinematicFocus = false;

            // Lựa chọn
            int pick = -1;
            ShowChoices(c => pick = c);
            while (pick < 0) yield return null;
            if (choiceUI != null) Destroy(choiceUI);
            ModalUI.Close(this);
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            EndingDirector.Instance?.Play(pick == 0 ? EndingId.Release : EndingId.NewMaster);
        }

        private static IEnumerator SlowPush(Transform cam, Vector3 target, float dist, float time)
        {
            var a = cam.position; var b = a + (target - a).normalized * dist;
            float t = 0f;
            while (t < time) { t += Time.deltaTime; cam.position = Vector3.Lerp(a, b, t / time); yield return null; }
        }

        private void ShowChoices(System.Action<int> picked)
        {
            ModalUI.Open(this);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            var gm = GameManager.Instance;
            bool canRelease = EndingRules.CanRelease(gm.Has);
            bool canClaim = EndingRules.CanClaim(gm.Has);
            var dim = UIFactory.Panel("BW_RitualChoice", UIKit.TopLayer, new Color(0, 0, 0, 0.78f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            choiceUI = dim.gameObject;
            var title = UIKit.Label(dim.transform, T.Get("ritual.choose"), 40, UIKit.Ink, TextAlignmentOptions.Center, true, true);
            title.rectTransform.anchorMin = new Vector2(0.1f, 0.62f); title.rectTransform.anchorMax = new Vector2(0.9f, 0.74f);
            var col = UIKit.Column(dim.transform, "Choices", new Vector2(0.22f, 0.3f), new Vector2(0.78f, 0.58f), Vector2.zero, Vector2.zero, 10);
            var a = UIKit.MenuButton(col, T.Get("lord.choice.burn") + (canRelease ? "" : "  " + T.Get("ritual.need_relics")), () => picked(0), 32, 64);
            a.interactable = canRelease;
            var b = UIKit.MenuButton(col, T.Get("lord.choice.blood") + (canClaim ? "" : "  " + T.Get("ritual.need_verse")), () => picked(1), 32, 64);
            b.interactable = canClaim;
            // Không đủ điều kiện cả hai (không xảy ra vì hầm chỉ mở khi đủ 5 kỷ vật) → vẫn cho chọn để không kẹt
            if (!canRelease && !canClaim) a.interactable = true;
        }
    }
}
