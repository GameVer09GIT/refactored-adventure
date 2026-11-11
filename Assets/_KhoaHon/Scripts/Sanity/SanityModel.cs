namespace KhoaHon.Sanity
{
    /// <summary>Đầu vào một nhịp tính lý trí.</summary>
    public struct SanityInput
    {
        public bool inLight;          // đèn pin bật hoặc đứng gần nguồn sáng
        public bool seeingHousekeeper; // đang nhìn thẳng vào Mrs. Peake
        public bool hunted;            // đang bị truy đuổi
        public bool hiding;            // đang trốn (bóng tối an toàn hơn)
    }

    /// <summary>
    /// Lý trí ẩn (0–100, không có thanh hiển thị). Giảm trong bóng tối, khi nhìn Mrs. Peake, khi bị săn;
    /// hồi chậm khi ở chỗ sáng (tối đa tới trần hồi). Chia 4 bậc để kích hoạt hiệu ứng giác quan.
    /// Logic thuần — không phụ thuộc Unity.
    /// </summary>
    public class SanityModel
    {
        public const float Min = 5f, Max = 100f;

        public float darkDrain = 0.35f;     // /giây trong bóng tối
        public float seeDrain = 3.5f;       // /giây khi nhìn thẳng vào bà ta
        public float huntDrain = 1.2f;      // /giây khi bị săn
        public float lightRegen = 0.6f;     // /giây ở chỗ sáng
        public float regenCap = 75f;        // ánh sáng chỉ hồi tới mức này

        public float Value { get; private set; }

        public SanityModel(float start = 100f) { Value = Clamp(start); }

        /// <summary>Bậc: 0 ổn (≥70), 1 bất an (≥45), 2 hoảng (≥25), 3 vỡ vụn (&lt;25).</summary>
        public int Tier => Value >= 70f ? 0 : Value >= 45f ? 1 : Value >= 25f ? 2 : 3;

        /// <summary>Tiến một nhịp; trả về true nếu đổi bậc.</summary>
        public bool Step(SanityInput i, float dt)
        {
            int before = Tier;
            float d = 0f;
            if (i.seeingHousekeeper) d -= seeDrain;
            if (i.hunted) d -= huntDrain;
            if (!i.inLight && !i.hiding) d -= darkDrain;
            if (i.inLight && !i.seeingHousekeeper && !i.hunted && Value < regenCap) d += lightRegen;
            Value = Clamp(Value + d * dt);
            if (d > 0f && Value > regenCap && Value - d * dt <= regenCap) Value = regenCap;
            return Tier != before;
        }

        /// <summary>Sự kiện tức thời (jumpscare, đặt sai trang, nghe băng).</summary>
        public bool Add(float amount)
        {
            int before = Tier;
            Value = Clamp(Value + amount);
            return Tier != before;
        }

        public void Set(float v) => Value = Clamp(v);

        private static float Clamp(float v) => v < Min ? Min : v > Max ? Max : v;
    }
}
