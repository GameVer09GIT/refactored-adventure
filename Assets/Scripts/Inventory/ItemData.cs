using UnityEngine;

namespace HorrorGame.Inventory
{
    public enum ItemType
    {
        None,
        Flashlight,
        Radar,
        Generic,
        // --- Khóa Hồn: chỉ thêm ở cuối để không lệch giá trị đã serialize ---
        Recorder,
        FilmCamera,
        Crowbar,
        // --- Blackwood Manor: nhân vật Alex ---
        Phone,
        NightVision
    }

    public enum GripType
    {
        None,
        OneHandRight,
        OneHandLeft,
        TwoHands
    }

    /// <summary>
    /// Định nghĩa thông tin một vật phẩm trong Hotbar và cách bàn tay cầm nó
    /// </summary>
    [CreateAssetMenu(fileName = "NewItemData", menuName = "Horror Game/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Thông tin cơ bản")]
        [Tooltip("Tên hiển thị của vật phẩm (hiển thị trên thanh Hotbar)")]
        [SerializeField] private string displayName = "Vật phẩm";

        [Tooltip("Loại vật phẩm")]
        [SerializeField] private ItemType itemType = ItemType.Generic;

        [Tooltip("Icon hiển thị trên ô Hotbar")]
        [SerializeField] private Sprite icon;

        [Header("Mô hình cầm tay (Viewmodel)")]
        [Tooltip("Prefab mô hình 3D cầm trên tay người chơi")]
        [SerializeField] private GameObject heldPrefab;

        [Header("Cấu hình bàn tay cầm vật phẩm")]
        [Tooltip("Kiểu cầm tay: Một tay phải, Một tay trái, Hai tay hoặc Không hiện tay")]
        [SerializeField] private GripType gripType = GripType.OneHandRight;

        [Tooltip("Tư thế bàn tay phải (HandPose) khi cầm vật phẩm này")]
        [SerializeField] private HandPose rightPose;

        [Tooltip("Tư thế bàn tay trái (HandPose) khi cầm vật phẩm này")]
        [SerializeField] private HandPose leftPose;

        [Tooltip("Vị trí tinh chỉnh bù cho tay phải (mét)")]
        [SerializeField] private Vector3 rightGripOffset = Vector3.zero;

        [Tooltip("Vị trí tinh chỉnh bù cho tay trái (mét)")]
        [SerializeField] private Vector3 leftGripOffset = Vector3.zero;

        [Header("Cấu hình IK Xương Thật (Humanoid Player Arm IK)")]
        [Tooltip("Vị trí mục tiêu của bàn tay trong không gian Camera")]
        [SerializeField] private Vector3 ikTargetLocalPosition = new Vector3(0.18f, -0.15f, 0.36f);

        [Tooltip("Góc xoay mục tiêu của bàn tay trong không gian Camera")]
        [SerializeField] private Vector3 ikTargetLocalRotation = Vector3.zero;

        [Tooltip("Vị trí điều hướng cùi chỏ (Elbow Hint) trong không gian Camera")]
        [SerializeField] private Vector3 elbowHintLocal = new Vector3(0.20f, -0.35f, -0.10f);

        [Tooltip("Vị trí bù của vật phẩm trong lòng bàn tay (Socket offset position)")]
        [SerializeField] private Vector3 gripOffsetPosition = Vector3.zero;

        [Tooltip("Góc xoay bù của vật phẩm trong lòng bàn tay (Socket offset rotation)")]
        [SerializeField] private Vector3 gripOffsetRotation = Vector3.zero;

        [Tooltip("Độ cuộn các ngón tay khi cầm vật phẩm này (0 = mở thẳng, 1 = gập chặt)")]
        [Range(0f, 1f)][SerializeField] private float thumbCurl = 0.55f;
        [Range(0f, 1f)][SerializeField] private float indexCurl = 0.85f;
        [Range(0f, 1f)][SerializeField] private float middleCurl = 0.90f;
        [Range(0f, 1f)][SerializeField] private float ringCurl = 0.88f;
        [Range(0f, 1f)][SerializeField] private float pinkyCurl = 0.82f;

        public string DisplayName => displayName;
        public ItemType ItemType => itemType;
        public Sprite Icon => icon;
        public GameObject HeldPrefab => heldPrefab;
        public GripType GripType => gripType;
        public HandPose RightPose => rightPose;
        public HandPose LeftPose => leftPose;
        public Vector3 RightGripOffset => rightGripOffset;
        public Vector3 LeftGripOffset => leftGripOffset;

        public Vector3 IkTargetLocalPosition { get => ikTargetLocalPosition; set => ikTargetLocalPosition = value; }
        public Vector3 IkTargetLocalRotation { get => ikTargetLocalRotation; set => ikTargetLocalRotation = value; }
        public Vector3 ElbowHintLocal { get => elbowHintLocal; set => elbowHintLocal = value; }
        public Vector3 GripOffsetPosition { get => gripOffsetPosition; set => gripOffsetPosition = value; }
        public Vector3 GripOffsetRotation { get => gripOffsetRotation; set => gripOffsetRotation = value; }
        public float ThumbCurl { get => thumbCurl; set => thumbCurl = value; }
        public float IndexCurl { get => indexCurl; set => indexCurl = value; }
        public float MiddleCurl { get => middleCurl; set => middleCurl = value; }
        public float RingCurl { get => ringCurl; set => ringCurl = value; }
        public float PinkyCurl { get => pinkyCurl; set => pinkyCurl = value; }

        public void SetData(string name, ItemType type, Sprite itemIcon, GameObject prefab, GripType grip = GripType.OneHandRight, HandPose rPose = null, HandPose lPose = null)
        {
            displayName = name;
            itemType = type;
            icon = itemIcon;
            heldPrefab = prefab;
            gripType = grip;
            rightPose = rPose;
            leftPose = lPose;
        }

        public void SetGripConfig(GripType grip, HandPose rPose, HandPose lPose, Vector3 rOffset = default, Vector3 lOffset = default)
        {
            gripType = grip;
            rightPose = rPose;
            leftPose = lPose;
            rightGripOffset = rOffset;
            leftGripOffset = lOffset;
        }
    }
}
