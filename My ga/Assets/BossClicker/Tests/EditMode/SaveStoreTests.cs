using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace BossClicker.Tests
{
    public sealed class SaveStoreTests
    {
        string folder;
        string path;
        GameBalance balance;

        [SetUp]
        public void SetUp()
        {
            folder = Path.Combine(Path.GetTempPath(), "BossClickerV4Tests-" + Guid.NewGuid());
            Directory.CreateDirectory(folder);
            path = Path.Combine(folder, "lootshot-save-v4-test.json");
            balance = ScriptableObject.CreateInstance<GameBalance>();
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(folder, true);
            UnityEngine.Object.DestroyImmediate(balance);
        }

        [TestCase(-1)]
        [TestCase(4)]
        public void MissingSaveStartsFreshAndRoundTripKeepsV4Progress(int clearedBoss)
        {
            var store = new SaveStore(path, balance);
            Assert.IsTrue(store.TryLoad(out var data, out _));
            Assert.AreEqual(4, data.schemaVersion);
            Assert.AreEqual(8, data.powerLevels.Length);
            Assert.AreEqual(8, data.ammoLevels.Length);
            data.coins = 4321;
            data.currentWeaponIndex = 1;
            data.highestOwnedWeaponIndex = 2;
            data.powerLevels[0] = 2;
            data.ammoLevels[1] = 3;
            data.goldLevel = 2;
            data.highestClearedBossIndex = clearedBoss;
            data.selectedBossIndex = clearedBoss + 1;

            Assert.IsTrue(store.TrySave(data, out _));
            Assert.IsTrue(store.TryLoad(out var loaded, out _));

            Assert.AreEqual(JsonUtility.ToJson(data), JsonUtility.ToJson(loaded));
            Assert.AreEqual(BattlePhase.Menu, new GameSession(balance, loaded).Phase);
        }

        [Test]
        public void V4SaveLeavesV1V2AndV3SaveFilesUntouched()
        {
            string v1 = Path.Combine(folder, "boss-clicker-save-v1.json");
            string v2 = Path.Combine(folder, "boss-forge-save-v2.json");
            string v3 = Path.Combine(folder, "lootshot-save-v3.json");
            File.WriteAllText(v1, "legacy one");
            File.WriteAllText(v2, "legacy two");
            File.WriteAllText(v3, "legacy three");
            var store = new SaveStore(path, balance);

            Assert.IsTrue(store.TrySave(new SaveData(balance.weapons.Length), out _));

            Assert.AreEqual("legacy one", File.ReadAllText(v1));
            Assert.AreEqual("legacy two", File.ReadAllText(v2));
            Assert.AreEqual("legacy three", File.ReadAllText(v3));
        }

        [Test]
        public void ResetReplacesAllV4ProgressAndCannotRecoverTheOldBackup()
        {
            var store = new SaveStore(path, balance);
            var data = new SaveData(balance.weapons.Length) {
                coins = 9999,
                currentWeaponIndex = 2,
                highestOwnedWeaponIndex = 2,
                goldLevel = 3,
                highestClearedBossIndex = 5,
                selectedBossIndex = 6
            };
            data.powerLevels[2] = 4;
            data.ammoLevels[2] = 3;
            Assert.IsTrue(store.TrySave(data, out _));

            Assert.IsTrue(store.TryReset(out var reset, out _));

            File.WriteAllText(path, "{broken");
            Assert.IsTrue(new SaveStore(path, balance).TryLoad(out var recovered, out _));

            Assert.AreEqual(0, reset.coins);
            Assert.AreEqual(0, reset.currentWeaponIndex);
            Assert.AreEqual(0, reset.highestOwnedWeaponIndex);
            Assert.AreEqual(-1, reset.highestClearedBossIndex);
            Assert.AreEqual(0, reset.selectedBossIndex);
            CollectionAssert.AreEqual(new int[balance.weapons.Length], reset.powerLevels);
            CollectionAssert.AreEqual(new int[balance.weapons.Length], reset.ammoLevels);
            Assert.AreEqual(0, recovered.coins);
            Assert.AreEqual(-1, recovered.highestClearedBossIndex);
        }

        [Test]
        public void CorruptMainRecoversBackupWithoutOverwritingIt()
        {
            var store = new SaveStore(path, balance);
            var data = new SaveData(balance.weapons.Length) { coins = 20 };
            Assert.IsTrue(store.TrySave(data, out _));
            data.coins = 30;
            Assert.IsTrue(store.TrySave(data, out _));
            File.WriteAllText(path, "{broken");

            Assert.IsTrue(store.TryLoad(out var recovered, out var message));

            Assert.AreEqual(20, recovered.coins);
            Assert.IsNotEmpty(message);
        }

        [Test]
        public void InvalidRangesAreRejectedAndFilesArePreserved()
        {
            var invalid = new SaveData(balance.weapons.Length) { currentWeaponIndex = 99 };
            File.WriteAllText(path, JsonUtility.ToJson(invalid));
            var store = new SaveStore(path, balance);

            Assert.IsFalse(store.TryLoad(out _, out _));
            Assert.IsFalse(store.TrySave(invalid, out _));
            Assert.AreEqual(JsonUtility.ToJson(invalid), File.ReadAllText(path));
        }
    }
}
