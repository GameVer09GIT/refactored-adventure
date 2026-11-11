#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HorrorGame.Inventory
{
    /// <summary>
    /// Bảng công cụ tinh chỉnh IK và vị trí vật phẩm trực tiếp trong Play Mode (Phím F12 để bật/tắt).
    /// Hỗ trợ tinh chỉnh: ikTargetLocalPosition, ikTargetLocalRotation, elbowHintLocal, gripOffsetPosition, gripOffsetRotation, curl từng ngón, và vị trí tay thả lỏng của ô trống.
    /// Giữ Shift để đổi bước lớn, Ctrl để đổi bước nhỏ. Có phím in giá trị ra Console và clipboard.
    /// Có nút "Reset về animation gốc" (IK weight = 0) để so sánh tức thì.
    /// </summary>
    public class PlayerArmIKTuningUI : MonoBehaviour
    {
        public static PlayerArmIKTuningUI Instance { get; private set; }
        public static bool IsVisible => Instance != null && Instance.isVisible;

        private bool isVisible = false;
        private Rect windowRect = new Rect(20, 40, 520, 740);
        private Vector2 scrollPos;

        private CursorLockMode previousLockMode;
        private bool previousCursorVisible;

        void Awake()
        {
            Instance = this;
        }

        void Update()
        {
            bool f12Pressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame) f12Pressed = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKeyDown(KeyCode.F12)) f12Pressed = true;
            }
            catch {}
#endif
            if (f12Pressed)
            {
                isVisible = !isVisible;
                if (isVisible)
                {
                    previousLockMode = Cursor.lockState;
                    previousCursorVisible = Cursor.visible;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = previousLockMode;
                    Cursor.visible = previousCursorVisible;
                }
            }
        }

        void OnGUI()
        {
            if (!isVisible) return;

            GUI.skin.window.fontSize = 13;
            GUI.skin.label.fontSize = 12;
            GUI.skin.button.fontSize = 12;

            windowRect = GUI.Window(98765, windowRect, DrawWindow, "<b>[F12] BẢNG TINH CHỈNH IK TAY & VẬT PHẨM (Play Mode)</b>");
        }

        private void DrawWindow(int windowID)
        {
            PlayerArmIK armIK = PlayerArmIK.Instance;
            if (armIK == null)
            {
                GUILayout.Label("<color=yellow>Chưa khởi tạo PlayerArmIK!</color>");
                if (GUILayout.Button("Đóng bảng (F12)")) isVisible = false;
                GUI.DragWindow();
                return;
            }

            // Tính bước thay đổi theo phím Shift / Ctrl
            float stepPos = 0.01f;
            float stepRot = 5.0f;
            float stepCurl = 0.05f;

            bool shiftPressed = false;
            bool ctrlPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                shiftPressed = Keyboard.current.shiftKey.isPressed;
                ctrlPressed = Keyboard.current.ctrlKey.isPressed;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            }
            catch {}
#endif

            if (shiftPressed)
            {
                stepPos = 0.05f;
                stepRot = 15.0f;
                stepCurl = 0.15f;
            }
            else if (ctrlPressed)
            {
                stepPos = 0.002f;
                stepRot = 1.0f;
                stepCurl = 0.01f;
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos);

            // Nút so sánh animation gốc
            GUILayout.BeginHorizontal();
            if (armIK.ResetToOriginalAnimation)
            {
                GUI.backgroundColor = new Color(0.9f, 0.4f, 0.2f);
                if (GUILayout.Button("<b>ĐANG Ở ANIMATION GỐC (IK = 0) - NHẤN ĐỂ BẬT LẠI IK</b>", GUILayout.Height(32)))
                {
                    armIK.ResetToOriginalAnimation = false;
                }
            }
            else
            {
                GUI.backgroundColor = new Color(0.3f, 0.7f, 1.0f);
                if (GUILayout.Button("<b>RESET VỀ ANIMATION GỐC (IK Weight = 0) ĐỂ SO SÁNH</b>", GUILayout.Height(32)))
                {
                    armIK.ResetToOriginalAnimation = true;
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
            GUILayout.Space(6);

            GUILayout.Label($"<i>Phím tắt bước nhảy: Giữ [Shift] = Bước lớn | Giữ [Ctrl] = Bước tinh vi</i>");
            GUILayout.Space(6);

            ItemData data = armIK.CurrentItemData;

            if (data != null)
            {
                // ĐANG CẦM VẬT PHẨM
                GUILayout.Label($"<b>Vật phẩm đang cầm:</b> <color=cyan>{data.DisplayName}</color> ({data.GripType})");
                GUILayout.Space(4);

                Transform targetTrans = armIK.HandTargetR;
                Transform hintTrans = armIK.RightArm.elbowHint;
                Transform socketTrans = armIK.HoldSocketR;

                // 1. IK Target Local Position
                GUILayout.Label($"<b>1. Vị trí mục tiêu bàn tay phải (ikTargetLocalPosition):</b>");
                Vector3 pos = data.IkTargetLocalPosition;
                pos = DrawVector3Control("Target Pos", pos, stepPos, "m");
                data.IkTargetLocalPosition = pos;
                if (targetTrans != null) targetTrans.localPosition = pos;
                GUILayout.Space(6);

                // 2. IK Target Local Rotation
                GUILayout.Label($"<b>2. Góc xoay mục tiêu bàn tay phải (ikTargetLocalRotation):</b>");
                Vector3 rot = data.IkTargetLocalRotation;
                rot = DrawVector3Control("Target Rot", rot, stepRot, "°");
                data.IkTargetLocalRotation = rot;
                if (targetTrans != null) targetTrans.localRotation = Quaternion.Euler(rot);
                GUILayout.Space(6);

                // 3. Elbow Hint Local Position
                GUILayout.Label($"<b>3. Hướng cùi chỏ phải (ElbowHintLocal):</b>");
                Vector3 hint = data.ElbowHintLocal;
                hint = DrawVector3Control("Elbow Hint", hint, stepPos, "m");
                data.ElbowHintLocal = hint;
                if (hintTrans != null) hintTrans.localPosition = hint;
                GUILayout.Space(6);

                // 4. Grip Offset Position & Rotation (Khớp trong socket)
                GUILayout.Label($"<b>4. Vị trí vật trong lòng bàn tay (GripOffsetPos / Rot):</b>");
                Vector3 gripPos = data.GripOffsetPosition;
                gripPos = DrawVector3Control("Grip Pos", gripPos, stepPos, "m");
                data.GripOffsetPosition = gripPos;

                Vector3 gripRot = data.GripOffsetRotation;
                gripRot = DrawVector3Control("Grip Rot", gripRot, stepRot, "°");
                data.GripOffsetRotation = gripRot;

                if (socketTrans != null && socketTrans.childCount > 0)
                {
                    Transform itemInSocket = socketTrans.GetChild(0);
                    Transform gripRight = itemInSocket.Find("GripRight");
                    if (gripRight != null)
                    {
                        Quaternion invRot = Quaternion.Inverse(gripRight.localRotation);
                        itemInSocket.localRotation = invRot * Quaternion.Euler(gripRot);
                        itemInSocket.localPosition = -(itemInSocket.localRotation * gripRight.localPosition) + gripPos;
                    }
                    else
                    {
                        itemInSocket.localPosition = gripPos;
                        itemInSocket.localRotation = Quaternion.Euler(gripRot);
                    }
                }
                GUILayout.Space(6);

                // 5. Finger Curls
                GUILayout.Label($"<b>5. Độ gập ngón tay (Finger Curls 0-1):</b>");
                data.ThumbCurl = DrawSliderControl("Ngón cái (Thumb)", data.ThumbCurl, stepCurl);
                data.IndexCurl = DrawSliderControl("Ngón trỏ (Index)", data.IndexCurl, stepCurl);
                data.MiddleCurl = DrawSliderControl("Ngón giữa (Middle)", data.MiddleCurl, stepCurl);
                data.RingCurl = DrawSliderControl("Ngón áp út (Ring)", data.RingCurl, stepCurl);
                data.PinkyCurl = DrawSliderControl("Ngón út (Pinky)", data.PinkyCurl, stepCurl);
                GUILayout.Space(6);

                // 6. Vị trí đầu đèn Spot Light (nếu là đèn pin)
                if (data.ItemType == ItemType.Flashlight)
                {
                    GUILayout.Label($"<b>6. Vị trí đầu đèn Spot Light (Camera Local Offset):</b>");
                    armIK.FlashlightHeadLocalOffset = DrawVector3Control("Đầu đèn", armIK.FlashlightHeadLocalOffset, stepPos, "m");
                    GUILayout.Space(6);
                }

                // 7. Tinh chỉnh Lòng Bàn Tay & Ngón Tay Thủ Tục (Palm & Grip Procedural)
                GUILayout.Label($"<b>7. Tinh chỉnh Lòng Bàn Tay & Ngón Tay Thủ Tục:</b>");
                armIK.PalmDepthOffset = DrawSliderControlRange("Độ sâu lòng bàn tay", armIK.PalmDepthOffset, 0.01f, 0.08f, 0.002f);
                armIK.FingerCurlAngle = DrawSliderControlRange("Hệ số gập ngón tay", armIK.FingerCurlAngle, 0.5f, 1.5f, 0.05f);
                if (data.ItemType == ItemType.Radar)
                {
                    armIK.RadarScaleMultiplier = DrawSliderControlRange("Scale Radar", armIK.RadarScaleMultiplier, 1.0f, 2.5f, 0.05f);
                    armIK.WristSupinationAngle = DrawSliderControlRange("Góc ngửa cổ tay", armIK.WristSupinationAngle, 90f, 150f, 2.0f);
                    armIK.RadarPalmOffset = DrawSliderControlRange("Bù Palm Radar", armIK.RadarPalmOffset, -0.02f, 0.05f, 0.002f);
                }
                else if (data.ItemType == ItemType.Flashlight)
                {
                    armIK.FlashlightPalmOffset = DrawSliderControlRange("Bù Palm Đèn Pin", armIK.FlashlightPalmOffset, -0.02f, 0.04f, 0.002f);
                }
                GUILayout.Space(6);

                // Nút Sao Chép
                GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
                if (GUILayout.Button("<b>IN CONSOLE & COPY GIÁ TRỊ VÀO CLIPBOARD</b>", GUILayout.Height(36)))
                {
                    string report = GenerateValuesReport(data, armIK);
                    GUIUtility.systemCopyBuffer = report;
                    Debug.Log("<color=green><b>[PlayerArmIKTuningUI] ĐÃ SAO CHÉP GIÁ TRỊ VÀO CLIPBOARD & IN RA CONSOLE:</b></color>\n" + report);
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                // Ô TRỐNG (TƯ THẾ TAY THẢ LỎNG)
                GUILayout.Label("<color=yellow><b>Đang ở Ô Trống (Tư thế tay thả lỏng tự nhiên)</b></color>");
                GUILayout.Label("Cả hai cánh tay đang hướng về các điểm thả lỏng hai bên hông.");
                GUILayout.Space(6);

                GUILayout.Label("<b>1. Bàn tay phải thả lỏng (Right Idle Target):</b>");
                armIK.RightIdleLocalPosition = DrawVector3Control("R Idle Pos", armIK.RightIdleLocalPosition, stepPos, "m");
                armIK.RightIdleLocalRotation = DrawVector3Control("R Idle Rot", armIK.RightIdleLocalRotation, stepRot, "°");
                GUILayout.Space(6);

                GUILayout.Label("<b>2. Bàn tay trái thả lỏng (Left Idle Target):</b>");
                armIK.LeftIdleLocalPosition = DrawVector3Control("L Idle Pos", armIK.LeftIdleLocalPosition, stepPos, "m");
                armIK.LeftIdleLocalRotation = DrawVector3Control("L Idle Rot", armIK.LeftIdleLocalRotation, stepRot, "°");
                GUILayout.Space(8);

                GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
                if (GUILayout.Button("<b>IN CONSOLE & COPY TỌA ĐỘ TAY THẢ LỎNG VÀO CLIPBOARD</b>", GUILayout.Height(36)))
                {
                    string rep = $"// Tọa độ tư thế tay thả lỏng khi ô trống:\n"
                        + $"rightIdleLocalPosition = new Vector3({armIK.RightIdleLocalPosition.x:F3}f, {armIK.RightIdleLocalPosition.y:F3}f, {armIK.RightIdleLocalPosition.z:F3}f);\n"
                        + $"rightIdleLocalRotation = new Vector3({armIK.RightIdleLocalRotation.x:F1}f, {armIK.RightIdleLocalRotation.y:F1}f, {armIK.RightIdleLocalRotation.z:F1}f);\n"
                        + $"leftIdleLocalPosition = new Vector3({armIK.LeftIdleLocalPosition.x:F3}f, {armIK.LeftIdleLocalPosition.y:F3}f, {armIK.LeftIdleLocalPosition.z:F3}f);\n"
                        + $"leftIdleLocalRotation = new Vector3({armIK.LeftIdleLocalRotation.x:F1}f, {armIK.LeftIdleLocalRotation.y:F1}f, {armIK.LeftIdleLocalRotation.z:F1}f);\n";
                    GUIUtility.systemCopyBuffer = rep;
                    Debug.Log("<color=green><b>[PlayerArmIKTuningUI] ĐÃ SAO CHÉP TỌA ĐỘ TAY THẢ LỎNG VÀO CLIPBOARD:</b></color>\n" + rep);
                }
                GUI.backgroundColor = Color.white;
            }

            GUILayout.Space(6);
            if (GUILayout.Button("Đóng bảng (F12)", GUILayout.Height(26)))
            {
                isVisible = false;
                Cursor.lockState = previousLockMode;
                Cursor.visible = previousCursorVisible;
            }

            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        private Vector3 DrawVector3Control(string label, Vector3 val, float step, string unit)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: ({val.x:+0.000;-0.000},{val.y:+0.000;-0.000},{val.z:+0.000;-0.000}) {unit}", GUILayout.Width(240));

            if (GUILayout.Button("-X", GUILayout.Width(30))) val.x -= step;
            if (GUILayout.Button("+X", GUILayout.Width(30))) val.x += step;

            if (GUILayout.Button("-Y", GUILayout.Width(30))) val.y -= step;
            if (GUILayout.Button("+Y", GUILayout.Width(30))) val.y += step;

            if (GUILayout.Button("-Z", GUILayout.Width(30))) val.z -= step;
            if (GUILayout.Button("+Z", GUILayout.Width(30))) val.z += step;

            GUILayout.EndHorizontal();
            return val;
        }

        private float DrawSliderControl(string label, float val, float step)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: {val:F2}", GUILayout.Width(170));
            if (GUILayout.Button("-", GUILayout.Width(28))) val = Mathf.Clamp01(val - step);
            val = GUILayout.HorizontalSlider(val, 0f, 1f, GUILayout.Width(180));
            if (GUILayout.Button("+", GUILayout.Width(28))) val = Mathf.Clamp01(val + step);
            GUILayout.EndHorizontal();
            return val;
        }

        private float DrawSliderControlRange(string label, float val, float min, float max, float step)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: {val:F3}", GUILayout.Width(170));
            if (GUILayout.Button("-", GUILayout.Width(28))) val = Mathf.Clamp(val - step, min, max);
            val = GUILayout.HorizontalSlider(val, min, max, GUILayout.Width(180));
            if (GUILayout.Button("+", GUILayout.Width(28))) val = Mathf.Clamp(val + step, min, max);
            GUILayout.EndHorizontal();
            return val;
        }

        private string GenerateValuesReport(ItemData data, PlayerArmIK armIK)
        {
            string rep = $"// Giá trị tinh chỉnh cho {data.name} ({data.DisplayName}):\n"
                + $"ikTargetLocalPosition = new Vector3({data.IkTargetLocalPosition.x:F3}f, {data.IkTargetLocalPosition.y:F3}f, {data.IkTargetLocalPosition.z:F3}f);\n"
                + $"ikTargetLocalRotation = new Vector3({data.IkTargetLocalRotation.x:F1}f, {data.IkTargetLocalRotation.y:F1}f, {data.IkTargetLocalRotation.z:F1}f);\n"
                + $"elbowHintLocal = new Vector3({data.ElbowHintLocal.x:F3}f, {data.ElbowHintLocal.y:F3}f, {data.ElbowHintLocal.z:F3}f);\n"
                + $"gripOffsetPosition = new Vector3({data.GripOffsetPosition.x:F3}f, {data.GripOffsetPosition.y:F3}f, {data.GripOffsetPosition.z:F3}f);\n"
                + $"gripOffsetRotation = new Vector3({data.GripOffsetRotation.x:F1}f, {data.GripOffsetRotation.y:F1}f, {data.GripOffsetRotation.z:F1}f);\n";

            if (armIK != null)
            {
                rep += $"palmDepthOffset = {armIK.PalmDepthOffset:F3}f; fingerCurlAngle = {armIK.FingerCurlAngle:F2}f;\n";
                if (data.ItemType == ItemType.Radar)
                {
                    rep += $"radarScaleMultiplier = {armIK.RadarScaleMultiplier:F2}f; wristSupinationAngle = {armIK.WristSupinationAngle:F1}f; radarPalmOffset = {armIK.RadarPalmOffset:F3}f;\n";
                }
                else if (data.ItemType == ItemType.Flashlight)
                {
                    var fOffset = armIK.FlashlightHeadLocalOffset;
                    rep += $"flashlightHeadLocalOffset = new Vector3({fOffset.x:F3}f, {fOffset.y:F3}f, {fOffset.z:F3}f); flashlightPalmOffset = {armIK.FlashlightPalmOffset:F3}f;\n";
                }
            }
            return rep;
        }
    }
}
#else
namespace HorrorGame.Inventory
{
    /// <summary>Bản rỗng cho bản build phát hành: công cụ tinh chỉnh IK (F12) chỉ có trong Editor/Development build.</summary>
    public class PlayerArmIKTuningUI : UnityEngine.MonoBehaviour
    {
        public static bool IsVisible => false;
    }
}
#endif
