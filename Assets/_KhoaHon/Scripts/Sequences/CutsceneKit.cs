using System.Collections;
using KhoaHon.Core;
using KhoaHon.Horror;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace KhoaHon.Sequences
{
    /// <summary>
    /// Bộ công cụ cutscene dựng bằng code (không cần Timeline): màn mờ đen/trắng, camera điện ảnh tạm thời
    /// (tắt camera + điều khiển người chơi), thanh letterbox, chờ theo thời gian thực.
    /// </summary>
    public static class CutsceneKit
    {
        private static Canvas canvas;
        private static Image fade, barTop, barBottom;
        private static Camera cineCam;
        private static Camera playerCam;
        private static PlayerController player;
        private static GameObject oldCanvas;

        public static Camera Camera => cineCam;
        public static bool Active => cineCam != null;

        private static void EnsureCanvas()
        {
            if (canvas != null) return;
            canvas = UIFactory.Canvas("KH_CutsceneCanvas", 110);
            barTop = UIFactory.Panel("BarTop", canvas.transform, Color.black, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, 0));
            barBottom = UIFactory.Panel("BarBottom", canvas.transform, Color.black, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 0));
            fade = UIFactory.Panel("Fade", canvas.transform, new Color(0, 0, 0, 0), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            fade.raycastTarget = false; barTop.raycastTarget = false; barBottom.raycastTarget = false;
        }

        /// <summary>Bật camera điện ảnh ở vị trí/hướng cho trước; tắt điều khiển + camera người chơi.</summary>
        public static Camera Begin(Vector3 pos, Quaternion rot, float fov = 55f, bool letterbox = true)
        {
            EnsureCanvas();
            player = Object.FindFirstObjectByType<PlayerController>();
            playerCam = player != null && player.playerCamera != null ? player.playerCamera.GetComponent<Camera>() : null;
            if (player != null) player.enabled = false;
            if (cineCam == null)
            {
                var go = new GameObject("KH_CineCamera");
                cineCam = go.AddComponent<Camera>();
                cineCam.nearClipPlane = 0.05f; cineCam.farClipPlane = 600f;
                var d = go.AddComponent<UniversalAdditionalCameraData>();
                d.renderPostProcessing = true; d.stopNaN = true;
                go.AddComponent<AudioListener>();
            }
            if (playerCam != null)
            {
                playerCam.enabled = false;
                var al = playerCam.GetComponent<AudioListener>(); if (al != null) al.enabled = false;
            }
            cineCam.transform.SetPositionAndRotation(pos, rot);
            cineCam.fieldOfView = fov;
            cineCam.tag = "MainCamera";
            SetLetterbox(letterbox ? 1f : 0f);
            var hud = KhoaHonHUD.Instance; if (hud != null) hud.Root.gameObject.SetActive(false);
            oldCanvas = GameObject.Find("Canvas"); if (oldCanvas != null) oldCanvas.SetActive(false); // thanh đồ của bản gốc
            HorrorPostFX.CinematicFocus = false;
            return cineCam;
        }

        /// <summary>Trả quyền điều khiển cho người chơi.</summary>
        public static void End(bool restoreControl = true)
        {
            if (cineCam != null) Object.Destroy(cineCam.gameObject);
            cineCam = null;
            if (playerCam != null)
            {
                playerCam.enabled = true;
                var al = playerCam.GetComponent<AudioListener>(); if (al != null) al.enabled = true;
            }
            if (player != null && restoreControl) { player.enabled = true; player.LockCursor(); }
            SetLetterbox(0f);
            var hud = KhoaHonHUD.Instance; if (hud != null) hud.Root.gameObject.SetActive(true);
            if (oldCanvas != null) oldCanvas.SetActive(true);
            HorrorPostFX.CinematicFocus = false;
            HorrorPostFX.FocusDistance = 0.6f;
        }

        public static void SetLetterbox(float k)
        {
            EnsureCanvas();
            float h = 120f * k;
            barTop.rectTransform.offsetMin = new Vector2(0, -h);
            barBottom.rectTransform.offsetMax = new Vector2(0, h);
        }

        public static void SetFade(float a, Color? c = null)
        {
            EnsureCanvas();
            var col = c ?? Color.black;
            fade.color = new Color(col.r, col.g, col.b, a);
        }

        public static IEnumerator Fade(float to, float seconds, Color? c = null)
        {
            EnsureCanvas();
            var col = c ?? new Color(fade.color.r, fade.color.g, fade.color.b);
            if (c.HasValue) fade.color = new Color(col.r, col.g, col.b, fade.color.a);
            float from = fade.color.a, t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                fade.color = new Color(col.r, col.g, col.b, Mathf.Lerp(from, to, t / seconds));
                yield return null;
            }
            fade.color = new Color(col.r, col.g, col.b, to);
        }

        public static IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds) { if (GameFlow.State != FlowState.Paused) t += Time.unscaledDeltaTime; yield return null; }
        }

        /// <summary>Phụ đề cutscene (dùng chung HUD; HUD gốc ẩn nên dựng nhãn riêng trên canvas cutscene).</summary>
        public static IEnumerator Line(string text, float seconds, Color? color = null)
        {
            EnsureCanvas();
            var t = UIKit_Label(text, color ?? new Color(0.9f, 0.86f, 0.8f));
            yield return Wait(seconds);
            if (t != null) Object.Destroy(t.gameObject);
        }

        private static TMPro.TextMeshProUGUI UIKit_Label(string text, Color c)
        {
            var style = GameManager.Instance != null ? GameManager.Instance.Style : null;
            var l = UIFactory.Text("CutLine", canvas.transform, style, true, 34, c, TMPro.TextAlignmentOptions.Bottom);
            l.rectTransform.offsetMin = new Vector2(200, 150); l.rectTransform.offsetMax = new Vector2(-200, 0);
            l.text = text;
            l.fontMaterial.EnableKeyword("UNDERLAY_ON");
            l.fontMaterial.SetColor("_UnderlayColor", new Color(0, 0, 0, 0.9f));
            l.fontMaterial.SetFloat("_UnderlaySoftness", 0.5f);
            return l;
        }

        /// <summary>Hình người tối (bóng ma ven đường, người ở cửa sổ, kẻ ngồi ghế sau).</summary>
        public static GameObject Silhouette(Vector3 feet, Vector3 faceTowards, float alpha = 1f)
        {
            var root = new GameObject("KH_Silhouette");
            root.transform.position = feet;
            var face = faceTowards - feet; face.y = 0f;
            if (face.sqrMagnitude > 0.01f) root.transform.rotation = Quaternion.LookRotation(face);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = new Color(0.012f, 0.01f, 0.012f, alpha) };
            void Part(PrimitiveType t, Vector3 p, Vector3 s)
            {
                var g = GameObject.CreatePrimitive(t);
                Object.Destroy(g.GetComponent<Collider>());
                g.transform.SetParent(root.transform, false);
                g.transform.localPosition = p; g.transform.localScale = s;
                var r = g.GetComponent<Renderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            Part(PrimitiveType.Capsule, new Vector3(0, 0.9f, 0), new Vector3(0.42f, 0.8f, 0.3f));
            Part(PrimitiveType.Sphere, new Vector3(0, 1.72f, 0), new Vector3(0.23f, 0.27f, 0.23f));
            Part(PrimitiveType.Cylinder, new Vector3(0, 0.38f, 0), new Vector3(0.62f, 0.38f, 0.62f));
            return root;
        }
    }
}
