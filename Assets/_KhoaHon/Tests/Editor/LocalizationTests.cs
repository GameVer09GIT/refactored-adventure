using KhoaHon.Core;
using KhoaHon.Notebook;
using NUnit.Framework;
using UnityEngine;

namespace KhoaHon.Tests
{
    /// <summary>Đa ngôn ngữ Việt/Anh và chuyển đổi file lưu v1 → v2 (Phase A — Blackwood).</summary>
    public class LocalizationTests
    {
        private Language saved;

        [SetUp] public void Remember() => saved = Loc.Current;
        [TearDown] public void Restore() => Loc.Set(saved, persist: false);

        [Test]
        public void PickFallsBackToVietnamese()
        {
            Assert.AreEqual("xin chào", Loc.Pick(Language.Vi, "xin chào", "hello"));
            Assert.AreEqual("hello", Loc.Pick(Language.En, "xin chào", "hello"));
            Assert.AreEqual("xin chào", Loc.Pick(Language.En, "xin chào", ""), "Thiếu bản tiếng Anh thì dùng tiếng Việt");
            Assert.AreEqual("xin chào", Loc.Pick(Language.En, "xin chào", null));
        }

        [Test]
        public void TextTableFollowsCurrentLanguage()
        {
            var t = ScriptableObject.CreateInstance<TextTable>();
            t.Set("ui.yes", "Đồng ý", "Yes");
            t.Set("ui.only_vi", "Chỉ tiếng Việt");
            Loc.Set(Language.Vi, persist: false);
            Assert.AreEqual("Đồng ý", t.Get("ui.yes"));
            Loc.Set(Language.En, persist: false);
            Assert.AreEqual("Yes", t.Get("ui.yes"));
            Assert.AreEqual("Chỉ tiếng Việt", t.Get("ui.only_vi"));
            Assert.AreEqual("missing.key", t.Get("missing.key"));
            Object.DestroyImmediate(t);
        }

        [Test]
        public void ChangedFiresOnlyOnRealChange()
        {
            Loc.Set(Language.Vi, persist: false);
            int fired = 0;
            void H() => fired++;
            Loc.Changed += H;
            try
            {
                Loc.Set(Language.Vi, persist: false);
                Assert.AreEqual(0, fired);
                Loc.Set(Language.En, persist: false);
                Assert.AreEqual(1, fired);
            }
            finally { Loc.Changed -= H; }
        }

        [Test]
        public void NotebookSourceLocalizesTitleAndBody()
        {
            var d = ScriptableObject.CreateInstance<DocumentData>();
            d.SetBasic("DOC_X", "Di thư", ChronoYear.Y1996, "Nội dung");
            d.SetEnglish("Last Letter", "Body");
            Loc.Set(Language.Vi, persist: false);
            Assert.AreEqual("Di thư", d.Title);
            Loc.Set(Language.En, persist: false);
            Assert.AreEqual("Last Letter", d.Title);
            Assert.AreEqual("Body", d.Body);
            Object.DestroyImmediate(d);
        }

        [Test]
        public void SaveV1MigratesToV2WithDefaultCharacter()
        {
            var d = SaveData.FromJson("{\"version\":1,\"stage\":\"Stage2_Floor2\"}");
            Assert.IsNotNull(d);
            Assert.AreEqual(SaveData.CurrentVersion, d.version);
            Assert.AreEqual(PlayerCharacter.Evelyn, d.CharacterValue);
            Assert.AreEqual(GameStage.Stage2_Floor2, d.StageValue);
        }

        [Test]
        public void SaveV2KeepsCharacter()
        {
            var d = new SaveData { character = PlayerCharacter.Alex.ToString() };
            var back = SaveData.FromJson(d.ToJson());
            Assert.AreEqual(PlayerCharacter.Alex, back.CharacterValue);
            var bad = SaveData.FromJson("{\"version\":2,\"character\":\"Nobody\"}");
            Assert.AreEqual(PlayerCharacter.Evelyn, bad.CharacterValue, "Tên lạ thì về Evelyn");
        }
    }
}
