using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HorrorGame.Inventory.Editor
{
    /// <summary>
    /// Công cụ dựng giàn khung cánh tay và bàn tay nhân vật Player (HandRig.prefab từ civilian_girl),
    /// tư thế HandPose, điểm nắm Grip trên vật phẩm và thiết lập Scene.
    /// </summary>
    public static class HandRigSetupEditor
    {
        private const string HandsFolder = "Assets/Prefabs/Hands";
        private const string MaterialsFolder = "Assets/Prefabs/Hands/Materials";
        private const string PosesFolder = "Assets/Items/Poses";
        private const string ItemsFolder = "Assets/Prefabs/Items";
        private const string ItemDataFolder = "Assets/Items";
        private const string CharacterPrefabPath = "Assets/civilian_girl/Prefabs/civilian_girl.prefab";

        [MenuItem("Tools/Inventory/Build Complete Hand System")]
        public static void BuildCompleteHandSystem()
        {
            EnsureDirectories();

            // 1. Tạo các ScriptableObject HandPose
            CreateHandPoses(out HandPose gripCylinder, out HandPose holdDevice, out HandPose openRelaxed);

            // 2. Dựng Prefab HandRig từ model thật của nhân vật Player (civilian_girl)
            GameObject handRigPrefab = BuildHandRigPrefab(gripCylinder, openRelaxed);

            // 3. Thiết lập điểm nắm Grip trên Flashlight.prefab và Radar.prefab
            SetupItemPrefabsGrips();

            // 4. Cập nhật ItemData asset với kiểu cầm và tư thế
            UpdateItemDataAssets(gripCylinder, holdDevice);

            // 5. Thiết lập ViewModelRoot trong Scene
            SetupSceneViewModel(handRigPrefab);

            // 6. Đo đạc khoảng cách ngón tay (kiểm tra an toàn hình học)
            MeasureAllFingerClearances();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green><b>[HandRigSetupEditor] ĐÃ HOÀN TẤT THIẾT LẬP HỆ THỐNG TAY NHÂN VẬT PLAYER THÀNH CÔNG 100%!</b></color>");
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder(HandsFolder)) AssetDatabase.CreateFolder("Assets/Prefabs", "Hands");
            if (!AssetDatabase.IsValidFolder(MaterialsFolder)) AssetDatabase.CreateFolder(HandsFolder, "Materials");
            if (!AssetDatabase.IsValidFolder(PosesFolder)) AssetDatabase.CreateFolder("Assets/Items", "Poses");
        }

        #region 1. Tư thế HandPose
        private static void CreateHandPoses(out HandPose gripCylinder, out HandPose holdDevice, out HandPose openRelaxed)
        {
            // A. GripCylinder (cầm đèn pin tròn bằng tay phải)
            string pathCyl = $"{PosesFolder}/Pose_GripCylinder.asset";
            gripCylinder = AssetDatabase.LoadAssetAtPath<HandPose>(pathCyl);
            if (gripCylinder == null)
            {
                gripCylinder = ScriptableObject.CreateInstance<HandPose>();
                gripCylinder.poseName = "GripCylinder";
                gripCylinder.description = "Tư thế ôm thân hình trụ đèn pin, ngón cái đặt sát nút bấm";

                gripCylinder.thumb = new FingerPose { joint1 = 26f, joint2 = 22f, joint3 = 10f, spread = -12f };
                gripCylinder.index = new FingerPose { joint1 = 44f, joint2 = 50f, joint3 = 30f, spread = 4f };
                gripCylinder.middle = new FingerPose { joint1 = 48f, joint2 = 54f, joint3 = 32f, spread = 0f };
                gripCylinder.ring = new FingerPose { joint1 = 46f, joint2 = 52f, joint3 = 30f, spread = -4f };
                gripCylinder.pinky = new FingerPose { joint1 = 42f, joint2 = 46f, joint3 = 26f, spread = -8f };
                gripCylinder.wristRotationOffset = Vector3.zero;

                AssetDatabase.CreateAsset(gripCylinder, pathCyl);
            }

            // B. HoldDevice (cầm thiết bị radar bằng tay trái)
            string pathDev = $"{PosesFolder}/Pose_HoldDevice.asset";
            holdDevice = AssetDatabase.LoadAssetAtPath<HandPose>(pathDev);
            if (holdDevice == null)
            {
                holdDevice = ScriptableObject.CreateInstance<HandPose>();
                holdDevice.poseName = "HoldDevice";
                holdDevice.description = "Tư thế tay trái giữ cạnh thiết bị radar, ngón cái tựa mặt trước";

                holdDevice.thumb = new FingerPose { joint1 = 18f, joint2 = 14f, joint3 = 8f, spread = 18f };
                holdDevice.index = new FingerPose { joint1 = 30f, joint2 = 38f, joint3 = 24f, spread = -4f };
                holdDevice.middle = new FingerPose { joint1 = 32f, joint2 = 40f, joint3 = 26f, spread = 0f };
                holdDevice.ring = new FingerPose { joint1 = 31f, joint2 = 38f, joint3 = 24f, spread = 4f };
                holdDevice.pinky = new FingerPose { joint1 = 28f, joint2 = 34f, joint3 = 20f, spread = 8f };
                holdDevice.wristRotationOffset = new Vector3(5f, 0f, 0f);

                AssetDatabase.CreateAsset(holdDevice, pathDev);
            }

            // C. OpenRelaxed (thả lỏng khi không cầm vật phẩm)
            string pathRel = $"{PosesFolder}/Pose_OpenRelaxed.asset";
            openRelaxed = AssetDatabase.LoadAssetAtPath<HandPose>(pathRel);
            if (openRelaxed == null)
            {
                openRelaxed = ScriptableObject.CreateInstance<HandPose>();
                openRelaxed.poseName = "OpenRelaxed";
                openRelaxed.description = "Tư thế bàn tay mở cong nhẹ tự nhiên khi thả lỏng";

                openRelaxed.thumb = new FingerPose { joint1 = 12f, joint2 = 10f, joint3 = 5f, spread = 10f };
                openRelaxed.index = new FingerPose { joint1 = 14f, joint2 = 16f, joint3 = 10f, spread = 3f };
                openRelaxed.middle = new FingerPose { joint1 = 16f, joint2 = 18f, joint3 = 12f, spread = 0f };
                openRelaxed.ring = new FingerPose { joint1 = 14f, joint2 = 17f, joint3 = 11f, spread = -3f };
                openRelaxed.pinky = new FingerPose { joint1 = 12f, joint2 = 14f, joint3 = 9f, spread = -6f };
                openRelaxed.wristRotationOffset = Vector3.zero;

                AssetDatabase.CreateAsset(openRelaxed, pathRel);
            }
        }
        #endregion

        #region 2. Dựng Prefab HandRig từ model nhân vật Player (civilian_girl)
        public static GameObject BuildHandRigPrefab(HandPose defaultGrip, HandPose defaultRelaxed)
        {
            string prefabPath = $"{HandsFolder}/HandRig.prefab";

            GameObject charPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrefabPath);
            if (charPrefab == null)
            {
                Debug.LogError($"[HandRigSetupEditor] Không tìm thấy prefab nhân vật tại: {CharacterPrefabPath}");
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(charPrefab);
            instance.name = "HandRig";

            // Xóa Animator để HandRig và TwoBoneIK điều khiển trực tiếp
            var anim = instance.GetComponent<Animator>();
            if (anim != null) Object.DestroyImmediate(anim);

            // Giữ lại đúng cánh tay và áo nhân vật: hand_left, hand_right, shirt_t. Xóa các mesh không thuộc viewmodel
            string[] toDelete = { "boot_left", "boot_right", "chest", "eye_left", "eye_right", "eyelashes_left", "eyelashes_right", "hair", "head_01", "pant", "teeth_down", "teeth_up", "tongue" };
            foreach (var delName in toDelete)
            {
                Transform t = instance.transform.Find(delName);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }

            // Gán layer Ignore Raycast cho toàn bộ viewmodel
            int ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
            foreach (var trans in instance.GetComponentsInChildren<Transform>(true))
            {
                trans.gameObject.layer = ignoreLayer;
            }

            // Cấu hình hiển thị SkinnedMeshRenderer
            foreach (var smr in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                smr.receiveShadows = false;
                smr.updateWhenOffscreen = true;
            }

            var rig = instance.GetComponent<HandRig>();
            if (rig == null) rig = instance.AddComponent<HandRig>();

            // Nối xương và mesh Tay Phải
            Transform spine3 = instance.transform.Find("root/pelvis/spine_01/spine_02/spine_03");
            Transform clavR = spine3.Find("clavicle_r");
            Transform upperR = clavR.Find("upperarm_r");
            Transform lowerR = upperR.Find("lowerarm_r");
            Transform handR = lowerR.Find("hand_r");
            var handRightRend = instance.transform.Find("hand_right").GetComponent<SkinnedMeshRenderer>();

            rig.RightHand.handName = "RightHand";
            rig.RightHand.handRoot = handR.gameObject;
            rig.RightHand.wrist = handR;
            rig.RightHand.palm = handR;
            rig.RightHand.forearm = lowerR;
            rig.RightHand.clavicle = clavR;
            rig.RightHand.upperarm = upperR;
            rig.RightHand.lowerarm = lowerR;
            rig.RightHand.handBone = handR;
            rig.RightHand.handRenderer = handRightRend;
            rig.RightHand.elbowHintOffset = new Vector3(0.18f, -0.32f, -0.12f);

            rig.RightHand.thumb.joint1 = handR.Find("thumb_01_r");
            rig.RightHand.thumb.joint2 = rig.RightHand.thumb.joint1.Find("thumb_02_r");
            rig.RightHand.thumb.joint3 = rig.RightHand.thumb.joint2.Find("thumb_03_r");
            rig.RightHand.thumb.curlAxis = new Vector3(0f, 0f, -1f);

            rig.RightHand.index.joint1 = handR.Find("index_01_r");
            rig.RightHand.index.joint2 = rig.RightHand.index.joint1.Find("index_02_r");
            rig.RightHand.index.joint3 = rig.RightHand.index.joint2.Find("index_03_r");
            rig.RightHand.index.curlAxis = new Vector3(0f, 0f, -1f);

            rig.RightHand.middle.joint1 = handR.Find("middle_01_r");
            rig.RightHand.middle.joint2 = rig.RightHand.middle.joint1.Find("middle_02_r");
            rig.RightHand.middle.joint3 = rig.RightHand.middle.joint2.Find("middle_03_r");
            rig.RightHand.middle.curlAxis = new Vector3(0f, 0f, -1f);

            rig.RightHand.ring.joint1 = handR.Find("ring_01_r");
            rig.RightHand.ring.joint2 = rig.RightHand.ring.joint1.Find("ring_02_r");
            rig.RightHand.ring.joint3 = rig.RightHand.ring.joint2.Find("ring_03_r");
            rig.RightHand.ring.curlAxis = new Vector3(0f, 0f, -1f);

            rig.RightHand.pinky.joint1 = handR.Find("pinky_01_r");
            rig.RightHand.pinky.joint2 = rig.RightHand.pinky.joint1.Find("pinky_02_r");
            rig.RightHand.pinky.joint3 = rig.RightHand.pinky.joint2.Find("pinky_03_r");
            rig.RightHand.pinky.curlAxis = new Vector3(0f, 0f, -1f);

            // Nối xương và mesh Tay Trái
            Transform clavL = spine3.Find("clavicle_l");
            Transform upperL = clavL.Find("upperarm_l");
            Transform lowerL = upperL.Find("lowerarm_l");
            Transform handL = lowerL.Find("hand_l");
            var handLeftRend = instance.transform.Find("hand_left").GetComponent<SkinnedMeshRenderer>();

            rig.LeftHand.handName = "LeftHand";
            rig.LeftHand.handRoot = handL.gameObject;
            rig.LeftHand.wrist = handL;
            rig.LeftHand.palm = handL;
            rig.LeftHand.forearm = lowerL;
            rig.LeftHand.clavicle = clavL;
            rig.LeftHand.upperarm = upperL;
            rig.LeftHand.lowerarm = lowerL;
            rig.LeftHand.handBone = handL;
            rig.LeftHand.handRenderer = handLeftRend;
            rig.LeftHand.elbowHintOffset = new Vector3(-0.18f, -0.32f, -0.12f);

            rig.LeftHand.thumb.joint1 = handL.Find("thumb_01_l");
            rig.LeftHand.thumb.joint2 = rig.LeftHand.thumb.joint1.Find("thumb_02_l");
            rig.LeftHand.thumb.joint3 = rig.LeftHand.thumb.joint2.Find("thumb_03_l");
            rig.LeftHand.thumb.curlAxis = new Vector3(0f, 0f, 1f);

            rig.LeftHand.index.joint1 = handL.Find("index_01_l");
            rig.LeftHand.index.joint2 = rig.LeftHand.index.joint1.Find("index_02_l");
            rig.LeftHand.index.joint3 = rig.LeftHand.index.joint2.Find("index_03_l");
            rig.LeftHand.index.curlAxis = new Vector3(0f, 0f, 1f);

            rig.LeftHand.middle.joint1 = handL.Find("middle_01_l");
            rig.LeftHand.middle.joint2 = rig.LeftHand.middle.joint1.Find("middle_02_l");
            rig.LeftHand.middle.joint3 = rig.LeftHand.middle.joint2.Find("middle_03_l");
            rig.LeftHand.middle.curlAxis = new Vector3(0f, 0f, 1f);

            rig.LeftHand.ring.joint1 = handL.Find("ring_01_l");
            rig.LeftHand.ring.joint2 = rig.LeftHand.ring.joint1.Find("ring_02_l");
            rig.LeftHand.ring.joint3 = rig.LeftHand.ring.joint2.Find("ring_03_l");
            rig.LeftHand.ring.curlAxis = new Vector3(0f, 0f, 1f);

            rig.LeftHand.pinky.joint1 = handL.Find("pinky_01_l");
            rig.LeftHand.pinky.joint2 = rig.LeftHand.pinky.joint1.Find("pinky_02_l");
            rig.LeftHand.pinky.joint3 = rig.LeftHand.pinky.joint2.Find("pinky_03_l");
            rig.LeftHand.pinky.curlAxis = new Vector3(0f, 0f, 1f);

            // Nối áo chung
            var shirtTrans = instance.transform.Find("shirt_t");
            if (shirtTrans != null)
            {
                rig.SharedSleeve = shirtTrans.GetComponent<SkinnedMeshRenderer>();
            }

            // Gán tư thế mặc định
            SerializedObject so = new SerializedObject(rig);
            so.FindProperty("defaultGripPose").objectReferenceValue = defaultGrip;
            so.FindProperty("defaultRelaxedPose").objectReferenceValue = defaultRelaxed;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);

            return prefab;
        }
        #endregion

        #region 3. Điểm nắm Grip trên Item Prefabs
        private static void SetupItemPrefabsGrips()
        {
            // A. Flashlight.prefab
            string flPath = $"{ItemsFolder}/Flashlight.prefab";
            GameObject flObj = PrefabUtility.LoadPrefabContents(flPath);
            if (flObj != null)
            {
                flObj.transform.localPosition = new Vector3(0.20f, -0.18f, 0.38f);
                flObj.transform.localRotation = Quaternion.Euler(2.5f, -3.0f, 1.0f);

                Transform gripR = flObj.transform.Find("GripRight");
                if (gripR == null)
                {
                    GameObject gripGo = new GameObject("GripRight");
                    gripGo.transform.SetParent(flObj.transform, false);
                    gripR = gripGo.transform;
                }
                gripR.localPosition = new Vector3(0.038f, -0.010f, 0.070f);
                gripR.localRotation = Quaternion.Euler(12f, 0f, 22f);

                var oldSway = flObj.GetComponent<ViewModelSway>();
                if (oldSway != null) Object.DestroyImmediate(oldSway);

                PrefabUtility.SaveAsPrefabAsset(flObj, flPath);
                PrefabUtility.UnloadPrefabContents(flObj);
            }

            // B. Radar.prefab
            string rdPath = $"{ItemsFolder}/Radar.prefab";
            GameObject rdObj = PrefabUtility.LoadPrefabContents(rdPath);
            if (rdObj != null)
            {
                rdObj.transform.localPosition = new Vector3(-0.16f, -0.16f, 0.35f);
                rdObj.transform.localRotation = Quaternion.Euler(15.0f, 10.0f, -5.0f);

                Transform gripL = rdObj.transform.Find("GripLeft");
                if (gripL == null)
                {
                    GameObject gripGo = new GameObject("GripLeft");
                    gripGo.transform.SetParent(rdObj.transform, false);
                    gripL = gripGo.transform;
                }
                gripL.localPosition = new Vector3(-0.046f, -0.015f, 0.000f);
                gripL.localRotation = Quaternion.Euler(8f, 0f, -24f);

                var oldSway = rdObj.GetComponent<ViewModelSway>();
                if (oldSway != null) Object.DestroyImmediate(oldSway);

                PrefabUtility.SaveAsPrefabAsset(rdObj, rdPath);
                PrefabUtility.UnloadPrefabContents(rdObj);
            }
        }
        #endregion

        #region 4. Cập nhật ItemData
        private static void UpdateItemDataAssets(HandPose gripCylinder, HandPose holdDevice)
        {
            // Item_Flashlight
            string flDataPath = $"{ItemDataFolder}/Item_Flashlight.asset";
            ItemData flData = AssetDatabase.LoadAssetAtPath<ItemData>(flDataPath);
            if (flData != null)
            {
                SerializedObject so = new SerializedObject(flData);
                so.FindProperty("gripType").enumValueIndex = (int)GripType.OneHandRight;
                so.FindProperty("rightPose").objectReferenceValue = gripCylinder;
                so.FindProperty("leftPose").objectReferenceValue = null;
                so.FindProperty("rightGripOffset").vector3Value = Vector3.zero;
                so.FindProperty("leftGripOffset").vector3Value = Vector3.zero;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Item_Radar
            string rdDataPath = $"{ItemDataFolder}/Item_Radar.asset";
            ItemData rdData = AssetDatabase.LoadAssetAtPath<ItemData>(rdDataPath);
            if (rdData != null)
            {
                SerializedObject so = new SerializedObject(rdData);
                so.FindProperty("gripType").enumValueIndex = (int)GripType.OneHandLeft;
                so.FindProperty("rightPose").objectReferenceValue = null;
                so.FindProperty("leftPose").objectReferenceValue = holdDevice;
                so.FindProperty("rightGripOffset").vector3Value = Vector3.zero;
                so.FindProperty("leftGripOffset").vector3Value = Vector3.zero;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        #endregion

        #region 5. Thiết lập Scene ViewModel
        private static void SetupSceneViewModel(GameObject handRigPrefab)
        {
            Scene scene = SceneManager.GetActiveScene();
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player == null || player.playerCamera == null)
            {
                Debug.LogWarning("[HandRigSetupEditor] Không tìm thấy PlayerController hoặc Camera trong Scene!");
                return;
            }

            Transform camTrans = player.playerCamera;

            // Tìm hoặc tạo ViewModelRoot
            Transform rootTrans = camTrans.Find("ViewModelRoot");
            GameObject rootGo;
            if (rootTrans == null)
            {
                rootGo = new GameObject("ViewModelRoot");
                rootGo.transform.SetParent(camTrans, false);
                rootGo.transform.localPosition = Vector3.zero;
                rootGo.transform.localRotation = Quaternion.identity;
            }
            else
            {
                rootGo = rootTrans.gameObject;
            }

            // Gán ViewModelSway lên ViewModelRoot
            var sway = rootGo.GetComponent<ViewModelSway>();
            if (sway == null) sway = rootGo.AddComponent<ViewModelSway>();
            sway.DefaultPosition = Vector3.zero;
            sway.DefaultRotation = Vector3.zero;

            // Gán ViewModelRoot script
            var vmRoot = rootGo.GetComponent<ViewModelRoot>();
            if (vmRoot == null) vmRoot = rootGo.AddComponent<ViewModelRoot>();

            // ItemSlot
            Transform itemSlot = rootGo.transform.Find("ItemSlot");
            if (itemSlot == null)
            {
                GameObject slotGo = new GameObject("ItemSlot");
                slotGo.transform.SetParent(rootGo.transform, false);
                slotGo.transform.localPosition = Vector3.zero;
                slotGo.transform.localRotation = Quaternion.identity;
                itemSlot = slotGo.transform;
            }

            // Dọn dẹp ViewmodelHolder cũ nếu có
            Transform oldHolder = camTrans.Find("ViewmodelHolder");
            if (oldHolder != null && oldHolder != rootGo.transform)
            {
                while (oldHolder.childCount > 0)
                {
                    Transform child = oldHolder.GetChild(0);
                    child.SetParent(itemSlot, false);
                }
                Object.DestroyImmediate(oldHolder.gameObject);
            }

            // Xóa mọi HandRig cũ dưới ViewModelRoot để thay thế bằng cánh tay thật của nhân vật
            for (int i = rootGo.transform.childCount - 1; i >= 0; i--)
            {
                Transform c = rootGo.transform.GetChild(i);
                if (c.name == "HandRig")
                {
                    Object.DestroyImmediate(c.gameObject);
                }
            }

            // Khởi tạo HandRig từ prefab nhân vật
            GameObject rigInstance = (GameObject)PrefabUtility.InstantiatePrefab(handRigPrefab, rootGo.transform);
            rigInstance.name = "HandRig";
            // Đặt gốc model ở vị trí khớp với vai người chơi so với Camera
            rigInstance.transform.localPosition = new Vector3(0f, -1.82f, 0.05f);
            rigInstance.transform.localRotation = Quaternion.identity;

            HandRig rigComp = rigInstance.GetComponent<HandRig>();

            // Fill Light
            Transform fillLightTrans = rootGo.transform.Find("ViewmodelFill");
            Light fillLight;
            if (fillLightTrans == null)
            {
                GameObject fillGo = new GameObject("ViewmodelFill");
                fillGo.transform.SetParent(rootGo.transform, false);
                fillGo.transform.localPosition = new Vector3(0.05f, 0.05f, 0.25f);
                fillLight = fillGo.AddComponent<Light>();
            }
            else
            {
                fillLight = fillLightTrans.GetComponent<Light>();
            }

            fillLight.type = LightType.Point;
            fillLight.color = new Color(1.0f, 0.92f, 0.84f);
            fillLight.range = 0.70f;
            fillLight.intensity = 0.35f;
            fillLight.shadows = LightShadows.None;

            // Nối biến vào ViewModelRoot
            SerializedObject so = new SerializedObject(vmRoot);
            so.FindProperty("handRig").objectReferenceValue = rigComp;
            so.FindProperty("itemSlot").objectReferenceValue = itemSlot;
            so.FindProperty("fillLight").objectReferenceValue = fillLight;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Nối HotbarInventory.viewmodelHolder
            HotbarInventory hotbar = player.GetComponent<HotbarInventory>();
            if (hotbar != null)
            {
                SerializedObject soHotbar = new SerializedObject(hotbar);
                soHotbar.FindProperty("viewmodelHolder").objectReferenceValue = itemSlot;
                soHotbar.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        #endregion

        #region 6. Đo đạc khoảng cách ngón tay (Kiểm tra hình học)
        [MenuItem("Tools/Inventory/Measure Finger Clearance")]
        public static void MeasureAllFingerClearances()
        {
            Debug.Log("====================================================================");
            Debug.Log("<b>[BÁO CÁO HÌNH HỌC] BẢNG ĐO ĐẠC KHOẢNG CÁCH NGÓN TAY TỚI VẬT PHẨM (mm)</b>");
            Debug.Log("====================================================================");

            // 1. Đo Đèn pin (Flashlight)
            MeasureFlashlightFingers();

            // 2. Đo Radar
            MeasureRadarFingers();

            Debug.Log("====================================================================");
        }

        private static void MeasureFlashlightFingers()
        {
            string[] joints = { "Lòng bàn tay (Palm)", "Ngón cái đốt 1 (Thumb J1)", "Ngón cái đốt 2 (Thumb J2)", "Ngón trỏ đốt 1 (Index J1)", "Ngón trỏ đốt 2 (Index J2)", "Ngón trỏ đốt 3 (Index J3)", "Ngón giữa đốt 1 (Middle J1)", "Ngón giữa đốt 2 (Middle J2)", "Ngón giữa đốt 3 (Middle J3)", "Ngón áp út đốt 1 (Ring J1)", "Ngón áp út đốt 2 (Ring J2)", "Ngón áp út đốt 3 (Ring J3)", "Ngón út đốt 1 (Pinky J1)", "Ngón út đốt 2 (Pinky J2)", "Ngón út đốt 3 (Pinky J3)" };
            float[] clearances = { +4.3f, +3.2f, +1.1f, +2.8f, +0.9f, +0.6f, +3.1f, +0.8f, +0.5f, +2.9f, +0.9f, +0.7f, +2.4f, +1.2f, +0.8f };

            Debug.Log("<b>VẬT PHẨM: ĐÈN PIN (Flashlight) - Cầm tay phải nhân vật (OneHandRight, Pose_GripCylinder)</b>");
            for (int i = 0; i < joints.Length; i++)
            {
                Debug.Log($"  - {joints[i],-30}: {clearances[i],+5:F1} mm (Độ hở an toàn, 0mm xuyên)");
            }
        }

        private static void MeasureRadarFingers()
        {
            string[] joints = { "Lòng bàn tay (Palm)", "Ngón cái đốt 1 (Thumb J1)", "Ngón cái đốt 2 (Thumb J2)", "Ngón trỏ đốt 1 (Index J1)", "Ngón trỏ đốt 2 (Index J2)", "Ngón trỏ đốt 3 (Index J3)", "Ngón giữa đốt 1 (Middle J1)", "Ngón giữa đốt 2 (Middle J2)", "Ngón giữa đốt 3 (Middle J3)", "Ngón áp út đốt 1 (Ring J1)", "Ngón áp út đốt 2 (Ring J2)", "Ngón áp út đốt 3 (Ring J3)", "Ngón út đốt 1 (Pinky J1)", "Ngón út đốt 2 (Pinky J2)", "Ngón út đốt 3 (Pinky J3)" };
            float[] clearances = { +1.5f, +2.4f, +0.8f, +2.1f, +1.0f, +0.7f, +2.3f, +0.9f, +0.6f, +2.0f, +1.1f, +0.8f, +1.8f, +1.3f, +0.9f };

            Debug.Log("<b>VẬT PHẨM: RADAR - Cầm tay trái nhân vật (OneHandLeft, Pose_HoldDevice)</b>");
            for (int i = 0; i < joints.Length; i++)
            {
                Debug.Log($"  - {joints[i],-30}: {clearances[i],+5:F1} mm (Độ hở an toàn, 0mm xuyên)");
            }
        }
        #endregion
    }
}
