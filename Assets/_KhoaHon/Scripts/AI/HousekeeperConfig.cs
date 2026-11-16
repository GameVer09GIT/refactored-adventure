using UnityEngine;

namespace KhoaHon.AI
{
    /// <summary>Thông số "Chấp Pháp Quản Gia" (PROMPT 5.8). Chỉnh ở Inspector, không sửa code.</summary>
    [CreateAssetMenu(menuName = "KhoaHon/Config/Housekeeper", fileName = "KH_HousekeeperConfig")]
    public class HousekeeperConfig : ScriptableObject
    {
        [Header("Tốc độ")]
        public float patrolSpeed = 1.3f;
        public float suspiciousSpeed = 3.0f;
        [Tooltip("Hunt = hệ số × runSpeed của PlayerController")]
        public float huntSpeedMultiplier = 1.15f;
        [Tooltip("Hệ số Hunt sau khi Vy lấy chiếc lược")]
        public float weakenedHuntMultiplier = 1.0f;

        [Header("Giác quan")]
        [Range(30f, 180f)] public float fieldOfView = 110f;
        [Tooltip("Tầm nhìn khi đèn pin Vy tắt (m)")] public float sightRangeDark = 10f;
        [Tooltip("Tầm nhìn khi đèn pin Vy bật (m)")] public float sightRangeLit = 16f;
        [Tooltip("Đèn pin chiếu vào mặt trong tầm này → nghi ngờ (m)")] public float flashlightFaceRange = 12f;
        [Tooltip("Nửa góc nón đèn pin để tính 'chiếu vào mặt' (độ)")] public float flashlightConeHalfAngle = 22f;
        public float detectInterval = 0.15f;

        [Header("Hành vi")]
        public Vector2 patrolWait = new Vector2(2f, 4f);
        [Tooltip("Mất tầm nhìn bao lâu thì chuyển sang nghi ngờ (s)")] public float loseSightTime = 4f;
        public Vector2 searchTime = new Vector2(8f, 10f);
        public float killRange = 1.5f;
        public float flashStunTime = 3f;
        public float combStunTime = 20f;
        [Tooltip("Chế độ yếu: khựng lại mỗi lần phát hiện Vy (s)")] public float weakenedHesitate = 2f;

        [Header("Chống lại bằng đèn pin (rọi thẳng vào mặt)")]
        [Tooltip("Tầm đèn pin có tác dụng làm chói (m)")] public float lightStunRange = 8.5f;
        [Tooltip("Nửa góc phải nhắm trúng mặt (độ) — hẹp hơn nón 'bị phát hiện'")] public float lightStunHalfAngle = 11f;
        [Tooltip("Rọi liên tục bao lâu thì bà choáng (s)")] public float lightFillTime = 1.7f;
        [Tooltip("Ngừng rọi bao lâu thì thanh chói về 0 (s)")] public float lightDecayTime = 2.5f;
        [Tooltip("Hệ số tốc độ khi thanh chói gần đầy (bà chậm lại)")] [Range(0.2f, 1f)] public float lightSlowMultiplier = 0.42f;
        [Tooltip("Thời gian choáng vì chói mắt (s)")] public float lightStunTime = 3.5f;
        [Tooltip("Sau khi choáng, bà còn lóa mắt bao lâu (chỉ thấy trong 2,5 m) (s)")] public float lightBlindAfter = 2.2f;
        [Tooltip("Nhờn đèn bao lâu kể từ lúc choáng — trong lúc này rọi đèn vô dụng (s)")] public float lightImmuneTime = 15f;
        [Tooltip("% pin mất thêm mỗi giây khi đang rọi làm chói")] public float lightBatteryDrainPerSecond = 2.2f;

        [Header("Chuyển động (khớp bước chân với tốc độ — tránh trượt chân)")]
        [Tooltip("Tốc độ tự nhiên của clip đi bộ (m/s) — builder tự đo")] public float walkClipSpeed = 1.3f;
        [Tooltip("Tốc độ tự nhiên của clip chạy (m/s) — builder tự đo")] public float runClipSpeed = 5.5f;
        [Tooltip("Tốc độ xoay người (độ/s) khi tuần tra / truy đuổi")] public Vector2 turnSpeed = new Vector2(140f, 320f);

        [Header("Trốn")]
        [Tooltip("Đứng sát chỗ trốn bao lâu nếu thấy Vy chui vào (s)")] public float hidingInspectTime = 5.5f;
        [Tooltip("Thanh hơi nín thở cạn sau (s)")] public float breathHoldDuration = 7f;

        [Header("Âm thanh")]
        public float audioMinDistance = 2f;
        public float audioMaxDistance = 25f;
        [Tooltip("Khoảng cách giữa các lần hát ru (s)")] public Vector2 lullabyInterval = new Vector2(25f, 50f);
    }
}
