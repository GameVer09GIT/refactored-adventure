using KhoaHon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace KhoaHon.UI
{
    /// <summary>Menu tạm dừng (Esc): Tiếp tục · Cài đặt · Tải lại điểm lưu · Về menu chính · Thoát.</summary>
    public class PauseMenu : MonoBehaviour
    {
        private GameObject root;
        public bool IsOpen => root != null;

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (SettingsMenuUI.Instance != null && SettingsMenuUI.Instance.IsOpen) { SettingsMenuUI.Instance.Close(); return; }
            if (IsOpen) { Resume(); return; }
            if (GameFlow.State != FlowState.Playing || ModalUI.IsAnyOpen || PlayerState.IsHiding || Time.frameCount == ModalUI.LastClosedFrame) return;
            Open();
        }

        private void Open()
        {
            GameFlow.Set(FlowState.Paused);
            Time.timeScale = 0f;
            AudioListener.pause = true;
            ModalUI.Open(this);
            // Canvas trên cùng để che cả thanh đồ (hotbar dùng canvas riêng)
            var bg = UIFactory.Panel("KH_Pause", UIKit.TopLayer, new Color(0, 0, 0, 0.8f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root = bg.gameObject;
            UIFactory.Panel("Side", bg.transform, new Color(0, 0, 0, 0.6f), new Vector2(0, 0), new Vector2(0.38f, 1), Vector2.zero, Vector2.zero);
            var title = UIKit.Label(bg.transform, T.Get("ui.paused"), 72, UIKit.Blood, TextAlignmentOptions.TopLeft, true, true);
            title.rectTransform.anchorMin = new Vector2(0, 1); title.rectTransform.anchorMax = new Vector2(1, 1);
            title.rectTransform.offsetMin = new Vector2(120, -260); title.rectTransform.offsetMax = new Vector2(0, -150);
            title.characterSpacing = 10f;
            var col = UIKit.Column(bg.transform, "Buttons", new Vector2(0, 0), new Vector2(0.38f, 1), new Vector2(120, 200), new Vector2(0, -300), 4);
            UIKit.MenuButton(col, T.Get("ui.resume"), Resume, 38, 60);
            UIKit.MenuButton(col, T.Get("ui.settings"), () => { root.SetActive(false); SettingsMenuUI.Instance.Open(() => { if (root != null) root.SetActive(true); }); }, 38, 60);
            var reload = UIKit.MenuButton(col, T.Get("ui.reload_checkpoint"), () => MainMenuController.Confirm(T.Get("ui.confirm_reload"), () => { Resume(); GameManager.Instance.LoadFromDisk(); }), 38, 60);
            reload.interactable = SaveSystem.Exists;
            UIKit.MenuButton(col, T.Get("ui.main_menu"), () => MainMenuController.Confirm(T.Get("ui.confirm_menu"), ToMenu), 38, 60);
            UIKit.MenuButton(col, T.Get("ui.quit"), () => MainMenuController.Confirm(T.Get("ui.confirm_quit"), MainMenuController.Quit), 38, 60);
            var tip = UIKit.Label(bg.transform, T.Get("ui.pause_tip"), 22, UIKit.Dim, TextAlignmentOptions.BottomLeft);
            tip.rectTransform.offsetMin = new Vector2(120, 60); tip.rectTransform.offsetMax = new Vector2(-900, 0);
        }

        public void Resume()
        {
            if (root != null) Destroy(root);
            root = null;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            ModalUI.Close(this);
            GameFlow.Set(FlowState.Playing);
        }

        private void ToMenu()
        {
            Time.timeScale = 1f; AudioListener.pause = false;
            ModalUI.Reset();
            GameFlow.PendingStart = StartMode.None;
            SceneManager.LoadScene(SceneManager.GetActiveScene().path);
        }
    }
}
