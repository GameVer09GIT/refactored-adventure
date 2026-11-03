using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HorrorGame.Inventory
{
    /// <summary>
    /// Bảng hiển thị thông số gỡ lỗi nhanh cho Flashlight và hệ thống Input (Bật/Tắt bằng phím F9)
    /// </summary>
    public class FlashlightDebugOverlay : MonoBehaviour
    {
        public static FlashlightDebugOverlay Instance { get; private set; }

        private bool isVisible = false;
        private float lastClickFlashTime = -1f;
        private int clickCounter = 0;

        public bool IsVisible => isVisible;

        void Awake()
        {
            Instance = this;
        }

        public void NotifyLeftClick()
        {
            clickCounter++;
            lastClickFlashTime = Time.realtimeSinceStartup;
        }

        void Update()
        {
            bool f9Pressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame) f9Pressed = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKeyDown(KeyCode.F9)) f9Pressed = true;
            }
            catch {}
#endif
            if (f9Pressed)
            {
                isVisible = !isVisible;
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnGUI()
        {
            if (!isVisible) return;

            GUI.skin.box.fontSize = 12;
            GUI.skin.label.fontSize = 12;

            Rect rect = new Rect(15, 15, 340, 220);
            GUI.Box(rect, "");
            GUILayout.BeginArea(new Rect(25, 20, 320, 210));

            GUILayout.Label("<color=cyan><b>[F9] BẢNG CHẨN ĐOÁN FLASHLIGHT & INPUT</b></color>");

            HotbarInventory inv = HotbarInventory.Instance;
            PlayerArmIK armIK = PlayerArmIK.Instance;
            Camera cam = Camera.main;
            if (cam == null && armIK != null && armIK.PlayerCamera != null) cam = armIK.PlayerCamera;

            bool justClicked = (Time.realtimeSinceStartup - lastClickFlashTime) < 0.25f;
            string clickText = justClicked ? "<color=green><b>[CLICK ĐƯỢC NHẬN!]</b></color>" : "<color=gray>Đang chờ bấm...</color>";
            GUILayout.Label($"Chuột trái: {clickText} (Tổng: {clickCounter})");

            if (inv != null)
            {
                string itemName = inv.CurrentItem != null ? inv.CurrentItem.DisplayName : "Trống";
                GUILayout.Label($"Ô chọn: <b>{inv.SelectedIndex + 1}</b> ({itemName}) | Đang đổi ô: <b>{inv.IsSwitching}</b>");
            }

            GUILayout.Label($"Khóa chuột: <b>{Cursor.lockState}</b> | Hiện: <b>{Cursor.visible}</b> | TimeScale: <b>{Time.timeScale}</b>");

            if (armIK != null)
            {
                Light spot = armIK.CameraSpotLight;
                if (spot != null)
                {
                    string lightState = spot.enabled && spot.intensity > 0.01f ? "<color=yellow>BẬT</color>" : "<color=red>TẮT</color>";
                    GUILayout.Label($"Spot Light: {lightState} | Cường độ: <b>{spot.intensity:F2}</b> | Range: <b>{spot.range}m</b>");
                    if (cam != null)
                    {
                        float angle = Vector3.Angle(spot.transform.forward, cam.transform.forward);
                        GUILayout.Label($"Góc lệch rọi sáng so với Camera: <b>{angle:F1}°</b>");
                    }
                }
                else
                {
                    GUILayout.Label("<color=red>Chưa khởi tạo CameraSpotLight!</color>");
                }
            }

            GUILayout.Label("<color=silver><i>(Nhấn F9 để ẩn/hiện bảng này)</i></color>");
            GUILayout.EndArea();
        }
#endif
    }
}
