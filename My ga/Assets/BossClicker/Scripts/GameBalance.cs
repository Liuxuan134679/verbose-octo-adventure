using System;
using System.Linq;
using UnityEngine;

namespace BossClicker
{
    [CreateAssetMenu(menuName = "Boss Clicker/Balance", fileName = "PrototypeBalance")]
    public sealed class GameBalance : ScriptableObject
    {
        [Serializable]
        public sealed class Weapon
        {
            public string name;
            public double baseDamage;
            public int baseAmmo;
            public long cost;
            public long[] powerCosts;
            public long[] ammoCosts;

            public Weapon(string name, double damage, int ammo, long cost,
                long[] powerCosts, long[] ammoCosts)
            {
                this.name = name;
                baseDamage = damage;
                baseAmmo = ammo;
                this.cost = cost;
                this.powerCosts = powerCosts;
                this.ammoCosts = ammoCosts;
            }
        }

        [Serializable]
        public sealed class Boss
        {
            public string name;
            public double health;
            public long hitReward;
            public long killReward;
            public long firstBonus;

            public Boss(string name, double health, long hitReward, long killReward, long firstBonus)
            {
                this.name = name;
                this.health = health;
                this.hitReward = hitReward;
                this.killReward = killReward;
                this.firstBonus = firstBonus;
            }
        }

        public double powerPerLevel = .08;
        public double projectileSeconds = .12;
        public double goldPerLevel = .03;

        public Weapon[] weapons = {
            new Weapon("制式手枪", 10, 12, 0,
                new long[] { 25, 45, 80, 140, 240, 400, 650, 1000 },
                new long[] { 30, 55, 95, 160, 270, 450, 720, 1100 }),
            new Weapon("重型手枪", 30, 14, 180,
                new long[] { 120, 210, 360, 600, 1000, 1650, 2700, 4400 },
                new long[] { 100, 180, 310, 520, 880, 1450, 2400, 3900 }),
            new Weapon("冲锋手枪", 85, 16, 900,
                new long[] { 500, 850, 1450, 2400, 4000, 6600, 10800, 17500 },
                new long[] { 450, 760, 1300, 2150, 3550, 5900, 9700, 15800 }),
            new Weapon("紧凑冲锋枪", 240, 18, 3800,
                new long[] { 2200, 3700, 6200, 10300, 17000, 28000, 46000, 75000 },
                new long[] { 1900, 3200, 5400, 9000, 15000, 24500, 40000, 65000 })
        };

        public long[] goldCosts = { 120, 400, 1200, 3500, 9000, 22000, 52000, 120000 };

        public Boss[] bosses = {
            new Boss("街角菜鸟", 60, 17, 7, 20),
            new Boss("铁桶守卫", 90, 28, 12, 30),
            new Boss("屋顶拳王", 132, 49, 21, 50),
            new Boss("废料猎手", 210, 84, 36, 80),
            new Boss("钢牙队长", 315, 133, 57, 120),
            new Boss("港口暴徒", 450, 210, 90, 180),
            new Boss("爆破厨师", 680, 350, 150, 300),
            new Boss("霓虹保镖", 1020, 525, 225, 450),
            new Boss("地铁霸主", 1450, 770, 330, 650),
            new Boss("金库看守", 2160, 1260, 540, 1000),
            new Boss("装甲巨汉", 3240, 1890, 810, 1500),
            new Boss("城市冠军", 4750, 2800, 1200, 2200)
        };

        public bool IsValid()
        {
            if (weapons == null || weapons.Length == 0 || bosses == null || bosses.Length == 0 ||
                goldCosts == null || powerPerLevel <= 0 || projectileSeconds <= 0 || goldPerLevel <= 0)
                return false;
            if (goldCosts.Any(x => x <= 0)) return false;
            for (int i = 0; i < weapons.Length; i++)
            {
                var weapon = weapons[i];
                if (weapon == null || string.IsNullOrEmpty(weapon.name) || weapon.baseDamage <= 0 ||
                    weapon.baseAmmo <= 0 || weapon.cost < 0 || weapon.powerCosts == null ||
                    weapon.ammoCosts == null || weapon.powerCosts.Any(x => x <= 0) ||
                    weapon.ammoCosts.Any(x => x <= 0) || (i > 0 && weapon.cost <= weapons[i - 1].cost))
                    return false;
            }
            return bosses.All(x => x != null && !string.IsNullOrEmpty(x.name) && x.health > 0 &&
                x.hitReward >= 0 && x.killReward >= 0 && x.firstBonus >= 0);
        }
    }
}
