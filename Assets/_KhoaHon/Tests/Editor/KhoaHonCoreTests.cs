using System.Collections.Generic;
using KhoaHon.Core;
using KhoaHon.Notebook;
using NUnit.Framework;
using UnityEngine;

namespace KhoaHon.Tests
{
    /// <summary>EditMode test cho logic thuần của Phase 1.</summary>
    public class StoryFlagsTests
    {
        [Test]
        public void SetHasClear()
        {
            var f = new StoryFlags();
            Assert.IsFalse(f.Has(StoryFlag.SafeOpened));
            Assert.IsTrue(f.Set(StoryFlag.SafeOpened));
            Assert.IsFalse(f.Set(StoryFlag.SafeOpened), "Bật lần 2 không tính là mới");
            Assert.IsTrue(f.Has(StoryFlag.SafeOpened));
            Assert.IsTrue(f.Clear(StoryFlag.SafeOpened));
            Assert.IsFalse(f.Has(StoryFlag.SafeOpened));
        }

        [Test]
        public void NoneIsIgnored()
        {
            var f = new StoryFlags();
            Assert.IsFalse(f.Set(StoryFlag.None));
            Assert.IsFalse(f.Has(StoryFlag.None));
            Assert.AreEqual(0, f.Count);
        }

        [Test]
        public void ChangedEventFires()
        {
            var f = new StoryFlags();
            var got = new List<(StoryFlag, bool)>();
            f.Changed += (fl, v) => got.Add((fl, v));
            f.Set(StoryFlag.CellarOpened);
            f.Set(StoryFlag.CellarOpened);
            f.Clear(StoryFlag.CellarOpened);
            CollectionAssert.AreEqual(new[] { (StoryFlag.CellarOpened, true), (StoryFlag.CellarOpened, false) }, got);
        }

        [Test]
        public void NamesRoundTripAndUnknownIgnored()
        {
            var f = new StoryFlags();
            f.Set(StoryFlag.HasCrowbar);
            f.Set(StoryFlag.Relic_Hoa);
            var names = f.ToNames();
            names.Add("FlagKhongTonTai");
            var g = new StoryFlags();
            g.LoadNames(names);
            Assert.AreEqual(2, g.Count);
            Assert.IsTrue(g.HasAll(StoryFlag.HasCrowbar, StoryFlag.Relic_Hoa));
        }

        [Test]
        public void HanhFlagMapping()
        {
            Assert.AreEqual(StoryFlag.Relic_Moc, HanhUtil.RelicFlag(Hanh.Moc));
            Assert.AreEqual(StoryFlag.Relic_Hoa, HanhUtil.RelicFlag(Hanh.Hoa));
            Assert.AreEqual(StoryFlag.Talisman_Thuy_Placed, HanhUtil.TalismanPlacedFlag(Hanh.Thuy));
            Assert.AreEqual(StoryFlag.Talisman_Kim_Placed, HanhUtil.TalismanPlacedFlag(Hanh.Kim));
            for (int i = 0; i < 5; i++)
            {
                StringAssert.EndsWith(((Hanh)i).ToString(), HanhUtil.RelicFlag((Hanh)i).ToString());
                StringAssert.Contains(((Hanh)i).ToString(), HanhUtil.TalismanPlacedFlag((Hanh)i).ToString());
            }
        }
    }

    public class SaveDataTests
    {
        [Test]
        public void JsonRoundTrip()
        {
            var d = new SaveData
            {
                stage = GameStage.Stage2_Floor2.ToString(),
                checkpointId = "CP_Floor2Landing",
                flags = new List<string> { "SafeOpened", "HasUpstairsKey" },
                collected = new List<string> { "Pickup_QuanBackpack" },
                unlockedDoors = new List<string> { "Door_Stair" },
                keys = new List<string> { "key_upstairs" },
                notebookEntries = new List<string> { "TAPE_1", "DOC_1921_BangDong" },
                notebookSeen = new List<string> { "TAPE_1" },
                battery = 42.5f, film = 3, sanity = 61f,
            };
            var back = SaveData.FromJson(d.ToJson());
            Assert.NotNull(back);
            Assert.AreEqual(GameStage.Stage2_Floor2, back.StageValue);
            Assert.AreEqual("CP_Floor2Landing", back.checkpointId);
            CollectionAssert.AreEqual(d.flags, back.flags);
            CollectionAssert.AreEqual(d.collected, back.collected);
            CollectionAssert.AreEqual(d.notebookEntries, back.notebookEntries);
            Assert.AreEqual(42.5f, back.battery, 1e-4);
            Assert.AreEqual(3, back.film);
        }

        [Test]
        public void InvalidJsonReturnsNull()
        {
            Assert.IsNull(SaveData.FromJson(""));
            Assert.IsNull(SaveData.FromJson("{ khong phai json"));
        }

        [Test]
        public void MissingListsAreRepairedAndBadStageFallsBack()
        {
            var back = SaveData.FromJson("{\"version\":1,\"stage\":\"Stage99\"}");
            Assert.NotNull(back);
            Assert.NotNull(back.flags);
            Assert.NotNull(back.collected);
            Assert.NotNull(back.photoClues);
            Assert.AreEqual(GameStage.Stage0_Gate, back.StageValue);
        }
    }

    public class TextTableTests
    {
        [Test]
        public void GetFormatAndMissingKey()
        {
            var t = ScriptableObject.CreateInstance<TextTable>();
            t.Set("prompt.pickup", "[E] Nhặt {0}");
            Assert.AreEqual("[E] Nhặt balo của Quân", t.Format("prompt.pickup", "balo của Quân"));
            Assert.AreEqual("khong.co", t.Get("khong.co"));
            t.Set("prompt.pickup", "Nhặt {0}");
            Assert.AreEqual("Nhặt pin", t.Format("prompt.pickup", "pin"));
            Object.DestroyImmediate(t);
        }
    }

    public class NotebookTests
    {
        private static T Make<T>(string id, ChronoYear y) where T : NotebookSource
        {
            var s = ScriptableObject.CreateInstance<T>();
            s.SetBasic(id, id, y, "");
            return s;
        }

        [Test]
        public void SortedByYearThenOrderAndDeduplicated()
        {
            var go = new GameObject("nb");
            var nm = go.AddComponent<NotebookManager>();
            var a = Make<DocumentData>("A_1996", ChronoYear.Y1996);
            var b = Make<DocumentData>("B_1921", ChronoYear.Y1921);
            var c = Make<DocumentData>("C_1996", ChronoYear.Y1996);
            var tape = Make<TapeData>("T_now", ChronoYear.Present);
            Assert.IsTrue(nm.Add(a));
            Assert.IsTrue(nm.Add(b));
            Assert.IsTrue(nm.Add(c));
            Assert.IsFalse(nm.Add(a), "Không thêm trùng");
            nm.Add(tape);

            var docs = nm.EntriesFor(NotebookTab.Documents);
            CollectionAssert.AreEqual(new[] { "B_1921", "A_1996", "C_1996" }, docs.ConvertAll(x => x.Id));
            Assert.AreEqual(1, nm.EntriesFor(NotebookTab.Tapes).Count);
            Assert.AreEqual(3, nm.UnseenCount(NotebookTab.Documents));
            nm.MarkSeen(b);
            Assert.AreEqual(2, nm.UnseenCount(NotebookTab.Documents));
            Assert.AreEqual(3, nm.TotalUnseen);

            var save = new SaveData();
            nm.Capture(save);
            CollectionAssert.AreEqual(new[] { "A_1996", "B_1921", "C_1996", "T_now" }, save.notebookEntries);
            CollectionAssert.AreEqual(new[] { "B_1921" }, save.notebookSeen);

            Object.DestroyImmediate(go);
            foreach (var s in new NotebookSource[] { a, b, c, tape }) Object.DestroyImmediate(s);
        }
    }
}
