namespace Blackwood.Items
{
    /// <summary>Loại pin. Chỉ thêm giá trị mới ở cuối (lưu theo chỉ số).</summary>
    public enum CellType
    {
        D = 0,          // pin đại — đèn pin
        AA = 1,         // pin tiểu — máy ghi âm (Evelyn)
        PhonePack = 2,  // sạc dự phòng — điện thoại (Alex)
        CamPack = 3,    // pin máy quay — kính nhìn đêm (Alex)
    }

    /// <summary>
    /// Logic pin thuần (test được): mỗi loại pin có một viên đang lắp (0..Max %) và số viên dự phòng trong túi.
    /// Thiết bị chỉ dùng được khi viên đang lắp còn điện; thay pin = bỏ viên cũ, lắp một viên dự phòng (đầy).
    /// </summary>
    public class PowerCells
    {
        public const int Count = 4;
        public const int MaxSpares = 9;

        private readonly float[] charge = new float[Count];
        private readonly int[] spares = new int[Count];

        public float Max { get; }

        public PowerCells(float max = 100f)
        {
            Max = max;
            for (int i = 0; i < Count; i++) charge[i] = max;
        }

        public float Charge(CellType t) => charge[(int)t];
        public float Fraction(CellType t) => Max > 0f ? charge[(int)t] / Max : 0f;
        public int Spares(CellType t) => spares[(int)t];
        public bool IsEmpty(CellType t) => charge[(int)t] <= 0f;

        /// <summary>Trừ điện. Trả về true đúng lần viên pin vừa cạn.</summary>
        public bool Drain(CellType t, float amount)
        {
            int i = (int)t;
            if (amount <= 0f || charge[i] <= 0f) return false;
            charge[i] -= amount;
            if (charge[i] > 0f) return false;
            charge[i] = 0f;
            return true;
        }

        public void SetCharge(CellType t, float v) => charge[(int)t] = v < 0f ? 0f : (v > Max ? Max : v);

        public void AddSpare(CellType t, int n = 1)
        {
            int i = (int)t;
            spares[i] = spares[i] + n > MaxSpares ? MaxSpares : (spares[i] + n < 0 ? 0 : spares[i] + n);
        }

        public void SetSpares(CellType t, int n) => spares[(int)t] = n < 0 ? 0 : (n > MaxSpares ? MaxSpares : n);

        /// <summary>Có nên thay không: còn viên dự phòng và viên đang lắp đã hao (tránh phí viên mới khi pin còn gần đầy).</summary>
        public bool CanReplace(CellType t) => spares[(int)t] > 0 && charge[(int)t] < Max * 0.95f;

        /// <summary>Lắp một viên dự phòng (đầy). False nếu hết dự phòng.</summary>
        public bool Replace(CellType t)
        {
            int i = (int)t;
            if (spares[i] <= 0) return false;
            spares[i]--;
            charge[i] = Max;
            return true;
        }

        public float[] ChargeSnapshot() => (float[])charge.Clone();
        public int[] SparesSnapshot() => (int[])spares.Clone();

        /// <summary>Nạp từ save; mảng thiếu/ngắn (save cũ) thì giữ giá trị hiện có cho phần thiếu.</summary>
        public void Load(float[] c, int[] s)
        {
            for (int i = 0; i < Count; i++)
            {
                if (c != null && i < c.Length) SetCharge((CellType)i, c[i]);
                if (s != null && i < s.Length) SetSpares((CellType)i, s[i]);
            }
        }
    }
}
