using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace BossClicker.Tests
{
    public sealed class BalanceSimulationTests
    {
        GameBalance balance;

        [SetUp]
        public void SetUp() => balance = ScriptableObject.CreateInstance<GameBalance>();

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(balance);

        [Test]
        public void PrototypeTableHasFourIncreasingWeaponsAndTwelveBosses()
        {
            Assert.AreEqual(4, balance.weapons.Length);
            Assert.AreEqual(12, balance.bosses.Length);
            for (int i = 1; i < balance.weapons.Length; i++)
                Assert.Greater(balance.weapons[i].cost, balance.weapons[i - 1].cost);
            for (int i = 1; i < balance.bosses.Length; i++)
                Assert.Greater(balance.bosses[i].health, balance.bosses[i - 1].health);
            Assert.IsTrue(balance.goldCosts.All(x => x > 0));
            foreach (var weapon in balance.weapons)
            {
                Assert.AreEqual(8, weapon.powerCosts.Length);
                Assert.AreEqual(8, weapon.ammoCosts.Length);
                Assert.IsTrue(weapon.powerCosts.All(x => x > 0));
                Assert.IsTrue(weapon.ammoCosts.All(x => x > 0));
            }
        }

        [Test]
        public void EveryWeaponSegmentEndsWithAnUpgradeGate()
        {
            for (int weaponIndex = 0; weaponIndex < balance.weapons.Length; weaponIndex++)
            {
                var weapon = balance.weapons[weaponIndex];
                double potential = weapon.baseDamage * weapon.baseAmmo;
                int boss = weaponIndex * 3;
                Assert.That(balance.bosses[boss].health / potential, Is.InRange(.4, .55));
                Assert.That(balance.bosses[boss + 1].health / potential, Is.InRange(.65, .8));
                Assert.That(balance.bosses[boss + 2].health / potential, Is.GreaterThan(1));
                Assert.That(balance.bosses[boss + 2].health / potential, Is.LessThanOrEqualTo(1.1));
                double levelTwoDamage = Math.Round(weapon.baseDamage * (1 + balance.powerPerLevel * 2), MidpointRounding.AwayFromZero);
                Assert.GreaterOrEqual(levelTwoDamage * weapon.baseAmmo, balance.bosses[boss + 2].health);
            }
        }

        [Test]
        public void GreedyRouteClearsAllTwelveBossesWithoutDeadlock()
        {
            var game = new GameSession(balance);
            int steps = 0;
            int consecutiveFailures = 0;
            int maxFailures = 0;

            while (game.Data.highestClearedBossIndex < balance.bosses.Length - 1)
            {
                Assert.Less(++steps, 200, "Route stopped making progress.");
                int nextWeapon = game.Data.highestOwnedWeaponIndex + 1;
                bool weaponUnlocked = nextWeapon < balance.weapons.Length &&
                    game.Data.highestClearedBossIndex >= nextWeapon * 3 - 1;
                if (weaponUnlocked)
                {
                    if (game.TryBuyNextWeapon()) { consecutiveFailures = 0; continue; }
                    Assert.IsTrue(game.SelectBoss(game.Data.highestClearedBossIndex));
                    FinishBattle(game);
                    consecutiveFailures = 0;
                    continue;
                }

                int frontier = game.Data.highestClearedBossIndex + 1;
                Assert.IsTrue(game.SelectBoss(frontier));
                if (game.CurrentDamage * game.CurrentAmmo + 1e-8 < balance.bosses[frontier].health)
                {
                    long power = NextCost(balance.weapons[game.Data.currentWeaponIndex].powerCosts,
                        game.Data.powerLevels[game.Data.currentWeaponIndex]);
                    long ammo = NextCost(balance.weapons[game.Data.currentWeaponIndex].ammoCosts,
                        game.Data.ammoLevels[game.Data.currentWeaponIndex]);
                    bool bought = power <= ammo ? game.TryBuyPower() : game.TryBuyAmmo();
                    if (!bought) bought = power <= ammo ? game.TryBuyAmmo() : game.TryBuyPower();
                    if (bought) { consecutiveFailures = 0; continue; }
                }

                bool won = FinishBattle(game);
                consecutiveFailures = won ? 0 : consecutiveFailures + 1;
                maxFailures = Math.Max(maxFailures, consecutiveFailures);
            }

            Assert.AreEqual(11, game.Data.highestClearedBossIndex);
            Assert.LessOrEqual(maxFailures, 2);
            Assert.IsTrue(game.Data.IsValid(balance));
        }

        static long NextCost(long[] costs, int level) => level < costs.Length ? costs[level] : long.MaxValue;

        static bool FinishBattle(GameSession game)
        {
            Assert.IsTrue(game.StartBattle());
            while (game.TryFire()) { }
            while (game.PendingShots > 0) Assert.IsTrue(game.ResolveNextShot());
            bool won = game.Phase == BattlePhase.Won;
            Assert.IsTrue(won || game.Phase == BattlePhase.Lost);
            game.ReturnToMenu();
            return won;
        }
    }
}
