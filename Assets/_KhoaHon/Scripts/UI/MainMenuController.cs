using System.Collections;
using KhoaHon.AI;
using KhoaHon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KhoaHon.UI
{
    /// <summary>
    /// Luồng vào game kiểu game thương mại: màn cảnh báo nội dung → chỉnh độ sáng (lần đầu) → menu chính
    /// (cảnh nền điện ảnh biệt thự trong mưa, bà Quản Gia đứng sau cửa sổ, nhạc hộp nhạc) → intro → chơi.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip uiHover, uiClick, uiBack;
        [SerializeField] private Vector3[] cameraPath = { new Vector3(88f, 13.8f, -13f), new Vector3(90f, 13.6f, -15f), new Vector3(92f, 13.9f, -13.5f) };
        [SerializeField] private Vector3 cameraLookAt = new Vector3(84.5f, 18.2f, 8f);
        [SerializeField] private Vector3 housekeeperWindowPos = new Vector3(88.7f, 18.02f, 8.35f);

        private Canvas canvas; private GameObject menuRoot; private CanvasGroup fader;
        private Camera menuCam; private AudioSource music;
        private PlayerController player; private Camera playerCam;
        private GameObject oldCanvas;
        private TextMeshProUGUI titleMain;
        // Bộ đèn riêng cho cảnh nền menu (cảnh đêm thật quá tối để nhìn từ xa)
        private GameObject menuLights; private Light moonKey, windowGlow, lightning;
        private Color savedAmbient; private float nextLightning = 6f, lightningT = -1f;

        private void Awake()
        {
            UIKit.Hover = uiHover; UIKit.Click = uiClick; UIKit.Back = uiBack;
        }

        private void Start()
        {
            KhoaHonSettings.Load();
            player = FindFirstObjectByType<PlayerController>();
            playerCam = player != null && player.playerCamera != null ? player.playerCamera.GetComponent<Camera>() : null;
            oldCanvas = GameObject.Find("Canvas");
            canvas = UIFactory.Canvas("KH_MenuCanvas", 90);
            canvas.transform.SetParent(transform, false);
            fader = UIFactory.Panel("Fader", canvas.transform, Color.black, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<CanvasGroup>();
            fader.blocksRaycasts = false; fader.alpha = 1f;
            KhoaHonSettings.ApplyAll();

            var start = GameFlow.PendingStart;
            GameFlow.PendingStart = StartMode.None;
            if (start == StartMode.Continue) StartCoroutine(EnterPlaying(0.8f));
            else if (start == StartMode.NewGame) StartCoroutine(Intro());
            else StartCoroutine(BootThenMenu());
        }

        // ------------------------------------------------------------------ Boot
        private IEnumerator BootThenMenu()
        {
            SetMenuMode(true);
            if (!GameFlow.BootShown)
            {
                GameFlow.BootShown = true;
                yield return Warning();
                if (!KhoaHonSettings.BrightnessCalibrated) yield return BrightnessCalibration();
            }
            GameFlow.Set(FlowState.Menu);
            BuildMenu();
            yield return Fade(0f, 1.6f);
        }

        private IEnumerator Warning()
        {
            GameFlow.Set(FlowState.Boot);
            var root = UIFactory.Stretch("Warning", canvas.transform);
            root.SetSiblingIndex(0);
            UIFactory.Panel("Bg", root, Color.black, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var t1 = UIKit.Label(root, T.Get("ui.warning_title"), 54, UIKit.Blood, TextAlignmentOptions.Center, true, true);
            t1.rectTransform.anchorMin = new Vector2(0.15f, 0.55f); t1.rectTransform.anchorMax = new Vector2(0.85f, 0.7f);
            t1.characterSpacing = 10f;
            var t2 = UIKit.Label(root, T.Get("ui.warning_body"), 28, UIKit.Ink, TextAlignmentOptions.Top);
            t2.rectTransform.anchorMin = new Vector2(0.2f, 0.25f); t2.rectTransform.anchorMax = new Vector2(0.8f, 0.53f);
            t2.lineSpacing = 12;
            yield return Fade(0f, 1.2f);
            float t = 0; while (t < 5.5f && !AnyKey()) { t += Time.unscaledDeltaTime; yield return null; }
            yield return Fade(1f, 1f);
            Destroy(root.gameObject);
        }

        private IEnumerator BrightnessCalibration()
        {
            var root = UIFactory.Stretch("Brightness", canvas.transform);
            root.SetSiblingIndex(0);
            UIFactory.Panel("Bg", root, new Color(0.01f, 0.01f, 0.012f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var title = UIKit.Label(root, T.Get("ui.calib_title"), 46, UIKit.Ink, TextAlignmentOptions.Center, true, true);
            title.rectTransform.anchorMin = new Vector2(0.1f, 0.78f); title.rectTransform.anchorMax = new Vector2(0.9f, 0.9f);
            // 3 biểu tượng: rõ / mờ / gần như không thấy
            float[] lv = { 0.32f, 0.11f, 0.035f };
            var icons = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var img = UIFactory.Panel("Mark" + i, root, new Color(lv[i], lv[i], lv[i]), new Vector2(0.3f + i * 0.2f, 0.5f), new Vector2(0.3f + i * 0.2f, 0.5f), new Vector2(-70, -70), new Vector2(70, 70));
                img.transform.localRotation = Quaternion.Euler(0, 0, 45);
                icons[i] = img;
            }
            var desc = UIKit.Label(root, T.Get("ui.calib_body"), 26, UIKit.Dim, TextAlignmentOptions.Center);
            desc.rectTransform.anchorMin = new Vector2(0.15f, 0.27f); desc.rectTransform.anchorMax = new Vector2(0.85f, 0.37f);
            var sl = UIKit.Column(root, "Slider", new Vector2(0.3f, 0.17f), new Vector2(0.7f, 0.25f), Vector2.zero, Vector2.zero, 0);
            UIKit.SliderRow(sl, T.Get("ui.brightness"), -1.5f, 1.5f, KhoaHonSettings.Brightness, x => (x >= 0 ? "+" : "") + x.ToString("0.0"), x => KhoaHonSettings.Brightness = x);
            bool done = false;
            var btnRow = UIKit.Column(root, "Btn", new Vector2(0.44f, 0.06f), new Vector2(0.56f, 0.13f), Vector2.zero, Vector2.zero, 0);
            UIKit.SmallButton(btnRow, T.Get("ui.confirm"), () => done = true);
            yield return Fade(0f, 0.8f);
            while (!done)
            {
                float g = Mathf.Pow(2f, KhoaHonSettings.Brightness);
                for (int i = 0; i < 3; i++) { float v = Mathf.Clamp01(lv[i] * g); icons[i].color = new Color(v, v, v); }
                yield return null;
            }
            KhoaHonSettings.BrightnessCalibrated = true; KhoaHonSettings.Save(); KhoaHonSettings.ApplyLive();
            yield return Fade(1f, 0.8f);
            Destroy(root.gameObject);
        }

        // ------------------------------------------------------------------ Menu
        private void SetMenuMode(bool on)
        {
            if (on)
            {
                GameFlow.Set(FlowState.Menu);
                if (player != null) player.enabled = false;
                if (playerCam != null) playerCam.enabled = false;
                if (oldCanvas != null) oldCanvas.SetActive(false);
                var hud = KhoaHonHUD.Instance; if (hud != null) hud.Root.gameObject.SetActive(false);
                Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
                if (menuCam == null)
                {
                    menuCam = new GameObject("KH_MenuCamera").AddComponent<Camera>();
                    menuCam.fieldOfView = 40f; menuCam.nearClipPlane = 0.1f; menuCam.farClipPlane = 400f;
                    var data = menuCam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                    data.renderPostProcessing = true;
                    menuCam.gameObject.AddComponent<AudioListener>();
                    if (playerCam != null) { var al = playerCam.GetComponent<AudioListener>(); if (al != null) al.enabled = false; }
                }
                if (music == null && menuMusic != null)
                {
                    music = gameObject.AddComponent<AudioSource>(); music.clip = menuMusic; music.loop = true; music.spatialBlend = 0f; music.ignoreListenerPause = true;
                    music.volume = 0.7f * AudioMix.Music; music.Play();
                }
                if (HousekeeperAI.Instance != null) HousekeeperAI.Instance.PoseAt(housekeeperWindowPos, 180f);
                BuildMenuLights();
            }
            else
            {
                if (menuCam != null) Destroy(menuCam.gameObject);
                if (menuLights != null) { Destroy(menuLights); RenderSettings.ambientLight = savedAmbient; }
                if (playerCam != null) { playerCam.enabled = true; var al = playerCam.GetComponent<AudioListener>(); if (al != null) al.enabled = true; }
                if (oldCanvas != null) oldCanvas.SetActive(true);
                var hud = KhoaHonHUD.Instance; if (hud != null) hud.Root.gameObject.SetActive(true);
                if (menuRoot != null) Destroy(menuRoot);
            }
        }

        private void BuildMenuLights()
        {
            if (menuLights != null) return;
            savedAmbient = RenderSettings.ambientLight;
            RenderSettings.ambientLight = new Color(0.05f, 0.06f, 0.09f);
            menuLights = new GameObject("KH_MenuLights");
            // Trăng lạnh chiếu xiên lên mặt tiền
            moonKey = MakeLight("MoonKey", LightType.Spot, new Vector3(70f, 32f, -22f), new Color(0.45f, 0.58f, 0.8f), 2500f, 90f);
            moonKey.spotAngle = 42f; moonKey.innerSpotAngle = 20f; moonKey.shadows = LightShadows.Soft;
            moonKey.transform.LookAt(new Vector3(87f, 17f, 7f));
            // Đèn đỏ đục phía sau bà Quản Gia → thành bóng đen trong khung cửa sổ
            windowGlow = MakeLight("WindowGlow", LightType.Point, housekeeperWindowPos + new Vector3(0f, 1.4f, 1.6f), new Color(1f, 0.25f, 0.12f), 30f, 8f);
            windowGlow.shadows = LightShadows.Soft;
            // Sét
            lightning = MakeLight("Lightning", LightType.Directional, Vector3.zero, new Color(0.75f, 0.82f, 1f), 0f, 0f);
            lightning.transform.rotation = Quaternion.Euler(35f, 20f, 0f);
        }

        private Light MakeLight(string n, LightType type, Vector3 pos, Color c, float intensity, float range)
        {
            var go = new GameObject(n); go.transform.SetParent(menuLights.transform, false); go.transform.position = pos;
            var l = go.AddComponent<Light>(); l.type = type; l.color = c; l.intensity = intensity; if (range > 0) l.range = range;
            return l;
        }

        private void UpdateMenuLights()
        {
            if (menuLights == null) return;
            float now = Time.unscaledTime;
            // Đèn cửa sổ chập chờn
            float f = Mathf.PerlinNoise(now * 3.1f, 0.2f);
            windowGlow.intensity = f < 0.22f ? 4f : 24f + f * 16f;
            // Sét: 2 nháy liên tiếp, 8–18 giây một lần
            if (lightningT < 0f && now > nextLightning) { lightningT = 0f; nextLightning = now + Random.Range(8f, 18f); }
            if (lightningT >= 0f)
            {
                lightningT += Time.unscaledDeltaTime;
                float a = lightningT < 0.08f ? 1f : (lightningT < 0.16f ? 0.1f : (lightningT < 0.3f ? 0.8f : Mathf.Max(0f, 0.8f - (lightningT - 0.3f) * 2.5f)));
                lightning.intensity = a * 2.2f;
                if (lightningT > 0.7f) { lightningT = -1f; lightning.intensity = 0f; }
            }
        }

        private void Update()
        {
            UpdateMenuLights();
            if (menuCam != null && cameraPath.Length >= 2)
            {
                float u = Mathf.PingPong(Time.unscaledTime * 0.018f, 1f);
                float s = u * (cameraPath.Length - 1); int i = Mathf.Min(cameraPath.Length - 2, (int)s);
                var p = Vector3.Lerp(cameraPath[i], cameraPath[i + 1], Mathf.SmoothStep(0, 1, s - i));
                p += new Vector3(Mathf.Sin(Time.unscaledTime * 0.3f) * 0.15f, Mathf.Sin(Time.unscaledTime * 0.47f) * 0.08f, 0);
                menuCam.transform.position = p;
                menuCam.transform.rotation = Quaternion.LookRotation(cameraLookAt - p);
            }
            if (music != null) music.volume = Mathf.MoveTowards(music.volume, (GameFlow.State == FlowState.Menu || GameFlow.State == FlowState.Boot ? 0.7f : 0f) * AudioMix.Music, Time.unscaledDeltaTime * 0.6f);
            if (titleMain != null)
            {
                // Tiêu đề chập chờn như đèn hỏng
                float f = Mathf.PerlinNoise(Time.unscaledTime * 6f, 0.7f);
                titleMain.alpha = f < 0.18f ? 0.35f : 1f;
            }
        }

        private void BuildMenu()
        {
            if (menuRoot != null) Destroy(menuRoot);
            var root = UIFactory.Stretch("Menu", canvas.transform);
            root.SetSiblingIndex(0);
            menuRoot = root.gameObject;
            // Gradient tối bên trái để chữ dễ đọc
            var grad = UIFactory.Panel("Shade", root, new Color(0, 0, 0, 0.78f), new Vector2(0, 0), new Vector2(0.45f, 1), Vector2.zero, Vector2.zero);
            var grad2 = UIFactory.Panel("Shade2", root, new Color(0, 0, 0, 0.35f), new Vector2(0.45f, 0), new Vector2(0.6f, 1), Vector2.zero, Vector2.zero);

            var top = UIKit.Label(root, T.Get("ui.title_top"), 30, UIKit.Dim, TextAlignmentOptions.BottomLeft, true);
            top.rectTransform.anchorMin = new Vector2(0, 1); top.rectTransform.anchorMax = new Vector2(1, 1);
            top.rectTransform.offsetMin = new Vector2(120, -220); top.rectTransform.offsetMax = new Vector2(0, -170);
            top.characterSpacing = 18f;
            titleMain = UIKit.Label(root, T.Get("ui.title_main"), 128, UIKit.Blood, TextAlignmentOptions.TopLeft, true, true);
            titleMain.rectTransform.anchorMin = new Vector2(0, 1); titleMain.rectTransform.anchorMax = new Vector2(1, 1);
            titleMain.rectTransform.offsetMin = new Vector2(112, -380); titleMain.rectTransform.offsetMax = new Vector2(0, -215);
            titleMain.fontMaterial.EnableKeyword("UNDERLAY_ON");
            titleMain.fontMaterial.SetColor("_UnderlayColor", new Color(0.05f, 0, 0, 1f));
            titleMain.fontMaterial.SetFloat("_UnderlayOffsetY", -1f); titleMain.fontMaterial.SetFloat("_UnderlaySoftness", 0.6f);

            var col = UIKit.Column(root, "Buttons", new Vector2(0, 0), new Vector2(0.4f, 1), new Vector2(120, 170), new Vector2(0, -430), 4);
            var cont = UIKit.MenuButton(col, T.Get("ui.continue"), () => StartCoroutine(Continue()));
            cont.interactable = SaveSystem.Exists;
            UIKit.MenuButton(col, T.Get("ui.new_game"), () =>
            {
                if (SaveSystem.Exists) Confirm(T.Get("ui.confirm_new"), ShowCharacterSelect);
                else ShowCharacterSelect();
            });
            UIKit.MenuButton(col, T.Get("ui.settings"), () => { menuRoot.SetActive(false); SettingsMenuUI.Instance.Open(BuildMenu); });
            UIKit.MenuButton(col, T.Get("ui.credits"), ShowCredits);
            UIKit.MenuButton(col, T.Get("ui.quit"), () => Confirm(T.Get("ui.confirm_quit"), Quit));

            var ver = UIKit.Label(root, T.Get("ui.version"), 18, new Color(0.45f, 0.42f, 0.4f), TextAlignmentOptions.BottomRight);
            ver.rectTransform.offsetMin = new Vector2(0, 30); ver.rectTransform.offsetMax = new Vector2(-40, 0);
            var hint = UIKit.Label(root, T.Get("ui.menu_hint"), 18, new Color(0.45f, 0.42f, 0.4f), TextAlignmentOptions.BottomLeft);
            hint.rectTransform.offsetMin = new Vector2(120, 30);
        }

        private void ShowCredits()
        {
            menuRoot.SetActive(false);
            var root = UIFactory.Panel("Credits", canvas.transform, new Color(0, 0, 0, 0.9f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var t = UIKit.Label(root.transform, T.Get("ui.credits_body"), 28, UIKit.Ink, TextAlignmentOptions.Center, true);
            t.rectTransform.anchorMin = new Vector2(0.2f, 0.2f); t.rectTransform.anchorMax = new Vector2(0.8f, 0.85f);
            var b = UIKit.Column(root.transform, "Btn", new Vector2(0.44f, 0.08f), new Vector2(0.56f, 0.15f), Vector2.zero, Vector2.zero, 0);
            UIKit.SmallButton(b, T.Get("ui.back"), () => { Destroy(root.gameObject); menuRoot.SetActive(true); });
        }

        // ------------------------------------------------------------------ Chọn nhân vật
        private void ShowCharacterSelect()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Characters == null || gm.Characters.Length == 0) { StartCoroutine(NewGame()); return; }
            menuRoot.SetActive(false);
            var root = UIFactory.Panel("CharacterSelect", canvas.transform, new Color(0, 0, 0, 0.88f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root.transform.SetSiblingIndex(1);
            var title = UIKit.Label(root.transform, T.Get("char.choose"), 52, UIKit.Ink, TextAlignmentOptions.Top, true, true);
            title.rectTransform.offsetMax = new Vector2(0, -70); title.characterSpacing = 10f;
            int count = Mathf.Min(2, gm.Characters.Length);
            for (int i = 0; i < count; i++)
            {
                var prof = gm.Characters[i];
                if (prof == null) continue;
                float x0 = i == 0 ? 0.08f : 0.53f;
                CharacterCard(root.transform, prof, new Vector2(x0, 0.16f), new Vector2(x0 + 0.39f, 0.84f), () =>
                {
                    GameFlow.PendingCharacter = prof.character;
                    StartCoroutine(NewGame());
                });
            }
            var back = UIKit.Column(root.transform, "Back", new Vector2(0.44f, 0.04f), new Vector2(0.56f, 0.11f), Vector2.zero, Vector2.zero, 0);
            UIKit.SmallButton(back, T.Get("ui.back"), () => { Destroy(root.gameObject); menuRoot.SetActive(true); });
        }

        private void CharacterCard(Transform parent, CharacterProfile prof, Vector2 aMin, Vector2 aMax, System.Action pick)
        {
            var card = UIFactory.Panel("Card_" + prof.character, parent, new Color(0.04f, 0.035f, 0.035f, 0.92f), aMin, aMax, Vector2.zero, Vector2.zero);
            UIFactory.Panel("Accent", card.transform, prof.accent, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -5), Vector2.zero);
            var btn = card.gameObject.AddComponent<Button>(); btn.transition = Selectable.Transition.ColorTint;
            var cols = btn.colors; cols.highlightedColor = new Color(1.25f, 1.15f, 1.1f, 1f); btn.colors = cols;
            btn.onClick.AddListener(() => { Horror.AmbientScares.Play2D(UIKit.Click, 0.7f); pick(); });
            // "Chân dung": chữ cái đầu cỡ lớn trên nền màu nhấn (chưa có ảnh vẽ)
            var portrait = UIFactory.Panel("Portrait", card.transform, new Color(prof.accent.r * 0.25f, prof.accent.g * 0.25f, prof.accent.b * 0.25f, 1f),
                new Vector2(0.06f, 0.56f), new Vector2(0.38f, 0.94f), Vector2.zero, Vector2.zero);
            var initial = UIKit.Label(portrait.transform, prof.DisplayName.Substring(0, 1), 150, prof.accent, TextAlignmentOptions.Center, true, true);
            var name = UIKit.Label(card.transform, prof.DisplayName, 46, UIKit.Ink, TextAlignmentOptions.TopLeft, true, true);
            name.rectTransform.anchorMin = new Vector2(0.42f, 0.8f); name.rectTransform.anchorMax = new Vector2(0.96f, 0.94f);
            var role = UIKit.Label(card.transform, prof.Role, 20, prof.accent, TextAlignmentOptions.TopLeft, false, true);
            role.rectTransform.anchorMin = new Vector2(0.42f, 0.66f); role.rectTransform.anchorMax = new Vector2(0.96f, 0.79f);
            var motive = UIKit.Label(card.transform, prof.Motive, 22, UIKit.Ink, TextAlignmentOptions.TopLeft, true);
            motive.rectTransform.anchorMin = new Vector2(0.06f, 0.26f); motive.rectTransform.anchorMax = new Vector2(0.94f, 0.53f);
            motive.lineSpacing = 6f;
            var tools = UIKit.Label(card.transform, prof.Tools, 18, UIKit.Dim, TextAlignmentOptions.TopLeft);
            tools.rectTransform.anchorMin = new Vector2(0.06f, 0.1f); tools.rectTransform.anchorMax = new Vector2(0.94f, 0.25f);
            // Biểu tượng công cụ (icon ItemData)
            if (prof.hotbar != null)
                for (int k = 0; k < prof.hotbar.Length; k++)
                {
                    var it = prof.hotbar[k];
                    if (it == null || it.Icon == null) continue;
                    var ic = UIFactory.Rect("Icon" + k, card.transform, new Vector2(0.42f + k * 0.13f, 0.56f), new Vector2(0.53f + k * 0.13f, 0.65f), Vector2.zero, Vector2.zero);
                    var img = ic.gameObject.AddComponent<Image>(); img.sprite = it.Icon; img.preserveAspect = true;
                }
            var start = UIKit.Label(card.transform, T.Get("char.start") + "  >", 26, new Color(0.95f, 0.3f, 0.25f), TextAlignmentOptions.BottomRight, false, true);
            start.rectTransform.offsetMin = new Vector2(20, 18); start.rectTransform.offsetMax = new Vector2(-24, 0);
        }

        /// <summary>Hộp xác nhận dùng chung.</summary>
        public static void Confirm(string message, System.Action yes)
        {
            var dim = UIFactory.Panel("Confirm", UIKit.TopLayer, new Color(0, 0, 0, 0.75f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            dim.transform.SetAsLastSibling();
            var box = UIFactory.Panel("Box", dim.transform, UIKit.Panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420, -150), new Vector2(420, 150));
            UIFactory.Panel("Edge", box.transform, UIKit.Blood, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -3), Vector2.zero);
            var msg = UIKit.Label(box.transform, message, 30, UIKit.Ink, TextAlignmentOptions.Center);
            msg.rectTransform.offsetMin = new Vector2(40, 100); msg.rectTransform.offsetMax = new Vector2(-40, -30);
            var row = UIFactory.Rect("Row", box.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 30), new Vector2(-40, 90));
            var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 30; hl.childAlignment = TextAnchor.MiddleCenter; hl.childForceExpandWidth = false;
            UIKit.SmallButton(row, T.Get("ui.no"), () => Destroy(dim.gameObject));
            UIKit.SmallButton(row, T.Get("ui.yes"), () => { Destroy(dim.gameObject); yes?.Invoke(); });
        }

        // ------------------------------------------------------------------ Bắt đầu
        private IEnumerator NewGame()
        {
            yield return Fade(1f, 1.2f);
            SaveSystem.Delete();
            SaveSystem.PendingLoad = null;
            PlayerPrefs.SetInt("kh_deaths_run", 0);
            GameFlow.PendingStart = StartMode.NewGame;
            ModalUI.Reset();
            SceneManager.LoadScene(SceneManager.GetActiveScene().path);
        }

        private IEnumerator Continue()
        {
            yield return Fade(1f, 1.0f);
            SaveSystem.PendingLoad = SaveSystem.Read();
            GameFlow.PendingStart = StartMode.Continue;
            ModalUI.Reset();
            SceneManager.LoadScene(SceneManager.GetActiveScene().path);
        }

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>Intro: chữ trắng trên nền đen (PROMPT 8.1), rồi mở màn tại cổng.</summary>
        private IEnumerator Intro()
        {
            GameFlow.Set(FlowState.Intro);
            if (player != null) player.enabled = false;
            if (oldCanvas != null) oldCanvas.SetActive(false);
            fader.alpha = 1f;
            var text = UIKit.Label(canvas.transform, "", 40, UIKit.Ink, TextAlignmentOptions.Center, true);
            text.rectTransform.anchorMin = new Vector2(0.15f, 0.3f); text.rectTransform.anchorMax = new Vector2(0.85f, 0.7f);
            text.lineSpacing = 18;
            // Chữ mở đầu theo nhân vật đã chọn (thiếu hồ sơ thì dùng intro chung)
            var prof = GameManager.Instance != null ? GameManager.Instance.Profile : null;
            var keys = prof != null && prof.introKeys != null && prof.introKeys.Count > 0
                ? prof.introKeys.ToArray() : new[] { "intro.1", "intro.2", "intro.3" };
            foreach (var key in keys)
            {
                string s = T.Get(key);
                for (int i = 0; i <= s.Length; i++) { text.text = s.Substring(0, i); if (AnyKey()) { text.text = s; break; } yield return new WaitForSecondsRealtime(0.035f); }
                float w = 0; while (w < 2.4f) { w += Time.unscaledDeltaTime; if (w > 0.3f && AnyKey()) break; yield return null; }
                float a = 1; while (a > 0) { a -= Time.unscaledDeltaTime * 2f; text.alpha = a; yield return null; }
                text.alpha = 1; text.text = "";
            }
            Destroy(text.gameObject);
            // Giai đoạn 0: vòng lặp lái xe trong sương (chỉ chạy một lần mỗi lượt chơi)
            var gate = Sequences.GateLoopSequence.Instance;
            if (gate != null && gate.ShouldRun)
            {
                bool done = false;
                Sequences.CutsceneKit.SetFade(1f);
                fader.alpha = 0f;
                gate.Run(() => done = true);
                while (!done) yield return null;
                fader.alpha = 1f;
                Sequences.CutsceneKit.SetFade(0f);
            }
            yield return EnterPlaying(2.0f);
        }

        private IEnumerator EnterPlaying(float fadeTime)
        {
            SetMenuMode(false);
            if (player != null) { player.enabled = true; player.LockCursor(); }
            KhoaHonSettings.ApplyLive();
            GameFlow.Set(FlowState.Playing);
            yield return Fade(0f, fadeTime);
        }

        private IEnumerator Fade(float to, float time)
        {
            float from = fader.alpha, t = 0;
            fader.transform.SetAsLastSibling();
            while (t < time) { t += Time.unscaledDeltaTime; fader.alpha = Mathf.Lerp(from, to, t / time); yield return null; }
            fader.alpha = to;
        }

        private static bool AnyKey() =>
            (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
    }
}
