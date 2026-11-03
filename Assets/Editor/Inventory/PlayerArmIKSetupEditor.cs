using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HorrorGame.Inventory.Editor
{
    public static class PlayerArmIKSetupEditor
    {
        [MenuItem("Tools/Inventory/Setup Character Arm IK")]
        public static void SetupCharacterArmIK()
        {
            Scene scene = SceneManager.GetActiveScene();
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                Debug.LogError("[PlayerArmIKSetupEditor] Không tìm thấy PlayerController trong scene!");
                return;
            }

            // 1. Gắn PlayerArmIK, PlayerArmIKTuningUI, FlashlightDebugOverlay vào Player
            PlayerArmIK armIK = player.GetComponent<PlayerArmIK>();
            if (armIK == null) armIK = player.gameObject.AddComponent<PlayerArmIK>();

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            PlayerArmIKTuningUI tuningUI = player.GetComponent<PlayerArmIKTuningUI>();
            if (tuningUI == null) tuningUI = player.gameObject.AddComponent<PlayerArmIKTuningUI>();

            FlashlightDebugOverlay debugOverlay = player.GetComponent<FlashlightDebugOverlay>();
            if (debugOverlay == null) debugOverlay = player.gameObject.AddComponent<FlashlightDebugOverlay>();
            #endif

            // 2. Chỉnh nearClipPlane của Camera về 0.03m
            if (player.playerCamera != null)
            {
                Camera cam = player.playerCamera.GetComponent<Camera>();
                if (cam != null)
                {
                    Undo.RecordObject(cam, "Set nearClipPlane to 0.03");
                    cam.nearClipPlane = 0.03f;
                    EditorUtility.SetDirty(cam);
                }

                Transform vmRoot = player.playerCamera.Find("ViewModelRoot");
                if (vmRoot != null)
                {
                    Transform oldRig = vmRoot.Find("HandRig");
                    if (oldRig != null)
                    {
                        oldRig.gameObject.SetActive(false);
                    }
                }
            }

            // 3. Cập nhật dữ liệu Item_Flashlight.asset theo tỷ lệ và vị trí mới
            string flPath = "Assets/Items/Item_Flashlight.asset";
            ItemData flData = AssetDatabase.LoadAssetAtPath<ItemData>(flPath);
            if (flData != null)
            {
                flData.IkTargetLocalPosition = new Vector3(0.18f, -0.14f, 0.34f);
                flData.IkTargetLocalRotation = new Vector3(5f, -8f, 20f);
                flData.ElbowHintLocal = new Vector3(0.30f, -0.45f, -0.05f);
                flData.GripOffsetPosition = new Vector3(0.015f, -0.012f, 0.055f);
                flData.GripOffsetRotation = Vector3.zero;
                flData.ThumbCurl = 0.42f;
                flData.IndexCurl = 0.78f;
                flData.MiddleCurl = 0.82f;
                flData.RingCurl = 0.80f;
                flData.PinkyCurl = 0.75f;
                EditorUtility.SetDirty(flData);
            }

            // 4. Cập nhật dữ liệu Item_Radar.asset theo tỷ lệ và vị trí mới
            string rdPath = "Assets/Items/Item_Radar.asset";
            ItemData rdData = AssetDatabase.LoadAssetAtPath<ItemData>(rdPath);
            if (rdData != null)
            {
                rdData.IkTargetLocalPosition = new Vector3(-0.15f, -0.08f, 0.32f);
                rdData.IkTargetLocalRotation = new Vector3(16f, 18f, -15f);
                rdData.ElbowHintLocal = new Vector3(-0.30f, -0.45f, -0.05f);
                rdData.GripOffsetPosition = new Vector3(-0.010f, -0.015f, 0.020f);
                rdData.GripOffsetRotation = Vector3.zero;
                rdData.ThumbCurl = 0.25f;
                rdData.IndexCurl = 0.52f;
                rdData.MiddleCurl = 0.58f;
                rdData.RingCurl = 0.55f;
                rdData.PinkyCurl = 0.50f;
                EditorUtility.SetDirty(rdData);
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("<color=green><b>[PlayerArmIKSetupEditor] ĐÃ THIẾT LẬP THÀNH CÔNG PLAYER ARM IK TRÊN MODEL NHÂN VẬT THẬT!</b></color>");
        }
    }
}
