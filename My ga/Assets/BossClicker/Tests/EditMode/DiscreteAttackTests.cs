using NUnit.Framework;
using UnityEngine;

namespace BossClicker.Tests
{
    public sealed class DiscreteAttackTests
    {
        GameBalance balance;

        [SetUp]
        public void SetUp() => balance = ScriptableObject.CreateInstance<GameBalance>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(balance);

        [Test]
        public void RapidClicksQueueIndependentProjectilesWithoutCooldown()
        {
            var game = new GameSession(balance);
            game.StartBattle();

            for (int i = 0; i < 4; i++) Assert.IsTrue(game.TryFire());

            Assert.AreEqual(4, game.PendingShots);
            Assert.AreEqual(4, game.ShotSerial);
            Assert.AreEqual(balance.bosses[0].health, game.Health);
        }

        [Test]
        public void KillingHitCancelsUnresolvedTailShots()
        {
            balance.bosses[0].health = 15;
            var game = new GameSession(balance);
            game.StartBattle();
            game.TryFire();
            game.TryFire();
            game.TryFire();

            game.ResolveNextShot();
            game.ResolveNextShot();

            Assert.AreEqual(BattlePhase.Won, game.Phase);
            Assert.AreEqual(0, game.PendingShots);
            Assert.IsFalse(game.ResolveNextShot());
        }

        [Test]
        public void LastProjectileCanWinAfterMagazineIsEmpty()
        {
            balance.bosses[0].health = 120;
            var game = new GameSession(balance);
            game.StartBattle();
            for (int i = 0; i < 12; i++) game.TryFire();

            Assert.AreEqual(BattlePhase.Resolving, game.Phase);
            while (game.PendingShots > 0) game.ResolveNextShot();

            Assert.AreEqual(BattlePhase.Won, game.Phase);
            Assert.AreEqual(0, game.Health);
        }

        [Test]
        public void ReturningToMenuDiscardsUnresolvedShots()
        {
            var game = new GameSession(balance);
            game.StartBattle();
            game.TryFire();
            game.TryFire();

            game.ReturnToMenu();

            Assert.AreEqual(BattlePhase.Menu, game.Phase);
            Assert.AreEqual(0, game.PendingShots);
            Assert.AreEqual(0, game.Data.coins);
            Assert.IsTrue(game.StartBattle());
            Assert.AreEqual(balance.bosses[0].health, game.Health);
            Assert.AreEqual(balance.weapons[0].baseAmmo, game.AmmoRemaining);
        }
    }
}

