using System;
using System.Globalization;
using System.IO;
using Game.Core.Save;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class SaveStoreTests
    {
        private string _directory;
        private string _filePath;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "SaveStoreTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _filePath = Path.Combine(_directory, "save.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        // JsonUtility 대신 단순한 구분자 형식을 쓴다.
        // SaveStore가 책임지는 것은 파일 처리와 검증이지 JSON 형식이 아니고,
        // 이렇게 해야 이 테스트가 엔진 없이도 돈다.
        private static string Serialize(SaveData data) => string.Join("|",
            data.version.ToString(CultureInfo.InvariantCulture),
            data.savedAtUtc,
            data.floor.ToString(CultureInfo.InvariantCulture),
            data.killsOnFloor.ToString(CultureInfo.InvariantCulture),
            data.goldMantissa.ToString("R", CultureInfo.InvariantCulture),
            data.goldExponent.ToString(CultureInfo.InvariantCulture),
            data.attackPowerLevel.ToString(CultureInfo.InvariantCulture),
            data.criticalMultiplierLevel.ToString(CultureInfo.InvariantCulture),
            data.diamonds.ToString(CultureInfo.InvariantCulture));

        private static SaveData Deserialize(string text)
        {
            string[] parts = text.Split('|');
            return new SaveData
            {
                version = int.Parse(parts[0], CultureInfo.InvariantCulture),
                savedAtUtc = parts[1],
                floor = int.Parse(parts[2], CultureInfo.InvariantCulture),
                killsOnFloor = int.Parse(parts[3], CultureInfo.InvariantCulture),
                goldMantissa = double.Parse(parts[4], CultureInfo.InvariantCulture),
                goldExponent = int.Parse(parts[5], CultureInfo.InvariantCulture),
                attackPowerLevel = int.Parse(parts[6], CultureInfo.InvariantCulture),
                criticalMultiplierLevel = int.Parse(parts[7], CultureInfo.InvariantCulture),
                diamonds = int.Parse(parts[8], CultureInfo.InvariantCulture)
            };
        }

        private SaveStore CreateStore() => new SaveStore(_filePath, Serialize, Deserialize);

        [Test]
        public void SaveThenLoad_RoundTripsValues()
        {
            CreateStore().Save(new SaveData
            {
                floor = 42,
                killsOnFloor = 7,
                goldMantissa = 1.2345d,
                goldExponent = 400,
                diamonds = 135
            });

            Assert.IsTrue(CreateStore().TryLoad(out SaveData loaded));
            Assert.AreEqual(42, loaded.floor);
            Assert.AreEqual(7, loaded.killsOnFloor);
            Assert.AreEqual(135, loaded.diamonds);

            // 지수 400은 double로는 표현할 수 없는 크기다. 가수와 지수를 따로 저장하는 이유다.
            Assert.AreEqual(1.2345d, loaded.goldMantissa, 1e-12);
            Assert.AreEqual(400, loaded.goldExponent);
        }

        [Test]
        public void Save_StampsVersionAndTimestamp()
        {
            DateTime before = DateTime.UtcNow.AddSeconds(-1d);
            var data = new SaveData { floor = 1 };

            CreateStore().Save(data);

            Assert.AreEqual(SaveData.CurrentVersion, data.version);
            DateTime savedAt = DateTime.Parse(data.savedAtUtc, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);
            Assert.GreaterOrEqual(savedAt, before);
        }

        [Test]
        public void TryLoad_MissingFile_ReturnsFalse()
        {
            Assert.IsFalse(CreateStore().TryLoad(out SaveData loaded));
            Assert.IsNull(loaded);
        }

        [Test]
        public void TryLoad_TamperedPayload_ReturnsFalse()
        {
            CreateStore().Save(new SaveData { floor = 3 });

            // 골드를 손으로 고친 상황. 체크섬은 그대로 두었다.
            string content = File.ReadAllText(_filePath);
            File.WriteAllText(_filePath, content.Replace("|3|", "|9999|"));

            Assert.IsFalse(CreateStore().TryLoad(out SaveData loaded));
            Assert.IsNull(loaded);
        }

        [Test]
        public void TryLoad_GarbageFile_ReturnsFalse()
        {
            File.WriteAllText(_filePath, "이건 세이브 파일이 아니다");

            Assert.IsFalse(CreateStore().TryLoad(out SaveData loaded));
            Assert.IsNull(loaded);
        }

        /// <summary>파일은 멀쩡하지만 역직렬화 결과가 특정 버전으로 나오는 상황을 만든다.</summary>
        private SaveStore CreateStoreSeeingVersion(int version) =>
            new SaveStore(_filePath, Serialize, text =>
            {
                SaveData data = Deserialize(text);
                data.version = version;
                return data;
            });

        [Test]
        public void TryLoad_FutureVersion_ReturnsFalse()
        {
            CreateStore().Save(new SaveData { floor = 3 });

            // 이 빌드보다 새 세이브는 무엇이 들었는지 알 수 없으므로 건드리지 않는다.
            Assert.IsFalse(CreateStoreSeeingVersion(SaveData.CurrentVersion + 1).TryLoad(out SaveData loaded));
            Assert.IsNull(loaded);
        }

        [Test]
        public void TryLoad_UnknownOldVersion_ReturnsFalse()
        {
            CreateStore().Save(new SaveData { floor = 3 });

            // 마이그레이션 경로가 없는 옛 버전. 잘못 복원하느니 새 게임이 낫다.
            Assert.IsFalse(CreateStoreSeeingVersion(0).TryLoad(out SaveData loaded));
            Assert.IsNull(loaded);
        }

        [Test]
        public void TryLoad_MigratesVersion1ThroughEveryStep()
        {
            CreateStore().Save(new SaveData
            {
                floor = 5,
                killsOnFloor = 2,
                goldMantissa = 3.5d,
                goldExponent = 4
            });

            // v1은 현재 버전보다 두 단계 아래다. 체인이 1 -> 2 -> 3을 순서대로 통과해야 한다.
            Assert.IsTrue(CreateStoreSeeingVersion(1).TryLoad(out SaveData loaded));

            Assert.AreEqual(SaveData.CurrentVersion, loaded.version);

            // v1에 있던 값은 그대로 살아남는다.
            Assert.AreEqual(5, loaded.floor);
            Assert.AreEqual(2, loaded.killsOnFloor);
            Assert.AreEqual(4, loaded.goldExponent);

            // 이후 버전에서 추가된 값은 0에서 시작한다.
            Assert.AreEqual(0, loaded.attackPowerLevel);
            Assert.AreEqual(0, loaded.criticalMultiplierLevel);
            Assert.AreEqual(0, loaded.diamonds);
        }

        [Test]
        public void TryLoad_MigratesVersion2ToCurrent()
        {
            CreateStore().Save(new SaveData { floor = 12, attackPowerLevel = 8 });

            Assert.IsTrue(CreateStoreSeeingVersion(2).TryLoad(out SaveData loaded));

            Assert.AreEqual(SaveData.CurrentVersion, loaded.version);
            Assert.AreEqual(12, loaded.floor);
            Assert.AreEqual(8, loaded.attackPowerLevel, "v2에 있던 강화 단계는 유지된다");
            Assert.AreEqual(0, loaded.diamonds);
        }

        [Test]
        public void SaveThenLoad_RoundTripsUpgradeLevels()
        {
            CreateStore().Save(new SaveData { attackPowerLevel = 31, criticalMultiplierLevel = 7 });

            Assert.IsTrue(CreateStore().TryLoad(out SaveData loaded));
            Assert.AreEqual(31, loaded.attackPowerLevel);
            Assert.AreEqual(7, loaded.criticalMultiplierLevel);
        }

        [Test]
        public void TryLoad_DeserializerReturnsNull_ReturnsFalse()
        {
            CreateStore().Save(new SaveData { floor = 3 });

            var store = new SaveStore(_filePath, Serialize, _ => null);

            Assert.IsFalse(store.TryLoad(out SaveData loaded));
            Assert.IsNull(loaded);
        }

        [Test]
        public void Delete_RemovesTheSaveFile()
        {
            SaveStore store = CreateStore();
            store.Save(new SaveData { floor = 7 });

            store.Delete();

            Assert.IsFalse(File.Exists(_filePath));
            Assert.IsFalse(store.TryLoad(out SaveData loaded));
            Assert.IsNull(loaded);
        }

        [Test]
        public void Delete_WithoutASaveFile_DoesNothing()
        {
            Assert.DoesNotThrow(() => CreateStore().Delete());
        }

        [Test]
        public void SaveAfterDelete_StartsAFreshFile()
        {
            SaveStore store = CreateStore();
            store.Save(new SaveData { floor = 7 });
            store.Delete();

            store.Save(new SaveData { floor = 1 });

            Assert.IsTrue(store.TryLoad(out SaveData loaded));
            Assert.AreEqual(1, loaded.floor);
        }

        [Test]
        public void Save_OverwritesExistingSave()
        {
            SaveStore store = CreateStore();
            store.Save(new SaveData { floor = 1 });
            store.Save(new SaveData { floor = 2 });

            Assert.IsTrue(store.TryLoad(out SaveData loaded));
            Assert.AreEqual(2, loaded.floor);
        }

        [Test]
        public void Save_LeavesNoTemporaryFileBehind()
        {
            SaveStore store = CreateStore();
            store.Save(new SaveData { floor = 1 });
            store.Save(new SaveData { floor = 2 });

            Assert.IsFalse(File.Exists(_filePath + ".tmp"), "임시 파일이 남았다");
            Assert.AreEqual(1, Directory.GetFiles(_directory).Length);
        }
    }
}
