using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý Jumpscare ảnh ma 3 giây, tiếng hét nữ thất thanh cực lớn,
/// và Màn hình YOU DIED với 2 nút HỒI SINH / THOÁT GAME ngay chính giữa màn hình, dễ nhìn thấy nhất và dễ bấm nhất.
/// </summary>
public class DeathScreenManager : MonoBehaviour
{
    public static DeathScreenManager Instance { get; private set; }

    // Các sự kiện vòng đời phục vụ hệ thống âm thanh (PlayerFootsteps, PlayerBreathing, PlayerHeartbeat)
    public static event System.Action OnPlayerDied;
    public static event System.Action OnPlayerRespawned;

    [Header("1. Cấu hình Jumpscare")]
    [Tooltip("Thời gian hiện ảnh ma jumpscare (giây)")]
    [SerializeField] private float jumpscareDuration = 3.0f;

    [Tooltip("Clip âm thanh tiếng hét nữ thất thanh")]
    [SerializeField] private AudioClip screamAudioClip;

    [Header("2. Tham chiếu Player & Ma")]
    [SerializeField] private PlayerController player;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GhostAI ghostAI;

    // Các thành phần UI tự động quản lý
    private Canvas deathCanvas;
    private GameObject jumpscareRoot;
    private RawImage jumpscareRawImage;
    private GameObject deathScreenRoot;
    private RawImage deathScreenRawImage;
    private CanvasGroup deathScreenGroup;
    private Button respawnBtn;
    private Button quitBtn;
    private AudioSource audioSource;

    private Texture2D jumpscareTexture;
    private Texture2D youDiedTexture;
    private Coroutine activeSequence;
    private Vector3 originalJumpscarePos;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        FindReferences();
        LoadTexturesAndAudio();
        BuildCanvasAndUI();
    }

    void Start()
    {
        HideAllImmediate();
    }

    private void FindReferences()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (ghostAI == null) ghostAI = FindFirstObjectByType<GhostAI>();
        if (spawnPoint == null)
        {
            GameObject sp = GameObject.Find("SpawnPoint");
            if (sp != null) spawnPoint = sp.transform;
        }
    }

    /// <summary>
    /// Tải trực tiếp texture và audio từ Resources hoặc AssetDatabase/File
    /// </summary>
    public void LoadTexturesAndAudio()
    {
        // 1. Tải ảnh ma kinh dị anh-ma-kinh-di-30
        jumpscareTexture = LoadTextureSafely("anh-ma-kinh-di-30", "anh-ma-kinh-di-30.jpg");

        // 2. Tải ảnh YOU DIED
        youDiedTexture = LoadTextureSafely("you-died", "you-died.jpg");

        // 3. Tải âm thanh tiếng hét nữ thất thanh
#if UNITY_EDITOR
        if (screamAudioClip == null)
        {
            screamAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Ghost/Audio/jumpscare_scream.wav");
        }
#endif
        if (screamAudioClip == null)
        {
            screamAudioClip = Resources.Load<AudioClip>("jumpscare_scream");
        }

        // Tạo AudioSource riêng phát 2D âm lượng tối đa 1.0 (to nhất thẳng vào tai người chơi)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0.0f; // 2D Sound
        audioSource.playOnAwake = false;
        audioSource.volume = 1.0f;
    }

    private Texture2D LoadTextureSafely(string resourceName, string fileName)
    {
        Texture2D tex = Resources.Load<Texture2D>(resourceName);
        if (tex != null) return tex;

        string diskPath = Path.Combine(Application.dataPath, "Ghost/Textures", fileName);
        if (File.Exists(diskPath))
        {
            byte[] bytes = File.ReadAllBytes(diskPath);
            Texture2D diskTex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (diskTex.LoadImage(bytes)) return diskTex;
        }

        string resDiskPath = Path.Combine(Application.dataPath, "Ghost/Resources", fileName);
        if (File.Exists(resDiskPath))
        {
            byte[] bytes = File.ReadAllBytes(resDiskPath);
            Texture2D diskTex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (diskTex.LoadImage(bytes)) return diskTex;
        }

        return null;
    }

    /// <summary>
    /// Tạo hoặc làm mới hoàn toàn Canvas độc lập với sortingOrder = 32767,
    /// đặt 2 nút bấm NGAY CHÍNH GIỮA MÀN HÌNH bên dưới dòng chữ YOU DIED để người chơi nhìn thấy rõ nhất và bấm dễ nhất.
    /// </summary>
    public void BuildCanvasAndUI()
    {
        // 1. Luôn hủy bỏ canvas cũ nếu có để tránh lưu vết RectTransform sai lệch trước đó
        Transform existing = transform.Find("DeathJumpscareCanvas");
        if (existing != null)
        {
            if (Application.isPlaying)
                Destroy(existing.gameObject);
            else
                DestroyImmediate(existing.gameObject);
        }

        GameObject canvasObj = new GameObject("DeathJumpscareCanvas");
        canvasObj.transform.SetParent(transform, false);

        deathCanvas = canvasObj.AddComponent<Canvas>();
        deathCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        deathCanvas.sortingOrder = 32767; // Luôn hiển thị đè lên trên tất cả UI khác

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // 2. Tạo Layer Jumpscare (Full screen ảnh ma kinh dị)
        jumpscareRoot = new GameObject("JumpscareLayer");
        jumpscareRoot.transform.SetParent(canvasObj.transform, false);

        RectTransform jRt = jumpscareRoot.AddComponent<RectTransform>();
        jRt.anchorMin = Vector2.zero;
        jRt.anchorMax = Vector2.one;
        jRt.offsetMin = Vector2.zero;
        jRt.offsetMax = Vector2.zero;

        jumpscareRawImage = jumpscareRoot.AddComponent<RawImage>();
        jumpscareRawImage.color = Color.white;
        if (jumpscareTexture != null)
        {
            jumpscareRawImage.texture = jumpscareTexture;
        }
        originalJumpscarePos = jumpscareRawImage.transform.localPosition;
        jumpscareRoot.SetActive(false);

        // 3. Tạo Layer Death Screen (Full screen ảnh YOU DIED + 2 nút trung tâm)
        deathScreenRoot = new GameObject("DeathScreenLayer");
        deathScreenRoot.transform.SetParent(canvasObj.transform, false);

        RectTransform dRt = deathScreenRoot.AddComponent<RectTransform>();
        dRt.anchorMin = Vector2.zero;
        dRt.anchorMax = Vector2.one;
        dRt.offsetMin = Vector2.zero;
        dRt.offsetMax = Vector2.zero;

        // Thêm nền đen tuyền phủ 100% màn hình
        Image blackBg = deathScreenRoot.AddComponent<Image>();
        blackBg.color = Color.black;
        blackBg.raycastTarget = false;

        // Tạo GameObject riêng cho ảnh YOU DIED: Dịch nhẹ lên Y = +35 để dòng chữ YOU DIED nằm cân xứng ngay tâm màn hình
        GameObject youDiedObj = new GameObject("YouDiedImage");
        youDiedObj.transform.SetParent(deathScreenRoot.transform, false);

        RectTransform ydRt = youDiedObj.AddComponent<RectTransform>();
        ydRt.anchorMin = new Vector2(0.5f, 0.5f);
        ydRt.anchorMax = new Vector2(0.5f, 0.5f);
        ydRt.pivot = new Vector2(0.5f, 0.5f);
        ydRt.anchoredPosition = new Vector2(0f, 35f);
        ydRt.sizeDelta = new Vector2(1920f, 1080f);

        deathScreenRawImage = youDiedObj.AddComponent<RawImage>();
        deathScreenRawImage.color = Color.white;
        if (youDiedTexture != null)
        {
            deathScreenRawImage.texture = youDiedTexture;
        }

        deathScreenGroup = deathScreenRoot.AddComponent<CanvasGroup>();
        deathScreenGroup.alpha = 0f;
        deathScreenGroup.interactable = false;
        deathScreenGroup.blocksRaycasts = false;

        // 4. CỤM NÚT BẤM ĐẶT NGAY BÊN DƯỚI DÒNG CHỮ YOU DIED (Khoảng cách thoáng 42px, tuyệt đối không bị che khuất)
        GameObject buttonContainer = new GameObject("ButtonContainer");
        buttonContainer.transform.SetParent(deathScreenRoot.transform, false);

        RectTransform btnContainerRt = buttonContainer.AddComponent<RectTransform>();
        // Neo chính giữa màn hình (0.5, 0.5)
        btnContainerRt.anchorMin = new Vector2(0.5f, 0.5f);
        btnContainerRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnContainerRt.pivot = new Vector2(0.5f, 0.5f);
        // Tọa độ Y = -185: cách đáy chữ YOU DIED hơn 41 pixel, bố cục vàng đẹp mắt
        btnContainerRt.anchoredPosition = new Vector2(0f, -185f);
        btnContainerRt.sizeDelta = new Vector2(650f, 86f);

        // Nền mờ tối sang trọng bao quanh cụm nút giúp nổi bật trên mọi nền
        Image containerBg = buttonContainer.AddComponent<Image>();
        containerBg.color = new Color(0.04f, 0.04f, 0.04f, 0.70f);
        containerBg.raycastTarget = false;

        // NÚT 1: HỒI SINH (Màu đỏ máu nổi bật rực rỡ, to lớn, dễ bấm)
        respawnBtn = CreateButton(
            parent: buttonContainer.transform,
            name: "RespawnButton",
            pos: new Vector2(-155f, 0f),
            size: new Vector2(280f, 72f),
            textLabel: "HỒI SINH",
            normalColor: new Color(0.83f, 0.18f, 0.18f, 1.0f),      // Đỏ son kinh dị
            highlightedColor: new Color(1.0f, 0.32f, 0.32f, 1.0f),  // Đỏ tươi khi rê chuột
            pressedColor: new Color(0.52f, 0.05f, 0.05f, 1.0f)      // Đỏ sẫm khi click
        );
        respawnBtn.onClick.RemoveAllListeners();
        respawnBtn.onClick.AddListener(OnRespawnButtonClicked);

        // NÚT 2: THOÁT GAME (Màu xám đậm sang trọng, tương phản cao, dễ bấm)
        quitBtn = CreateButton(
            parent: buttonContainer.transform,
            name: "QuitButton",
            pos: new Vector2(155f, 0f),
            size: new Vector2(280f, 72f),
            textLabel: "THOÁT GAME",
            normalColor: new Color(0.24f, 0.28f, 0.32f, 1.0f),      // Xám than lịch lãm
            highlightedColor: new Color(0.38f, 0.44f, 0.50f, 1.0f),  // Sáng hơn khi hover
            pressedColor: new Color(0.12f, 0.14f, 0.16f, 1.0f)      // Tối khi nhấn
        );
        quitBtn.onClick.RemoveAllListeners();
        quitBtn.onClick.AddListener(OnQuitButtonClicked);

        deathScreenRoot.SetActive(false);
    }

    private Button CreateButton(
        Transform parent,
        string name,
        Vector2 pos,
        Vector2 size,
        string textLabel,
        Color normalColor,
        Color highlightedColor,
        Color pressedColor)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = btnObj.AddComponent<Image>();
        img.color = normalColor;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock cb = btn.colors;
        cb.normalColor = normalColor;
        cb.highlightedColor = highlightedColor;
        cb.pressedColor = pressedColor;
        cb.selectedColor = highlightedColor;
        cb.colorMultiplier = 1.0f;
        cb.fadeDuration = 0.12f;
        btn.colors = cb;

        // Nhãn chữ lớn, đậm nét, căn giữa hoàn hảo
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = textLabel;
        tmp.fontSize = 30;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        // Gán font SDF nếu có
        TMP_FontAsset defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (defaultFont != null)
        {
            tmp.font = defaultFont;
        }

        return btn;
    }

    /// <summary>
    /// Kích hoạt chuỗi Jumpscare 3 giây kèm tiếng hét nữ thất thanh cực lớn,
    /// sau đó hiện màn hình YOU DIED và 2 nút bấm ngay chính giữa màn hình.
    /// </summary>
    public void TriggerJumpscareSequence()
    {
        OnPlayerDied?.Invoke();

        if (jumpscareTexture == null || youDiedTexture == null || screamAudioClip == null)
        {
            LoadTexturesAndAudio();
            if (jumpscareRawImage != null && jumpscareTexture != null) jumpscareRawImage.texture = jumpscareTexture;
            if (deathScreenRawImage != null && youDiedTexture != null) deathScreenRawImage.texture = youDiedTexture;
        }

        if (activeSequence != null) StopCoroutine(activeSequence);
        activeSequence = StartCoroutine(JumpscareAndDeathRoutine());
    }

    private IEnumerator JumpscareAndDeathRoutine()
    {
        // 1. Khóa ngay điều khiển Player
        if (player != null)
        {
            player.SetControlEnabled(false);
        }

        // 2. Bật ảnh ma kinh dị full màn hình
        if (jumpscareRoot != null)
        {
            jumpscareRoot.SetActive(true);
            jumpscareRoot.transform.SetAsLastSibling();
        }

        // 3. Phát tiếng hét nữ thất thanh cực lớn (Volume tối đa, không bị trễ)
        if (audioSource != null && screamAudioClip != null)
        {
            audioSource.Stop();
            audioSource.clip = screamAudioClip;
            audioSource.volume = 1.0f;
            audioSource.Play();
        }

        // 4. Hiệu ứng rung giật, phóng to và chớp đỏ kinh dị trong 3 giây
        float elapsed = 0f;
        while (elapsed < jumpscareDuration)
        {
            elapsed += Time.deltaTime;

            if (jumpscareRawImage != null)
            {
                // Rung lắc dữ dội
                float shakeX = Random.Range(-26f, 26f);
                float shakeY = Random.Range(-26f, 26f);
                jumpscareRawImage.transform.localPosition = originalJumpscarePos + new Vector3(shakeX, shakeY, 0f);

                // Giật kích thước co giãn
                float scale = 1.06f + Mathf.PingPong(elapsed * 10f, 0.18f);
                jumpscareRawImage.transform.localScale = new Vector3(scale, scale, 1f);

                // Chớp ánh đỏ kinh dị
                jumpscareRawImage.color = Color.Lerp(Color.white, new Color(1f, 0.2f, 0.2f, 1f), Mathf.PingPong(elapsed * 14f, 1f));
            }

            yield return null;
        }

        // 5. Kết thúc jumpscare, tắt ảnh ma
        if (jumpscareRoot != null)
        {
            jumpscareRoot.SetActive(false);
            if (jumpscareRawImage != null)
            {
                jumpscareRawImage.transform.localPosition = originalJumpscarePos;
                jumpscareRawImage.transform.localScale = Vector3.one;
                jumpscareRawImage.color = Color.white;
            }
        }

        // 6. Hiển thị màn hình YOU DIED và 2 nút HỒI SINH & THOÁT GAME NGAY CHÍNH GIỮA MÀN HÌNH
        if (deathScreenRoot != null)
        {
            deathScreenRoot.SetActive(true);
            deathScreenRoot.transform.SetAsLastSibling();

            if (deathScreenGroup != null)
            {
                deathScreenGroup.alpha = 0f;
                deathScreenGroup.interactable = false;
                deathScreenGroup.blocksRaycasts = true;

                float fade = 0f;
                while (fade < 0.5f)
                {
                    fade += Time.deltaTime;
                    deathScreenGroup.alpha = Mathf.Clamp01(fade / 0.5f);
                    yield return null;
                }
                deathScreenGroup.alpha = 1f;
                deathScreenGroup.interactable = true;
            }
        }

        // 7. Mở khóa chuột và hiển thị con trỏ để người chơi bấm nút
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Xử lý bấm nút Hồi sinh
    /// </summary>
    public void OnRespawnButtonClicked()
    {
        HideAllImmediate();
        OnPlayerRespawned?.Invoke();

        // 1. Dịch chuyển Player về vị trí SpawnPoint
        if (player != null && spawnPoint != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = spawnPoint.position;
            player.transform.rotation = spawnPoint.rotation;

            if (cc != null) cc.enabled = true;

            // 2. Hồi đầy thanh thể lực (stamina)
            player.ResetStamina();

            // 3. Khóa chuột và bật lại điều khiển người chơi
            player.SetControlEnabled(true);
            player.LockCursor();
        }

        // 4. Hồi sinh con ma ở khoảng cách an toàn (>= 28m) ngoài tầm nhìn camera
        if (ghostAI != null)
        {
            Camera playerCam = player != null && player.playerCamera != null ? player.playerCamera.GetComponent<Camera>() : Camera.main;
            Vector3 respawnPos = spawnPoint != null ? spawnPoint.position : (player != null ? player.transform.position : Vector3.zero);
            ghostAI.RespawnGhost(respawnPos, playerCam);
        }
    }

    /// <summary>
    /// Xử lý bấm nút Thoát game
    /// </summary>
    public void OnQuitButtonClicked()
    {
        Debug.Log("[DeathScreenManager] Người chơi chọn Thoát game!");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void HideAllImmediate()
    {
        if (jumpscareRoot != null)
        {
            jumpscareRoot.SetActive(false);
        }

        if (deathScreenRoot != null)
        {
            if (deathScreenGroup != null)
            {
                deathScreenGroup.alpha = 0f;
                deathScreenGroup.interactable = false;
                deathScreenGroup.blocksRaycasts = false;
            }
            deathScreenRoot.SetActive(false);
        }
    }
}
