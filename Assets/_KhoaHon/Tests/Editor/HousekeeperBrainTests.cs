using KhoaHon.AI;
using NUnit.Framework;

namespace KhoaHon.Tests
{
    /// <summary>Bảng chuyển trạng thái "Chấp Pháp Quản Gia" (PROMPT 5.8).</summary>
    public class HousekeeperBrainTests
    {
        // loseSight 4, search 8–10, flash 3, comb 20, hesitate 2, kill 1.5, inspect 5.5
        private static HousekeeperBrain New() => new HousekeeperBrain(4f, 8f, 10f, 3f, 20f, 2f, 1.5f, 5.5f, 7);

        private static Perception See(float dist = 6f) => new Perception { seesPlayer = true, playerReachable = true, distanceToPlayer = dist };
        private static Perception Nothing() => new Perception { playerReachable = true, distanceToPlayer = 20f };

        [Test]
        public void PatrolToHuntWhenSeen()
        {
            var b = New();
            Assert.AreEqual(HousekeeperState.Hunt, b.Tick(0.1f, See()));
        }

        [Test]
        public void PatrolToSuspiciousOnNoiseOrFlashlight()
        {
            var b = New();
            Assert.AreEqual(HousekeeperState.Suspicious, b.Tick(0.1f, new Perception { heardNoise = true, playerReachable = true }));
            var c = New();
            Assert.AreEqual(HousekeeperState.Suspicious, c.Tick(0.1f, new Perception { flashlightOnFace = true, playerReachable = true }));
        }

        [Test]
        public void SuspiciousTimesOutToPatrol()
        {
            var b = New();
            b.Tick(0.1f, new Perception { heardNoise = true, playerReachable = true });
            for (int i = 0; i < 110; i++) b.Tick(0.1f, Nothing());
            Assert.AreEqual(HousekeeperState.Patrol, b.State);
        }

        [Test]
        public void HuntLosesSightAfter4Seconds()
        {
            var b = New();
            b.Tick(0.1f, See());
            for (int i = 0; i < 38; i++) b.Tick(0.1f, Nothing());
            Assert.AreEqual(HousekeeperState.Hunt, b.State, "3.8 s chưa mất dấu");
            for (int i = 0; i < 4; i++) b.Tick(0.1f, Nothing());
            Assert.AreEqual(HousekeeperState.Suspicious, b.State);
        }

        [Test]
        public void HuntKillsInRange()
        {
            var b = New();
            b.Tick(0.1f, See());
            Assert.AreEqual(HousekeeperState.Kill, b.Tick(0.1f, See(1.2f)));
        }

        [Test]
        public void HuntStopsWhenPlayerLeavesFloor2()
        {
            var b = New();
            b.Tick(0.1f, See());
            var p = See(); p.playerReachable = false;
            Assert.AreEqual(HousekeeperState.Suspicious, b.Tick(0.1f, p));
        }

        [Test]
        public void FlashStunsThreeSecondsThenHunt()
        {
            var b = New();
            b.OnFlashed();
            Assert.AreEqual(HousekeeperState.Stunned, b.State);
            for (int i = 0; i < 29; i++) b.Tick(0.1f, Nothing());
            Assert.AreEqual(HousekeeperState.Stunned, b.State);
            b.Tick(0.2f, Nothing());
            Assert.AreEqual(HousekeeperState.Hunt, b.State);
            Assert.IsFalse(b.Hesitating, "Sau flash lao thẳng, không khựng");
        }

        [Test]
        public void CombStunsTwentySecondsThenPatrolAndWeakens()
        {
            var b = New();
            b.OnCombTaken();
            Assert.IsTrue(b.Weakened);
            for (int i = 0; i < 199; i++) b.Tick(0.1f, Nothing());
            Assert.AreEqual(HousekeeperState.Stunned, b.State);
            b.Tick(0.2f, Nothing());
            Assert.AreEqual(HousekeeperState.Patrol, b.State);
        }

        [Test]
        public void WeakenedHesitatesTwoSecondsBeforeCharging()
        {
            var b = New();
            b.SetWeakened(true);
            b.Tick(0.1f, See());
            Assert.IsTrue(b.Hesitating);
            // Trong lúc khựng, dù ở sát cũng chưa giết
            Assert.AreEqual(HousekeeperState.Hunt, b.Tick(0.5f, See(1.0f)));
            b.Tick(1.0f, See(5f)); b.Tick(0.6f, See(5f));
            Assert.IsFalse(b.Hesitating);
            Assert.AreEqual(HousekeeperState.Kill, b.Tick(0.1f, See(1.2f)));
        }

        [Test]
        public void HiddenUnseenIsIgnored_SeenEnteringIsInspected()
        {
            var b = New();
            var hidden = new Perception { playerHidden = true, playerReachable = true, seesPlayer = false };
            Assert.AreEqual(HousekeeperState.Patrol, b.Tick(0.1f, hidden));
            hidden.sawPlayerHide = true;
            Assert.AreEqual(HousekeeperState.Inspect, b.Tick(0.1f, hidden));
            b.OnBreathFailed();
            Assert.AreEqual(HousekeeperState.Kill, b.State);
        }

        [Test]
        public void InspectEndsAfterStandingTime()
        {
            var b = New();
            var hidden = new Perception { playerHidden = true, sawPlayerHide = true, playerReachable = true };
            b.Tick(0.1f, hidden);
            hidden.arrivedAtTarget = true;
            for (int i = 0; i < 50; i++) b.Tick(0.1f, hidden);
            Assert.AreEqual(HousekeeperState.Inspect, b.State);
            for (int i = 0; i < 8; i++) b.Tick(0.1f, hidden);
            Assert.AreEqual(HousekeeperState.Patrol, b.State);
        }
    }
}
