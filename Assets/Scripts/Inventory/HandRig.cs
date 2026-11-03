using System;
using System.Collections;
using UnityEngine;

namespace HorrorGame.Inventory
{
    /// <summary>
    /// Điều khiển hệ thống bàn tay viewmodel của nhân vật Player (khớp ngón tay, tư thế HandPose, cử động ngón cái, bám điểm nắm Grip, TwoBoneIK)
    /// </summary>
    public class HandRig : MonoBehaviour
    {
        [Serializable]
        public class FingerJoints
        {
            [Tooltip("Đốt 1 (khớp gốc)")]
            public Transform joint1;
            [Tooltip("Đốt 2 (khớp giữa)")]
            public Transform joint2;
            [Tooltip("Đốt 3 (khớp đầu ngón)")]
            public Transform joint3;

            [Tooltip("Trục xoay cuộn ngón")]
            public Vector3 curlAxis = new Vector3(0f, 0f, -1f);

            [Tooltip("Trục xoay xòe/khép")]
            public Vector3 spreadAxis = Vector3.up;

            [HideInInspector] public Quaternion baseRot1;
            [HideInInspector] public Quaternion baseRot2;
            [HideInInspector] public Quaternion baseRot3;

            public void CacheBaseRotations()
            {
                if (joint1 != null) baseRot1 = joint1.localRotation;
                if (joint2 != null) baseRot2 = joint2.localRotation;
                if (joint3 != null) baseRot3 = joint3.localRotation;
            }

            public void ApplyPose(FingerPose pose, bool isThumb = false, float extraCurl = 0f)
            {
                float curl1 = pose.joint1 + extraCurl;
                float curl2 = pose.joint2 + extraCurl * 0.8f;
                float curl3 = pose.joint3 + extraCurl * 0.6f;

                if (joint1 != null)
                {
                    joint1.localRotation = baseRot1 * Quaternion.AngleAxis(curl1, curlAxis) * Quaternion.AngleAxis(pose.spread, spreadAxis);
                }
                if (joint2 != null)
                {
                    joint2.localRotation = baseRot2 * Quaternion.AngleAxis(curl2, curlAxis);
                }
                if (!isThumb && joint3 != null)
                {
                    joint3.localRotation = baseRot3 * Quaternion.AngleAxis(curl3, curlAxis);
                }
            }
        }

        [Serializable]
        public class SingleHand
        {
            public string handName = "Hand";
            public GameObject handRoot;
            public Transform wrist;
            public Transform palm;
            public Transform forearm;
            public Transform sleeve;

            [Header("Xương giải phẫu SkinnedMesh (Humanoid Arms)")]
            public Transform clavicle;
            public Transform upperarm;
            public Transform lowerarm;
            public Transform handBone;
            public Renderer handRenderer; // SkinnedMeshRenderer (hand_right hoặc hand_left)
            public Vector3 elbowHintOffset = new Vector3(0.15f, -0.25f, -0.10f);

            [Header("Các ngón tay")]
            public FingerJoints thumb = new FingerJoints();
            public FingerJoints index = new FingerJoints();
            public FingerJoints middle = new FingerJoints();
            public FingerJoints ring = new FingerJoints();
            public FingerJoints pinky = new FingerJoints();

            [HideInInspector] public HandPose currentPose;
            [HideInInspector] public HandPose targetPose;
            [HideInInspector] public float thumbExtraCurl = 0f;
            [HideInInspector] public Coroutine buttonPressCoroutine;

            public void CacheBaseRotations()
            {
                thumb.CacheBaseRotations();
                index.CacheBaseRotations();
                middle.CacheBaseRotations();
                ring.CacheBaseRotations();
                pinky.CacheBaseRotations();
            }

            public void SetActive(bool active)
            {
                if (handRoot != null) handRoot.SetActive(active);
                if (handRenderer != null) handRenderer.gameObject.SetActive(active);
            }

            public bool IsActive => (handRenderer != null && handRenderer.gameObject.activeSelf) || (handRoot != null && handRoot.activeSelf);

            public void ApplyCurrentPose()
            {
                if (currentPose == null) return;
                thumb.ApplyPose(currentPose.thumb, true, thumbExtraCurl);
                index.ApplyPose(currentPose.index);
                middle.ApplyPose(currentPose.middle);
                ring.ApplyPose(currentPose.ring);
                pinky.ApplyPose(currentPose.pinky);

                if (wrist != null)
                {
                    wrist.localRotation = Quaternion.Euler(currentPose.wristRotationOffset);
                }
            }
        }

        [Header("Bàn tay Phải & Trái (Player Character Arms)")]
        [Tooltip("Cấu trúc bàn tay phải")]
        [SerializeField] private SingleHand rightHand = new SingleHand { handName = "RightHand" };

        [Tooltip("Cấu trúc bàn tay trái")]
        [SerializeField] private SingleHand leftHand = new SingleHand { handName = "LeftHand" };

        [Tooltip("Renderer áo chung (shirt_t)")]
        [SerializeField] private Renderer sharedSleeve;

        [Header("Tùy chọn hiển thị")]
        [Tooltip("Hiện tay phải ở tư thế nghỉ khi cầm Radar bằng tay trái")]
        [SerializeField] private bool rightHandIdleWithRadar = false;

        [Tooltip("Hiện cả hai tay ở tư thế xòe thả lỏng khi ô hotbar trống")]
        [SerializeField] private bool showIdleHandsWhenEmpty = false;

        [Header("Tư thế dự phòng mặc định")]
        [Tooltip("Tư thế cầm trụ (GripCylinder)")]
        [SerializeField] private HandPose defaultGripPose;

        [Tooltip("Tư thế xòe thả lỏng (OpenRelaxed)")]
        [SerializeField] private HandPose defaultRelaxedPose;

        private Transform currentGripR;
        private Transform currentGripL;
        private Vector3 currentOffsetR;
        private Vector3 currentOffsetL;

        public SingleHand RightHand => rightHand;
        public SingleHand LeftHand => leftHand;
        public Renderer SharedSleeve { get => sharedSleeve; set => sharedSleeve = value; }
        public bool RightHandIdleWithRadar { get => rightHandIdleWithRadar; set => rightHandIdleWithRadar = value; }
        public bool ShowIdleHandsWhenEmpty { get => showIdleHandsWhenEmpty; set => showIdleHandsWhenEmpty = value; }

        void Awake()
        {
            rightHand.CacheBaseRotations();
            leftHand.CacheBaseRotations();
        }

        /// <summary>
        /// Gắn bàn tay vào điểm nắm (Grip) của vật phẩm
        /// </summary>
        public void AttachHandsToItem(ItemData itemData, Transform itemTransform)
        {
            if (itemData == null || itemTransform == null)
            {
                HandleEmptyHands();
                return;
            }

            Transform gripR = itemTransform.Find("GripRight");
            Transform gripL = itemTransform.Find("GripLeft");

            HandPose poseR = itemData.RightPose != null ? itemData.RightPose : defaultGripPose;
            HandPose poseL = itemData.LeftPose != null ? itemData.LeftPose : defaultGripPose;

            switch (itemData.GripType)
            {
                case GripType.OneHandRight:
                    AttachHand(rightHand, gripR, itemTransform, poseR, itemData.RightGripOffset, true);
                    leftHand.SetActive(false);
                    currentGripL = null;
                    break;

                case GripType.OneHandLeft:
                    AttachHand(leftHand, gripL, itemTransform, poseL, itemData.LeftGripOffset, true);
                    if (rightHandIdleWithRadar)
                    {
                        AttachHand(rightHand, null, itemTransform, defaultRelaxedPose, Vector3.zero, true);
                    }
                    else
                    {
                        rightHand.SetActive(false);
                        currentGripR = null;
                    }
                    break;

                case GripType.TwoHands:
                    AttachHand(rightHand, gripR, itemTransform, poseR, itemData.RightGripOffset, true);
                    AttachHand(leftHand, gripL, itemTransform, poseL, itemData.LeftGripOffset, true);
                    break;

                case GripType.None:
                default:
                    HandleEmptyHands();
                    break;
            }

            UpdateSleeveVisibility();
        }

        private void AttachHand(SingleHand hand, Transform grip, Transform itemTransform, HandPose pose, Vector3 offset, bool active)
        {
            if (hand == null) return;

            hand.SetActive(active);
            if (!active)
            {
                if (hand == rightHand) currentGripR = null;
                else currentGripL = null;
                return;
            }

            Transform target = grip != null ? grip : itemTransform;
            if (hand == rightHand)
            {
                currentGripR = target;
                currentOffsetR = offset;
            }
            else
            {
                currentGripL = target;
                currentOffsetL = offset;
            }

            hand.currentPose = pose;
            hand.ApplyCurrentPose();
        }

        void LateUpdate()
        {
            // 1. Tay phải
            if (rightHand != null && rightHand.IsActive && currentGripR != null)
            {
                Vector3 targetPos = currentGripR.position + currentGripR.TransformDirection(currentOffsetR);
                Quaternion targetRot = currentGripR.rotation;

                if (rightHand.upperarm != null && rightHand.lowerarm != null && rightHand.handBone != null)
                {
                    Vector3 hint = rightHand.upperarm.position + transform.TransformDirection(rightHand.elbowHintOffset);
                    SolveTwoBoneIK(rightHand.upperarm, rightHand.lowerarm, rightHand.handBone, targetPos, targetRot, hint, false);
                }
                else if (rightHand.handRoot != null)
                {
                    rightHand.handRoot.transform.position = targetPos;
                    rightHand.handRoot.transform.rotation = targetRot;
                }

                rightHand.ApplyCurrentPose();
            }

            // 2. Tay trái
            if (leftHand != null && leftHand.IsActive && currentGripL != null)
            {
                Vector3 targetPos = currentGripL.position + currentGripL.TransformDirection(currentOffsetL);
                Quaternion targetRot = currentGripL.rotation;

                if (leftHand.upperarm != null && leftHand.lowerarm != null && leftHand.handBone != null)
                {
                    Vector3 hint = leftHand.upperarm.position + transform.TransformDirection(leftHand.elbowHintOffset);
                    SolveTwoBoneIK(leftHand.upperarm, leftHand.lowerarm, leftHand.handBone, targetPos, targetRot, hint, true);
                }
                else if (leftHand.handRoot != null)
                {
                    leftHand.handRoot.transform.position = targetPos;
                    leftHand.handRoot.transform.rotation = targetRot;
                }

                leftHand.ApplyCurrentPose();
            }
        }

        private void UpdateSleeveVisibility()
        {
            if (sharedSleeve != null)
            {
                bool anyHandActive = (rightHand != null && rightHand.IsActive) || (leftHand != null && leftHand.IsActive);
                sharedSleeve.gameObject.SetActive(anyHandActive);
            }
        }

        public void HandleEmptyHands()
        {
            if (showIdleHandsWhenEmpty && defaultRelaxedPose != null)
            {
                rightHand.SetActive(true);
                leftHand.SetActive(true);
                rightHand.currentPose = defaultRelaxedPose;
                leftHand.currentPose = defaultRelaxedPose;
                rightHand.ApplyCurrentPose();
                leftHand.ApplyCurrentPose();
            }
            else
            {
                rightHand.SetActive(false);
                leftHand.SetActive(false);
                currentGripR = null;
                currentGripL = null;
            }
            UpdateSleeveVisibility();
        }

        /// <summary>
        /// Thuật toán giải động học ngược TwoBoneIK cho cánh tay giải phẫu
        /// </summary>
        public static void SolveTwoBoneIK(Transform upper, Transform lower, Transform hand, Vector3 targetPos, Quaternion targetRot, Vector3 hintPos, bool isLeft)
        {
            if (upper == null || lower == null || hand == null) return;

            Vector3 a = upper.position;
            Vector3 b = lower.position;

            float lab = Vector3.Distance(a, b);
            float lbc = Vector3.Distance(b, hand.position);
            float lac = Mathf.Clamp(Vector3.Distance(a, targetPos), 0.05f, lab + lbc - 0.002f);

            float cosA = Mathf.Clamp((lab * lab + lac * lac - lbc * lbc) / (2f * lab * lac), -1f, 1f);
            float angleA = Mathf.Acos(cosA) * Mathf.Rad2Deg;

            Vector3 targetDir = (targetPos - a).normalized;
            Vector3 hintDir = (hintPos - a).normalized;
            Vector3 planeNormal = Vector3.Cross(targetDir, hintDir).normalized;
            if (planeNormal.sqrMagnitude < 0.001f) planeNormal = isLeft ? -Vector3.right : Vector3.right;

            float sign = isLeft ? 1f : -1f;
            Vector3 armDir = Quaternion.AngleAxis(sign * angleA, planeNormal) * targetDir;
            Vector3 desiredMid = a + armDir * lab;

            Quaternion rotA = Quaternion.FromToRotation(lower.position - a, desiredMid - a);
            upper.rotation = rotA * upper.rotation;

            Quaternion rotB = Quaternion.FromToRotation(hand.position - lower.position, targetPos - lower.position);
            lower.rotation = rotB * lower.rotation;

            hand.position = targetPos;
            hand.rotation = targetRot;
        }

        /// <summary>
        /// Kích hoạt cử động ấn nút của ngón cái tay phải (ví dụ bật đèn pin)
        /// </summary>
        public void TriggerRightThumbPress(float duration = 0.08f, float pressAmount = 25f)
        {
            if (rightHand.buttonPressCoroutine != null) StopCoroutine(rightHand.buttonPressCoroutine);
            rightHand.buttonPressCoroutine = StartCoroutine(ThumbPressRoutine(rightHand, duration, pressAmount));
        }

        /// <summary>
        /// Kích hoạt cử động ấn nút của ngón cái tay trái (ví dụ quét radar)
        /// </summary>
        public void TriggerLeftThumbPress(float duration = 0.08f, float pressAmount = 25f)
        {
            if (leftHand.buttonPressCoroutine != null) StopCoroutine(leftHand.buttonPressCoroutine);
            leftHand.buttonPressCoroutine = StartCoroutine(ThumbPressRoutine(leftHand, duration, pressAmount));
        }

        private IEnumerator ThumbPressRoutine(SingleHand hand, float duration, float pressAmount)
        {
            float halfDuration = duration * 0.5f;
            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                hand.thumbExtraCurl = Mathf.Lerp(0f, pressAmount, t);
                hand.ApplyCurrentPose();
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                hand.thumbExtraCurl = Mathf.Lerp(pressAmount, 0f, t);
                hand.ApplyCurrentPose();
                yield return null;
            }

            hand.thumbExtraCurl = 0f;
            hand.ApplyCurrentPose();
            hand.buttonPressCoroutine = null;
        }

        public void HideAllHands()
        {
            rightHand.SetActive(false);
            leftHand.SetActive(false);
            currentGripR = null;
            currentGripL = null;
            UpdateSleeveVisibility();
        }
    }
}
