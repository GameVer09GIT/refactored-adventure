using Blackwood.AI;
using NUnit.Framework;

namespace Blackwood.Tests
{
    /// <summary>Cơ chế rọi đèn pin chống lại Mrs. Peake: thanh chói, chậm dần, choáng, nhờn đèn.</summary>
    public class LightExposureTests
    {
        // đầy sau 2 s, tụt hết sau 4 s, nhờn 10 s
        private static LightExposure New() => new LightExposure(2f, 4f, 10f);

        [Test]
        public void FillsWhileLitAndTriggersOnce()
        {
            var e = New();
            int fired = 0;
            for (int i = 0; i < 19; i++) if (e.Tick(0.1f, true)) fired++;
            Assert.AreEqual(0, fired, "1,9 s chưa đủ");
            Assert.Greater(e.Value, 0.9f);
            for (int i = 0; i < 5; i++) if (e.Tick(0.1f, true)) fired++;
            Assert.AreEqual(1, fired, "chỉ kích hoạt đúng một lần");
            Assert.IsTrue(e.Immune);
            Assert.AreEqual(0f, e.Value);
        }

        [Test]
        public void DecaysWhenNotLit()
        {
            var e = New();
            for (int i = 0; i < 10; i++) e.Tick(0.1f, true);   // 50%
            Assert.AreEqual(0.5f, e.Value, 0.02f);
            for (int i = 0; i < 20; i++) e.Tick(0.1f, false);  // −50% sau 2 s
            Assert.AreEqual(0f, e.Value, 0.02f);
        }

        [Test]
        public void ImmuneBlocksRefillUntilExpired()
        {
            var e = New();
            for (int i = 0; i < 25; i++) e.Tick(0.1f, true);
            Assert.IsTrue(e.Immune);
            bool fired = false;
            for (int i = 0; i < 70; i++) fired |= e.Tick(0.1f, true); // rọi suốt trong lúc nhờn (tổng ~7,4 s < 10 s)
            Assert.IsFalse(fired);
            Assert.AreEqual(0f, e.Value);
            for (int i = 0; i < 30; i++) e.Tick(0.1f, false);
            Assert.IsFalse(e.Immune);
            for (int i = 0; i < 25; i++) fired |= e.Tick(0.1f, true);
            Assert.IsTrue(fired, "hết nhờn thì rọi lại được");
        }

        [Test]
        public void SpeedFactorSlowsWithExposure()
        {
            var e = New();
            Assert.AreEqual(1f, e.SpeedFactor(false, 0.4f));
            float first = e.SpeedFactor(true, 0.4f);
            Assert.AreEqual(0.8f, first, 0.001f, "vừa chạm đèn đã khựng nhẹ");
            for (int i = 0; i < 18; i++) e.Tick(0.1f, true);
            float late = e.SpeedFactor(true, 0.4f);
            Assert.Less(late, 0.5f);
            Assert.GreaterOrEqual(late, 0.4f);
            for (int i = 0; i < 5; i++) e.Tick(0.1f, true);
            Assert.AreEqual(1f, e.SpeedFactor(true, 0.4f), "đang nhờn thì không chậm");
        }

        [Test]
        public void BrainBlindedStunsThenLosesTrack()
        {
            // loseSight 4, search 8–10, flash 3, comb 20, hesitate 2, kill 1.5, inspect 5.5, seed 7, lightStun 3.5
            var b = new HousekeeperBrain(4f, 8f, 10f, 3f, 20f, 2f, 1.5f, 5.5f, 7, 3.5f);
            var see = new Perception { seesPlayer = true, playerReachable = true, distanceToPlayer = 6f };
            var none = new Perception { playerReachable = true, distanceToPlayer = 20f };
            b.Tick(0.1f, see);
            Assert.AreEqual(HousekeeperState.Hunt, b.State);
            b.OnBlinded();
            Assert.AreEqual(HousekeeperState.Stunned, b.State);
            Assert.AreEqual(StunCause.Light, b.Stun);
            for (int i = 0; i < 34; i++) b.Tick(0.1f, see);
            Assert.AreEqual(HousekeeperState.Stunned, b.State, "3,4 s vẫn choáng dù người chơi đứng trước mặt");
            for (int i = 0; i < 3; i++) b.Tick(0.1f, none);
            Assert.AreEqual(HousekeeperState.Suspicious, b.State, "hết choáng thì mất dấu, không lao tới ngay");
        }

        [Test]
        public void BlindedDoesNotInterruptKillOrRestartStun()
        {
            var b = new HousekeeperBrain(4f, 8f, 10f, 3f, 20f, 2f, 1.5f, 5.5f, 7, 3.5f);
            b.Tick(0.1f, new Perception { seesPlayer = true, playerReachable = true, distanceToPlayer = 6f });
            b.Tick(0.1f, new Perception { seesPlayer = true, playerReachable = true, distanceToPlayer = 1.0f });
            Assert.AreEqual(HousekeeperState.Kill, b.State);
            b.OnBlinded();
            Assert.AreEqual(HousekeeperState.Kill, b.State);
        }
    }
}
