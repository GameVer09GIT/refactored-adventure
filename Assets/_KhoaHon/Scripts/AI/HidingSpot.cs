using KhoaHon.Core;
using KhoaHon.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KhoaHon.AI
{
    /// <summary>
    /// Chỗ trốn: tủ áo hoặc gầm giường (port ý tưởng FpsHorrorKit.HidingSystem, viết lại cho PlayerController).
    /// E để vào/ra. Trong lúc trốn: camera đặt tại hidingPoint, chỉ xoay trong giới hạn, có viền tối.
    /// Nếu bà Quản Gia thấy Vy chui vào → bà tới đứng sát → mini-game nín thở.
    /// </summary>
    public class HidingSpot : MonoBehaviour, IInteractable
    {
        public enum Kind { Wardrobe, UnderBed }

        public static HidingSpot Current { get; private set; }

        [SerializeField] private Kind kind = Kind.Wardrobe;
        [Tooltip("Vị trí mắt Vy khi trốn (forward = hướng nhìn ra ngoài)")]
        [SerializeField] private Transform hidingPoint;
        [Tooltip("Vị trí chân Vy khi bước ra")]
        [SerializeField] private Transform outPoint;
        [SerializeField] private float yawLimit = 35f;
        [SerializeField] private float pitchLimit = 18f;
        [SerializeField] private float lookSensitivity = 0.08f;
        [SerializeField] private float transitionTime = 0.35f;
        [SerializeField] private AudioClip enterClip;

        public bool SeenEntering { get; private set; }
        public Transform HidingPoint => hidingPoint;
        public Transform OutPoint => outPoint;
        public Kind SpotKind => kind;

        private PlayerController pc;
        private CharacterController cc;
        private float yaw, pitch;
        private Quaternion baseRot;
        private float lerpT;
        private Vector3 fromPos; private Quaternion fromRot;
        private int enteredFrame;

        public void Configure(Kind k, Transform hide, Transform outP) { kind = k; hidingPoint = hide; outPoint = outP; }

        public string Prompt => Current == this ? T.Get("prompt.hide_exit") : T.Get(kind == Kind.Wardrobe ? "prompt.hide_wardrobe" : "prompt.hide_bed");

        public bool CanInteract() => Current == null && hidingPoint != null;

        public void Interact()
        {
            if (Current != null || hidingPoint == null) return;
            pc = FindFirstObjectByType<PlayerController>();
            if (pc == null) return;
            cc = pc.GetComponent<CharacterController>();
            SeenEntering = HousekeeperAI.Instance != null && HousekeeperAI.Instance.CanSeePlayerNow();
            Current = this;
            PlayerState.IsHiding = true;
            enteredFrame = Time.frameCount;
            pc.enabled = false;
            if (cc != null) cc.enabled = false;
            fromPos = pc.playerCamera.position; fromRot = pc.playerCamera.rotation;
            baseRot = hidingPoint.rotation;
            yaw = 0f; pitch = 0f; lerpT = 0f;
            if (enterClip != null) AudioSource.PlayClipAtPoint(enterClip, transform.position, 0.8f);
            KhoaHonHUD.Instance?.SetVignette(kind == Kind.Wardrobe ? 0.85f : 0.95f);
            KhoaHonHUD.Prompt(T.Get("prompt.hide_exit"));
        }

        private void LateUpdate()
        {
            if (Current != this || pc == null) return;
            var cam = pc.playerCamera;
            // Nội suy vào chỗ trốn
            if (lerpT < 1f)
            {
                lerpT = Mathf.Min(1f, lerpT + Time.deltaTime / transitionTime);
                cam.position = Vector3.Lerp(fromPos, hidingPoint.position, Mathf.SmoothStep(0, 1, lerpT));
                cam.rotation = Quaternion.Slerp(fromRot, baseRot, Mathf.SmoothStep(0, 1, lerpT));
                return;
            }
            if (Mouse.current != null)
            {
                var d = Mouse.current.delta.ReadValue() * lookSensitivity;
                yaw = Mathf.Clamp(yaw + d.x, -yawLimit, yawLimit);
                pitch = Mathf.Clamp(pitch - d.y, -pitchLimit, pitchLimit);
            }
            cam.position = hidingPoint.position;
            cam.rotation = baseRot * Quaternion.Euler(pitch, yaw, 0f);

            if (!PlayerState.IsHoldingBreath && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && Time.frameCount > enteredFrame + 2)
                Exit();
        }

        /// <summary>Bước ra (hoặc bị lôi ra khi bị bắt).</summary>
        public void Exit()
        {
            if (Current != this) return;
            var cam = pc.playerCamera;
            var target = outPoint != null ? outPoint : transform;
            pc.transform.SetPositionAndRotation(target.position, Quaternion.Euler(0, target.eulerAngles.y, 0));
            cam.localPosition = camLocalDefault;
            cam.localRotation = Quaternion.identity;
            pc.SetPitch(0f);
            if (cc != null) cc.enabled = true;
            pc.enabled = true;
            Current = null;
            PlayerState.IsHiding = false;
            SeenEntering = false;
            KhoaHonHUD.Instance?.SetVignette(0f);
        }

        private Vector3 camLocalDefault;

        private void Start()
        {
            var p = FindFirstObjectByType<PlayerController>();
            if (p != null && p.playerCamera != null) camLocalDefault = p.playerCamera.localPosition;
        }

        private void OnDisable()
        {
            if (Current == this) { Current = null; PlayerState.IsHiding = false; }
        }

        private void OnDrawGizmos()
        {
            if (hidingPoint != null) { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(hidingPoint.position, 0.12f); Gizmos.DrawRay(hidingPoint.position, hidingPoint.forward * 0.6f); }
            if (outPoint != null) { Gizmos.color = Color.green; Gizmos.DrawWireCube(outPoint.position + Vector3.up * 0.9f, new Vector3(0.5f, 1.8f, 0.5f)); }
        }
    }
}
