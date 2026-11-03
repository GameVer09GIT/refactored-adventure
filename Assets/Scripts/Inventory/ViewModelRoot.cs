using System;
using System.Collections;
using UnityEngine;

namespace HorrorGame.Inventory
{
    /// <summary>
    /// Gốc Viewmodel chứa cả hai bàn tay và vật phẩm đang cầm.
    /// Mọi chuyển động (sway, bobbing, né tường, giật recoil, nâng/hạ) đều áp lên gốc này
    /// để bàn tay và vật phẩm luôn luôn chuyển động đồng bộ hoàn hảo 100%.
    /// </summary>
    [RequireComponent(typeof(ViewModelSway))]
    public class ViewModelRoot : MonoBehaviour
    {
        public static ViewModelRoot Instance { get; private set; }

        [Header("Thành phần con")]
        [Tooltip("Hệ thống giàn khung bàn tay (HandRig)")]
        [SerializeField] private HandRig handRig;

        [Tooltip("Khu vực chứa các mô hình vật phẩm")]
        [SerializeField] private Transform itemSlot;

        [Header("Ánh sáng bù sáng tay ban đêm (Viewmodel Fill Light)")]
        [Tooltip("Bật đèn bù sáng nhẹ để nhìn rõ bàn tay và vật phẩm trong phòng tối")]
        [SerializeField] private bool enableFillLight = true;

        [Tooltip("Point Light bù sáng cho viewmodel")]
        [SerializeField] private Light fillLight;

        [Tooltip("Cường độ ánh sáng bù sáng")]
        [SerializeField] private float fillLightIntensity = 0.35f;

        [Tooltip("Màu ánh sáng bù sáng (ấm nhẹ)")]
        [SerializeField] private Color fillLightColor = new Color(1.0f, 0.92f, 0.84f);

        [Tooltip("Phạm vi ánh sáng bù sáng (mét)")]
        [SerializeField] private float fillLightRange = 0.70f;

        private ViewModelSway swayComponent;
        private Coroutine transitionCoroutine;
        private bool isTransitioning = false;

        public HandRig HandRig => handRig;
        public Transform ItemSlot => itemSlot;
        public ViewModelSway SwayComponent => swayComponent;
        public bool IsTransitioning => isTransitioning;

        void Awake()
        {
            Instance = this;
            swayComponent = GetComponent<ViewModelSway>();

            if (handRig == null)
            {
                handRig = GetComponentInChildren<HandRig>();
            }

            if (itemSlot == null)
            {
                Transform slot = transform.Find("ItemSlot");
                if (slot == null)
                {
                    GameObject slotGo = new GameObject("ItemSlot");
                    slotGo.transform.SetParent(transform, false);
                    slotGo.transform.localPosition = Vector3.zero;
                    slotGo.transform.localRotation = Quaternion.identity;
                    itemSlot = slotGo.transform;
                }
                else
                {
                    itemSlot = slot;
                }
            }

            ConfigureFillLight();
        }

        private void ConfigureFillLight()
        {
            if (fillLight == null)
            {
                fillLight = GetComponentInChildren<Light>();
                if (fillLight == null && enableFillLight)
                {
                    GameObject lightGo = new GameObject("ViewmodelFill");
                    lightGo.transform.SetParent(transform, false);
                    lightGo.transform.localPosition = new Vector3(0.05f, 0.05f, 0.25f);
                    fillLight = lightGo.AddComponent<Light>();
                }
            }

            if (fillLight != null)
            {
                fillLight.type = LightType.Point;
                fillLight.color = fillLightColor;
                fillLight.range = fillLightRange;
                fillLight.intensity = enableFillLight ? fillLightIntensity : 0f;
                fillLight.shadows = LightShadows.None;
            }
        }

        /// <summary>
        /// Gắn vật phẩm vào Viewmodel và cập nhật bàn tay tương ứng
        /// </summary>
        public void UpdateEquippedItem(HeldItem item, ItemData itemData)
        {
            if (PlayerArmIK.Instance != null)
            {
                PlayerArmIK.Instance.EquipItem(itemData, item);
            }

            if (handRig != null)
            {
                handRig.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Kích hoạt hiệu ứng giật recoil và cử động ngón cái bấm nút
        /// </summary>
        public void TriggerItemAction(bool isRightHand, float duration = 0.08f)
        {
            if (swayComponent != null)
            {
                swayComponent.ApplyClickRecoil(0.010f, 1.2f);
            }

            if (PlayerArmIK.Instance != null)
            {
                PlayerArmIK.Instance.TriggerThumbPress(isRightHand, duration);
            }
            else if (handRig != null)
            {
                if (isRightHand) handRig.TriggerRightThumbPress(duration);
                else handRig.TriggerLeftThumbPress(duration);
            }
        }

        /// <summary>
        /// Hạ cả cụm ViewmodelRoot (tay + vật) xuống rồi nâng lên mượt mà khi đổi ô
        /// </summary>
        public void TransitionSwitch(Action onMiddleSwapped, float lowerDur = 0.12f, float raiseDur = 0.15f, float lowerDist = 0.25f)
        {
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(SwitchTransitionRoutine(onMiddleSwapped, lowerDur, raiseDur, lowerDist));
        }

        private IEnumerator SwitchTransitionRoutine(Action onMiddleSwapped, float lowerDur, float raiseDur, float lowerDist)
        {
            isTransitioning = true;
            Vector3 targetLowerOffset = Vector3.down * lowerDist;

            // 1. Hạ xuống
            if (swayComponent != null)
            {
                Vector3 startOffset = swayComponent.SwitchOffset;
                float elapsed = 0f;
                while (elapsed < lowerDur)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / lowerDur);
                    swayComponent.SwitchOffset = Vector3.Lerp(startOffset, targetLowerOffset, t);
                    yield return null;
                }
                swayComponent.SwitchOffset = targetLowerOffset;
            }
            else
            {
                Vector3 startPos = transform.localPosition;
                Vector3 loweredPos = startPos + Vector3.down * lowerDist;
                float elapsed = 0f;
                while (elapsed < lowerDur)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / lowerDur);
                    transform.localPosition = Vector3.Lerp(startPos, loweredPos, t);
                    yield return null;
                }
                transform.localPosition = loweredPos;
            }

            // Tráo đổi vật phẩm và tư thế tay ở điểm đáy
            onMiddleSwapped?.Invoke();

            // 2. Nâng lên
            if (swayComponent != null)
            {
                float elapsed = 0f;
                while (elapsed < raiseDur)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / raiseDur);
                    swayComponent.SwitchOffset = Vector3.Lerp(targetLowerOffset, Vector3.zero, t);
                    yield return null;
                }
                swayComponent.SwitchOffset = Vector3.zero;
            }
            else
            {
                Vector3 defaultPos = Vector3.zero;
                Vector3 loweredPos = defaultPos + Vector3.down * lowerDist;
                float elapsed = 0f;
                while (elapsed < raiseDur)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / raiseDur);
                    transform.localPosition = Vector3.Lerp(loweredPos, defaultPos, t);
                    yield return null;
                }
                transform.localPosition = defaultPos;
            }

            isTransitioning = false;
            transitionCoroutine = null;
        }

        public void SetVisibility(bool visible)
        {
            gameObject.SetActive(visible);
            if (!visible && handRig != null)
            {
                handRig.HideAllHands();
            }
        }
    }
}
