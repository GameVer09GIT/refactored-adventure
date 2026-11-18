using System;

namespace Blackwood.AI
{
    /// <summary>
    /// Mức "chói mắt" của Mrs. Peake khi bị rọi đèn pin thẳng vào mặt — C# thuần để test EditMode.
    /// Rọi liên tục: thanh đầy dần (đồng thời bà chậm lại); đầy thì choáng và bà "nhờn" đèn một lúc
    /// (không thể làm choáng liên tục). Ngừng rọi: thanh tụt dần.
    /// </summary>
    public class LightExposure
    {
        /// <summary>0 = chưa bị rọi, 1 = sắp choáng.</summary>
        public float Value { get; private set; }
        /// <summary>Thời gian nhờn đèn còn lại (giây).</summary>
        public float ImmuneLeft { get; private set; }
        public bool Immune => ImmuneLeft > 0f;

        private readonly float fillTime, decayTime, immuneTime;

        public LightExposure(float fillTime, float decayTime, float immuneTime)
        {
            this.fillTime = Math.Max(0.05f, fillTime);
            this.decayTime = Math.Max(0.05f, decayTime);
            this.immuneTime = Math.Max(0f, immuneTime);
        }

        /// <summary>Cập nhật một khung. Trả về true đúng khung thanh đầy (→ gọi choáng).</summary>
        public bool Tick(float dt, bool lit)
        {
            if (ImmuneLeft > 0f)
            {
                ImmuneLeft = Math.Max(0f, ImmuneLeft - dt);
                Value = 0f;
                return false;
            }
            if (lit)
            {
                Value += dt / fillTime;
                if (Value >= 1f)
                {
                    Value = 0f;
                    ImmuneLeft = immuneTime;
                    return true;
                }
            }
            else Value = Math.Max(0f, Value - dt / decayTime);
            return false;
        }

        /// <summary>Hệ số tốc độ khi đang bị rọi: 1 (không ảnh hưởng) → slowAtFull khi thanh gần đầy.</summary>
        public float SpeedFactor(bool lit, float slowAtFull)
        {
            if (!lit || Immune) return 1f;
            // Vừa chạm đèn đã khựng nhẹ (0.8), càng rọi lâu càng chậm
            float start = Math.Min(1f, Math.Max(slowAtFull, 0.8f));
            return start + (slowAtFull - start) * Value;
        }

        public void Reset() { Value = 0f; ImmuneLeft = 0f; }
    }
}
