using UnityEngine;
using UnityEngine.InputSystem;

namespace HorrorGame.Inventory
{
    /// <summary>
    /// Điều khiển chuyển động mô hình cầm tay (Viewmodel Sway, Bobbing, Wall Avoidance)
    /// </summary>
    public class ViewModelSway : MonoBehaviour
    {
        [Header("Vị trí & Góc xoay cơ bản")]
        [Tooltip("Vị trí mặc định trên tay")]
        [SerializeField] private Vector3 defaultPosition = new Vector3(0.22f, -0.20f, 0.42f);

        [Tooltip("Góc xoay mặc định trên tay (Euler angles)")]
        [SerializeField] private Vector3 defaultRotation = new Vector3(0f, 0f, 0f);

        [Header("Mouse Sway (Quay chuột lắc model)")]
        [Tooltip("Hệ số lắc theo vị trí")]
        [SerializeField] private float swayAmount = 0.015f;

        [Tooltip("Hệ số lắc theo góc xoay")]
        [SerializeField] private float swayRotationAmount = 1.8f;

        [Tooltip("Giới hạn dịch chuyển lắc tối đa (mét)")]
        [SerializeField] private float maxSwayOffset = 0.04f;

        [Tooltip("Thời gian làm mượt lắc (SmoothDamp)")]
        [SerializeField] private float swaySmoothTime = 0.08f;

        [Header("Bobbing (Bước đi & Nhịp thở)")]
        [Tooltip("Tần số nhịp thở khi đứng yên")]
        [SerializeField] private float idleBreathSpeed = 1.2f;

        [Tooltip("Biên độ nhịp thở khi đứng yên")]
        [SerializeField] private float idleBreathAmount = 0.003f;

        [Tooltip("Tần số nhún bước đi")]
        [SerializeField] private float walkBobSpeed = 8.0f;

        [Tooltip("Biên độ nhún bước đi (dọc)")]
        [SerializeField] private float walkBobAmountY = 0.012f;

        [Tooltip("Biên độ lắc bước đi (ngang)")]
        [SerializeField] private float walkBobAmountX = 0.008f;

        [Tooltip("Hệ số nhân khi chạy nhanh")]
        [SerializeField] private float runBobMultiplier = 1.6f;

        [Tooltip("Góc nghiêng về trước khi chạy nhanh")]
        [SerializeField] private float runTiltForward = 4.0f;

        [Header("Airborne (Trên không / Rơi)")]
        [Tooltip("Độ hạ thấp model khi ở trên không")]
        [SerializeField] private float jumpDipAmount = 0.025f;

        [Header("Wall Avoidance (Chống xuyên tường)")]
        [Tooltip("Kích hoạt chống xuyên tường")]
        [SerializeField] private bool enableWallAvoidance = true;

        [Tooltip("Khoảng cách bắt đầu kéo lùi đèn khi lại gần tường (mét)")]
        [SerializeField] private float wallCheckDistance = 0.65f;

        [Tooltip("Độ lùi về sau tối đa khi sát tường (mét)")]
        [SerializeField] private float maxWallRetractZ = 0.22f;

        [Tooltip("Độ hạ xuống dưới tối đa khi sát tường (mét)")]
        [SerializeField] private float maxWallRetractY = 0.12f;

        [Tooltip("LayerMask chướng ngại vật chống xuyên tường (bỏ qua Player và Ma)")]
        [SerializeField] private LayerMask obstacleLayerMask = ~0;

        // Vận tốc SmoothDamp
        private Vector3 posVelocity;
        private Vector3 rotVelocity;

        private Vector3 currentPosOffset;
        private Vector3 currentRotOffset;

        private float bobTimer = 0f;
        private PlayerController playerController;
        private Transform cameraTransform;

        // Recoil tạm thời (ví dụ khi click bật đèn)
        private Vector3 recoilPosOffset;
        private Vector3 recoilRotOffset;

        public Vector3 DefaultPosition
        {
            get => defaultPosition;
            set => defaultPosition = value;
        }

        public Vector3 DefaultRotation
        {
            get => defaultRotation;
            set => defaultRotation = value;
        }

        /// <summary>
        /// Offset hạ thấp khi đổi ô vật phẩm
        /// </summary>
        public Vector3 SwitchOffset { get; set; } = Vector3.zero;

        void Awake()
        {
            playerController = GetComponentInParent<PlayerController>();
            if (playerController != null && playerController.playerCamera != null)
            {
                cameraTransform = playerController.playerCamera;
            }
            else
            {
                Camera cam = GetComponentInParent<Camera>();
                if (cam != null) cameraTransform = cam.transform;
            }

            // Loại bỏ Layer của Player (thường là 0 hoặc player layer) và Ma khỏi layerMask
            obstacleLayerMask &= ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
            int playerLayer = playerController != null ? playerController.gameObject.layer : -1;
            if (playerLayer >= 0) obstacleLayerMask &= ~(1 << playerLayer);
        }

        void OnEnable()
        {
            transform.localPosition = defaultPosition;
            transform.localRotation = Quaternion.Euler(defaultRotation);
            currentPosOffset = Vector3.zero;
            currentRotOffset = Vector3.zero;
            posVelocity = Vector3.zero;
            rotVelocity = Vector3.zero;
            recoilPosOffset = Vector3.zero;
            recoilRotOffset = Vector3.zero;
        }

        public void ApplyClickRecoil(float posRecoil = 0.015f, float rotRecoil = 2.0f)
        {
            recoilPosOffset += new Vector3(0f, -posRecoil * 0.4f, -posRecoil);
            recoilRotOffset += new Vector3(-rotRecoil, 0f, 0f);
        }

        void Update()
        {
            if (Time.timeScale <= 0f) return;

            // 1. Mouse Sway
            Vector3 targetSwayPos = Vector3.zero;
            Vector3 targetSwayRot = Vector3.zero;

            if (Mouse.current != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                float mouseX = Mathf.Clamp(mouseDelta.x * swayAmount, -maxSwayOffset, maxSwayOffset);
                float mouseY = Mathf.Clamp(mouseDelta.y * swayAmount, -maxSwayOffset, maxSwayOffset);

                targetSwayPos = new Vector3(-mouseX, -mouseY, 0f);
                targetSwayRot = new Vector3(mouseDelta.y * swayRotationAmount * 0.05f, -mouseDelta.x * swayRotationAmount * 0.05f, mouseDelta.x * swayRotationAmount * 0.03f);
            }

            // 2. Bobbing (Bước chân & Nhịp thở)
            Vector3 bobPos = Vector3.zero;
            Vector3 bobRot = Vector3.zero;

            float moveSpeed = 0f;
            bool isGrounded = true;
            bool isRunning = false;

            if (playerController != null)
            {
                moveSpeed = playerController.HorizontalSpeed;
                isGrounded = playerController.IsGrounded;
                isRunning = playerController.IsRunning;
            }

            if (isGrounded)
            {
                if (moveSpeed > 0.15f)
                {
                    float speedMultiplier = isRunning ? runBobMultiplier : 1.0f;
                    bobTimer += Time.deltaTime * walkBobSpeed * (moveSpeed / 3.5f) * speedMultiplier;

                    float sinCurve = Mathf.Sin(bobTimer);
                    float cosCurve = Mathf.Cos(bobTimer * 0.5f);

                    float yBob = -Mathf.Abs(sinCurve) * walkBobAmountY * speedMultiplier;
                    float xBob = cosCurve * walkBobAmountX * speedMultiplier;

                    bobPos = new Vector3(xBob, yBob, 0f);
                    bobRot = new Vector3(-Mathf.Abs(sinCurve) * 1.5f + (isRunning ? runTiltForward : 0f), cosCurve * 1.2f, -cosCurve * 1.5f);
                }
                else
                {
                    // Đứng yên: nhịp thở nhẹ nhàng
                    bobTimer += Time.deltaTime * idleBreathSpeed;
                    float breathSin = Mathf.Sin(bobTimer);
                    bobPos = new Vector3(0f, breathSin * idleBreathAmount, 0f);
                    bobRot = new Vector3(breathSin * 0.5f, 0f, 0f);
                }
            }
            else
            {
                // Trên không (nhảy/rơi)
                bobPos = new Vector3(0f, -jumpDipAmount, 0f);
                bobRot = new Vector3(2.5f, 0f, 0f);
            }

            // 3. Wall Avoidance (Kéo lùi khi chạm vật cản phía trước)
            Vector3 wallOffset = Vector3.zero;
            if (enableWallAvoidance && cameraTransform != null)
            {
                Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, wallCheckDistance, obstacleLayerMask, QueryTriggerInteraction.Ignore))
                {
                    float penetration = 1f - Mathf.Clamp01(hit.distance / wallCheckDistance);
                    float retractZ = -penetration * maxWallRetractZ;
                    float retractY = -penetration * maxWallRetractY;
                    wallOffset = new Vector3(0f, retractY, retractZ);
                }
            }

            // 4. Recoil Decay
            recoilPosOffset = Vector3.Lerp(recoilPosOffset, Vector3.zero, Time.deltaTime * 12f);
            recoilRotOffset = Vector3.Lerp(recoilRotOffset, Vector3.zero, Time.deltaTime * 12f);

            // 5. Kết hợp & SmoothDamp
            Vector3 targetTotalPos = defaultPosition + targetSwayPos + bobPos + wallOffset + recoilPosOffset + SwitchOffset;
            Vector3 targetTotalRot = defaultRotation + targetSwayRot + bobRot + recoilRotOffset;

            transform.localPosition = Vector3.SmoothDamp(transform.localPosition, targetTotalPos, ref posVelocity, swaySmoothTime);

            Vector3 currentEuler = transform.localEulerAngles;
            Vector3 targetEuler = new Vector3(
                Mathf.DeltaAngle(0, targetTotalRot.x),
                Mathf.DeltaAngle(0, targetTotalRot.y),
                Mathf.DeltaAngle(0, targetTotalRot.z)
            );
            Vector3 smoothedRot = new Vector3(
                Mathf.SmoothDampAngle(currentEuler.x, targetEuler.x, ref rotVelocity.x, swaySmoothTime),
                Mathf.SmoothDampAngle(currentEuler.y, targetEuler.y, ref rotVelocity.y, swaySmoothTime),
                Mathf.SmoothDampAngle(currentEuler.z, targetEuler.z, ref rotVelocity.z, swaySmoothTime)
            );
            transform.localRotation = Quaternion.Euler(smoothedRot);
        }
    }
}
