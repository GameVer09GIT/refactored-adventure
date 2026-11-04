using System.Collections;
using KhoaHon.Core;
using UnityEngine;

namespace KhoaHon.Interaction
{
    /// <summary>
    /// Cửa bản lề (port ý tưởng từ FpsHorrorKit.DoorSystem). Có thể khóa bằng chìa (keyId) hoặc khóa cứng.
    /// Đặt component lên pivot bản lề; collider cửa nằm trong con.
    /// </summary>
    public class DoorInteractable : MonoBehaviour, IInteractable, ISaveParticipant
    {
        [Tooltip("Id duy nhất trong scene (lưu trạng thái mở khóa)")]
        [SerializeField] private string doorId = "Door_";
        [SerializeField] private bool locked = false;
        [Tooltip("Id chìa mở được cửa này. Để trống = khóa cứng (chỉ mở bằng script/cheat)")]
        [SerializeField] private string keyId = "";
        [Tooltip("Key trong TextTable cho thông báo khi cửa khóa")]
        [SerializeField] private string lockedTextKey = "prompt.locked";
        [Tooltip("Key trong TextTable cho tên chìa khóa (hiện khi mở khóa)")]
        [SerializeField] private string keyNameTextKey = "";
        [SerializeField] private float openAngle = 100f;
        [SerializeField] private float openDuration = 0.6f;
        [SerializeField] private AudioClip openClip;
        [SerializeField] private AudioClip closeClip;
        [SerializeField] private AudioClip lockedClip;
        [Tooltip("Bật khi mở cửa lần đầu (tùy chọn)")]
        [SerializeField] private StoryFlag flagOnFirstOpen = StoryFlag.None;
        [Tooltip("Cánh còn lại của cửa đôi: mở/đóng/khóa cùng lúc")]
        [SerializeField] private DoorInteractable linkedDoor;

        private bool isOpen;
        private float leafSide = 1f;
        private bool propagating;
        private Quaternion closedRot;
        private Coroutine anim;
        private AudioSource audioSource;

        public string DoorId => doorId;
        public bool IsLocked => locked;
        public bool IsOpen => isOpen;

        public void Configure(string id, bool isLocked, string key, string lockedKey, string keyNameKey)
        {
            doorId = id; locked = isLocked; keyId = key; lockedTextKey = lockedKey; keyNameTextKey = keyNameKey;
        }

        public void SetLinked(DoorInteractable other) => linkedDoor = other;
        public void SetClips(AudioClip open, AudioClip close, AudioClip lockedSnd) { openClip = open; closeClip = close; lockedClip = lockedSnd; }

        private void Awake()
        {
            closedRot = transform.localRotation;
            // Cánh nằm phía +x hay -x của bản lề (cánh gốc Villa1_Door_A trải về -x)
            var r = GetComponentInChildren<Renderer>();
            if (r != null) leafSide = transform.InverseTransformPoint(r.bounds.center).x >= 0f ? 1f : -1f;
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.playOnAwake = false;
                audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
                audioSource.minDistance = 1.5f;
                audioSource.maxDistance = 20f;
            }
        }

        public string Prompt
        {
            get
            {
                if (locked)
                {
                    if (!string.IsNullOrEmpty(keyId) && KeyRing.Instance != null && KeyRing.Instance.Has(keyId))
                        return T.Format("prompt.unlock_with", T.Get(keyNameTextKey));
                    return T.Get(lockedTextKey);
                }
                return T.Get(isOpen ? "prompt.close_door" : "prompt.open_door");
            }
        }

        public bool CanInteract() => true;

        public void Interact()
        {
            if (locked)
            {
                if (!string.IsNullOrEmpty(keyId) && KeyRing.Instance != null && KeyRing.Instance.Has(keyId))
                {
                    Unlock();
                    Toggle();
                }
                else if (lockedClip != null) audioSource.PlayOneShot(lockedClip);
                return;
            }
            Toggle();
        }

        public void Unlock()
        {
            locked = false;
            if (linkedDoor != null && !propagating) { propagating = true; linkedDoor.Unlock(); propagating = false; }
        }

        public void ForceOpen()
        {
            locked = false;
            if (!isOpen) Toggle();
        }

        /// <summary>Đóng sầm và khóa cứng (ví dụ cửa chính sau khi Vy bước vào).</summary>
        public void CloseAndLock(string newLockedTextKey = null)
        {
            if (isOpen) Toggle();
            locked = true;
            keyId = "";
            if (!string.IsNullOrEmpty(newLockedTextKey)) lockedTextKey = newLockedTextKey;
            if (linkedDoor != null && !propagating) { propagating = true; linkedDoor.CloseAndLock(newLockedTextKey); propagating = false; }
        }

        private void Toggle()
        {
            isOpen = !isOpen;
            if (isOpen && flagOnFirstOpen != StoryFlag.None) GameManager.Instance?.SetFlag(flagOnFirstOpen);
            var clip = isOpen ? openClip : closeClip;
            if (clip != null) audioSource.PlayOneShot(clip);
            if (anim != null) StopCoroutine(anim);
            if (isOpen) openSign = SwingAwaySign();
            if (linkedDoor != null && !propagating && linkedDoor.isOpen != isOpen)
            {
                propagating = true; linkedDoor.locked = false; linkedDoor.Toggle(); propagating = false;
            }
            anim = StartCoroutine(Rotate(isOpen ? closedRot * Quaternion.Euler(0f, openAngle * openSign, 0f) : closedRot));
        }

        private float openSign = 1f;

        /// <summary>
        /// Cánh cửa nằm theo trục +x của bản lề; xoay +góc đẩy cánh về phía -z cục bộ.
        /// Chọn chiều để cánh mở RA XA người chơi (không chắn đường).
        /// </summary>
        private float SwingAwaySign()
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc == null) return 1f;
            var parentRot = transform.parent != null ? transform.parent.rotation : Quaternion.identity;
            var closedForward = parentRot * closedRot * Vector3.forward;
            float side = Vector3.Dot(pc.transform.position - transform.position, closedForward);
            return (side >= 0f ? 1f : -1f) * leafSide;
        }

        private IEnumerator Rotate(Quaternion target)
        {
            var start = transform.localRotation;
            float t = 0f;
            while (t < openDuration)
            {
                t += Time.deltaTime;
                transform.localRotation = Quaternion.Slerp(start, target, Mathf.SmoothStep(0f, 1f, t / openDuration));
                yield return null;
            }
            transform.localRotation = target;
            anim = null;
        }

        public void Capture(SaveData data)
        {
            if (!locked && !data.unlockedDoors.Contains(doorId)) data.unlockedDoors.Add(doorId);
        }

        public void Restore(SaveData data)
        {
            if (data.unlockedDoors.Contains(doorId)) locked = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = locked ? Color.red : Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.15f);
        }
    }
}
