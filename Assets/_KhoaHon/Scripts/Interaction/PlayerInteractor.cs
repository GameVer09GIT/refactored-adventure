using KhoaHon.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KhoaHon.Interaction
{
    /// <summary>
    /// Gắn lên Player. Raycast từ camera 2.2 m, hiện prompt và gọi Interact() khi nhấn E.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Transform rayOrigin;
        [Tooltip("Tầm với tương tác (m)")]
        [SerializeField] private float range = 2.2f;
        [Tooltip("Bán kính sphere cast để dễ nhắm vật nhỏ")]
        [SerializeField] private float radius = 0.08f;
        [SerializeField] private LayerMask mask = ~0;

        private IInteractable current;
        private CharacterController ownController;

        public IInteractable Current => current;

        private void Awake()
        {
            ownController = GetComponent<CharacterController>();
            if (rayOrigin == null)
            {
                var pc = GetComponent<PlayerController>();
                if (pc != null) rayOrigin = pc.playerCamera;
            }
        }

        private void Update()
        {
            if (rayOrigin == null) return;
            if (ModalUI.IsAnyOpen)
            {
                SetCurrent(null);
                return;
            }

            SetCurrent(FindTarget());

            if (current != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame
                && Time.frameCount != ModalUI.LastClosedFrame)
            {
                if (current.CanInteract()) current.Interact();
            }
            else if (current != null)
            {
                KhoaHonHUD.Prompt(current.Prompt);
            }
        }

        private IInteractable FindTarget()
        {
            var hits = Physics.SphereCastAll(rayOrigin.position, radius, rayOrigin.forward, range, mask, QueryTriggerInteraction.Collide);
            float best = float.MaxValue;
            IInteractable found = null;
            foreach (var h in hits)
            {
                if (h.collider == ownController) continue;
                if (h.collider.transform.IsChildOf(transform)) continue;
                var it = h.collider.GetComponentInParent<IInteractable>();
                if (it == null)
                {
                    // Vật chắn không tương tác được (tường) → chỉ chấp nhận vật gần hơn nó
                    if (!h.collider.isTrigger && h.distance < best) { best = h.distance; found = null; }
                    continue;
                }
                if (h.distance < best && ((MonoBehaviour)it).isActiveAndEnabled)
                {
                    best = h.distance;
                    found = it;
                }
            }
            return found;
        }

        private void SetCurrent(IInteractable it)
        {
            if (ReferenceEquals(it, current)) return;
            current = it;
            KhoaHonHUD.Prompt(current != null ? current.Prompt : "");
        }
    }
}
