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
        public void SessionQueuesEveryShotProducedByTheInputCadence()
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
            int ammo = balance.weapons[0].baseAmmo;
            balance.bosses[0].health = balance.weapons[0].baseDamage * ammo;
            for (int i = 1; i < balance.bosses.Length; i++)
                balance.bosses[i].health = balance.bosses[0].health + i * 100;
            var game = new GameSession(balance);
            game.StartBattle();
            for (int i = 0; i < ammo; i++) game.TryFire();

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
