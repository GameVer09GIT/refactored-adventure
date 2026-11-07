using KhoaHon.Sanity;
using NUnit.Framework;

namespace KhoaHon.Tests
{
    /// <summary>Lý trí ẩn (Phase E — Blackwood).</summary>
    public class SanityModelTests
    {
        [Test]
        public void DarknessDrainsLightRegensToCap()
        {
            var m = new SanityModel(80f);
            m.Step(new SanityInput { inLight = false }, 10f);
            Assert.Less(m.Value, 80f);
            var low = new SanityModel(60f);
            for (int i = 0; i < 200; i++) low.Step(new SanityInput { inLight = true }, 1f);
            Assert.AreEqual(low.regenCap, low.Value, 0.01f, "Ánh sáng chỉ hồi tới trần");
        }

        [Test]
        public void SeeingHerDrainsFastAndChangesTier()
        {
            var m = new SanityModel(72f);
            Assert.AreEqual(0, m.Tier);
            bool changed = m.Step(new SanityInput { inLight = true, seeingHousekeeper = true }, 2f);
            Assert.IsTrue(changed);
            Assert.AreEqual(1, m.Tier);
        }

        [Test]
        public void HidingInDarkDoesNotDrain()
        {
            var m = new SanityModel(50f);
            m.Step(new SanityInput { inLight = false, hiding = true }, 10f);
            Assert.AreEqual(50f, m.Value, 0.001f);
        }

        [Test]
        public void ClampedToRange()
        {
            var m = new SanityModel(10f);
            m.Add(-100f);
            Assert.AreEqual(SanityModel.Min, m.Value);
            m.Add(500f);
            Assert.AreEqual(SanityModel.Max, m.Value);
            Assert.AreEqual(3, new SanityModel(20f).Tier);
        }
    }
}
