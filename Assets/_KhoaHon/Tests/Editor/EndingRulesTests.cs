using System.Collections.Generic;
using KhoaHon.Core;
using KhoaHon.Sequences;
using NUnit.Framework;

namespace KhoaHon.Tests
{
    /// <summary>Điều kiện lựa chọn ở Sảnh Tế (Phase F — Blackwood).</summary>
    public class EndingRulesTests
    {
        private static System.Func<StoryFlag, bool> Has(params StoryFlag[] flags)
        {
            var set = new HashSet<StoryFlag>(flags);
            return set.Contains;
        }

        [Test]
        public void ReleaseNeedsAllRelicsIncludingHammer()
        {
            Assert.IsTrue(EndingRules.CanRelease(Has(StoryFlag.Relic_Moc, StoryFlag.Relic_Thuy, StoryFlag.Relic_Tho, StoryFlag.Relic_Kim, StoryFlag.Relic_Hoa)));
            Assert.IsFalse(EndingRules.CanRelease(Has(StoryFlag.Relic_Moc, StoryFlag.Relic_Thuy, StoryFlag.Relic_Tho, StoryFlag.Relic_Hoa)));
        }

        [Test]
        public void ClaimNeedsFifthVerse()
        {
            Assert.IsTrue(EndingRules.CanClaim(Has(StoryFlag.Relic_Hoa)));
            Assert.IsFalse(EndingRules.CanClaim(Has(StoryFlag.Relic_Kim)));
        }
    }
}
