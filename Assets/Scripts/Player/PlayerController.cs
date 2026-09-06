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

    void Start()
    {
        controller = GetComponent<CharacterController>();
        currentStamina = maxStamina;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleCameraLook();
        HandleMovement();
        UpdateStaminaUI(); // Gọi cái UI chạy liên tục
    }

    void HandleCameraLook()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseDelta.y;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseDelta.x);
    }

    void HandleMovement()
    {
        float x = (Keyboard.current.dKey.isPressed ? 1 : 0) - (Keyboard.current.aKey.isPressed ? 1 : 0);
        float y = (Keyboard.current.wKey.isPressed ? 1 : 0) - (Keyboard.current.sKey.isPressed ? 1 : 0);

        Vector3 move = transform.right * x + transform.forward * y;

        bool isTryingToRun = Keyboard.current.leftShiftKey.isPressed && move.sqrMagnitude > 0.01f;
        bool isRunning = false;

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
}