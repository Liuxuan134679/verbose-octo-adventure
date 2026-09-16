using System;

namespace BossClicker
{
    public enum BattlePhase { Menu, Fighting, Resolving, Won, Lost }

    [Serializable]
    public sealed class SaveData
    {
        public int schemaVersion = 4;
        public long coins;
        public int currentWeaponIndex;
        public int highestOwnedWeaponIndex;
        public int[] powerLevels;
        public int[] ammoLevels;
        public int goldLevel;
        public int highestClearedBossIndex = -1;
        public int selectedBossIndex;

        public SaveData(int weaponCount)
        {
            powerLevels = new int[weaponCount];
            ammoLevels = new int[weaponCount];
        }

        public bool IsValid(GameBalance balance)
        {
            if (balance == null || !balance.IsValid() || schemaVersion != 4 || coins < 0 ||
                currentWeaponIndex < 0 || highestOwnedWeaponIndex < 0 ||
                highestOwnedWeaponIndex >= balance.weapons.Length ||
                currentWeaponIndex > highestOwnedWeaponIndex || powerLevels == null ||
                ammoLevels == null || powerLevels.Length != balance.weapons.Length ||
                ammoLevels.Length != balance.weapons.Length || goldLevel < 0 ||
                goldLevel > balance.goldCosts.Length || highestClearedBossIndex < -1 ||
                highestClearedBossIndex >= balance.bosses.Length || selectedBossIndex < 0 ||
                selectedBossIndex > Math.Min(highestClearedBossIndex + 1, balance.bosses.Length - 1))
                return false;

            for (int i = 0; i < balance.weapons.Length; i++)
                if (powerLevels[i] < 0 || powerLevels[i] > balance.maxAttributeLevel ||
                    ammoLevels[i] < 0 || ammoLevels[i] > balance.maxAttributeLevel ||
                    powerLevels[i] + ammoLevels[i] > balance.TotalUpgradeLevels)
                    return false;
            return true;
        }
    }

    public sealed class GameSession
    {
        readonly GameBalance balance;

        public SaveData Data { get; }
        public BattlePhase Phase { get; private set; } = BattlePhase.Menu;
        public int CurrentBossIndex { get; private set; } = -1;
        public double Health { get; private set; }
        public int AmmoRemaining { get; private set; }
        public int PendingShots { get; private set; }
        public long ShotSerial { get; private set; }
        public long HitSerial { get; private set; }
        public double BattleDamage { get; private set; }
        public int BattleMaxAmmo { get; private set; }
        public decimal BattleGoldMultiplier { get; private set; }
        public double LastHitDamage { get; private set; }
        public long LastHitCoins { get; private set; }
        public long BattleHitCoins { get; private set; }
        public long LastKillReward { get; private set; }
        public long LastFirstBonus { get; private set; }
        public bool LastWasFirstClear { get; private set; }

        public double CurrentDamage
        {
            get
            {
                int weapon = Data.currentWeaponIndex;
                return Math.Round(balance.weapons[weapon].baseDamage *
                    (1 + balance.powerPerLevel * Data.powerLevels[weapon]),
                    MidpointRounding.AwayFromZero);
            }
        }

        public int CurrentAmmo => balance.weapons[Data.currentWeaponIndex].baseAmmo +
            balance.weapons[Data.currentWeaponIndex].ammoPerLevel *
            Data.ammoLevels[Data.currentWeaponIndex];

        public double CurrentFireRate => balance.weapons[Data.currentWeaponIndex].fireRate;

        public int TotalUpgradeLevel => Data.powerLevels[Data.currentWeaponIndex] +
            Data.ammoLevels[Data.currentWeaponIndex];

        public long NextPowerUpgradeCost =>
            WeaponUpgradeCost(Data.powerLevels[Data.currentWeaponIndex]);

        public long NextAmmoUpgradeCost =>
            WeaponUpgradeCost(Data.ammoLevels[Data.currentWeaponIndex]);

        long WeaponUpgradeCost(int level) => level < balance.maxAttributeLevel
            ? balance.weapons[Data.currentWeaponIndex].upgradeCosts[level] : 0;

        public decimal GoldMultiplier => 1m + (decimal)balance.goldPerLevel * Data.goldLevel;

        public GameSession(GameBalance balance, SaveData data = null)
        {
            this.balance = balance ?? throw new ArgumentNullException(nameof(balance));
            if (!balance.IsValid()) throw new ArgumentException("Invalid battle configuration.");
            Data = data ?? balance.CreateInitialSave();
            if (!Data.IsValid(balance)) throw new ArgumentException("Invalid saved progress.");
        }

        public bool StartBattle()
        {
            if (Phase != BattlePhase.Menu || !IsBossSelectable(Data.selectedBossIndex)) return false;
            CurrentBossIndex = Data.selectedBossIndex;
            Health = balance.bosses[CurrentBossIndex].health;
            BattleDamage = CurrentDamage;
            BattleMaxAmmo = CurrentAmmo;
            BattleGoldMultiplier = GoldMultiplier;
            AmmoRemaining = BattleMaxAmmo;
            PendingShots = 0;
            BattleHitCoins = 0;
            LastHitDamage = 0;
            LastHitCoins = 0;
            LastKillReward = 0;
            LastFirstBonus = 0;
            LastWasFirstClear = false;
            Phase = BattlePhase.Fighting;
            return true;
        }

        public bool TryFire()
        {
            if (Phase != BattlePhase.Fighting || AmmoRemaining <= 0) return false;
            AmmoRemaining--;
            PendingShots++;
            ShotSerial++;
            if (AmmoRemaining == 0) Phase = BattlePhase.Resolving;
            return true;
        }

        public bool ResolveNextShot()
        {
            if ((Phase != BattlePhase.Fighting && Phase != BattlePhase.Resolving) || PendingShots <= 0)
                return false;

            PendingShots--;
            var boss = balance.bosses[CurrentBossIndex];
            double beforeDamage = boss.health - Health;
            LastHitDamage = Math.Min(BattleDamage, Health);
            Health = Math.Max(0, Health - LastHitDamage);
            double afterDamage = boss.health - Health;
            long earnedToDate = (long)decimal.Floor(Scale(boss.hitReward, BattleGoldMultiplier) *
                (decimal)afterDamage / (decimal)boss.health);
            LastHitCoins = earnedToDate - BattleHitCoins;
            BattleHitCoins = earnedToDate;
            Data.coins = checked(Data.coins + LastHitCoins);
            HitSerial++;

            if (Health <= 0)
            {
                PendingShots = 0;
                LastWasFirstClear = CurrentBossIndex > Data.highestClearedBossIndex;
                LastKillReward = Scale(boss.killReward, BattleGoldMultiplier);
                LastFirstBonus = LastWasFirstClear ? boss.firstBonus : 0;
                Data.coins = checked(Data.coins + LastKillReward + LastFirstBonus);
                if (LastWasFirstClear)
                {
                    Data.highestClearedBossIndex = CurrentBossIndex;
                    Data.selectedBossIndex = Math.Min(CurrentBossIndex + 1, balance.bosses.Length - 1);
                }
                Phase = BattlePhase.Won;
            }
            else if (AmmoRemaining == 0 && PendingShots == 0)
            {
                Phase = BattlePhase.Lost;
            }
            return true;
        }

        public void ReturnToMenu()
        {
            PendingShots = 0;
            AmmoRemaining = 0;
            CurrentBossIndex = -1;
            Health = 0;
            Phase = BattlePhase.Menu;
        }

        public bool SelectBoss(int bossIndex)
        {
            if (Phase != BattlePhase.Menu || !IsBossSelectable(bossIndex)) return false;
            Data.selectedBossIndex = bossIndex;
            return true;
        }

        public bool SelectWeapon(int weaponIndex)
        {
            if (Phase != BattlePhase.Menu || weaponIndex < 0 ||
                weaponIndex > Data.highestOwnedWeaponIndex) return false;
            Data.currentWeaponIndex = weaponIndex;
            return true;
        }

        public bool TryBuyPower()
        {
            if (Phase != BattlePhase.Menu) return false;
            int weapon = Data.currentWeaponIndex;
            return BuyWeaponUpgrade(weapon, ref Data.powerLevels[weapon]);
        }

        public bool TryBuyAmmo()
        {
            if (Phase != BattlePhase.Menu) return false;
            int weapon = Data.currentWeaponIndex;
            return BuyWeaponUpgrade(weapon, ref Data.ammoLevels[weapon]);
        }

        public bool TryBuyGold()
        {
            if (Phase != BattlePhase.Menu) return false;
            return Buy(balance.goldCosts, ref Data.goldLevel);
        }

        public bool TryBuyNextWeapon()
        {
            if (Phase != BattlePhase.Menu) return false;
            int next = Data.highestOwnedWeaponIndex + 1;
            if (next >= balance.weapons.Length ||
                Data.coins < balance.weapons[next].cost) return false;
            Data.coins -= balance.weapons[next].cost;
            Data.highestOwnedWeaponIndex = next;
            Data.currentWeaponIndex = next;
            return true;
        }

        public int EstimatedShots(int bossIndex)
        {
            if (bossIndex < 0 || bossIndex >= balance.bosses.Length) return 0;
            return (int)Math.Ceiling(balance.bosses[bossIndex].health / CurrentDamage - 1e-9);
        }

        public long ScaledHitReward(int bossIndex) => Scale(balance.bosses[bossIndex].hitReward, GoldMultiplier);
        public long ScaledKillReward(int bossIndex) => Scale(balance.bosses[bossIndex].killReward, GoldMultiplier);

        bool IsBossSelectable(int index) => index >= 0 && index < balance.bosses.Length &&
            index <= Math.Min(Data.highestClearedBossIndex + 1, balance.bosses.Length - 1);

        bool BuyWeaponUpgrade(int weapon, ref int attributeLevel)
        {
            return attributeLevel < balance.maxAttributeLevel &&
                Buy(balance.weapons[weapon].upgradeCosts, ref attributeLevel);
        }

        bool Buy(long[] costs, ref int level)
        {
            if (level >= costs.Length || Data.coins < costs[level]) return false;
            Data.coins -= costs[level++];
            return true;
        }

        static long Scale(long value, decimal multiplier) =>
            checked((long)decimal.Floor(value * multiplier));
    }
}
