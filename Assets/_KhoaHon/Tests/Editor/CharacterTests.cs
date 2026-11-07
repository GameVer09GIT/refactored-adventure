using KhoaHon.Characters;
using KhoaHon.Core;
using NUnit.Framework;

namespace KhoaHon.Tests
{
    /// <summary>Livestream chat của Alex (Phase D — Blackwood).</summary>
    public class ChatDirectorTests
    {
        [Test]
        public void NoGhostsOnGroundFloor()
        {
            for (float r = 0f; r < 1f; r += 0.05f)
            {
                Assert.AreEqual("chat.s0", ChatDirector.PickPool(GameStage.Stage0_Gate, Threat.Hunting, r));
                Assert.AreEqual("chat.s1", ChatDirector.PickPool(GameStage.Stage1_GroundFloor, Threat.Near, r));
            }
        }

        [Test]
        public void GhostsWarnWhenHuntingUpstairs()
        {
            Assert.AreEqual("chat.ghost.hunt", ChatDirector.PickPool(GameStage.Stage2_Floor2, Threat.Hunting, 0.5f));
            Assert.AreEqual("chat.ghost.near", ChatDirector.PickPool(GameStage.Stage2_Floor2, Threat.Near, 0.1f));
            Assert.AreEqual("chat.s2", ChatDirector.PickPool(GameStage.Stage2_Floor2, Threat.None, 0.1f));
        }

        [Test]
        public void ViewersGrowWithDepth()
        {
            Assert.Less(ChatDirector.TargetViewers(GameStage.Stage0_Gate, Threat.None), ChatDirector.TargetViewers(GameStage.Stage3_Cellar, Threat.None));
            Assert.Less(ChatDirector.TargetViewers(GameStage.Stage2_Floor2, Threat.None), ChatDirector.TargetViewers(GameStage.Stage2_Floor2, Threat.Hunting));
        }
    }
}
