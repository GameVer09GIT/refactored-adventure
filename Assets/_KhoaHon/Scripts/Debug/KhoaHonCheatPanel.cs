using System;
using KhoaHon.Core;
using KhoaHon.Interaction;
using KhoaHon.Items;
using KhoaHon.Notebook;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KhoaHon.DebugTools
{
    /// <summary>Cờ debug dùng chung (AI đọc để tạm dừng).</summary>
    public static class CheatSettings
    {
        public static bool AIEnabled = true;
    }

    /// <summary>
    /// Bảng cheat F10 (chỉ Editor / Development build): nhảy checkpoint, cấp đồ, set cờ, sanity, AI, lưu/nạp.
    /// Dùng IMGUI và nhãn không dấu để không phụ thuộc font.
    /// </summary>
    public class KhoaHonCheatPanel : MonoBehaviour
    {
        [SerializeField] private RelicData[] relics = new RelicData[5];
        [SerializeField] private string upstairsKeyId = "key_upstairs";

        private bool visible;
        private Vector2 scroll;
        private Rect window = new Rect(20, 20, 420, 680);
        private bool showFlags;

        private void Update()
        {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
            return;
#endif
            if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame)
            {
                visible = !visible;
                if (visible) ModalUI.Open(this); else ModalUI.Close(this);
            }
        }

        private void OnGUI()
        {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
            return;
#endif
            if (!visible) return;
            window = GUILayout.Window(91021, window, Draw, "KhoaHon Cheat (F10)");
        }

        private void Draw(int id)
        {
            var gm = GameManager.Instance;
            scroll = GUILayout.BeginScrollView(scroll);

            GUILayout.Label($"Stage: {gm?.Stage}   CP: {CheckpointManager.Instance?.CurrentId}");
            GUILayout.Label($"Battery: {BatteryPool.Instance?.Current:F0}%  Film: {FilmStock.Instance?.Current}  Sanity: {gm?.Sanity:F0}");

            GUILayout.Space(6);
            GUILayout.Label("Teleport:");
            if (CheckpointManager.Instance != null)
            {
                foreach (var cp in CheckpointManager.Instance.AllIds)
                    if (GUILayout.Button(cp)) CheckpointManager.Instance.TeleportTo(cp);
            }

            GUILayout.Space(6);
            GUILayout.Label("Give:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Upstairs key")) { KeyRing.Instance?.Add(upstairsKeyId); gm?.SetFlag(StoryFlag.HasUpstairsKey); }
            if (GUILayout.Button("Crowbar")) gm?.SetFlag(StoryFlag.HasCrowbar);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Battery 100")) BatteryPool.Instance?.SetValue(100f);
            if (GUILayout.Button("Battery 10")) BatteryPool.Instance?.SetValue(10f);
            if (GUILayout.Button("Film +6")) FilmStock.Instance?.Add(6);
            GUILayout.EndHorizontal();
            if (GUILayout.Button("All 5 relics + talismans"))
                foreach (var r in relics) if (r != null) RelicInventory.Instance?.Add(r);

            GUILayout.Space(6);
            if (GUILayout.Button("Unlock + open ALL doors"))
                foreach (var d in FindObjectsByType<DoorInteractable>(FindObjectsSortMode.None)) d.ForceOpen();
            if (GUILayout.Button("Open cellar (flag)")) gm?.SetFlag(StoryFlag.CellarOpened);

            GUILayout.Space(6);
            if (gm != null)
            {
                GUILayout.Label($"Sanity {gm.Sanity:F0}");
                gm.Sanity = GUILayout.HorizontalSlider(gm.Sanity, 5f, 100f);
            }
            CheatSettings.AIEnabled = GUILayout.Toggle(CheatSettings.AIEnabled, " AI enabled");

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save")) gm?.SaveNow();
            if (GUILayout.Button("Load")) { Close(); gm?.LoadFromDisk(); }
            if (GUILayout.Button("New game")) { Close(); gm?.NewGame(); }
            GUILayout.EndHorizontal();
            GUILayout.Label(SaveSystem.Exists ? "Save: " + SaveSystem.FilePath : "Save: (none)");

            GUILayout.Space(6);
            showFlags = GUILayout.Toggle(showFlags, " Flags");
            if (showFlags && gm != null)
            {
                foreach (StoryFlag f in Enum.GetValues(typeof(StoryFlag)))
                {
                    if (f == StoryFlag.None) continue;
                    bool has = gm.Has(f);
                    bool now = GUILayout.Toggle(has, " " + f);
                    if (now != has) { if (now) gm.SetFlag(f); else gm.Flags.Clear(f); }
                }
            }

            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        private void Close()
        {
            visible = false;
            ModalUI.Close(this);
        }
    }
}
