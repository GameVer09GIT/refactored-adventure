using KhoaHon.Core;
using KhoaHon.Puzzles;
using NUnit.Framework;

namespace KhoaHon.Tests
{
    /// <summary>Két sắt 1921 và 5 bệ nến (Phase C — Blackwood).</summary>
    public class SafeCodeTests
    {
        [Test]
        public void WrongThenRight()
        {
            var s = new SafeLock("1921");
            Assert.IsFalse(s.Try("1924"), "Năm hoàn thành không phải mã");
            Assert.AreEqual(1, s.Attempts);
            Assert.IsTrue(s.Try("1921"));
            Assert.IsTrue(s.IsOpen);
            Assert.IsTrue(s.Try("0000"), "Đã mở thì luôn mở");
        }

        [Test]
        public void DigitsWrapAround()
        {
            Assert.AreEqual("1921", SafeLock.FromDigits(new[] { 1, 9, 2, 1 }));
            Assert.AreEqual("0919", SafeLock.FromDigits(new[] { 10, -1, 11, 19 }));
        }
    }

    public class AltarPuzzleTests
    {
        [Test]
        public void CorrectPageFillsStand()
        {
            var a = new AltarPuzzle();
            Assert.AreEqual(AltarResult.Correct, a.Place(Hanh.Tho, Hanh.Tho, true));
            Assert.IsTrue(a.IsFilled(Hanh.Tho));
            Assert.AreEqual(AltarResult.AlreadyFilled, a.Place(Hanh.Tho, Hanh.Tho, true));
        }

        [Test]
        public void WrongOrMissingPageRejected()
        {
            var a = new AltarPuzzle();
            Assert.AreEqual(AltarResult.Wrong, a.Place(Hanh.Moc, Hanh.Hoa, true));
            Assert.IsFalse(a.IsFilled(Hanh.Moc));
            Assert.AreEqual(AltarResult.NotInHand, a.Place(Hanh.Moc, Hanh.Moc, false));
        }

        [Test]
        public void CompleteAfterFive()
        {
            var a = new AltarPuzzle();
            for (int i = 0; i < 4; i++) a.Place((Hanh)i, (Hanh)i, true);
            Assert.IsFalse(a.IsComplete);
            a.Place(Hanh.Hoa, Hanh.Hoa, true);
            Assert.IsTrue(a.IsComplete);
        }
    }
}
