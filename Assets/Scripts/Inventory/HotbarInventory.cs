using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HorrorGame.Inventory
{
    /// <summary>
    /// Quản lý túi đồ 4 ô (Hotbar), chuyển đổi ô vật phẩm, chuyển động nâng/hạ vật phẩm cầm tay
    /// </summary>
    [DisallowMultipleComponent]
    public class HotbarInventory : MonoBehaviour
    {
        private static HotbarInventory _instance;
        public static HotbarInventory Instance
        {
            get
            {
                if (_instance == null) _instance = FindAnyObjectByType<HotbarInventory>();
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Cấu hình 4 ô Hotbar")]
        [Tooltip("Dữ liệu vật phẩm ở 4 ô (Ô 1: Đèn pin, Ô 2: Radar, Ô 3-4: Trống)")]
        [SerializeField] private ItemData[] slots = new ItemData[4];

        [Tooltip("Ô đang được chọn hiện tại (0-3, tương ứng phím 1-4)")]
        [SerializeField] private int selectedIndex = 0;

        [Header("Vị trí gắn mô hình cầm tay (Viewmodel)")]
        [Tooltip("Transform chứa các mô hình cầm tay (thường là con của Camera)")]
        [SerializeField] private Transform viewmodelHolder;

        [Header("Thời gian chuyển đổi vật phẩm (Equip Dip)")]
        [Tooltip("Thời gian hạ vật phẩm cũ xuống (giây)")]
        [SerializeField] private float lowerDuration = 0.12f;

        [Tooltip("Thời gian nâng vật phẩm mới lên (giây)")]
        [SerializeField] private float raiseDuration = 0.15f;

        [Tooltip("Khoảng cách hạ thấp vật phẩm khi đổi ô (mét)")]
        [SerializeField] private float lowerDistance = 0.25f;

        [Header("Ngưỡng con lăn chuột")]
        [Tooltip("Ngưỡng giá trị cuộn chuột tối thiểu để đổi 1 ô")]
        [SerializeField] private float scrollThreshold = 0.5f;

        [Tooltip("Thời gian hồi giữa 2 lần cuộn chuột (chống nhảy ô trên chuột lăn mịn)")]
        [SerializeField] private float scrollDebounceTime = 0.12f;

        [Header("Âm thanh")]
        [Tooltip("Clip âm thanh khi chuyển ô Hotbar")]
        [SerializeField] private AudioClip switchSound;

        [Tooltip("AudioSource phát âm thanh chuyển ô")]
        [SerializeField] private AudioSource audioSource;

        [Header("Giao diện UI")]
        [Tooltip("Tham chiếu tới script điều khiển HotbarUI")]
        [SerializeField] private HotbarUI hotbarUI;

        // Sự kiện thông báo khi đổi ô
        public event Action<int, ItemData> OnSelectionChanged;

        private PlayerController playerController;
        private HeldItem[] instantiatedHeldItems = new HeldItem[4];
        private HeldItem currentHeldItem;
        private Coroutine switchCoroutine;
        private bool isSwitching = false;
        private float lastScrollTime = 0f;
        private bool isPlayerDead = false;

        public int SelectedIndex => selectedIndex;
        public ItemData CurrentItem => (selectedIndex >= 0 && selectedIndex < slots.Length) ? slots[selectedIndex] : null;
        public HeldItem CurrentHeldItem => currentHeldItem;
        public bool IsSwitching => isSwitching;

        void Awake()
        {
            Instance = this;
            playerController = GetComponent<PlayerController>();

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            }
            if (audioSource != null)
            {
                audioSource.spatialBlend = 0f;
                audioSource.playOnAwake = false;
            }

            EnsureViewmodelHolder();
            InitializeHeldItemInstances();
        }

        private void EnsureViewmodelHolder()
        {
            if (viewmodelHolder == null)
            {
                if (ViewModelRoot.Instance != null && ViewModelRoot.Instance.ItemSlot != null)
                {
                    viewmodelHolder = ViewModelRoot.Instance.ItemSlot;
                    return;
                }

                if (playerController != null && playerController.playerCamera != null)
                {
                    Transform vmRoot = playerController.playerCamera.Find("ViewModelRoot");
                    if (vmRoot != null)
                    {
                        var slot = vmRoot.Find("ItemSlot");
                        viewmodelHolder = slot != null ? slot : vmRoot;
                        return;
                    }

                    // Tìm hoặc tạo child "ViewmodelHolder" dưới Camera nếu chưa có ViewModelRoot
                    Transform existing = playerController.playerCamera.Find("ViewmodelHolder");
                    if (existing != null)
                    {
                        viewmodelHolder = existing;
                    }
                    else
                    {
                        GameObject holderGo = new GameObject("ViewmodelHolder");
                        holderGo.transform.SetParent(playerController.playerCamera, false);
                        holderGo.transform.localPosition = Vector3.zero;
                        holderGo.transform.localRotation = Quaternion.identity;
                        viewmodelHolder = holderGo.transform;
                    }
                }
            }
        }

        private void InitializeHeldItemInstances()
        {
            if (viewmodelHolder == null) return;

            for (int i = 0; i < slots.Length; i++)
            {
                ItemData data = slots[i];
                if (data != null && data.HeldPrefab != null)
                {
                    // Kiểm tra xem đã có sẵn child tương ứng chưa
                    HeldItem existingItem = null;
                    for (int c = 0; c < viewmodelHolder.childCount; c++)
                    {
                        var child = viewmodelHolder.GetChild(c);
                        var held = child.GetComponent<HeldItem>();
                        if (held != null && (held.ItemData == data || child.name == data.HeldPrefab.name))
                        {
                            existingItem = held;
                            break;
                        }
                    }

                    if (existingItem == null)
                    {
                        GameObject itemObj = Instantiate(data.HeldPrefab, viewmodelHolder);
                        itemObj.name = data.HeldPrefab.name;
                        existingItem = itemObj.GetComponent<HeldItem>();
                    }

                    if (existingItem != null)
                    {
                        instantiatedHeldItems[i] = existingItem;
                        existingItem.gameObject.SetActive(false);
                    }
                }
            }
        }

        void OnEnable()
        {
            DeathScreenManager.OnPlayerDied += HandlePlayerDied;
            DeathScreenManager.OnPlayerRespawned += HandlePlayerRespawned;
        }

        void OnDisable()
        {
            DeathScreenManager.OnPlayerDied -= HandlePlayerDied;
            DeathScreenManager.OnPlayerRespawned -= HandlePlayerRespawned;
        }

        void Start()
        {
            if (hotbarUI == null)
            {
                hotbarUI = FindAnyObjectByType<HotbarUI>();
            }

            if (hotbarUI != null)
            {
                hotbarUI.InitializeSlots(slots, selectedIndex);
            }

            // Kích hoạt vật phẩm ô ban đầu (ô 1)
            EquipInitialSlot(selectedIndex);
        }

        private void EquipInitialSlot(int index)
        {
            selectedIndex = Mathf.Clamp(index, 0, 3);
            currentHeldItem = instantiatedHeldItems[selectedIndex];

            for (int i = 0; i < instantiatedHeldItems.Length; i++)
            {
                if (instantiatedHeldItems[i] != null)
                {
                    instantiatedHeldItems[i].gameObject.SetActive(i == selectedIndex);
                }
            }

            if (currentHeldItem != null)
            {
                currentHeldItem.OnEquip(playerController);
            }

            if (ViewModelRoot.Instance != null)
            {
                ViewModelRoot.Instance.UpdateEquippedItem(currentHeldItem, CurrentItem);
            }

            OnSelectionChanged?.Invoke(selectedIndex, CurrentItem);
        }

        void Update()
        {
            if (isPlayerDead || Time.timeScale <= 0f) return;
            if (playerController != null && !playerController.enabled) return;

            // Nếu Tuning UI đang mở thì không xử lý đổi slot / click đồ
            if (PlayerArmIKTuningUI.IsVisible) return;

            HandleSlotInput();
            HandlePrimaryActionInput();
        }

        private void HandleSlotInput()
        {
            if (Keyboard.current == null && Mouse.current == null) return;

            int targetIndex = -1;

            // 1. Phím số 1-4 (hàng phím số hoặc numpad)
            if (Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame) targetIndex = 0;
                else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame) targetIndex = 1;
                else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame) targetIndex = 2;
                else if (Keyboard.current.digit4Key.wasPressedThisFrame || Keyboard.current.numpad4Key.wasPressedThisFrame) targetIndex = 3;
            }

            // 2. Con lăn chuột
            if (targetIndex < 0 && Mouse.current != null && Time.time - lastScrollTime >= scrollDebounceTime)
            {
                float scrollY = Mouse.current.scroll.ReadValue().y;
                if (scrollY > scrollThreshold)
                {
                    // Lăn lên: ô trước (wrap)
                    targetIndex = (selectedIndex - 1 + 4) % 4;
                    lastScrollTime = Time.time;
                }
                else if (scrollY < -scrollThreshold)
                {
                    // Lăn xuống: ô sau (wrap)
                    targetIndex = (selectedIndex + 1) % 4;
                    lastScrollTime = Time.time;
                }
            }

            // Chuyển sang ô mới nếu hợp lệ và khác ô hiện tại
            if (targetIndex >= 0 && targetIndex != selectedIndex)
            {
                SelectSlot(targetIndex);
            }
        }

        public void SelectSlot(int targetIndex)
        {
            if (targetIndex < 0 || targetIndex >= 4) return;
            if (targetIndex == selectedIndex && !isSwitching) return;

            int oldIndex = selectedIndex;
            selectedIndex = targetIndex;

            // Phát âm thanh chuyển ô
            if (audioSource != null && switchSound != null)
            {
                audioSource.pitch = UnityEngine.Random.Range(0.96f, 1.04f);
                audioSource.PlayOneShot(switchSound, 0.75f);
            }

            // Cập nhật UI
            if (hotbarUI != null)
            {
                hotbarUI.SelectSlot(selectedIndex, CurrentItem);
            }

            OnSelectionChanged?.Invoke(selectedIndex, CurrentItem);

            // Bắt đầu animation hạ vật cũ và nâng vật mới
            HeldItem oldItem = currentHeldItem;
            HeldItem newItem = instantiatedHeldItems[selectedIndex];

            if (switchCoroutine != null) StopCoroutine(switchCoroutine);
            switchCoroutine = StartCoroutine(SwitchItemRoutine(newItem));
        }

        private IEnumerator SwitchItemRoutine(HeldItem newItem)
        {
            isSwitching = true;

            if (ViewModelRoot.Instance != null)
            {
                ViewModelRoot.Instance.TransitionSwitch(() =>
                {
                    // Tráo đổi vật phẩm ở điểm đáy của chuyển động hạ
                    for (int i = 0; i < instantiatedHeldItems.Length; i++)
                    {
                        var item = instantiatedHeldItems[i];
                        if (item != null && item != newItem && item.gameObject.activeSelf)
                        {
                            item.OnUnequip();
                            item.gameObject.SetActive(false);
                        }
                    }

                    currentHeldItem = newItem;
                    if (newItem != null)
                    {
                        newItem.gameObject.SetActive(true);
                        newItem.OnEquip(playerController);
                    }

                    if (PlayerArmIK.Instance != null)
                    {
                        PlayerArmIK.Instance.EquipItem(CurrentItem, currentHeldItem);
                    }

                    ViewModelRoot.Instance.UpdateEquippedItem(currentHeldItem, CurrentItem);
                }, lowerDuration, raiseDuration, lowerDistance);

                while (ViewModelRoot.Instance != null && ViewModelRoot.Instance.IsTransitioning)
                {
                    yield return null;
                }

                isSwitching = false;
                switchCoroutine = null;
                yield break;
            }

            // Fallback nếu không dùng ViewModelRoot
            // 1. Hạ bất kỳ vật phẩm nào đang active (trừ vật mới)
            for (int i = 0; i < instantiatedHeldItems.Length; i++)
            {
                var item = instantiatedHeldItems[i];
                if (item != null && item != newItem && item.gameObject.activeSelf)
                {
                    Transform t = item.transform;
                    Vector3 startPos = t.localPosition;
                    Vector3 lowerPos = startPos + Vector3.down * lowerDistance;

                    float elapsed = 0f;
                    while (elapsed < lowerDuration)
                    {
                        elapsed += Time.deltaTime;
                        float progress = Mathf.Clamp01(elapsed / lowerDuration);
                        t.localPosition = Vector3.Lerp(startPos, lowerPos, progress);
                        yield return null;
                    }

                    t.localPosition = lowerPos;
                    item.OnUnequip();
                }
            }

            currentHeldItem = newItem;

            if (PlayerArmIK.Instance != null)
            {
                PlayerArmIK.Instance.EquipItem(CurrentItem, currentHeldItem);
            }

            // 2. Nâng vật phẩm mới lên
            if (newItem != null)
            {
                newItem.gameObject.SetActive(true);
                newItem.OnEquip(playerController);

                Transform t = newItem.transform;
                var sway = newItem.GetComponent<ViewModelSway>();
                Vector3 defaultPos = sway != null ? sway.DefaultPosition : Vector3.zero;
                Vector3 startPos = defaultPos + Vector3.down * lowerDistance;

                t.localPosition = startPos;

                float elapsed = 0f;
                while (elapsed < raiseDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = Mathf.Clamp01(elapsed / raiseDuration);
                    t.localPosition = Vector3.Lerp(startPos, defaultPos, progress);
                    yield return null;
                }

                t.localPosition = defaultPos;
            }

            isSwitching = false;
            switchCoroutine = null;
        }

        private void HandlePrimaryActionInput()
        {
            if (isSwitching) return; // Không cho click khi đang nâng/hạ

            bool leftClicked = false;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                leftClicked = true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetMouseButtonDown(0))
                {
                    leftClicked = true;
                }
            }
            catch {}
#endif

            if (leftClicked)
            {
                // Nếu con trỏ chưa khóa và người chơi click, tự động khóa con trỏ lại
                if (Cursor.lockState != CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }

                // Gửi thông báo cho Debug Overlay (F9)
                if (FlashlightDebugOverlay.Instance != null)
                {
                    FlashlightDebugOverlay.Instance.NotifyLeftClick();
                }

                // Ghi log chẩn đoán xác nhận sự kiện click tới script
                Debug.Log($"[INPUT_CLICK] Chuột trái ghi nhận! Slot: {selectedIndex + 1}, Item: {(CurrentItem != null ? CurrentItem.DisplayName : "Trống")}, HeldItem: {(currentHeldItem != null ? currentHeldItem.name : "null")}, isSwitching: {isSwitching}, CursorLock: {Cursor.lockState}, TimeScale: {Time.timeScale}, isDead: {isPlayerDead}");

                if (currentHeldItem != null)
                {
                    currentHeldItem.OnPrimaryUse();
                }
            }
        }

        private void HandlePlayerDied()
        {
            isPlayerDead = true;
            if (switchCoroutine != null)
            {
                StopCoroutine(switchCoroutine);
                switchCoroutine = null;
            }
            isSwitching = false;

            if (ViewModelRoot.Instance != null)
            {
                ViewModelRoot.Instance.SetVisibility(false);
            }

            if (currentHeldItem != null)
            {
                currentHeldItem.OnPlayerDied();
            }

            if (PlayerArmIK.Instance != null)
            {
                PlayerArmIK.Instance.OnPlayerDied();
            }

            for (int i = 0; i < instantiatedHeldItems.Length; i++)
            {
                if (instantiatedHeldItems[i] != null)
                {
                    instantiatedHeldItems[i].gameObject.SetActive(false);
                }
            }
        }

        private void HandlePlayerRespawned()
        {
            isPlayerDead = false;

            if (PlayerArmIK.Instance != null)
            {
                PlayerArmIK.Instance.OnPlayerRespawned();
            }

            if (ViewModelRoot.Instance != null)
            {
                ViewModelRoot.Instance.SetVisibility(true);
            }

            // Hồi sinh: reset ngay về ô 1 không bị chặn bởi kiểm tra selectedIndex trùng
            EquipInitialSlot(0);

            if (hotbarUI != null)
            {
                hotbarUI.SelectSlot(0, CurrentItem);
            }
        }

        void OnDrawGizmosSelected()
        {
            // Vẽ Gizmo phạm vi quét 35m của Radar khi chọn Player trong Scene View
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, 35.0f);
        }

        public void SetSlotData(int index, ItemData item)
        {
            if (index >= 0 && index < slots.Length)
            {
                slots[index] = item;
                if (hotbarUI != null)
                {
                    hotbarUI.UpdateSlotContent(index, item);
                }
            }
        }
    }
}
