using NUnit.Framework;
using System.IO;
using System.Linq;
using TheWheeler.Editor;

namespace TheWheeler.Tests
{
    public class EditorLocalizationTests
    {
        [Test]
        public void KoreanTranslationsKeepTheFirstDuplicateAndIgnoreEmptyKeys()
        {
            var table = EditorLocalization.Parse("{\"stringData\":[{\"key\":\"record_name_10\",\"kr\":\"결단의 검\"},{\"key\":\"record_name_10\",\"kr\":\"다른 이름\"},{\"key\":\"\",\"kr\":\"무시\"}]}");
            Assert.That(table.Count, Is.EqualTo(1));
            Assert.That(table["record_name_10"], Is.EqualTo("결단의 검"));
        }
        [Test]
        public void MissingKeysAndLiteralNamesRemainReadable()
        {
            Assert.That(EditorLocalization.Resolve("No translation for this literal"), Is.EqualTo("No translation for this literal"));
            Assert.That(EditorLocalization.Resolve(null), Is.EqualTo(""));
            Assert.That(EditorLocalization.Matches("123", 123, "some_key"), Is.True);
            Assert.That(EditorLocalization.Matches("some_key", 123, "some_key"), Is.True);
        }
        [Test]
        public void LiveJsonResolvesInEditModeWithoutChangingTheSource()
        {
            if (File.Exists(EditorLocalization.DefaultPath) == false)
            {
                Assert.Ignore("No project localization JSON configured.");
            }

            var before = File.ReadAllBytes(EditorLocalization.DefaultPath);
            var translations = EditorLocalization.Parse(File.ReadAllText(EditorLocalization.DefaultPath));
            var row = translations.First(p => (string.IsNullOrEmpty(p.Value) == false));
            EditorLocalization.Invalidate();
            Assert.That(EditorLocalization.Resolve(row.Key), Is.EqualTo(row.Value));
            Assert.That(EditorLocalization.Matches(row.Value, 10, row.Key), Is.True);
            CollectionAssert.AreEqual(before, File.ReadAllBytes(EditorLocalization.DefaultPath));
        }
    }
}
