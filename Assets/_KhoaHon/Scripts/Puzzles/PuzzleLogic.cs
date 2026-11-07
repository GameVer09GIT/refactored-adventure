using KhoaHon.Core;

namespace KhoaHon.Puzzles
{
    /// <summary>Khóa số của két sắt (logic thuần, không phụ thuộc Unity).</summary>
    public class SafeLock
    {
        private readonly string code;
        public int Attempts { get; private set; }
        public bool IsOpen { get; private set; }
        public int Length => code.Length;

        public SafeLock(string code) { this.code = code ?? ""; }

        /// <summary>Thử mã. Đúng → mở vĩnh viễn; sai → tăng số lần thử.</summary>
        public bool Try(string input)
        {
            if (IsOpen) return true;
            Attempts++;
            if (input == code) IsOpen = true;
            return IsOpen;
        }

        /// <summary>Ghép các chữ số trên vòng quay thành chuỗi.</summary>
        public static string FromDigits(int[] digits)
        {
            var c = new char[digits.Length];
            for (int i = 0; i < digits.Length; i++) c[i] = (char)('0' + ((digits[i] % 10) + 10) % 10);
            return new string(c);
        }
    }

    public enum AltarResult
    {
        Correct,
        Wrong,
        AlreadyFilled,
        NotInHand,
    }

    /// <summary>
    /// 5 bệ nến, mỗi bệ đặt dưới tấm bia của một Mắt Trận. Đặt đúng trang Vademecum (trùng Hanh) thì bệ sáng;
    /// sai thì trang quay về túi. Đủ 5 bệ → xích hầm đứt.
    /// </summary>
    public class AltarPuzzle
    {
        private readonly bool[] filled = new bool[5];

        public bool IsFilled(Hanh slot) => filled[(int)slot];

        public int FilledCount
        {
            get { int n = 0; foreach (var f in filled) if (f) n++; return n; }
        }

        public bool IsComplete => FilledCount == 5;

        /// <param name="slot">Bệ (bia của Mắt Trận nào).</param>
        /// <param name="page">Trang Vademecum người chơi chọn.</param>
        /// <param name="inHand">Người chơi có trang đó trong túi không.</param>
        public AltarResult Place(Hanh slot, Hanh page, bool inHand)
        {
            if (filled[(int)slot]) return AltarResult.AlreadyFilled;
            if (!inHand) return AltarResult.NotInHand;
            if (slot != page) return AltarResult.Wrong;
            filled[(int)slot] = true;
            return AltarResult.Correct;
        }

        /// <summary>Khôi phục từ save (cờ Talisman_*_Placed).</summary>
        public void SetFilled(Hanh slot, bool value) => filled[(int)slot] = value;
    }
}
