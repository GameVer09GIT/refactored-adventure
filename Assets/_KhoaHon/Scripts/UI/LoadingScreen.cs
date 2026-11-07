using System.Collections;
using KhoaHon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KhoaHon.UI
{
    /// <summary>
    /// Màn hình tải kiểu game thương mại: nền đen, tên dinh thự, mẹo chơi ngẫu nhiên, thanh tiến trình;
    /// nạp scene bất đồng bộ, giữ màn tải thêm chốc lát cho game khởi tạo rồi mờ dần.
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        private static LoadingScreen active;
        public static bool IsLoading => active != null;

        private CanvasGroup group;
        private Image bar;

        /// <summary>Nạp lại scene hiện tại (hoặc path) có màn tải.</summary>
        public static void Load(string scenePath = null)
        {
            if (active != null) return;
            if (string.IsNullOrEmpty(scenePath)) scenePath = SceneManager.GetActiveScene().path;
            var go = new GameObject("KH_LoadingScreen");
            DontDestroyOnLoad(go);
            active = go.AddComponent<LoadingScreen>();
            active.Build();
            active.StartCoroutine(active.Run(scenePath));
        }

        private void Build()
        {
            var canvas = UIFactory.Canvas("Canvas", 200);
            canvas.transform.SetParent(transform, false);
            var bg = UIFactory.Panel("Bg", canvas.transform, Color.black, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            group = bg.gameObject.AddComponent<CanvasGroup>();
            var style = T.Table != null && GameManager.Instance != null ? GameManager.Instance.Style : null;
            var title = UIFactory.Text("Title", bg.transform, style, true, 64, new Color(0.66f, 0.05f, 0.04f), TextAlignmentOptions.BottomLeft);
            title.text = T.Get("ui.title_main"); title.characterSpacing = 6f;
            title.rectTransform.anchorMin = new Vector2(0.06f, 0.14f); title.rectTransform.anchorMax = new Vector2(0.9f, 0.26f);
            int n = 0; while (T.Table != null && T.Table.Has("tip." + (n + 1))) n++;
            var tip = UIFactory.Text("Tip", bg.transform, style, false, 26, new Color(0.75f, 0.72f, 0.68f), TextAlignmentOptions.TopLeft);
            tip.text = n > 0 ? T.Get("tip." + Random.Range(1, n + 1)) : "";
            tip.rectTransform.anchorMin = new Vector2(0.06f, 0.06f); tip.rectTransform.anchorMax = new Vector2(0.7f, 0.13f);
            var loading = UIFactory.Text("Loading", bg.transform, style, false, 22, new Color(0.5f, 0.48f, 0.45f), TextAlignmentOptions.BottomRight);
            loading.text = T.Get("ui.loading");
            loading.rectTransform.anchorMin = new Vector2(0.7f, 0.06f); loading.rectTransform.anchorMax = new Vector2(0.94f, 0.1f);
            UIFactory.Panel("BarBg", bg.transform, new Color(1, 1, 1, 0.08f), new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.04f), new Vector2(0, -1), new Vector2(0, 2));
            bar = UIFactory.Panel("Bar", bg.transform, new Color(0.66f, 0.05f, 0.04f), new Vector2(0.06f, 0.04f), new Vector2(0.06f, 0.04f), new Vector2(0, -1), new Vector2(0, 2));
        }

        private IEnumerator Run(string scenePath)
        {
            group.alpha = 0f;
            for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime) { group.alpha = t / 0.35f; yield return null; }
            group.alpha = 1f;
            var op = SceneManager.LoadSceneAsync(scenePath);
            while (!op.isDone) { SetBar(op.progress / 0.9f * 0.85f); yield return null; }
            // Chờ game khởi tạo (shader, Start, áp save) trước khi lộ ra
            for (float t = 0f; t < 1.2f; t += Time.unscaledDeltaTime) { SetBar(0.85f + 0.15f * t / 1.2f); yield return null; }
            for (float t = 0f; t < 0.6f; t += Time.unscaledDeltaTime) { group.alpha = 1f - t / 0.6f; yield return null; }
            active = null;
            Destroy(gameObject);
        }

        private void SetBar(float k)
        {
            k = Mathf.Clamp01(k);
            bar.rectTransform.anchorMax = new Vector2(0.06f + 0.88f * k, 0.04f);
        }
    }
}
