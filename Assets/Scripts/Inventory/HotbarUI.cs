using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorrorGame.Inventory
{
    /// <summary>
    /// Giao diện thanh Hotbar 4 ô ở giữa-dưới màn hình
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class HotbarUI : MonoBehaviour
    {
        [System.Serializable]
        public class SlotUI
        {
            public RectTransform rootRect;
            public Image backgroundImage;
            public Image borderImage;
            public Image iconImage;
            public TMP_Text numberText;
        }

        [Header("Danh sách 4 ô vật phẩm")]
        [SerializeField] private SlotUI[] slots = new SlotUI[4];

        [Header("Dòng tên vật phẩm")]
        [Tooltip("Text hiển thị tên vật phẩm ở phía trên thanh Hotbar")]
        [SerializeField] private TMP_Text itemNameText;

        [Tooltip("CanvasGroup của dòng tên vật phẩm")]
        [SerializeField] private CanvasGroup itemNameCanvasGroup;

        [Header("Màu sắc & Hiệu ứng")]
        [Tooltip("Màu viền khi ô đang được chọn (trắng ấm)")]
        [SerializeField] private Color selectedBorderColor = new Color(1.0f, 0.96f, 0.88f, 1.0f);

        [Tooltip("Màu viền khi ô không được chọn")]
        [SerializeField] private Color unselectedBorderColor = new Color(0.35f, 0.35f, 0.35f, 0.45f);

        [Tooltip("Độ phóng to của ô đang chọn (1.08x)")]
        [SerializeField] private float selectedScale = 1.08f;

        [Tooltip("Thời gian chuyển đổi phóng to (0.1s)")]
        [SerializeField] private float transitionDuration = 0.1f;

        [Header("Thời gian hiện tên")]
        [SerializeField] private float nameFadeInDuration = 0.15f;
        [SerializeField] private float nameHoldDuration = 2.5f;
        [SerializeField] private float nameFadeOutDuration = 0.5f;

        private Coroutine[] scaleCoroutines = new Coroutine[4];
        private Coroutine nameTextCoroutine;
        private CanvasGroup mainCanvasGroup;
        private int currentSelectedIndex = -1;

        void Awake()
        {
            mainCanvasGroup = GetComponent<CanvasGroup>();
            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.blocksRaycasts = false;
                mainCanvasGroup.interactable = false;
            }

            if (itemNameCanvasGroup != null)
            {
                itemNameCanvasGroup.alpha = 0f;
            }
        }

        /// <summary>
        /// Khởi tạo dữ liệu các ô từ HotbarInventory
        /// </summary>
        public void InitializeSlots(ItemData[] items, int selectedIndex)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;

                ItemData item = (items != null && i < items.Length) ? items[i] : null;
                UpdateSlotContent(i, item);

                bool isSelected = (i == selectedIndex);
                SetSlotVisualInstant(i, isSelected);
            }

            currentSelectedIndex = selectedIndex;
            if (items != null && selectedIndex >= 0 && selectedIndex < items.Length && items[selectedIndex] != null)
            {
                ShowItemName(items[selectedIndex].DisplayName);
            }
            else
            {
                HideItemNameInstant();
            }
        }

        public void UpdateSlotContent(int slotIndex, ItemData item)
        {
            if (slotIndex < 0 || slotIndex >= slots.Length || slots[slotIndex] == null) return;

            var slot = slots[slotIndex];
            if (slot.iconImage != null)
            {
                if (item != null && item.Icon != null)
                {
                    slot.iconImage.sprite = item.Icon;
                    slot.iconImage.gameObject.SetActive(true);
                    slot.iconImage.color = Color.white;
                }
                else
                {
                    slot.iconImage.gameObject.SetActive(false);
                }
            }
        }

        public void SelectSlot(int newIndex, ItemData item)
        {
            if (newIndex < 0 || newIndex >= slots.Length) return;

            int oldIndex = currentSelectedIndex;
            currentSelectedIndex = newIndex;

            // Thu nhỏ ô cũ
            if (oldIndex >= 0 && oldIndex < slots.Length && oldIndex != newIndex)
            {
                AnimateSlotVisual(oldIndex, false);
            }

            // Phóng to ô mới
            AnimateSlotVisual(newIndex, true);

            // Hiển thị tên vật phẩm
            if (item != null && !string.IsNullOrEmpty(item.DisplayName))
            {
                ShowItemName(item.DisplayName);
            }
            else
            {
                HideItemNameInstant();
            }
        }

        private void SetSlotVisualInstant(int index, bool isSelected)
        {
            if (index < 0 || index >= slots.Length || slots[index] == null) return;
            var slot = slots[index];

            float scale = isSelected ? selectedScale : 1.0f;
            if (slot.rootRect != null) slot.rootRect.localScale = new Vector3(scale, scale, 1.0f);

            Color borderColor = isSelected ? selectedBorderColor : unselectedBorderColor;
            if (slot.borderImage != null) slot.borderImage.color = borderColor;
        }

        private void AnimateSlotVisual(int index, bool isSelected)
        {
            if (index < 0 || index >= slots.Length || slots[index] == null) return;

            if (scaleCoroutines[index] != null) StopCoroutine(scaleCoroutines[index]);
            scaleCoroutines[index] = StartCoroutine(SlotTransitionRoutine(index, isSelected));
        }

        private IEnumerator SlotTransitionRoutine(int index, bool isSelected)
        {
            var slot = slots[index];
            if (slot == null || slot.rootRect == null) yield break;

            Vector3 startScale = slot.rootRect.localScale;
            float targetScale = isSelected ? selectedScale : 1.0f;
            Vector3 endScale = new Vector3(targetScale, targetScale, 1.0f);

            Color startColor = slot.borderImage != null ? slot.borderImage.color : unselectedBorderColor;
            Color endColor = isSelected ? selectedBorderColor : unselectedBorderColor;

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);

                slot.rootRect.localScale = Vector3.Lerp(startScale, endScale, t);
                if (slot.borderImage != null) slot.borderImage.color = Color.Lerp(startColor, endColor, t);
                yield return null;
            }

            slot.rootRect.localScale = endScale;
            if (slot.borderImage != null) slot.borderImage.color = endColor;
            scaleCoroutines[index] = null;
        }

        public void ShowItemName(string name)
        {
            if (itemNameText == null || itemNameCanvasGroup == null) return;

            itemNameText.text = name;

            if (nameTextCoroutine != null) StopCoroutine(nameTextCoroutine);
            nameTextCoroutine = StartCoroutine(NameTextFadeRoutine());
        }

        private IEnumerator NameTextFadeRoutine()
        {
            if (itemNameCanvasGroup == null) yield break;

            // Fade in
            float elapsed = 0f;
            float startAlpha = itemNameCanvasGroup.alpha;
            while (elapsed < nameFadeInDuration)
            {
                elapsed += Time.deltaTime;
                itemNameCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / nameFadeInDuration);
                yield return null;
            }
            itemNameCanvasGroup.alpha = 1f;

            // Hold
            yield return new WaitForSeconds(nameHoldDuration);

            // Fade out
            elapsed = 0f;
            while (elapsed < nameFadeOutDuration)
            {
                elapsed += Time.deltaTime;
                itemNameCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / nameFadeOutDuration);
                yield return null;
            }
            itemNameCanvasGroup.alpha = 0f;
            nameTextCoroutine = null;
        }

        private void HideItemNameInstant()
        {
            if (nameTextCoroutine != null)
            {
                StopCoroutine(nameTextCoroutine);
                nameTextCoroutine = null;
            }
            if (itemNameCanvasGroup != null) itemNameCanvasGroup.alpha = 0f;
            if (itemNameText != null) itemNameText.text = "";
        }
    }
}
