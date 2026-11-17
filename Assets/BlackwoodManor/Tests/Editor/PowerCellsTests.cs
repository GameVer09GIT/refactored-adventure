using Blackwood.Items;
using NUnit.Framework;

namespace Blackwood.Tests
{
    public class PowerCellsTests
    {
        [Test]
        public void StartsFullWithNoSpares()
        {
            var p = new PowerCells(100f);
            for (int i = 0; i < PowerCells.Count; i++)
            {
                Assert.AreEqual(100f, p.Charge((CellType)i));
                Assert.AreEqual(0, p.Spares((CellType)i));
            }
        }

        [Test]
        public void DrainReportsEmptyOnceAndClampsAtZero()
        {
            var p = new PowerCells(100f);
            Assert.IsFalse(p.Drain(CellType.D, 60f));
            Assert.IsTrue(p.Drain(CellType.D, 60f));
            Assert.AreEqual(0f, p.Charge(CellType.D));
            Assert.IsTrue(p.IsEmpty(CellType.D));
            Assert.IsFalse(p.Drain(CellType.D, 10f), "đã cạn thì không báo lại");
            Assert.AreEqual(100f, p.Charge(CellType.AA), "các loại pin độc lập");
        }

        [Test]
        public void ReplaceUsesASpareAndRefills()
        {
            var p = new PowerCells(100f);
            p.SetCharge(CellType.CamPack, 0f);
            Assert.IsFalse(p.Replace(CellType.CamPack), "không có dự phòng");
            p.AddSpare(CellType.CamPack, 2);
            Assert.IsTrue(p.CanReplace(CellType.CamPack));
            Assert.IsTrue(p.Replace(CellType.CamPack));
            Assert.AreEqual(100f, p.Charge(CellType.CamPack));
            Assert.AreEqual(1, p.Spares(CellType.CamPack));
        }

        [Test]
        public void CannotReplaceWhenNearlyFull()
        {
            var p = new PowerCells(100f);
            p.AddSpare(CellType.D, 1);
            Assert.IsFalse(p.CanReplace(CellType.D));
            p.SetCharge(CellType.D, 50f);
            Assert.IsTrue(p.CanReplace(CellType.D));
        }

        [Test]
        public void SparesAreCapped()
        {
            var p = new PowerCells(100f);
            p.AddSpare(CellType.AA, 50);
            Assert.AreEqual(PowerCells.MaxSpares, p.Spares(CellType.AA));
            p.AddSpare(CellType.AA, -100);
            Assert.AreEqual(0, p.Spares(CellType.AA));
        }

        [Test]
        public void LoadToleratesOldShortArrays()
        {
            var p = new PowerCells(100f);
            p.Load(new[] { 30f }, null);
            Assert.AreEqual(30f, p.Charge(CellType.D));
            Assert.AreEqual(100f, p.Charge(CellType.PhonePack));
            p.Load(new[] { 10f, 20f, 30f, 40f }, new[] { 1, 2, 3, 4 });
            Assert.AreEqual(40f, p.Charge(CellType.CamPack));
            Assert.AreEqual(3, p.Spares(CellType.PhonePack));
        }
    }
}
