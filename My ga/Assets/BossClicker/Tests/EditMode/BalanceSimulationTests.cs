using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace BossClicker.Tests
{
    public sealed class BalanceSimulationTests
    {
        static readonly int[] BossTwoLevels = { 1, 1, 2, 3, 4, 4, 4, 4 };
        static readonly int[] BossThreeLevels = { 2, 3, 4, 6, 8, 8, 8, 8 };
        static readonly string[] UpgradePlans = {
            "AP", "AAP", "PAPA", "APAPAP",
            "PAPAPAPA", "PAPAPAPA", "PAPAPAPA", "PAPAPAPA"
        };
        static readonly int[] ExpectedPostBossThreeReplays = { 1, 1, 2, 2, 3, 3, 4 };

        GameBalance balance;

        [SetUp]
        public void SetUp() => balance = ScriptableObject.CreateInstance<GameBalance>();

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(balance);

        [Test]
        public void PrototypeTableHasEightWeaponsAndTwentyFourIncreasingBosses()
        {
            Assert.AreEqual(8, balance.weapons.Length);
            Assert.AreEqual(24, balance.bosses.Length);
            Assert.AreEqual(3, GameBalance.BossesPerWeapon);
            for (int i = 1; i < balance.weapons.Length; i++)
                Assert.Greater(balance.weapons[i].cost, balance.weapons[i - 1].cost);
            for (int i = 1; i < balance.bosses.Length; i++)
                Assert.Greater(balance.bosses[i].health, balance.bosses[i - 1].health);
            Assert.IsTrue(balance.goldCosts.All(x => x > 0));

            foreach (var weapon in balance.weapons)
            {
                Assert.Greater(weapon.ammoPerLevel, 0);
                Assert.Greater(weapon.fireRate, 0);
                Assert.AreEqual(GameBalance.TotalUpgradeLevels, weapon.upgradeCosts.Length);
                Assert.IsTrue(weapon.upgradeCosts.All(x => x > 0));
            }
        }

        [Test]
        public void FireRateStrictlyIncreasesFromOneToTwoShotsPerSecond()
        {
            Assert.AreEqual(1d, balance.weapons[0].fireRate, 1e-8);
            Assert.AreEqual(2d, balance.weapons[7].fireRate, 1e-8);
            for (int i = 1; i < balance.weapons.Length; i++)
                Assert.Greater(balance.weapons[i].fireRate, balance.weapons[i - 1].fireRate);
        }

        [Test]
        public void EveryBossGateRequiresTheDocumentedTotalUpgradeLevel()
        {
            for (int weapon = 0; weapon < balance.weapons.Length; weapon++)
            {
                AssertGate(weapon, 1, BossTwoLevels[weapon]);
                AssertGate(weapon, 2, BossThreeLevels[weapon]);
            }
        }

        [Test]
        public void TwelveBossVisualsEachHaveNormalAndEnhancedForms()
        {
            Assert.AreEqual(12, balance.bosses.Select(x => x.visualId).Distinct().Count());
            for (int visual = 0; visual < 12; visual++)
            {
                var forms = balance.bosses.Where(x => x.visualId == visual).ToArray();
                Assert.AreEqual(2, forms.Length, "Boss visual " + visual);
                Assert.AreEqual(1, forms.Count(x => !x.enhanced));
                Assert.AreEqual(1, forms.Count(x => x.enhanced));
            }
        }

        [Test]
        public void EveryBossKeepsTheSeventyThirtyNormalRewardSplit()
        {
            foreach (var boss in balance.bosses)
            {
                double hitShare = (double)boss.hitReward / (boss.hitReward + boss.killReward);
                Assert.That(hitShare, Is.InRange(.68, .73), boss.name);
            }
        }

        [Test]
        public void ReferenceRouteUsesThePlannedPostBossThreeReplays()
        {
            var game = new GameSession(balance);
            int battles = 0;

            for (int weapon = 0; weapon < balance.weapons.Length; weapon++)
            {
                int[] targets = { 0, BossTwoLevels[weapon], BossThreeLevels[weapon] };
                for (int localBoss = 0; localBoss < GameBalance.BossesPerWeapon; localBoss++)
                {
                    int frontier = weapon * GameBalance.BossesPerWeapon + localBoss;
                    while (game.TotalUpgradeLevel < targets[localBoss])
                    {
                        char upgrade = UpgradePlans[weapon][game.TotalUpgradeLevel];
                        bool bought = upgrade == 'P' ? game.TryBuyPower() : game.TryBuyAmmo();
                        if (bought) continue;

                        Assert.Greater(frontier, 0, "No cleared Boss is available for farming.");
                        Assert.IsTrue(game.SelectBoss(frontier - 1));
                        Assert.IsTrue(FinishBattle(game));
                        battles++;
                    }

                    Assert.IsTrue(game.SelectBoss(frontier));
                    Assert.IsTrue(FinishBattle(game), "Frontier B" + (frontier + 1));
                    battles++;
                }

                if (weapon >= balance.weapons.Length - 1) continue;
                int replays = 0;
                while (!game.TryBuyNextWeapon())
                {
                    int thirdBoss = weapon * GameBalance.BossesPerWeapon + 2;
                    Assert.IsTrue(game.SelectBoss(thirdBoss));
                    Assert.IsTrue(FinishBattle(game));
                    battles++;
                    replays++;
                    Assert.Less(replays, 10, "Weapon price caused an unexpected grind spike.");
                }
                Assert.AreEqual(ExpectedPostBossThreeReplays[weapon], replays, "W" + (weapon + 2));
            }

            Assert.AreEqual(71, battles);
            Assert.AreEqual(23, game.Data.highestClearedBossIndex);
            Assert.AreEqual(7, game.Data.highestOwnedWeaponIndex);
            Assert.IsTrue(game.Data.IsValid(balance));
        }

        [Test]
        public void OneGoldUpgradePerWeaponSegmentStillCompletesWithoutADetourSpike()
        {
            var game = new GameSession(balance);
            int battles = 0;

            for (int weapon = 0; weapon < balance.weapons.Length; weapon++)
            {
                int[] targets = { 0, BossTwoLevels[weapon], BossThreeLevels[weapon] };
                for (int localBoss = 0; localBoss < GameBalance.BossesPerWeapon; localBoss++)
                {
                    int frontier = weapon * GameBalance.BossesPerWeapon + localBoss;
                    while (game.TotalUpgradeLevel < targets[localBoss])
                    {
                        char upgrade = UpgradePlans[weapon][game.TotalUpgradeLevel];
                        bool bought = upgrade == 'P' ? game.TryBuyPower() : game.TryBuyAmmo();
                        if (bought) continue;
                        Assert.IsTrue(game.SelectBoss(frontier - 1));
                        Assert.IsTrue(FinishBattle(game));
                        battles++;
                    }

                    Assert.IsTrue(game.SelectBoss(frontier));
                    Assert.IsTrue(FinishBattle(game));
                    battles++;

                    if (localBoss != 0) continue;
                    while (!game.TryBuyGold())
                    {
                        Assert.IsTrue(game.SelectBoss(frontier));
                        Assert.IsTrue(FinishBattle(game));
                        battles++;
                    }
                }

                if (weapon >= balance.weapons.Length - 1) continue;
                while (!game.TryBuyNextWeapon())
                {
                    int thirdBoss = weapon * GameBalance.BossesPerWeapon + 2;
                    Assert.IsTrue(game.SelectBoss(thirdBoss));
                    Assert.IsTrue(FinishBattle(game));
                    battles++;
                }
            }

            Assert.AreEqual(68, battles);
            Assert.AreEqual(8, game.Data.goldLevel);
            Assert.AreEqual(23, game.Data.highestClearedBossIndex);
            Assert.AreEqual(7, game.Data.highestOwnedWeaponIndex);
        }

        [Test]
        public void EveryGoldLevelChangesAContemporaryBossRewardAfterRounding()
        {
            for (int level = 0; level < balance.goldCosts.Length; level++)
            {
                var boss = balance.bosses[Math.Min((level + 1) * GameBalance.BossesPerWeapon - 1,
                    balance.bosses.Length - 1)];
                decimal before = 1m + (decimal)balance.goldPerLevel * level;
                decimal after = before + (decimal)balance.goldPerLevel;
                long beforeReward = (long)decimal.Floor(boss.hitReward * before) +
                    (long)decimal.Floor(boss.killReward * before);
                long afterReward = (long)decimal.Floor(boss.hitReward * after) +
                    (long)decimal.Floor(boss.killReward * after);
                Assert.Greater(afterReward, beforeReward, "Gold level " + (level + 1));
            }
        }

        void AssertGate(int weaponIndex, int localBossIndex, int requiredLevel)
        {
            var weapon = balance.weapons[weaponIndex];
            var boss = balance.bosses[weaponIndex * GameBalance.BossesPerWeapon + localBossIndex];
            double bestBelow = 0;
            for (int total = 0; total < requiredLevel; total++)
                for (int power = 0; power <= total; power++)
                    bestBelow = Math.Max(bestBelow, Capacity(weapon, power, total - power));

            double bestAtGate = 0;
            for (int power = 0; power <= requiredLevel; power++)
                bestAtGate = Math.Max(bestAtGate, Capacity(weapon, power, requiredLevel - power));

            Assert.Less(bestBelow, boss.health, boss.name + " can be cleared below its gate.");
            Assert.GreaterOrEqual(bestAtGate, boss.health, boss.name + " cannot be cleared at its gate.");
        }

        double Capacity(GameBalance.Weapon weapon, int power, int ammo)
        {
            double damage = Math.Round(weapon.baseDamage * (1 + balance.powerPerLevel * power),
                MidpointRounding.AwayFromZero);
            return damage * (weapon.baseAmmo + weapon.ammoPerLevel * ammo);
        }

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
