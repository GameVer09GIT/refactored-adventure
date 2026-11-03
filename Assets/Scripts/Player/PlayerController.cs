using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform playerCamera;
    public float mouseSensitivity = 15f;

    [Header("Movement Settings")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 6.5f;
    public float gravity = -18f;

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float drainRate = 22f;
    public float regenRate = 8f;

    [Header("Animation")]
    public Animator animator;

    [Header("Stamina UI")]
    public CanvasGroup staminaCanvas;
    public Image staminaFillBar;
    public Outline borderOutline; // Cắm cái viền rỗng ruột vào đây
    public Color normalBorderColor = Color.black; // Màu viền mặc định
    public Color blinkColor = Color.red; // Màu viền khi cạn lực

    private CharacterController controller;
    private float currentStamina;
    private float verticalVelocity;
    private float xRotation = 0f;
    private bool isRunning;

    // Thuộc tính chỉ-đọc tối thiểu hỗ trợ hệ thống âm thanh (PlayerFootsteps, PlayerBreathing)
    public float CurrentStamina => currentStamina;
    public float NormalizedStamina => maxStamina > 0f ? Mathf.Clamp01(currentStamina / maxStamina) : 1f;
    public bool IsRunning => isRunning;
    public bool IsGrounded => controller != null && controller.isGrounded;
    public Vector3 HorizontalVelocity => controller != null ? new Vector3(controller.velocity.x, 0f, controller.velocity.z) : Vector3.zero;
    public float HorizontalSpeed => controller != null ? new Vector2(controller.velocity.x, controller.velocity.z).magnitude : 0f;
    public CharacterController CharacterController => controller;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        EnsureAudioComponents();
    }

    private void EnsureAudioComponents()
    {
        if (GetComponent<HorrorAudio.PlayerFootsteps>() == null)
        {
            gameObject.AddComponent<HorrorAudio.PlayerFootsteps>();
        }
        if (GetComponent<HorrorAudio.PlayerBreathing>() == null)
        {
            gameObject.AddComponent<HorrorAudio.PlayerBreathing>();
        }
        if (GetComponent<HorrorAudio.PlayerHeartbeat>() == null)
        {
            gameObject.AddComponent<HorrorAudio.PlayerHeartbeat>();
        }
    }

    void Start()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        currentStamina = maxStamina;

        LockCursor();
    }

    void Update()
    {
        // Tự động khóa con trỏ khi click vào màn hình game
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)
            {
                if (Cursor.lockState != CursorLockMode.Locked && !HorrorGame.Inventory.PlayerArmIKTuningUI.IsVisible)
                {
                    LockCursor();
                }
            }
        }

        // Bấm Escape để nhả con trỏ chuột
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            UnlockCursor();
        }

        HandleCameraLook();
        HandleMovement();
        UpdateStaminaUI(); // Gọi cái UI chạy liên tục
    }

    void HandleCameraLook()
    {
        Vector2 mouseDelta = Vector2.zero;
        if (Mouse.current != null)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity * Time.deltaTime;
            }
        }
#if ENABLE_LEGACY_INPUT_MANAGER
        else
        {
            try
            {
                mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * mouseSensitivity * 10f * Time.deltaTime;
            }
            catch {}
        }
#endif

        xRotation -= mouseDelta.y;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
        transform.Rotate(Vector3.up * mouseDelta.x);
    }

    public void SetPitch(float pitch)
    {
        xRotation = Mathf.Clamp(pitch, -90f, 90f);
        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    void HandleMovement()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (Keyboard.current == null) return;

        float x = (Keyboard.current.dKey.isPressed ? 1 : 0) - (Keyboard.current.aKey.isPressed ? 1 : 0);
        float y = (Keyboard.current.wKey.isPressed ? 1 : 0) - (Keyboard.current.sKey.isPressed ? 1 : 0);

        Vector3 move = transform.right * x + transform.forward * y;

        bool isTryingToRun = Keyboard.current.leftShiftKey.isPressed && move.sqrMagnitude > 0.01f;
        isRunning = false;

        if (isTryingToRun && currentStamina > 0)
        {
            currentStamina -= drainRate * Time.deltaTime;
            isRunning = true;
        }
        else if (currentStamina < maxStamina)
        {
            currentStamina += regenRate * Time.deltaTime;
        }

        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move.normalized * currentSpeed;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);

        Vector3 flatVelocity = new Vector3(velocity.x, 0, velocity.z);
        if (animator != null)
        {
            animator.SetFloat("Speed", flatVelocity.magnitude);
        }
    }

    void UpdateStaminaUI()
    {
        // Kiểm tra xem đã cắm đủ dây chưa
        if (staminaFillBar == null || staminaCanvas == null || borderOutline == null) return;

        float pct = currentStamina / maxStamina;

        // Ép ruột trắng rút về giữa
        staminaFillBar.rectTransform.localScale = new Vector3(pct, 1f, 1f);

        // Tàng hình khi đầy
        if (pct >= 1f) staminaCanvas.alpha = 0f;
        else staminaCanvas.alpha = 1f;

        // Chớp viền Outline khi cạn lực
        if (pct <= 0.01f)
        {
            borderOutline.effectColor = Color.Lerp(normalBorderColor, blinkColor, Mathf.PingPong(Time.time * 10f, 1f));
        }
        else
        {
            borderOutline.effectColor = normalBorderColor;
        }
    }

    /// <summary>
    /// Hồi đầy stamina và cập nhật UI ngay lập tức
    /// </summary>
    public void ResetStamina()
    {
        currentStamina = maxStamina;
        UpdateStaminaUI();
    }

    /// <summary>
    /// Bật hoặc tắt điều khiển nhân vật
    /// </summary>
    public void SetControlEnabled(bool isEnabled)
    {
        this.enabled = isEnabled;
        if (!isEnabled)
        {
            verticalVelocity = 0f;
            if (animator != null) animator.SetFloat("Speed", 0f);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// Khóa chuột vào tâm màn hình khi chơi
    /// </summary>
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Mở khóa chuột để tương tác UI
    /// </summary>
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}