using NUnit.Framework;
using UnityEngine;

namespace BossClicker.Tests
{
    public sealed class GameSessionTests
    {
        GameBalance balance;

        [SetUp]
        public void SetUp() => balance = ScriptableObject.CreateInstance<GameBalance>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(balance);

        GameSession Progressed(long coins = 100000)
        {
            return new GameSession(balance, new SaveData(balance.weapons.Length) {
                coins = coins,
                currentWeaponIndex = 0,
                highestOwnedWeaponIndex = 1,
                highestClearedBossIndex = 2,
                selectedBossIndex = 3
            });
        }

        [Test]
        public void ClickCreatesOnePendingShotAndConsumesOneAmmo()
        {
            var game = new GameSession(balance);

            Assert.IsTrue(game.StartBattle());
            Assert.IsTrue(game.TryFire());

            Assert.AreEqual(balance.weapons[0].baseAmmo - 1, game.AmmoRemaining);
            Assert.AreEqual(1, game.PendingShots);
            Assert.AreEqual(balance.bosses[0].health, game.Health);
            Assert.AreEqual(1, game.ShotSerial);

            Assert.IsTrue(game.ResolveNextShot());
            Assert.AreEqual(balance.bosses[0].health - game.BattleDamage, game.Health, 1e-8);
            Assert.AreEqual(0, game.PendingShots);
            Assert.AreEqual(1, game.HitSerial);
        }

        [Test]
        public void EmptyMagazineWaitsThenLosesAndKeepsCoinsAndTarget()
        {
            int ammo = balance.weapons[0].baseAmmo;
            balance.bosses[0].health = balance.weapons[0].baseDamage * ammo + 10;
            for (int i = 1; i < balance.bosses.Length; i++)
                balance.bosses[i].health = balance.bosses[0].health + i * 100;
            balance.bosses[0].hitReward = 70;
            var game = new GameSession(balance);

            Assert.IsTrue(game.StartBattle());
            for (int i = 0; i < ammo; i++) Assert.IsTrue(game.TryFire());
            Assert.AreEqual(BattlePhase.Resolving, game.Phase);
            Assert.AreEqual(ammo, game.PendingShots);

            while (game.PendingShots > 0) Assert.IsTrue(game.ResolveNextShot());

            Assert.AreEqual(BattlePhase.Lost, game.Phase);
            Assert.AreEqual(66, game.Data.coins);
            Assert.AreEqual(-1, game.Data.highestClearedBossIndex);
            Assert.AreEqual(0, game.Data.selectedBossIndex);
            Assert.IsFalse(game.StartBattle(), "A result cannot directly restart combat.");

            game.ReturnToMenu();
            Assert.IsTrue(game.StartBattle());
            Assert.AreEqual(0, game.CurrentBossIndex);
        }

        [Test]
        public void OnlyFirstClearAdvancesTheDefaultTarget()
        {
            balance.bosses[0].health = 10;
            var game = new GameSession(balance);

            game.StartBattle();
            game.TryFire();
            game.ResolveNextShot();

            Assert.AreEqual(BattlePhase.Won, game.Phase);
            Assert.IsTrue(game.LastWasFirstClear);
            Assert.AreEqual(0, game.Data.highestClearedBossIndex);
            Assert.AreEqual(1, game.Data.selectedBossIndex);

            game.ReturnToMenu();
            Assert.IsTrue(game.StartBattle());
            Assert.AreEqual(1, game.CurrentBossIndex);
        }

        [Test]
        public void FarmingClearedBossDoesNotAdvanceTheFrontier()
        {
            balance.bosses[0].health = 10;
            var data = new SaveData(balance.weapons.Length) {
                highestClearedBossIndex = 1,
                selectedBossIndex = 2
            };
            var game = new GameSession(balance, data);

            Assert.IsTrue(game.SelectBoss(0));
            game.StartBattle();
            game.TryFire();
            game.ResolveNextShot();

            Assert.IsFalse(game.LastWasFirstClear);
            Assert.AreEqual(1, game.Data.highestClearedBossIndex);
            Assert.AreEqual(0, game.Data.selectedBossIndex);
        }

        [Test]
        public void PurchasesAndSelectionsAreBlockedDuringBattle()
        {
            var game = Progressed();
            Assert.IsTrue(game.StartBattle());

            Assert.IsFalse(game.TryBuyPower());
            Assert.IsFalse(game.TryBuyAmmo());
            Assert.IsFalse(game.TryBuyGold());
            Assert.IsFalse(game.TryBuyNextWeapon());
            Assert.IsFalse(game.SelectWeapon(1));
            Assert.IsFalse(game.SelectBoss(0));
        }

        [Test]
        public void PowerAndAmmoLevelsBelongToEachWeapon()
        {
            var game = Progressed();
            Assert.IsTrue(game.TryBuyPower());
            Assert.IsTrue(game.TryBuyAmmo());
            double upgradedDamage = game.CurrentDamage;
            int upgradedAmmo = game.CurrentAmmo;

            Assert.IsTrue(game.SelectWeapon(1));
            Assert.AreEqual(balance.weapons[1].baseDamage, game.CurrentDamage);
            Assert.AreEqual(balance.weapons[1].baseAmmo, game.CurrentAmmo);
            Assert.IsTrue(game.TryBuyPower());

            Assert.IsTrue(game.SelectWeapon(0));
            Assert.AreEqual(upgradedDamage, game.CurrentDamage);
            Assert.AreEqual(upgradedAmmo, game.CurrentAmmo);
            Assert.AreEqual(1, game.Data.powerLevels[0]);
            Assert.AreEqual(1, game.Data.powerLevels[1]);
            Assert.AreEqual(1, game.Data.ammoLevels[0]);
            Assert.AreEqual(0, game.Data.ammoLevels[1]);
        }

        [Test]
        public void PowerAndAmmoShareTheTotalUpgradePriceLadder()
        {
            var game = new GameSession(balance);
            var weapon = balance.weapons[0];
            game.Data.coins = weapon.upgradeCosts[0] + weapon.upgradeCosts[1];

            Assert.AreEqual(weapon.upgradeCosts[0], game.NextUpgradeCost);
            Assert.IsTrue(game.TryBuyPower());
            Assert.AreEqual(weapon.upgradeCosts[1], game.NextUpgradeCost);
            Assert.IsTrue(game.TryBuyAmmo());

            Assert.AreEqual(2, game.TotalUpgradeLevel);
            Assert.AreEqual(weapon.baseAmmo + weapon.ammoPerLevel, game.CurrentAmmo);
            Assert.AreEqual(0, game.Data.coins);
        }

        [Test]
        public void InspectorUpgradeLimitControlsEachAttribute()
        {
            balance.maxAttributeLevel = 1;
            foreach (var weapon in balance.weapons)
                weapon.upgradeCosts = new[] { weapon.upgradeCosts[0], weapon.upgradeCosts[1] };
            var game = new GameSession(balance);
            game.Data.coins = long.MaxValue;

            Assert.IsTrue(game.TryBuyPower());
            Assert.IsFalse(game.TryBuyPower());
            Assert.IsTrue(game.TryBuyAmmo());
            Assert.IsFalse(game.TryBuyAmmo());
            Assert.AreEqual(2, game.TotalUpgradeLevel);
        }

        [Test]
        public void InspectorBossesPerWeaponControlsNextWeaponGate()
        {
            balance.bossesPerWeapon = 2;
            System.Array.Resize(ref balance.bosses, balance.weapons.Length * balance.bossesPerWeapon);
            var data = new SaveData(balance.weapons.Length) {
                coins = balance.weapons[1].cost,
                highestClearedBossIndex = 0,
                selectedBossIndex = 1
            };
            var game = new GameSession(balance, data);

            Assert.IsFalse(game.TryBuyNextWeapon());
            game.Data.highestClearedBossIndex = 1;
            Assert.IsTrue(game.TryBuyNextWeapon());
        }

        [Test]
        public void InspectorStartingCoinsControlsFreshProgress()
        {
            balance.startingCoins = 321;

            Assert.AreEqual(321, new GameSession(balance).Data.coins);
        }

        [Test]
        public void GoldUpgradeIsLinearAndFirstClearBonusStaysFixed()
        {
            balance.bosses[0].health = 10;
            balance.bosses[0].hitReward = 100;
            balance.bosses[0].killReward = 100;
            balance.bosses[0].firstBonus = 30;
            var game = new GameSession(balance);
            game.Data.coins = balance.goldCosts[0];

            Assert.IsTrue(game.TryBuyGold());
            Assert.AreEqual(1.03m, game.GoldMultiplier);
            game.StartBattle();
            game.TryFire();
            game.ResolveNextShot();

            Assert.AreEqual(236, game.Data.coins);
            Assert.AreEqual(103, game.LastKillReward);
            Assert.AreEqual(30, game.LastFirstBonus);
        }

        [Test]
        public void WeaponEightUnlocksAfterBossTwentyOneAndBossTwentyFourIsTheFinalFrontier()
        {
            var data = new SaveData(balance.weapons.Length) {
                coins = balance.weapons[7].cost,
                currentWeaponIndex = 6,
                highestOwnedWeaponIndex = 6,
                highestClearedBossIndex = 19,
                selectedBossIndex = 20
            };
            var game = new GameSession(balance, data);

            Assert.IsFalse(game.TryBuyNextWeapon());
            game.Data.highestClearedBossIndex = 20;
            game.Data.selectedBossIndex = 21;
            Assert.IsTrue(game.TryBuyNextWeapon());
            Assert.AreEqual(7, game.Data.currentWeaponIndex);
            game.Data.highestClearedBossIndex = 22;
            game.Data.selectedBossIndex = 23;
            Assert.IsTrue(game.SelectBoss(23));
            Assert.IsFalse(game.SelectBoss(24));
        }
    }
}
