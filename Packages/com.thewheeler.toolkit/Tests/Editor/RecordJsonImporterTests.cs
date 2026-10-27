using System;
using System.IO;
using NUnit.Framework;
using TheWheeler.Editor;

namespace TheWheeler.Tests
{
    public class RecordJsonImporterTests
    {
        static RecordImportInfo Info(int id)
        {
            return new RecordImportInfo
            {
                id = id,
                namekeycode = "name_" + id,
                description = "desc_" + id
            };
        }
        static RecordImportData Data(int id)
        {
            return new RecordImportData
            {
                id = id,
                recordType = 2,
                rarity = 0,
                targetFilter = "ALL"
            };
        }
        [Test]
        public void DuplicateAndMismatchedIdsAreRejectedBeforeImport()
        {
            Assert.Throws<InvalidDataException>(() => RecordJsonImporter.Validate(new[]
            {
                Info(8),
                Info(8)
            }, new[]
            {
                Data(8),
                Data(9)
            }));
            Assert.Throws<InvalidDataException>(() => RecordJsonImporter.Validate(new[]
            {
                Info(8)
            }, new[]
            {
                Data(9)
            }));
        }
        [Test]
        public void InvalidStatsAndSkillModifiersAreRejected()
        {
            var row = Data(8);
            row.stats = new[]
            {
                new RecordImportStat
                {
                    stat = "typo",
                    calcType = 1,
                    value = .05f
                }
            };
            Assert.Throws<InvalidDataException>(() => RecordJsonImporter.Validate(new[]
            {
                Info(8)
            }, new[]
            {
                row
            }));
            row.stats = null;
            row.skills = new[]
            {
                new RecordImportSkill
                {
                    skillID = 1001,
                    modifier = "Damage",
                    operation = "typo",
                    value = 1.2f
                }
            };
            Assert.Throws<InvalidDataException>(() => RecordJsonImporter.Validate(new[]
            {
                Info(8)
            }, new[]
            {
                row
            }));
        }
        [TestCase("ATK", 1)]
        [TestCase("ATTACK", 1)]
        [TestCase("ASPD", 3)]
        [TestCase("MOVESPEED", 4)]
        [TestCase("HP", 7)]
        [TestCase("HEALTH", 7)]
        public void LegacyStatAliasesHaveTheCurrentSerializedValue(string name, int value)
        {
            Assert.That(RecordJsonImporter.StatIndex(name), Is.EqualTo(value));
        }
        [Test]
        public void PackageDoesNotReferenceGameAssemblies()
        {
            foreach (var dependency in typeof(RecordJsonImporter).Assembly.GetReferencedAssemblies())
            {
                Assert.That(dependency.Name.StartsWith("Assembly-CSharp", StringComparison.Ordinal), Is.False);
            }

            Assert.That(typeof(RecordJsonImporter).Assembly.GetName().Name, Is.EqualTo("TheWheeler.Editor"));
        }
    }
}
