using UnityEngine;

namespace HorrorGame.Inventory
{
    /// <summary>
    /// Lớp cơ sở cho các vật phẩm người chơi cầm trên tay (Viewmodel)
    /// </summary>
    public abstract class HeldItem : MonoBehaviour
    {
        [Header("Thông tin cơ bản")]
        [Tooltip("Dữ liệu vật phẩm tương ứng")]
        [SerializeField] protected ItemData itemData;

        [Tooltip("AudioSource phát âm thanh của vật phẩm")]
        [SerializeField] protected AudioSource audioSource;

        protected PlayerController player;
        protected ViewModelSway swayComponent;
        protected bool isEquipped = false;

        public ItemData ItemData => itemData;
        public bool IsEquipped => isEquipped;

        protected virtual void Awake()
        {
            swayComponent = GetComponent<ViewModelSway>();
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // Âm thanh 2D cho người chơi
                audioSource.loop = false;
            }
        }

        public virtual void OnEquip(PlayerController playerController)
        {
            player = playerController;
            isEquipped = true;
            gameObject.SetActive(true);
        }

        public virtual void OnUnequip()
        {
            isEquipped = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Gọi khi người chơi bấm chuột trái (Primary Action)
        /// </summary>
        public abstract void OnPrimaryUse();

        /// <summary>
        /// Xử lý khi người chơi chết
        /// </summary>
        public virtual void OnPlayerDied()
        {
            // Mặc định ẩn vật phẩm
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Xử lý khi người chơi hồi sinh
        /// </summary>
        public virtual void OnPlayerRespawned()
        {
        }

        protected void PlayOneShotSound(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (audioSource == null || clip == null) return;
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }
    }
}
