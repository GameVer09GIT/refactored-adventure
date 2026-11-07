using System.Collections;
using KhoaHon.Core;
using KhoaHon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace KhoaHon.Narrative
{
    /// <summary>Ba kết thúc của Blackwood Manor. Chỉ thêm giá trị mới ở cuối.</summary>
    public enum EndingId
    {
        Release = 0,   // Kết 1 — Giải thoát
        NewMaster = 1, // Kết 2 — Tân Chủ Nhân
        Coward = 2,    // Kết ẩn — Kẻ Hèn Nhát
    }

    /// <summary>
    /// Điều phối kết thúc: đặt cờ, lưu thành tích, chạy cutscene (nếu có) rồi hiện màn kết thúc (tiêu đề, lời kể, thống kê)
    /// và về menu chính. Cutscene từng kết đăng ký qua <see cref="CutsceneHook"/>.
    /// </summary>
    public class EndingDirector : MonoBehaviour
    {
        public static EndingDirector Instance { get; private set; }

        /// <summary>Cutscene riêng cho từng kết (Phase F gán). Trả về IEnumerator chạy trước màn chữ.</summary>
        public static System.Func<EndingId, IEnumerator> CutsceneHook;

        public static readonly string[] EndingKeys = { "end.1", "end.2", "end.3" };
        public bool Running { get; private set; }

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void Play(EndingId id)
        {
            if (Running) return;
            StartCoroutine(Run(id));
        }

        private IEnumerator Run(EndingId id)
        {
            Running = true;
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.SetFlag(id == EndingId.Release ? StoryFlag.Ending1 : id == EndingId.NewMaster ? StoryFlag.Ending2 : StoryFlag.EndingSecret);
                gm.SetStage(GameStage.Ending);
            }
            PlayerPrefs.SetInt("kh_ending_" + (int)id, 1);
            PlayerPrefs.Save();
            GameFlow.Set(FlowState.Intro);
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) pc.enabled = false;

            if (CutsceneHook != null)
            {
                var cs = CutsceneHook(id);
                if (cs != null) yield return cs;
            }

            // Màn chữ kết thúc
            var canvas = UIFactory.Canvas("KH_EndingCanvas", 120);
            var bg = UIFactory.Panel("Bg", canvas.transform, Color.black, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var cg = bg.gameObject.AddComponent<CanvasGroup>(); cg.alpha = 0f;
            string k = EndingKeys[(int)id];
            var title = UIKit.Label(bg.transform, T.Get(k + ".title"), 54, UIKit.Blood, TextAlignmentOptions.Center, true, true);
            title.rectTransform.anchorMin = new Vector2(0.1f, 0.72f); title.rectTransform.anchorMax = new Vector2(0.9f, 0.86f);
            title.characterSpacing = 8f;
            var body = UIKit.Label(bg.transform, T.Get(k + ".body"), 30, UIKit.Ink, TextAlignmentOptions.Top, true);
            body.rectTransform.anchorMin = new Vector2(0.18f, 0.2f); body.rectTransform.anchorMax = new Vector2(0.82f, 0.68f);
            body.lineSpacing = 14f;
            var stats = UIKit.Label(bg.transform, StatsLine(), 22, UIKit.Dim, TextAlignmentOptions.Bottom);
            stats.rectTransform.anchorMin = new Vector2(0.1f, 0.08f); stats.rectTransform.anchorMax = new Vector2(0.9f, 0.14f);
            var cont = UIKit.Label(bg.transform, T.Get("end.continue"), 20, new Color(0.45f, 0.42f, 0.4f), TextAlignmentOptions.Bottom);
            cont.rectTransform.anchorMin = new Vector2(0.1f, 0.03f); cont.rectTransform.anchorMax = new Vector2(0.9f, 0.07f);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;

            float t = 0f;
            while (t < 2.5f) { t += Time.unscaledDeltaTime; cg.alpha = t / 2.5f; yield return null; }
            float wait = 0f;
            while (wait < 1.5f || !AnyKey()) { wait += Time.unscaledDeltaTime; yield return null; }

            // Xóa save chơi dở (đã kết thúc) rồi về menu
            SaveSystem.Delete();
            GameFlow.PendingStart = StartMode.None;
            ModalUI.Reset();
            SceneManager.LoadScene(SceneManager.GetActiveScene().path);
        }

        private static string StatsLine()
        {
            int docs = Notebook.NotebookManager.Instance != null ? Notebook.NotebookManager.Instance.Entries.Count : 0;
            var span = System.TimeSpan.FromSeconds(Time.timeSinceLevelLoad);
            return T.Format("end.stats", $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}", PlayerPrefs.GetInt("kh_deaths_run", 0), docs);
        }

        private static bool AnyKey() =>
            (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
    }
}
