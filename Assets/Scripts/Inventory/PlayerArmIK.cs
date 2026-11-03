using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HorrorGame.Inventory
{
    /// <summary>
    /// Cầu nối nhận sự kiện OnAnimatorIK từ Unity Animator trên civilian_girl
    /// và chuyển tiếp tới PlayerArmIK.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class ArmIKAnimatorBridge : MonoBehaviour
    {
        public Action<int> onAnimatorIK;
        public PlayerArmIK playerArmIK;

        private void OnAnimatorIK(int layerIndex)
        {
            if (onAnimatorIK != null)
            {
                onAnimatorIK(layerIndex);
            }
            else if (playerArmIK != null)
            {
                playerArmIK.OnAnimatorIKCallback(layerIndex);
            }
        }
    }

    /// <summary>
    /// Điều khiển IK Humanoid gốc của Unity (OnAnimatorIK) cho cánh tay thật của Player (civilian_girl).
    /// Khớp chính xác với giới hạn xương nhân vật, tuyệt đối không kéo giãn hoặc làm dị dạng mesh.
    /// Quản lý socket cầm vật phẩm bằng tay phải, tư thế ngón tay và chuyển đổi mượt mà.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class PlayerArmIK : MonoBehaviour
    {
        private static PlayerArmIK _instance;
        public static PlayerArmIK Instance
        {
            get
            {
                if (_instance == null) _instance = FindAnyObjectByType<PlayerArmIK>();
                return _instance;
            }
            private set => _instance = value;
        }

        [Serializable]
        public class FingerBoneChain
        {
            [Tooltip("Đốt 1 (Proximal)")]
            public Transform joint1;
            [Tooltip("Đốt 2 (Intermediate)")]
            public Transform joint2;
            [Tooltip("Đốt 3 (Distal)")]
            public Transform joint3;

            [HideInInspector] public Quaternion baseRot1;
            [HideInInspector] public Quaternion baseRot2;
            [HideInInspector] public Quaternion baseRot3;

            [Tooltip("Trục gập cục bộ của ngón tay")]
            public Vector3 curlAxis = new Vector3(0f, 0f, -1f);

            public void CacheBaseRotations()
            {
                if (joint1 != null) baseRot1 = joint1.localRotation;
                if (joint2 != null) baseRot2 = joint2.localRotation;
                if (joint3 != null) baseRot3 = joint3.localRotation;
            }

            public void ApplyCurl(float curl01, float extraAngle = 0f, float weight = 1f)
            {
                if (weight <= 0.001f) return;

                // Giới hạn biên độ gập tự nhiên của khớp: 45°, 55°, 35°
                float a1 = (curl01 * 45f + extraAngle) * weight;
                float a2 = (curl01 * 55f + extraAngle * 0.8f) * weight;
                float a3 = (curl01 * 35f + extraAngle * 0.6f) * weight;

                if (joint1 != null) joint1.localRotation = Quaternion.Slerp(joint1.localRotation, baseRot1 * Quaternion.AngleAxis(a1, curlAxis), weight);
                if (joint2 != null) joint2.localRotation = Quaternion.Slerp(joint2.localRotation, baseRot2 * Quaternion.AngleAxis(a2, curlAxis), weight);
                if (joint3 != null) joint3.localRotation = Quaternion.Slerp(joint3.localRotation, baseRot3 * Quaternion.AngleAxis(a3, curlAxis), weight);
            }
        }

        [Serializable]
        public class ArmChain
        {
            public Transform clavicle;
            public Transform upperarm;
            public Transform lowerarm;
            public Transform hand;

            public Transform holdSocket;
            public Transform handTarget;
            public Transform elbowHint;

            [Range(0f, 1f)] public float currentWeight = 0f;
            [Range(0f, 1f)] public float targetWeight = 0f;

            public FingerBoneChain thumb = new FingerBoneChain();
            public FingerBoneChain index = new FingerBoneChain();
            public FingerBoneChain middle = new FingerBoneChain();
            public FingerBoneChain ring = new FingerBoneChain();
            public FingerBoneChain pinky = new FingerBoneChain();

            [HideInInspector] public float thumbExtraAngle = 0f;
            [HideInInspector] public Coroutine buttonPressCoroutine;

            public void CacheBaseRotations()
            {
                thumb.CacheBaseRotations();
                index.CacheBaseRotations();
                middle.CacheBaseRotations();
                ring.CacheBaseRotations();
                pinky.CacheBaseRotations();
            }

            public void ApplyFingers(float tCurl, float iCurl, float mCurl, float rCurl, float pCurl, float weight)
            {
                thumb.ApplyCurl(tCurl, thumbExtraAngle, weight);
                index.ApplyCurl(iCurl, 0f, weight);
                middle.ApplyCurl(mCurl, 0f, weight);
                ring.ApplyCurl(rCurl, 0f, weight);
                pinky.ApplyCurl(pCurl, 0f, weight);
            }
        }

        [Header("Xương Cánh Tay Player")]
        [SerializeField] private ArmChain rightArm = new ArmChain();
        [SerializeField] private ArmChain leftArm = new ArmChain();

        [Header("Tốc độ chuyển đổi trọng số IK")]
        [Tooltip("Tốc độ chuyển đổi mượt mà giữa các tư thế (1 / 0.20s = 5.0)")]
        [SerializeField] private float weightTransitionSpeed = 5.0f;

        [Header("Trọng số IK cánh tay phải khi cầm vật phẩm")]
        [Tooltip("Trọng số IK khi đứng yên hoặc đi bộ (theo yêu cầu: 0.90f)")]
        [SerializeField] private float standingIKWeight = 0.90f;

        [Tooltip("Trọng số IK khi chạy nhanh (theo yêu cầu: 0.65f - 0.75f, mặc định 0.70f)")]
        [SerializeField] private float sprintIKWeight = 0.70f;

        [Tooltip("Cường độ cộng dao động xương ngực khi chạy nhanh")]
        [SerializeField] private float chestBobMultiplier = 1.0f;

        [Header("Chống xuyên tường (Wall Avoidance SphereCast)")]
        [Tooltip("Bật cơ chế chống đâm xuyên tường")]
        [SerializeField] private bool enableWallAvoidance = true;

        [Tooltip("Bán kính SphereCast kiểm tra va chạm (m)")]
        [SerializeField] private float wallSphereRadius = 0.08f;

        [Tooltip("Khoảng cách kiểm tra phía trước (m)")]
        [SerializeField] private float wallCheckDistance = 0.55f;

        [Tooltip("LayerMask chướng ngại vật chống xuyên tường")]
        [SerializeField] private LayerMask wallObstacleMask = ~0;

        private Vector3 currentWallPosOffset = Vector3.zero;
        private float currentWallPitchOffset = 0f;

        [Header("Tinh chỉnh Lòng Bàn Tay & Cầm Đồ (Palm & Grip Tuning)")]
        [Tooltip("Độ lùi xuống lòng bàn tay (m) để vật phẩm không bị trồi lên mu bàn tay (mặc định 0.035m)")]
        [Range(0.01f, 0.08f)]
        [SerializeField] private float palmDepthOffset = 0.035f;

        [Tooltip("Hệ số gập ngón tay ôm vật phẩm (1.0 = chuẩn theo góc thiết kế)")]
        [Range(0.5f, 1.5f)]
        [SerializeField] private float fingerCurlAngle = 1.0f;

        [Tooltip("Hệ số phóng to máy Radar (mặc định 1.70x để vừa khít lòng bàn tay)")]
        [Range(1.0f, 2.5f)]
        [SerializeField] private float radarScaleMultiplier = 1.70f;

        [Tooltip("Góc xoay ngửa cổ tay khi cầm Radar (độ, mặc định 120°)")]
        [Range(90f, 150f)]
        [SerializeField] private float wristSupinationAngle = 120.0f;

        [Tooltip("Độ dịch chuyển bổ sung của Đèn pin theo palmNormal")]
        [Range(-0.02f, 0.04f)]
        [SerializeField] private float flashlightPalmOffset = 0.010f;
        [Tooltip("Vị trí cục bộ cố định của Radar khi là con trực tiếp của RightHand (tâm lưng máy đặt chuẩn xác giữa lòng bàn tay)")]
        [SerializeField] private Vector3 radarHandLocalPosition = new Vector3(-0.0600f, -0.0580f, -0.0550f);

        [Tooltip("Góc xoay cục bộ cố định của Radar khi là con trực tiếp của RightHand")]
        [SerializeField] private Vector3 radarHandLocalRotation = new Vector3(90.00f, 180.00f, 0.00f);

        [Tooltip("Góc xoay cổ tay IK khi ở Slot 2 để lòng bàn tay hướng về mắt người chơi")]
        [SerializeField] private Vector3 radarWristEulerOffset = new Vector3(-180.00f, 90.00f, 70.00f);

        [Tooltip("Kích thước Radar (giảm 15% so với ban đầu để vừa tầm mắt)")]
        [SerializeField] private float radarScale = 0.72f;

        [Tooltip("Độ dịch chuyển Radar so với tâm lòng bàn tay trong Camera Space: (X: Trái/Phải, Y: Lên/Xuống, Z: Tiến/Lùi)")]
        [SerializeField] private Vector3 radarCameraOffset = new Vector3(-0.045f, 0.025f, -0.055f);

        [Tooltip("Góc xoay ngón cái mở rộng sang phải (+Camera.right) để tựa vào cạnh phải Radar")]
        [SerializeField] private float radarThumbOutwardAngle = 40.0f;

        [Header("Đèn pin chiếu sáng gắn Camera")]
        [Tooltip("Offset cục bộ của đầu đèn Spot Light so với camera")]
        [SerializeField] private Vector3 flashlightHeadLocalOffset = new Vector3(0.070f, -0.025f, 0.270f);

        [Tooltip("Tốc độ Slerp của chùm sáng theo hướng nhìn camera")]
        [SerializeField] private float spotLightFollowSlerpSpeed = 16f;

        [Tooltip("Cường độ ánh sáng đèn pin tối đa")]
        [SerializeField] private float flashlightMaxIntensity = 3.8f;

        [Header("Vị trí tay thả lỏng khi ô trống (Tọa độ cục bộ Player)")]
        [SerializeField] private Vector3 rightIdleLocalPosition = new Vector3(0.24f, 1.28f, 0.32f);
        [SerializeField] private Vector3 rightIdleLocalRotation = new Vector3(15f, -15f, -20f);
        [SerializeField] private Vector3 rightIdleHintPosition = new Vector3(0.38f, 1.35f, -0.10f);

        [SerializeField] private Vector3 leftIdleLocalPosition = new Vector3(-0.24f, 1.28f, 0.32f);
        [SerializeField] private Vector3 leftIdleLocalRotation = new Vector3(15f, 15f, 20f);
        [SerializeField] private Vector3 leftIdleHintPosition = new Vector3(-0.38f, 1.35f, -0.10f);

        // Tham chiếu hệ thống
        private PlayerController playerController;
        private Animator characterAnimator;
        private Camera playerCamera;
        private Transform viewModelRoot;
        private Light fillLight;
        private Light flashlightSpotLight;

        // Xương ngực dao động khi chạy
        private Transform chestBone;
        private Vector3 baseChestLocalPos;

        // Điểm mục tiêu thả lỏng gắn dưới Player
        private Transform idleTargetsRoot;
        private Transform rightIdleTarget;
        private Transform rightIdleHint;
        private Transform leftIdleTarget;
        private Transform leftIdleHint;

        private ItemData currentItemData;
        private HeldItem currentHeldItem;
        private bool isPlayerDead = false;
        private bool resetToOriginalAnimation = false;

        public ArmChain RightArm => rightArm;
        public ArmChain LeftArm => leftArm;
        public Transform HoldSocketR => rightArm.holdSocket;
        public Transform HoldSocketL => leftArm.holdSocket;
        public Transform HandTargetR => rightArm.handTarget;
        public Transform HandTargetL => leftArm.handTarget;
        public ItemData CurrentItemData => currentItemData;
        public Camera PlayerCamera => playerCamera;
        public Light CameraSpotLight => flashlightSpotLight;

        public Vector3 FlashlightHeadLocalOffset
        {
            get => flashlightHeadLocalOffset;
            set => flashlightHeadLocalOffset = value;
        }

        public float FlashlightMaxIntensity
        {
            get => flashlightMaxIntensity;
            set => flashlightMaxIntensity = value;
        }

        public float PalmDepthOffset { get => palmDepthOffset; set => palmDepthOffset = value; }
        public float FingerCurlAngle { get => fingerCurlAngle; set => fingerCurlAngle = value; }
        public float RadarScaleMultiplier { get => radarScaleMultiplier; set => radarScaleMultiplier = value; }
        public float WristSupinationAngle { get => wristSupinationAngle; set => wristSupinationAngle = value; }
        public float FlashlightPalmOffset { get => flashlightPalmOffset; set => flashlightPalmOffset = value; }
        public float RadarPalmOffset { get; set; } = 0.025f;
        public Vector3 RadarHandLocalPosition { get => radarHandLocalPosition; set => radarHandLocalPosition = value; }
        public Vector3 RadarHandLocalRotation { get => radarHandLocalRotation; set => radarHandLocalRotation = value; }
        public Vector3 RadarWristEulerOffset { get => radarWristEulerOffset; set => radarWristEulerOffset = value; }
        public float RadarScale { get => radarScale; set => radarScale = value; }
        public Vector3 RadarCameraOffset { get => radarCameraOffset; set => radarCameraOffset = value; }
        public float RadarThumbOutwardAngle { get => radarThumbOutwardAngle; set => radarThumbOutwardAngle = value; }

        public Vector3 RightIdleLocalPosition
        {
            get => rightIdleLocalPosition;
            set
            {
                rightIdleLocalPosition = value;
                if (rightIdleTarget != null) rightIdleTarget.localPosition = value;
            }
        }

        public Vector3 RightIdleLocalRotation
        {
            get => rightIdleLocalRotation;
            set
            {
                rightIdleLocalRotation = value;
                if (rightIdleTarget != null) rightIdleTarget.localRotation = Quaternion.Euler(value);
            }
        }

        public Vector3 LeftIdleLocalPosition
        {
            get => leftIdleLocalPosition;
            set
            {
                leftIdleLocalPosition = value;
                if (leftIdleTarget != null) leftIdleTarget.localPosition = value;
            }
        }

        public Vector3 LeftIdleLocalRotation
        {
            get => leftIdleLocalRotation;
            set
            {
                leftIdleLocalRotation = value;
                if (leftIdleTarget != null) leftIdleTarget.localRotation = Quaternion.Euler(value);
            }
        }

        public bool ResetToOriginalAnimation
        {
            get => resetToOriginalAnimation;
            set => resetToOriginalAnimation = value;
        }

        void Awake()
        {
            Instance = this;
            playerController = GetComponent<PlayerController>();
            if (playerController != null && playerController.playerCamera != null)
            {
                playerCamera = playerController.playerCamera.GetComponent<Camera>();
            }

            // Đặt nearClipPlane = 0.01f theo yêu cầu để tránh cắt xén cánh tay khi nhìn gần
            if (playerCamera != null)
            {
                playerCamera.nearClipPlane = 0.01f;
            }

            Application.runInBackground = true;

            characterAnimator = GetComponentInChildren<Animator>();
            if (characterAnimator != null)
            {
                characterAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var smrs = characterAnimator.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var smr in smrs)
                {
                    smr.updateWhenOffscreen = true;
                    smr.localBounds = new Bounds(Vector3.zero, new Vector3(4f, 5f, 4f));

                    // Tránh clipping đầu khi cúi nhìn xuống mà vẫn giữ trọn bóng đầu trên tường/mặt đất
                    string mName = smr.gameObject.name.ToLower();
                    if (mName.Contains("head") || mName.Contains("hair") || mName.Contains("eye") || mName.Contains("teeth") || mName.Contains("tongue"))
                    {
                        smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                    }
                    else
                    {
                        smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    }
                }
            }

            InitializeBonesAndSockets();
            InitializeTargetsAndLighting();
            InitializeIdleTargets();
            SetupAnimatorBridge();
        }

        void OnEnable()
        {
            SetupAnimatorBridge();
        }

        void Start()
        {
            var hotbar = GetComponent<HotbarInventory>();
            if (hotbar != null)
            {
                EquipItem(hotbar.CurrentItem, hotbar.CurrentHeldItem);
            }
        }

        private void InitializeBonesAndSockets()
        {
            if (characterAnimator == null) return;

            // 1. Lấy xương cánh tay phải
            rightArm.clavicle = characterAnimator.GetBoneTransform(HumanBodyBones.RightShoulder);
            rightArm.upperarm = characterAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            rightArm.lowerarm = characterAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            rightArm.hand = characterAnimator.GetBoneTransform(HumanBodyBones.RightHand);

            if (rightArm.hand != null)
            {
                Transform existingSocket = rightArm.hand.Find("HoldSocket_R");
                if (existingSocket == null)
                {
                    GameObject sGo = new GameObject("HoldSocket_R");
                    sGo.transform.SetParent(rightArm.hand, false);
                    sGo.transform.localPosition = new Vector3(-0.050f, -0.015f, -0.025f);
                    sGo.transform.localRotation = Quaternion.Euler(0f, 90f, 90f);
                    rightArm.holdSocket = sGo.transform;
                }
                else
                {
                    rightArm.holdSocket = existingSocket;
                }

                // Xương ngón tay phải (trục gập kiểm chứng bằng số)
                rightArm.thumb.joint1 = characterAnimator.GetBoneTransform(HumanBodyBones.RightThumbProximal);
                rightArm.thumb.joint2 = characterAnimator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate);
                rightArm.thumb.joint3 = characterAnimator.GetBoneTransform(HumanBodyBones.RightThumbDistal);
                rightArm.thumb.curlAxis = new Vector3(0f, -1f, 0f); // Trục -Y cho ngón cái

                rightArm.index.joint1 = characterAnimator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
                rightArm.index.joint2 = characterAnimator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate);
                rightArm.index.joint3 = characterAnimator.GetBoneTransform(HumanBodyBones.RightIndexDistal);
                rightArm.index.curlAxis = new Vector3(0f, 0f, -1f);

                rightArm.middle.joint1 = characterAnimator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
                rightArm.middle.joint2 = characterAnimator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate);
                rightArm.middle.joint3 = characterAnimator.GetBoneTransform(HumanBodyBones.RightMiddleDistal);
                rightArm.middle.curlAxis = new Vector3(0f, 0f, -1f);

                rightArm.ring.joint1 = characterAnimator.GetBoneTransform(HumanBodyBones.RightRingProximal);
                rightArm.ring.joint2 = characterAnimator.GetBoneTransform(HumanBodyBones.RightRingIntermediate);
                rightArm.ring.joint3 = characterAnimator.GetBoneTransform(HumanBodyBones.RightRingDistal);
                rightArm.ring.curlAxis = new Vector3(0f, 0f, -1f);

                rightArm.pinky.joint1 = characterAnimator.GetBoneTransform(HumanBodyBones.RightLittleProximal);
                rightArm.pinky.joint2 = characterAnimator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate);
                rightArm.pinky.joint3 = characterAnimator.GetBoneTransform(HumanBodyBones.RightLittleDistal);
                rightArm.pinky.curlAxis = new Vector3(0f, 0f, -1f);

                rightArm.CacheBaseRotations();
            }

            // 2. Lấy xương cánh tay trái
            leftArm.clavicle = characterAnimator.GetBoneTransform(HumanBodyBones.LeftShoulder);
            leftArm.upperarm = characterAnimator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            leftArm.lowerarm = characterAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            leftArm.hand = characterAnimator.GetBoneTransform(HumanBodyBones.LeftHand);

            if (leftArm.hand != null)
            {
                Transform existingSocket = leftArm.hand.Find("HoldSocket_L");
                if (existingSocket == null)
                {
                    GameObject sGo = new GameObject("HoldSocket_L");
                    sGo.transform.SetParent(leftArm.hand, false);
                    sGo.transform.localPosition = new Vector3(-0.050f, -0.015f, 0.025f);
                    sGo.transform.localRotation = Quaternion.Euler(0f, -90f, -90f);
                    leftArm.holdSocket = sGo.transform;
                }
                else
                {
                    leftArm.holdSocket = existingSocket;
                }

                // Xương ngón tay trái
                leftArm.thumb.joint1 = characterAnimator.GetBoneTransform(HumanBodyBones.LeftThumbProximal);
                leftArm.thumb.joint2 = characterAnimator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate);
                leftArm.thumb.joint3 = characterAnimator.GetBoneTransform(HumanBodyBones.LeftThumbDistal);
                leftArm.thumb.curlAxis = new Vector3(0f, 1f, 0f); // Trục +Y cho ngón cái trái

                leftArm.index.joint1 = characterAnimator.GetBoneTransform(HumanBodyBones.LeftIndexProximal);
                leftArm.index.joint2 = characterAnimator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate);
                leftArm.index.joint3 = characterAnimator.GetBoneTransform(HumanBodyBones.LeftIndexDistal);
                leftArm.index.curlAxis = new Vector3(0f, 0f, -1f);

                leftArm.middle.joint1 = characterAnimator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);
                leftArm.middle.joint2 = characterAnimator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate);
                leftArm.middle.joint3 = characterAnimator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal);
                leftArm.middle.curlAxis = new Vector3(0f, 0f, -1f);

                leftArm.ring.joint1 = characterAnimator.GetBoneTransform(HumanBodyBones.LeftRingProximal);
                leftArm.ring.joint2 = characterAnimator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate);
                leftArm.ring.joint3 = characterAnimator.GetBoneTransform(HumanBodyBones.LeftRingDistal);
                leftArm.ring.curlAxis = new Vector3(0f, 0f, -1f);

                leftArm.pinky.joint1 = characterAnimator.GetBoneTransform(HumanBodyBones.LeftLittleProximal);
                leftArm.pinky.joint2 = characterAnimator.GetBoneTransform(HumanBodyBones.LeftLittleIntermediate);
                leftArm.pinky.joint3 = characterAnimator.GetBoneTransform(HumanBodyBones.LeftLittleDistal);
                leftArm.pinky.curlAxis = new Vector3(0f, 0f, -1f);

                leftArm.CacheBaseRotations();
            }

            // 3. Lấy xương ngực để nhận dao động chuyển động khi chạy
            chestBone = characterAnimator.GetBoneTransform(HumanBodyBones.UpperChest);
            if (chestBone == null) chestBone = characterAnimator.GetBoneTransform(HumanBodyBones.Chest);
            if (chestBone != null)
            {
                baseChestLocalPos = transform.InverseTransformPoint(chestBone.position);
            }
        }

        private void InitializeTargetsAndLighting()
        {
            if (playerCamera == null) return;

            Transform camTrans = playerCamera.transform;
            Transform root = camTrans.Find("ViewModelRoot");
            if (root == null)
            {
                GameObject rGo = new GameObject("ViewModelRoot");
                rGo.transform.SetParent(camTrans, false);
                rGo.transform.localPosition = Vector3.zero;
                rGo.transform.localRotation = Quaternion.identity;
                viewModelRoot = rGo.transform;
            }
            else
            {
                viewModelRoot = root;
            }

            // HandTarget_R & ElbowHint_R (con của ViewModelRoot để nhận sway/bobbing/switch)
            Transform targetR = viewModelRoot.Find("HandTarget_R");
            if (targetR == null)
            {
                GameObject tGo = new GameObject("HandTarget_R");
                tGo.transform.SetParent(viewModelRoot, false);
                tGo.transform.localPosition = new Vector3(0.080f, -0.025f, 0.165f);
                tGo.transform.localRotation = Quaternion.Euler(15f, -25f, 25f);
                targetR = tGo.transform;
            }
            rightArm.handTarget = targetR;

            Transform hintR = viewModelRoot.Find("ElbowHint_R");
            if (hintR == null)
            {
                GameObject hGo = new GameObject("ElbowHint_R");
                hGo.transform.SetParent(viewModelRoot, false);
                hGo.transform.localPosition = new Vector3(0.35f, -0.45f, -0.15f);
                hintR = hGo.transform;
            }
            else
            {
                hintR.localPosition = new Vector3(0.35f, -0.45f, -0.15f);
            }
            rightArm.elbowHint = hintR;

            // ViewmodelFill Light (bù sáng nhẹ ban đêm)
            Transform fillTrans = viewModelRoot.Find("ViewmodelFill");
            if (fillTrans == null)
            {
                GameObject fillGo = new GameObject("ViewmodelFill");
                fillGo.transform.SetParent(viewModelRoot, false);
                fillGo.transform.localPosition = new Vector3(0.05f, 0.05f, 0.25f);
                fillLight = fillGo.AddComponent<Light>();
            }
            else
            {
                fillLight = fillTrans.GetComponent<Light>();
            }

            if (fillLight != null)
            {
                fillLight.type = LightType.Point;
                fillLight.color = new Color(1.0f, 0.92f, 0.84f);
                fillLight.range = 0.70f;
                fillLight.intensity = 0f;
                fillLight.enabled = false;
                fillLight.shadows = LightShadows.None;
            }

            // Flashlight Spot Light (chiếu sáng từ đầu đèn gắn theo camera)
            Transform spotTrans = viewModelRoot.Find("Flashlight_CameraSpotLight");
            if (spotTrans == null)
            {
                GameObject spotGo = new GameObject("Flashlight_CameraSpotLight");
                spotGo.transform.SetParent(viewModelRoot, false);
                spotGo.transform.localPosition = flashlightHeadLocalOffset;
                flashlightSpotLight = spotGo.AddComponent<Light>();
            }
            else
            {
                flashlightSpotLight = spotTrans.GetComponent<Light>();
            }

            if (flashlightSpotLight != null)
            {
                flashlightSpotLight.type = LightType.Spot;
                flashlightSpotLight.color = new Color(1.0f, 0.96f, 0.88f);
                flashlightSpotLight.range = 55.0f;
                flashlightSpotLight.spotAngle = 60.0f;
                flashlightSpotLight.innerSpotAngle = 28.0f;
                flashlightSpotLight.shadowNearPlane = 0.25f;
                flashlightSpotLight.intensity = 0f;
                flashlightSpotLight.shadows = LightShadows.Soft;
                flashlightSpotLight.enabled = false;
            }
        }

        private void InitializeIdleTargets()
        {
            // Các điểm mục tiêu thả lỏng gắn trực tiếp dưới Player để giữ vị trí tự nhiên hai bên hông
            Transform root = transform.Find("IdleTargetsRoot");
            if (root == null)
            {
                GameObject rGo = new GameObject("IdleTargetsRoot");
                rGo.transform.SetParent(transform, false);
                rGo.transform.localPosition = Vector3.zero;
                rGo.transform.localRotation = Quaternion.identity;
                idleTargetsRoot = rGo.transform;
            }
            else
            {
                idleTargetsRoot = root;
            }

            // Right Idle
            Transform rT = idleTargetsRoot.Find("HandTarget_R_Idle");
            if (rT == null)
            {
                GameObject go = new GameObject("HandTarget_R_Idle");
                go.transform.SetParent(idleTargetsRoot, false);
                go.transform.localPosition = rightIdleLocalPosition;
                go.transform.localRotation = Quaternion.Euler(rightIdleLocalRotation);
                rT = go.transform;
            }
            rightIdleTarget = rT;

            Transform rH = idleTargetsRoot.Find("ElbowHint_R_Idle");
            if (rH == null)
            {
                GameObject go = new GameObject("ElbowHint_R_Idle");
                go.transform.SetParent(idleTargetsRoot, false);
                go.transform.localPosition = rightIdleHintPosition;
                rH = go.transform;
            }
            rightIdleHint = rH;

            // Left Idle
            Transform lT = idleTargetsRoot.Find("HandTarget_L_Idle");
            if (lT == null)
            {
                GameObject go = new GameObject("HandTarget_L_Idle");
                go.transform.SetParent(idleTargetsRoot, false);
                go.transform.localPosition = leftIdleLocalPosition;
                go.transform.localRotation = Quaternion.Euler(leftIdleLocalRotation);
                lT = go.transform;
            }
            leftIdleTarget = lT;
            leftArm.handTarget = leftIdleTarget;

            Transform lH = idleTargetsRoot.Find("ElbowHint_L_Idle");
            if (lH == null)
            {
                GameObject go = new GameObject("ElbowHint_L_Idle");
                go.transform.SetParent(idleTargetsRoot, false);
                go.transform.localPosition = leftIdleHintPosition;
                lH = go.transform;
            }
            leftIdleHint = lH;
            leftArm.elbowHint = leftIdleHint;
        }

        private void SetupAnimatorBridge()
        {
            if (characterAnimator == null) return;

            var bridge = characterAnimator.GetComponent<ArmIKAnimatorBridge>();
            if (bridge == null)
            {
                bridge = characterAnimator.gameObject.AddComponent<ArmIKAnimatorBridge>();
            }
            bridge.playerArmIK = this;
            bridge.onAnimatorIK = OnAnimatorIKCallback;
        }

        /// <summary>
        /// Trang bị vật phẩm vào tay phải
        /// </summary>
        public void EquipItem(ItemData itemData, HeldItem itemInstance)
        {
            currentItemData = itemData;
            currentHeldItem = itemInstance;

            if (itemData == null || itemInstance == null)
            {
                // Ô trống: Trả cả 2 tay về animation gốc ban đầu của nhân vật
                rightArm.targetWeight = 0f;
                leftArm.targetWeight = 0f;
                SetFlashlightIntensity(0f);
                return;
            }

            // Gắn vật phẩm vào Socket tay phải
            if (itemData.ItemType == ItemType.Radar && rightArm.hand != null)
            {
                // BƯỚC 1: Khóa cứng Radar trực tiếp vào xương RightHand với bộ localPosition và localRotation cố định duy nhất
                itemInstance.transform.SetParent(rightArm.hand, false);
                itemInstance.transform.localPosition = radarHandLocalPosition;
                itemInstance.transform.localRotation = Quaternion.Euler(radarHandLocalRotation);
                itemInstance.transform.localScale = Vector3.one * radarScale;
            }
            else if (rightArm.holdSocket != null)
            {
                itemInstance.transform.SetParent(rightArm.holdSocket, false);

                // Nếu có GripRight, căn chỉnh GripRight khớp hoàn hảo với Socket
                Transform gripRight = itemInstance.transform.Find("GripRight");
                if (gripRight != null)
                {
                    Quaternion invRot = Quaternion.Inverse(gripRight.localRotation);
                    itemInstance.transform.localRotation = invRot * Quaternion.Euler(itemData.GripOffsetRotation);
                    itemInstance.transform.localPosition = -(itemInstance.transform.localRotation * gripRight.localPosition) + itemData.GripOffsetPosition;
                }
                else
                {
                    itemInstance.transform.localPosition = itemData.GripOffsetPosition;
                    itemInstance.transform.localRotation = Quaternion.Euler(itemData.GripOffsetRotation);
                }
            }

            // Cập nhật vị trí mục tiêu IK của tay phải theo ItemData
            if (rightArm.handTarget != null)
            {
                rightArm.handTarget.localPosition = itemData.IkTargetLocalPosition;
                rightArm.handTarget.localRotation = Quaternion.Euler(itemData.IkTargetLocalRotation);
            }
            if (rightArm.elbowHint != null)
            {
                rightArm.elbowHint.localPosition = itemData.ElbowHintLocal;
            }

            bool isSprinting = playerController != null && playerController.IsRunning;
            rightArm.targetWeight = isSprinting ? (itemData.ItemType == ItemType.Radar ? standingIKWeight : sprintIKWeight) : standingIKWeight;
            // Tay trái luôn giữ nguyên animation gốc ban đầu của nhân vật
            leftArm.targetWeight = 0f;
        }

        public void UnequipCurrentItem()
        {
            currentItemData = null;
            currentHeldItem = null;

            // Ô trống: cả 2 tay về animation gốc ban đầu
            rightArm.targetWeight = 0f;
            leftArm.targetWeight = 0f;
            SetFlashlightIntensity(0f);
        }

        public void ConfigureCameraSpotLight(float range, float spotAngle, float innerSpotAngle, Color color, bool shadows)
        {
            if (flashlightSpotLight != null)
            {
                flashlightSpotLight.range = range;
                flashlightSpotLight.spotAngle = spotAngle;
                flashlightSpotLight.innerSpotAngle = innerSpotAngle;
                flashlightSpotLight.color = color;
                flashlightSpotLight.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            }
        }

        public void SetFlashlightIntensity(float intensity)
        {
            if (flashlightSpotLight != null)
            {
                flashlightSpotLight.intensity = intensity;
                flashlightSpotLight.enabled = intensity > 0.001f;
            }
        }

        public void OnPlayerDied()
        {
            isPlayerDead = true;
            rightArm.targetWeight = 0f;
            leftArm.targetWeight = 0f;
            rightArm.currentWeight = 0f;
            leftArm.currentWeight = 0f;

            if (currentHeldItem != null)
            {
                currentHeldItem.gameObject.SetActive(false);
            }
            SetFlashlightIntensity(0f);
        }

        public void OnPlayerRespawned()
        {
            isPlayerDead = false;
            rightArm.currentWeight = 0f;
            leftArm.currentWeight = 0f;
            rightArm.targetWeight = 1f;
            leftArm.targetWeight = 0f;
        }

        public void TriggerThumbPress(bool isRight, float duration = 0.08f, float pressAngle = 25f)
        {
            ArmChain arm = isRight ? rightArm : leftArm;
            if (arm.buttonPressCoroutine != null) StopCoroutine(arm.buttonPressCoroutine);
            arm.buttonPressCoroutine = StartCoroutine(ThumbPressRoutine(arm, duration, pressAngle));
        }

        private IEnumerator ThumbPressRoutine(ArmChain arm, float duration, float pressAngle)
        {
            float halfDur = duration * 0.5f;
            float elapsed = 0f;

            while (elapsed < halfDur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDur);
                arm.thumbExtraAngle = Mathf.Lerp(0f, pressAngle, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDur);
                arm.thumbExtraAngle = Mathf.Lerp(pressAngle, 0f, t);
                yield return null;
            }

            arm.thumbExtraAngle = 0f;
            arm.buttonPressCoroutine = null;
        }

        void Update()
        {
            if (isPlayerDead) return;

            // Chuyển đổi mượt trọng số IK:
            // - Khi đang cầm vật phẩm: đứng yên/đi bộ = standingIKWeight (0.90f), chạy nhanh = sprintIKWeight (0.70f)
            // - Khi chuyển sang ô trống (Slot 3 & 4): hạ targetWeight về 0 trong 0.2s để cánh tay hoàn toàn theo PlayerAnim
            bool isHoldingItem = currentItemData != null && currentItemData.GripType == GripType.OneHandRight;
            float targetR = 0f;
            if (!resetToOriginalAnimation && isHoldingItem)
            {
                bool isSprinting = playerController != null && playerController.IsRunning;
                targetR = isSprinting ? (currentItemData.ItemType == ItemType.Radar ? standingIKWeight : sprintIKWeight) : standingIKWeight;
            }
            float targetL = 0f; // Tay trái luôn giữ nguyên 100% animation gốc của PlayerAnim

            rightArm.currentWeight = Mathf.MoveTowards(rightArm.currentWeight, targetR, Time.deltaTime * weightTransitionSpeed);
            leftArm.currentWeight = Mathf.MoveTowards(leftArm.currentWeight, targetL, Time.deltaTime * weightTransitionSpeed);

            // Tính toán chống xuyên tường bằng SphereCast (bán kính 0.08m, khoảng cách 0.55m)
            Vector3 targetWallPos = Vector3.zero;
            float targetWallPitch = 0f;
            if (enableWallAvoidance && playerCamera != null && isHoldingItem)
            {
                Vector3 origin = playerCamera.transform.position;
                Vector3 dir = playerCamera.transform.forward;
                RaycastHit[] hits = Physics.SphereCastAll(origin, wallSphereRadius, dir, wallCheckDistance, ~LayerMask.GetMask("Ignore Raycast"), QueryTriggerInteraction.Ignore);
                foreach (var wallHit in hits)
                {
                    if (wallHit.collider != null && !wallHit.collider.transform.IsChildOf(transform))
                    {
                        float penetration = 1f - Mathf.Clamp01(wallHit.distance / wallCheckDistance);
                        // Co tay về phía ngực (-Z, -Y) và pitch cổ tay lên (+X)
                        targetWallPos = new Vector3(0f, -penetration * 0.12f, -penetration * 0.20f);
                        targetWallPitch = penetration * 30f;
                        break;
                    }
                }
            }
            currentWallPosOffset = Vector3.Lerp(currentWallPosOffset, targetWallPos, Time.deltaTime * 10f);
            currentWallPitchOffset = Mathf.Lerp(currentWallPitchOffset, targetWallPitch, Time.deltaTime * 10f);

            // Cập nhật vị trí và góc xoay của nguồn sáng Spotlight đèn pin
            if (flashlightSpotLight != null && playerCamera != null)
            {
                flashlightSpotLight.transform.localPosition = flashlightHeadLocalOffset;
                flashlightSpotLight.transform.rotation = Quaternion.Slerp(flashlightSpotLight.transform.rotation, playerCamera.transform.rotation, Time.deltaTime * spotLightFollowSlerpSpeed);
            }
        }

        public void OnAnimatorIKCallback(int layerIndex)
        {
            if (isPlayerDead || resetToOriginalAnimation || characterAnimator == null)
            {
                characterAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
                characterAnimator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
                characterAnimator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 0f);

                characterAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
                characterAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
                characterAnimator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 0f);
                return;
            }

            // 1. Giải IK cho Tay Phải
            if (rightArm.currentWeight > 0.001f)
            {
                bool isHoldingItem = currentItemData != null && currentItemData.GripType == GripType.OneHandRight;
                Transform target = isHoldingItem ? rightArm.handTarget : rightIdleTarget;
                Transform hint = isHoldingItem ? rightArm.elbowHint : rightIdleHint;

                if (target != null)
                {
                    Vector3 targetPos = target.position;

                    // Khi chạy nhanh: Cộng thêm chuyển động dao động từ xương ngực (UpperChest/Chest) và Figure-8 bobbing
                    if (isHoldingItem)
                    {
                        bool isSprinting = playerController != null && playerController.IsRunning;
                        if (chestBone != null)
                        {
                            Vector3 currentChestLocal = transform.InverseTransformPoint(chestBone.position);
                            Vector3 chestDelta = currentChestLocal - baseChestLocalPos;
                            Vector3 chestBobWorld = transform.TransformDirection(chestDelta) * chestBobMultiplier;
                            float runFactor = isSprinting ? 1f : Mathf.Clamp01(playerController != null ? playerController.HorizontalSpeed / Mathf.Max(0.1f, playerController.walkSpeed) : 0f);
                            targetPos += chestBobWorld * runFactor;
                        }

                        if (isSprinting)
                        {
                            // Nhịp Figure-8 bobbing theo bước chạy
                            float figTime = Time.time * 8.0f;
                            float fig8X = Mathf.Sin(figTime * 0.5f) * 0.016f;
                            float fig8Y = Mathf.Sin(figTime) * 0.012f;
                            targetPos += transform.right * fig8X + transform.up * fig8Y;
                        }

                        // Áp dụng chống xuyên tường (co tay lùi lại và hướng lên)
                        if (playerCamera != null)
                        {
                            targetPos += playerCamera.transform.TransformDirection(currentWallPosOffset);
                        }

                        // Khi cúi nhìn xuống: Dịch chuyển HandTarget_R phù hợp với từng loại vật phẩm
                        if (playerCamera != null)
                        {
                            float p = playerCamera.transform.localEulerAngles.x;
                            if (p > 180f) p -= 360f;

                            if (currentItemData.ItemType == ItemType.Flashlight)
                            {
                                // Flashlight (Slot 1): Duỗi tự nhiên theo trục cơ thể, triệt tiêu gãy cổ tay hình chữ Z (GIỮ NGUYÊN 100%)
                                if (p > 15f)
                                {
                                    float downT = Mathf.Clamp01((p - 15f) / 55f);
                                    Transform shoulder = characterAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                                    if (shoulder != null)
                                    {
                                        Vector3 lookdownPos = shoulder.position + transform.right * 0.18f - transform.up * 0.28f + transform.forward * 0.30f;
                                        targetPos = Vector3.Lerp(targetPos, lookdownPos, downT * 0.75f);
                                    }
                                }
                            }
                            else if (currentItemData.ItemType == ItemType.Radar)
                            {
                                // Radar (Slot 2): Giữ vị trí trong camera space góc phần tư dưới-phải (0.18, -0.18, 0.32) để quan sát màn hình rõ ràng
                                if (p > 10f)
                                {
                                    float downT = Mathf.Clamp01((p - 10f) / 50f);
                                    Vector3 lookdownCamLocal = new Vector3(0.18f, -0.18f, 0.32f);
                                    Vector3 lookdownWorldPos = playerCamera.transform.TransformPoint(lookdownCamLocal);
                                    targetPos = Vector3.Lerp(targetPos, lookdownWorldPos, downT);
                                }
                            }
                        }
                    }

                    Transform sh = characterAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                    if (sh != null)
                    {
                        float maxReach = 0.5188f * 0.94f;
                        Vector3 toT = targetPos - sh.position;
                        if (toT.magnitude > maxReach)
                        {
                            targetPos = sh.position + toT.normalized * maxReach;
                        }
                    }

                    characterAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, rightArm.currentWeight);
                    characterAnimator.SetIKPosition(AvatarIKGoal.RightHand, targetPos);

                    Quaternion targetRot = target.rotation;
                    float camPitch = 0f;
                    if (playerCamera != null)
                    {
                        camPitch = playerCamera.transform.localEulerAngles.x;
                        if (camPitch > 180f) camPitch -= 360f;
                    }

                    if (isHoldingItem && playerCamera != null)
                    {
                        // Giới hạn (Clamp) góc Pitch truyền vào IK Target cổ tay
                        float clampedPitch = currentItemData.ItemType == ItemType.Radar
                            ? Mathf.Clamp(camPitch, -45f, 65f)
                            : Mathf.Clamp(camPitch, -35f, 25f);
                        Quaternion camYawOnly = Quaternion.Euler(0f, playerCamera.transform.eulerAngles.y, 0f);
                        Quaternion clampedCamRot = camYawOnly * Quaternion.Euler(clampedPitch, 0f, 0f);

                        if (currentItemData.ItemType == ItemType.Flashlight)
                        {
                            // BƯỚC 1: Xoay lật cổ tay (Roll Offset = -130°) để lòng bàn tay hơi ngửa chéo lên trên,
                            // mu bàn tay quay xuống dưới, 4 ngón tay nâng đỡ phía dưới thân đèn tự nhiên, ngón cái tựa hờ sườn trên
                            targetRot = clampedCamRot * Quaternion.Euler(-12f, -10f, -130f);
                        }
                        else if (currentItemData.ItemType == ItemType.Radar)
                        {
                            // BƯỚC 1: Xoay cổ tay để lòng bàn tay quay hướng về phía mặt người chơi (nghiêng nhẹ lên trên 15-20 độ).
                            // Cả bàn tay và máy Radar di chuyển như một khối thống nhất tuyệt đối, vị trí ngón tay không bao giờ bị xê dịch
                            targetRot = clampedCamRot * Quaternion.Euler(radarWristEulerOffset);
                        }
                        else
                        {
                            targetRot = clampedCamRot * (target != null ? target.localRotation : Quaternion.identity);
                        }
                    }

                    if (isHoldingItem && playerCamera != null && currentWallPitchOffset > 0.01f)
                    {
                        targetRot = Quaternion.AngleAxis(-currentWallPitchOffset, playerCamera.transform.right) * targetRot;
                    }

                    characterAnimator.SetIKRotationWeight(AvatarIKGoal.RightHand, rightArm.currentWeight);
                    characterAnimator.SetIKRotation(AvatarIKGoal.RightHand, targetRot);

                    if (hint != null)
                    {
                        Vector3 hintPos = hint.position;
                        if (isHoldingItem && sh != null)
                        {
                            // BƯỚC 2: Khi cúi nhìn xuống, đưa ElbowHint_R lùi về phía sau-dưới để cánh tay tự nhiên duỗi thẳng
                            float downT = Mathf.Clamp01((camPitch - 15f) / 55f);
                            Vector3 normalElbow = sh.position + transform.right * 0.35f - transform.up * 0.45f - transform.forward * 0.15f;
                            Vector3 downElbow = sh.position + transform.right * 0.40f - transform.up * 0.55f - transform.forward * 0.25f;
                            hintPos = Vector3.Lerp(normalElbow, downElbow, downT);
                        }
                        characterAnimator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, rightArm.currentWeight);
                        characterAnimator.SetIKHintPosition(AvatarIKHint.RightElbow, hintPos);
                    }
                }
            }
            else
            {
                characterAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
                characterAnimator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
                characterAnimator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 0f);
            }

            // 2. Giải IK cho Tay Trái (Luôn đưa về tư thế thả lỏng tự nhiên bên hông)
            if (leftArm.currentWeight > 0.001f && leftIdleTarget != null)
            {
                Vector3 targetPosL = leftIdleTarget.position;
                Transform shL = characterAnimator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                if (shL != null)
                {
                    float maxReach = 0.5200f * 0.94f;
                    Vector3 toT = targetPosL - shL.position;
                    if (toT.magnitude > maxReach)
                    {
                        targetPosL = shL.position + toT.normalized * maxReach;
                    }
                }

                characterAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftArm.currentWeight);
                characterAnimator.SetIKPosition(AvatarIKGoal.LeftHand, targetPosL);

                characterAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, leftArm.currentWeight);
                characterAnimator.SetIKRotation(AvatarIKGoal.LeftHand, leftIdleTarget.rotation);

                if (leftIdleHint != null)
                {
                    characterAnimator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, leftArm.currentWeight);
                    characterAnimator.SetIKHintPosition(AvatarIKHint.LeftElbow, leftIdleHint.position);
                }
            }
            else
            {
                characterAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
                characterAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
                characterAnimator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 0f);
            }
        }

        public struct PalmData
        {
            public Vector3 center;
            public Vector3 normal;
            public Vector3 fingerDir;
            public Vector3 handRight;
        }

        /// <summary>
        /// BƯỚC 1: Tự động tính hệ trục tọa độ và tâm lòng bàn tay phải từ các xương ngón tay
        /// </summary>
        public PalmData CalculatePalmData()
        {
            PalmData data = new PalmData();
            if (characterAnimator == null) return data;

            Transform hand = characterAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            Transform indexKnuckle = characterAnimator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
            Transform middleKnuckle = characterAnimator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
            Transform littleKnuckle = characterAnimator.GetBoneTransform(HumanBodyBones.RightLittleProximal);
            Transform thumbProximal = characterAnimator.GetBoneTransform(HumanBodyBones.RightThumbProximal);

            if (hand == null || middleKnuckle == null || indexKnuckle == null || littleKnuckle == null || thumbProximal == null)
            {
                data.center = hand != null ? hand.position : transform.position;
                data.normal = hand != null ? -hand.up : Vector3.down;
                data.fingerDir = hand != null ? -hand.right : Vector3.forward;
                data.handRight = hand != null ? hand.forward : Vector3.right;
                return data;
            }

            // 1. fingerDir (Hướng dọc theo bàn tay từ cổ tay ra ngón giữa)
            data.fingerDir = (middleKnuckle.position - hand.position).normalized;

            // 2. handRight (Hướng ngang bàn tay từ ngón trỏ sang ngón út)
            data.handRight = (littleKnuckle.position - indexKnuckle.position).normalized;

            // 3. palmNormal (Hướng từ MU BÀN TAY đi thẳng xuống LÒNG BÀN TAY)
            data.normal = Vector3.Cross(data.fingerDir, data.handRight).normalized;
            // Đảm bảo palmNormal luôn chỉ từ MU BÀN TAY xuống LÒNG BÀN TAY (-hand.up), tuyệt đối không chỉ lên mu bàn tay
            if (Vector3.Dot(data.normal, -hand.up) < 0f)
            {
                data.normal = -data.normal;
            }

            // 4. palmCenter (Tâm lòng bàn tay, dịch hẳn xuống dưới bề mặt lòng bàn tay theo palmDepthOffset)
            data.center = Vector3.Lerp(hand.position, middleKnuckle.position, 0.65f) + data.normal * palmDepthOffset;
            return data;
        }

        private float currentFingerWeight = 0f;

        // =====================================================================
        // Radar Finger Pose - Absolute Force Write (Slot 2 Only)
        // Góc Euler cục bộ TUYỆT ĐỐI cho từng đốt ngón tay khi cầm Radar.
        // Ghi đè hoàn toàn animation Humanoid Walk/Run/Idle mỗi frame.
        // =====================================================================
        [Header("Radar Finger Pose - Force Override (Slot 2 Only)")]
        [Tooltip("Euler cục bộ cố định của đốt gốc 4 ngón (Proximal) khi cầm Radar")]
        [SerializeField] private Vector3 radarFingerProximalEuler = new Vector3(8f, 170f, 120f);
        [Tooltip("Euler cục bộ cố định của đốt giữa 4 ngón (Intermediate) khi cầm Radar")]
        [SerializeField] private Vector3 radarFingerIntermediateEuler = new Vector3(0f, 0f, 290f);
        [Tooltip("Euler cục bộ cố định của đốt ngọn 4 ngón (Distal) khi cầm Radar")]
        [SerializeField] private Vector3 radarFingerDistalEuler = new Vector3(0f, 0f, 315f);
        [Tooltip("Euler cục bộ cố định đốt gốc ngón cái (Thumb Proximal) khi cầm Radar")]
        [SerializeField] private Vector3 radarThumbProximalEuler = new Vector3(60f, 25f, 40f);
        [Tooltip("Euler cục bộ cố định đốt giữa ngón cái (Thumb Intermediate) khi cầm Radar")]
        [SerializeField] private Vector3 radarThumbIntermediateEuler = new Vector3(0f, 0f, 320f);
        [Tooltip("Euler cục bộ cố định đốt ngọn ngón cái (Thumb Distal) khi cầm Radar")]
        [SerializeField] private Vector3 radarThumbDistalEuler = new Vector3(0f, 0f, 320f);

        /// <summary>
        /// BƯỚC 2: Viết hàm nắm ngón tay tự động trong LateUpdate (Procedural Finger Grip).
        /// Đối với Radar (Slot 2): FORCE WRITE - gán trực tiếp localRotation = Quaternion.Euler(cố định)
        /// mỗi frame, hoàn toàn bỏ qua animation Humanoid Walk/Run.
        /// Đối với Flashlight (Slot 1): Giữ nguyên 100% cơ chế multiplicative cũ.
        /// </summary>
        private void ApplyProceduralFingerGrip()
        {
            bool isHolding = currentItemData != null && currentItemData.GripType == GripType.OneHandRight && !resetToOriginalAnimation && !isPlayerDead;
            // Khi ở Slot 2 (Radar), yêu cầu khóa cứng 100% ngón tay (Weight = 1.0f)
            float targetWeight = isHolding ? (currentItemData.ItemType == ItemType.Radar ? 1.0f : rightArm.currentWeight) : 0f;
            currentFingerWeight = Mathf.MoveTowards(currentFingerWeight, targetWeight, Time.deltaTime * weightTransitionSpeed * 2.5f);

            if (currentFingerWeight <= 0.001f) return;

            Vector3 fingerAxis = new Vector3(0f, 0f, -1f);
            Vector3 thumbAxis = new Vector3(0f, -1f, 0f);

            if (currentItemData != null && currentItemData.ItemType == ItemType.Flashlight)
            {
                // === FLASHLIGHT (Slot 1) - GIỮ NGUYÊN 100% cơ chế multiplicative cũ ===
                float f1 = 45f * fingerCurlAngle;
                float f2 = 55f * fingerCurlAngle;
                float f3 = 40f * fingerCurlAngle;
                float t1 = 15f * fingerCurlAngle;
                float t2 = 0f;
                float t3 = 10f * fingerCurlAngle;

                ApplyFingerJoints(rightArm.index, f1, f2, f3, fingerAxis, currentFingerWeight);
                ApplyFingerJoints(rightArm.middle, f1, f2, f3, fingerAxis, currentFingerWeight);
                ApplyFingerJoints(rightArm.ring, f1, f2, f3, fingerAxis, currentFingerWeight);
                ApplyFingerJoints(rightArm.pinky, f1, f2, f3, fingerAxis, currentFingerWeight);
                ApplyFingerJoints(rightArm.thumb, t1 + rightArm.thumbExtraAngle, t2, t3, thumbAxis, currentFingerWeight);
            }
            else if (currentItemData != null && currentItemData.ItemType == ItemType.Radar)
            {
                // === RADAR (Slot 2) - FORCE WRITE ABSOLUTE ===
                // Gán trực tiếp localRotation = Quaternion.Euler(giá trị cố định) cho từng đốt xương.
                // Animation Humanoid Walk/Run/Idle KHÔNG THỂ ghi đè vì LateUpdate chạy SAU Animator.

                // 4 ngón tay: Ôm mặt lưng thân máy, gập nhẹ, KHÔNG vòng ra mặt kính
                ForceWriteFingerPose(rightArm.index,  radarFingerProximalEuler, radarFingerIntermediateEuler, radarFingerDistalEuler, currentFingerWeight);
                ForceWriteFingerPose(rightArm.middle, radarFingerProximalEuler, radarFingerIntermediateEuler, radarFingerDistalEuler, currentFingerWeight);
                ForceWriteFingerPose(rightArm.ring,   radarFingerProximalEuler, radarFingerIntermediateEuler, radarFingerDistalEuler, currentFingerWeight);
                ForceWriteFingerPose(rightArm.pinky,  radarFingerProximalEuler, radarFingerIntermediateEuler, radarFingerDistalEuler, currentFingerWeight);

                // Ngón cái: Duỗi thẳng dọc theo cạnh sườn bên phải của vỏ hộp Radar
                Vector3 thumbP = radarThumbProximalEuler;
                thumbP.y += rightArm.thumbExtraAngle; // Cộng thêm thumbExtraAngle khi bấm nút
                ForceWriteFingerPose(rightArm.thumb, thumbP, radarThumbIntermediateEuler, radarThumbDistalEuler, currentFingerWeight);
            }
        }

        /// <summary>
        /// FORCE WRITE: Gán trực tiếp localRotation = Quaternion.Euler(euler) cho từng đốt xương.
        /// Khi weight >= 0.85f (gần tối đa), gán trực tiếp 100% để ngăn chặn hoàn toàn animation humanoid rò rỉ.
        /// </summary>
        private void ForceWriteFingerPose(FingerBoneChain chain, Vector3 euler1, Vector3 euler2, Vector3 euler3, float weight)
        {
            if (chain == null) return;
            if (chain.joint1 != null)
            {
                Quaternion target = Quaternion.Euler(euler1);
                chain.joint1.localRotation = weight >= 0.85f ? target : Quaternion.Slerp(chain.joint1.localRotation, target, weight);
            }
            if (chain.joint2 != null)
            {
                Quaternion target = Quaternion.Euler(euler2);
                chain.joint2.localRotation = weight >= 0.85f ? target : Quaternion.Slerp(chain.joint2.localRotation, target, weight);
            }
            if (chain.joint3 != null)
            {
                Quaternion target = Quaternion.Euler(euler3);
                chain.joint3.localRotation = weight >= 0.85f ? target : Quaternion.Slerp(chain.joint3.localRotation, target, weight);
            }
        }

        /// <summary>
        /// Cơ chế multiplicative cũ - CHỈ dùng cho Flashlight (Slot 1). GIỮ NGUYÊN 100%.
        /// </summary>
        private void ApplyFingerJoints(FingerBoneChain chain, float a1, float a2, float a3, Vector3 axis, float weight)
        {
            if (chain == null) return;
            if (chain.joint1 != null) chain.joint1.localRotation *= Quaternion.AngleAxis(a1 * weight, axis);
            if (chain.joint2 != null) chain.joint2.localRotation *= Quaternion.AngleAxis(a2 * weight, axis);
            if (chain.joint3 != null) chain.joint3.localRotation *= Quaternion.AngleAxis(a3 * weight, axis);
        }

        /// <summary>
        /// BƯỚC 3 & 4: Định vị chính xác vật phẩm vào lòng bàn tay
        /// </summary>
        private void UpdateHeldItemTransform()
        {
            if (currentHeldItem == null || currentItemData == null || isPlayerDead) return;

            PalmData palm = CalculatePalmData();

            // Đồng bộ vị trí holdSocket về tâm lòng bàn tay (chỉ áp dụng cho Đèn pin / các vật phẩm khác)
            if (rightArm.holdSocket != null && currentItemData.ItemType != ItemType.Radar)
            {
                rightArm.holdSocket.position = palm.center;
            }

            if (currentItemData.ItemType == ItemType.Flashlight)
            {
                // Đèn Pin (Slot 1) - Tỷ lệ chuẩn theo ảnh mẫu (GIỮ NGUYÊN 100%)
                currentHeldItem.transform.localScale = new Vector3(0.82f, 0.82f, 1.25f);

                if (playerCamera != null)
                {
                    // Chùm sáng luôn hướng thẳng về phía trước theo camera, hơi hội tụ nhẹ về tâm ngắm (crosshair)
                    Vector3 beamDir = (playerCamera.transform.forward - playerCamera.transform.right * 0.02f).normalized;
                    currentHeldItem.transform.rotation = Quaternion.LookRotation(beamDir, playerCamera.transform.up);
                }

                // Gốc thân đèn nằm trong lòng bàn tay, 4 ngón tay nâng đỡ phía dưới, thân đèn hướng thẳng
                currentHeldItem.transform.position = palm.center + palm.normal * 0.012f + playerCamera.transform.up * 0.018f - currentHeldItem.transform.forward * 0.085f;
            }
            else if (currentItemData.ItemType == ItemType.Radar)
            {
                // BƯỚC 1 & 3: Radar là con trực tiếp của RightHand, KHÓA CỨNG thành một khối thống nhất tuyệt đối
                // Cả bàn tay và máy Radar di chuyển cùng hệ quy chiếu, triệt tiêu 100% rung lắc lệch pha khi chạy (Bobbing / Sway)
                if (currentHeldItem.transform.parent != rightArm.hand && rightArm.hand != null)
                {
                    currentHeldItem.transform.SetParent(rightArm.hand, false);
                }
                currentHeldItem.transform.localPosition = radarHandLocalPosition;
                currentHeldItem.transform.localRotation = Quaternion.Euler(radarHandLocalRotation);
                currentHeldItem.transform.localScale = Vector3.one * radarScale;
            }
        }

        void LateUpdate()
        {
            if (isPlayerDead || resetToOriginalAnimation) return;

            // 1. Áp độ gập ngón tay thủ tục theo từng loại vật phẩm
            ApplyProceduralFingerGrip();

            // 2. Định vị vật phẩm cầm tay chính xác theo tâm lòng bàn tay
            UpdateHeldItemTransform();

            // 3. Radar Slot 2: Đảm bảo vị trí cục bộ và góc xoay cố định trên RightHand ở cuối LateUpdate
            if (currentItemData != null && currentItemData.ItemType == ItemType.Radar && currentHeldItem != null && rightArm.hand != null)
            {
                if (currentHeldItem.transform.parent != rightArm.hand)
                {
                    currentHeldItem.transform.SetParent(rightArm.hand, false);
                }
                currentHeldItem.transform.localPosition = radarHandLocalPosition;
                currentHeldItem.transform.localRotation = Quaternion.Euler(radarHandLocalRotation);
            }
        }
    }
}
