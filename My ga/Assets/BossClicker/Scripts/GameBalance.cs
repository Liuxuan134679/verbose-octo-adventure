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
            [InspectorName("武器名称")]
            public string name;
            [InspectorName("基础单发伤害")]
            public double baseDamage;
            [InspectorName("基础弹匣容量")]
            public int baseAmmo;
            [InspectorName("每级增加子弹")]
            public int ammoPerLevel;
            [InspectorName("每秒射击次数")]
            public double fireRate;
            [InspectorName("购买价格")]
            public long cost;
            [InspectorName("共享强化价格（按总等级）")]
            public long[] upgradeCosts;

            public Weapon(string name, double damage, int ammo, int ammoGain, double shotsPerSecond,
                long cost, long[] upgradeCosts)
            {
                this.name = name;
                baseDamage = damage;
                baseAmmo = ammo;
                ammoPerLevel = ammoGain;
                fireRate = shotsPerSecond;
                this.cost = cost;
                this.upgradeCosts = upgradeCosts;
            }
        }

        [Serializable]
        public sealed class Boss
        {
            [InspectorName("Boss 名称")]
            public string name;
            [InspectorName("生命值")]
            public double health;
            [InspectorName("命中金币总额")]
            public long hitReward;
            [InspectorName("击杀奖励")]
            public long killReward;
            [InspectorName("首次通关奖励")]
            public long firstBonus;
            [InspectorName("外观编号")]
            public int visualId;
            [InspectorName("强化外观")]
            public bool enhanced;

            public Boss(string name, double health, long hitReward, long killReward,
                long firstBonus, int visualId, bool enhanced)
            {
                this.name = name;
                this.health = health;
                this.hitReward = hitReward;
                this.killReward = killReward;
                this.firstBonus = firstBonus;
                this.visualId = visualId;
                this.enhanced = enhanced;
            }
        }

        [Header("全局成长配置")]
        [InspectorName("初始金币")]
        [Min(0)] public long startingCoins;
        [InspectorName("每把武器对应 Boss 数")]
        [Tooltip("Boss 列表数量必须等于武器数量 × 此数值。")]
        [Min(1)] public int bossesPerWeapon = 3;
        [InspectorName("火力/弹量单项等级上限")]
        [Tooltip("每把武器的共享强化价格数量必须等于此数值 × 2。")]
        [Min(1)] public int maxAttributeLevel = 8;
        [InspectorName("Boss 外观数量")]
        [Min(1)] public int bossVisualVariantCount = 12;
        [InspectorName("每级火力加成")]
        [Min(0)] public double powerPerLevel = .08;
        [InspectorName("子弹飞行时间（秒）")]
        [Min(.01f)] public double projectileSeconds = .12;
        [InspectorName("每级金币加成")]
        [Min(0)] public double goldPerLevel = .03;

        public int TotalUpgradeLevels => maxAttributeLevel * 2;

        [Header("武器配置")]
        [InspectorName("武器列表")]
        public Weapon[] weapons = {
            new Weapon("制式手枪", 10, 18, 2, 1.00, 0, UpgradeCosts(10)),
            new Weapon("大口径手枪", 30, 20, 2, 1.15, 1350, UpgradeCosts(40)),
            new Weapon("冲锋手枪", 85, 22, 2, 1.30, 4800, UpgradeCosts(150)),
            new Weapon("紧凑冲锋枪", 240, 24, 2, 1.45, 20000, UpgradeCosts(550)),
            new Weapon("个人防卫武器", 680, 26, 3, 1.60, 74000, UpgradeCosts(2000)),
            new Weapon("战术霰弹枪", 1900, 28, 3, 1.75, 315000, UpgradeCosts(7000)),
            new Weapon("短管卡宾枪", 5300, 30, 3, 1.90, 1100000, UpgradeCosts(24000)),
            new Weapon("突击步枪", 15000, 32, 3, 2.00, 4450000, UpgradeCosts(80000))
        };

        [Header("金币强化配置")]
        [InspectorName("各级金币强化价格")]
        public long[] goldCosts = { 100, 400, 1500, 5500, 20000, 70000, 240000, 800000 };

        [Header("Boss 配置")]
        [InspectorName("Boss 列表")]
        public Boss[] bosses = {
            new Boss("街角菜鸟", 100, 70, 30, 150, 0, false),
            new Boss("铁桶守卫", 190, 130, 50, 220, 1, false),
            new Boss("屋顶拳王", 210, 200, 80, 350, 2, false),
            new Boss("装甲菜鸟", 330, 280, 120, 600, 0, true),
            new Boss("重甲守卫", 640, 520, 200, 880, 1, true),
            new Boss("钢盔拳王", 730, 800, 320, 1400, 2, true),
            new Boss("废料猎手", 1050, 1050, 450, 2250, 3, false),
            new Boss("钢牙队长", 2150, 1950, 750, 3300, 4, false),
            new Boss("港口暴徒", 2450, 3000, 1200, 5250, 5, false),
            new Boss("强化猎手", 3200, 3850, 1650, 8250, 3, true),
            new Boss("重装队长", 7000, 7150, 2750, 12100, 4, true),
            new Boss("港口霸主", 8500, 11000, 4400, 19250, 5, true),
            new Boss("爆破厨师", 9800, 14000, 6000, 30000, 6, false),
            new Boss("霓虹保镖", 24000, 26000, 10000, 44000, 7, false),
            new Boss("地铁霸主", 33000, 40000, 16000, 70000, 8, false),
            new Boss("装甲厨师", 35000, 49000, 21000, 105000, 6, true),
            new Boss("霓虹重卫", 72000, 91000, 35000, 154000, 7, true),
            new Boss("地铁暴君", 96000, 140000, 56000, 245000, 8, true),
            new Boss("金库看守", 102000, 168000, 72000, 360000, 9, false),
            new Boss("装甲巨汉", 208000, 312000, 120000, 528000, 10, false),
            new Boss("城市冠军", 280000, 480000, 192000, 840000, 11, false),
            new Boss("金库统领", 300000, 560000, 240000, 1200000, 9, true),
            new Boss("装甲战神", 625000, 1040000, 400000, 1760000, 10, true),
            new Boss("终极冠军", 830000, 1600000, 640000, 2800000, 11, true)
        };

        static long[] UpgradeCosts(long unit)
        {
            long[] multipliers = { 10, 11, 13, 16, 20, 24, 28, 34, 42, 52, 64, 79, 98, 121, 150, 186 };
            return multipliers.Select(x => checked(x * unit)).ToArray();
        }

        public SaveData CreateInitialSave() => new SaveData(weapons.Length) { coins = startingCoins };

        public bool IsValid()
        {
            if (weapons == null || weapons.Length == 0 || bosses == null ||
                bossesPerWeapon <= 0 || bosses.Length != weapons.Length * bossesPerWeapon ||
                maxAttributeLevel <= 0 || bossVisualVariantCount <= 0 || startingCoins < 0 ||
                goldCosts == null || goldCosts.Length == 0 || powerPerLevel <= 0 ||
                projectileSeconds <= 0 || goldPerLevel <= 0 || goldCosts.Any(x => x <= 0))
                return false;

            for (int i = 0; i < weapons.Length; i++)
            {
                var weapon = weapons[i];
                if (weapon == null || string.IsNullOrEmpty(weapon.name) || weapon.baseDamage <= 0 ||
                    weapon.baseAmmo <= 0 || weapon.ammoPerLevel <= 0 || weapon.fireRate <= 0 ||
                    weapon.cost < 0 || weapon.upgradeCosts == null ||
                    weapon.upgradeCosts.Length != TotalUpgradeLevels ||
                    weapon.upgradeCosts.Any(x => x <= 0) ||
                    (i > 0 && (weapon.cost <= weapons[i - 1].cost ||
                        weapon.fireRate <= weapons[i - 1].fireRate)))
                    return false;
            }

            for (int i = 0; i < bosses.Length; i++)
            {
                var boss = bosses[i];
                if (boss == null || string.IsNullOrEmpty(boss.name) || boss.health <= 0 ||
                    boss.hitReward < 0 || boss.killReward < 0 || boss.firstBonus < 0 ||
                    boss.visualId < 0 || boss.visualId >= bossVisualVariantCount ||
                    (i > 0 && boss.health <= bosses[i - 1].health))
                    return false;
            }
            return true;
        }
    }
}
