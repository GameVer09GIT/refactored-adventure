using UnityEngine;

namespace KhoaHon.Interaction
{
    /// <summary>
    /// Cho người chơi (CharacterController) xô ngã đồ vật có Rigidbody khi đi/chạy qua — chạy thì đẩy mạnh hơn.
    /// Gắn cùng GameObject với PlayerController (không sửa PlayerController).
    /// </summary>
    public class PlayerPushProps : MonoBehaviour
    {
        [SerializeField] private float pushWalk = 0.6f, pushRun = 1.6f;
        private PlayerController pc;

        private void Awake() => pc = GetComponent<PlayerController>();

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var rb = hit.collider.attachedRigidbody;
            if (rb == null || rb.isKinematic || hit.moveDirection.y < -0.3f) return;
            float f = pc != null && pc.IsRunning ? pushRun : pushWalk;
            var dir = new Vector3(hit.moveDirection.x, 0.15f, hit.moveDirection.z);
            rb.AddForceAtPosition(dir * f, hit.point, ForceMode.Impulse);
        }
    }
}
