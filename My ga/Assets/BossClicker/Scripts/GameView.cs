using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BossClicker
{
    public sealed class GameView : MonoBehaviour
    {
        sealed class ShotFx
        {
            public RectTransform rect;
            public Vector2 start, end;
            public float age, duration;
        }

        sealed class CoinFx
        {
            public RectTransform rect;
            public Vector2 start, end;
            public float age, duration;
        }

        public GameBalance balance;
        public GameObject menuScreen, battleScreen, resultPanel;

        public TMP_Text menuCoinsText, progressText, weaponNameText, weaponStatsText;
        public TMP_Text powerInfoText, powerPriceText, ammoInfoText, ammoPriceText;
        public TMP_Text goldInfoText, goldPriceText, nextWeaponInfoText, nextWeaponPriceText;
        public TMP_Text bossNameText, bossInfoText, saveText, resetLabel;
        public Button powerButton, ammoButton, goldButton, nextWeaponButton;
        public Button bossPreviousButton, bossNextButton, fightButton, resetButton, saveReloadButton;
        public Button[] weaponButtons;
        public TMP_Text[] weaponButtonLabels;
        public Image menuBossBody;
        public RectTransform menuGunBarrel, menuGunBody;
        public GameObject menuGunStock;

        public TMP_Text battleBossText, battleHealthText, battleAmmoText, battleCoinsText;
        public TMP_Text battleStatusText, hitText, resultTitle, resultDetails;
        public Button exitButton, resultBackButton;
        public Image healthFill, healthLagFill, battleBossBody, muzzleFlash;
        public RectTransform bossRoot, gunRoot, battleGunBarrel, battleGunBody;
        public RectTransform shotOrigin, hitTarget, projectileLayer, coinTarget;
        public GameObject battleGunStock, armor75, armor50, armor25;

        readonly List<ShotFx> shots = new List<ShotFx>();
        readonly List<CoinFx> coins = new List<CoinFx>();
        readonly Color[] bossColors = {
            new Color(.85f,.32f,.20f), new Color(.16f,.62f,.68f), new Color(.76f,.25f,.42f),
            new Color(.91f,.53f,.16f), new Color(.26f,.55f,.79f), new Color(.46f,.30f,.68f)
        };
        SaveStore store;
        Vector2 gunBase, bossBase;
        float recoil, flash, bossKick, hitFade, healthLag = 1, resetConfirmUntil;

        public GameSession Session { get; private set; }

        void Awake()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            store = new SaveStore(Path.Combine(Application.persistentDataPath,
                "lootshot-save-v3.json"), balance);
            gunBase = gunRoot.anchoredPosition;
            bossBase = bossRoot.anchoredPosition;

            powerButton.onClick.AddListener(() => Change(() => Session.TryBuyPower()));
            ammoButton.onClick.AddListener(() => Change(() => Session.TryBuyAmmo()));
            goldButton.onClick.AddListener(() => Change(() => Session.TryBuyGold()));
            nextWeaponButton.onClick.AddListener(() => Change(() => Session.TryBuyNextWeapon()));
            bossPreviousButton.onClick.AddListener(() => SelectBoss(-1));
            bossNextButton.onClick.AddListener(() => SelectBoss(1));
            fightButton.onClick.AddListener(BeginBattle);
            exitButton.onClick.AddListener(ReturnToMenu);
            resultBackButton.onClick.AddListener(ReturnToMenu);
            resetButton.onClick.AddListener(ResetProgress);
            saveReloadButton.onClick.AddListener(Load);
            for (int i = 0; i < weaponButtons.Length; i++)
            {
                int index = i;
                weaponButtons[i].onClick.AddListener(() => Change(() => Session.SelectWeapon(index)));
            }
            Load();
        }

        void Load()
        {
            if (store.TryLoad(out var data, out string message))
            {
                Session = new GameSession(balance, data);
                saveText.text = string.IsNullOrEmpty(message) ? "进度自动保存" : message;
                saveReloadButton.gameObject.SetActive(false);
            }
            else
            {
                Session = null;
                saveText.text = message;
                saveReloadButton.gameObject.SetActive(true);
            }
            ShowMenu();
        }

        void Change(Func<bool> action)
        {
            if (Session != null && action())
            {
                Save();
                RefreshMenu();
            }
        }

        void SelectBoss(int direction)
        {
            if (Session == null) return;
            int next = Session.Data.selectedBossIndex + direction;
            if (Session.SelectBoss(next))
            {
                Save();
                RefreshMenu();
            }
        }

        void BeginBattle()
        {
            if (Session == null || !Session.StartBattle()) return;
            ClearEffects();
            menuScreen.SetActive(false);
            battleScreen.SetActive(true);
            resultPanel.SetActive(false);
            recoil = flash = bossKick = hitFade = 0;
            healthLag = 1;
            bossRoot.anchoredPosition = bossBase;
            bossRoot.localRotation = Quaternion.identity;
            RefreshBattle();
        }

        public void Fire()
        {
            if (Session == null || !Session.TryFire()) return;
            recoil = 1;
            flash = 1;
            SpawnShot();
            RefreshBattle();
        }

        void ReturnToMenu()
        {
            if (Session == null) return;
            Session.ReturnToMenu();
            Save();
            ShowMenu();
        }

        void ShowMenu()
        {
            ClearEffects();
            battleScreen.SetActive(false);
            menuScreen.SetActive(true);
            resultPanel.SetActive(false);
            RefreshMenu();
        }

        void RefreshMenu()
        {
            bool ready = Session != null;
            powerButton.interactable = ammoButton.interactable = goldButton.interactable =
                nextWeaponButton.interactable = bossPreviousButton.interactable =
                bossNextButton.interactable = fightButton.interactable = ready;
            foreach (var button in weaponButtons) button.interactable = ready;
            if (!ready) return;

            var data = Session.Data;
            int weaponIndex = data.currentWeaponIndex;
            var weapon = balance.weapons[weaponIndex];
            int powerLevel = data.powerLevels[weaponIndex];
            int ammoLevel = data.ammoLevels[weaponIndex];
            menuCoinsText.text = Number(data.coins);
            progressText.text = $"{data.highestClearedBossIndex + 1} / {balance.bosses.Length} 已通关";
            weaponNameText.text = $"W{weaponIndex + 1}  {weapon.name}";
            weaponStatsText.text = $"单发火力  {Number(Session.CurrentDamage)}     弹量  {Session.CurrentAmmo}";

            bool powerMax = powerLevel >= weapon.powerCosts.Length;
            powerInfoText.text = $"火力  Lv.{powerLevel}/{weapon.powerCosts.Length}\n{Number(Session.CurrentDamage)}" +
                (powerMax ? "  已满级" : $"  →  {Number(DamageAt(weaponIndex, powerLevel + 1))}");
            powerPriceText.text = powerMax ? "已满级" : $"升级  {Number(weapon.powerCosts[powerLevel])}";
            powerButton.interactable = !powerMax && data.coins >= weapon.powerCosts[powerLevel];

            bool ammoMax = ammoLevel >= weapon.ammoCosts.Length;
            ammoInfoText.text = $"弹量  Lv.{ammoLevel}/{weapon.ammoCosts.Length}\n{Session.CurrentAmmo}" +
                (ammoMax ? "  已满级" : $"  →  {Session.CurrentAmmo + 1}");
            ammoPriceText.text = ammoMax ? "已满级" : $"升级  {Number(weapon.ammoCosts[ammoLevel])}";
            ammoButton.interactable = !ammoMax && data.coins >= weapon.ammoCosts[ammoLevel];

            bool goldMax = data.goldLevel >= balance.goldCosts.Length;
            goldInfoText.text = $"金币加成  Lv.{data.goldLevel}/{balance.goldCosts.Length}\n×{Session.GoldMultiplier:0.00}" +
                (goldMax ? "  已满级" : $"  →  ×{Session.GoldMultiplier + (decimal)balance.goldPerLevel:0.00}");
            goldPriceText.text = goldMax ? "已满级" : $"升级  {Number(balance.goldCosts[data.goldLevel])}";
            goldButton.interactable = !goldMax && data.coins >= balance.goldCosts[data.goldLevel];

            int selectedBoss = data.selectedBossIndex;
            var boss = balance.bosses[selectedBoss];
            bool firstClear = selectedBoss > data.highestClearedBossIndex;
            bossNameText.text = $"BOSS {selectedBoss + 1:00}  {boss.name}";
            bossInfoText.text = $"生命  {Number(boss.health)}\n预计 {Session.EstimatedShots(selectedBoss)} 发 / 当前 {Session.CurrentAmmo} 发\n" +
                $"普通金币  {Number(Session.ScaledHitReward(selectedBoss) + Session.ScaledKillReward(selectedBoss))}" +
                (firstClear ? $"   首通 +{Number(boss.firstBonus)}" : "   已首通");
            bossPreviousButton.interactable = selectedBoss > 0;
            bossNextButton.interactable = selectedBoss < Math.Min(data.highestClearedBossIndex + 1,
                balance.bosses.Length - 1);

            int nextWeapon = data.highestOwnedWeaponIndex + 1;
            if (nextWeapon >= balance.weapons.Length)
            {
                nextWeaponInfoText.text = "4 把原型武器已全部获得";
                nextWeaponPriceText.text = "已完成";
                nextWeaponButton.interactable = false;
            }
            else
            {
                var next = balance.weapons[nextWeapon];
                int gate = nextWeapon * 3 - 1;
                bool unlocked = data.highestClearedBossIndex >= gate;
                nextWeaponInfoText.text = $"下一把：W{nextWeapon + 1}  {next.name}\n基础火力 {Number(next.baseDamage)} · 弹量 {next.baseAmmo}" +
                    (unlocked ? "" : $"\n通关 B{gate + 1} 后可购买");
                nextWeaponPriceText.text = unlocked ? $"购买  {Number(next.cost)}" : "尚未解锁";
                nextWeaponButton.interactable = unlocked && data.coins >= next.cost;
            }

            for (int i = 0; i < weaponButtons.Length; i++)
            {
                bool owned = i <= data.highestOwnedWeaponIndex;
                weaponButtonLabels[i].text = owned ? $"W{i + 1}\n{balance.weapons[i].name}" : $"W{i + 1}\n未获得";
                weaponButtons[i].interactable = owned && i != weaponIndex;
            }
            menuBossBody.color = bossColors[selectedBoss % bossColors.Length];
            RefreshGun(menuGunBarrel, menuGunBody, menuGunStock, weaponIndex);
        }

        double DamageAt(int weaponIndex, int level) => Math.Round(
            balance.weapons[weaponIndex].baseDamage * (1 + balance.powerPerLevel * level),
            MidpointRounding.AwayFromZero);

        void RefreshBattle()
        {
            if (Session == null || Session.CurrentBossIndex < 0) return;
            var boss = balance.bosses[Session.CurrentBossIndex];
            float health = Mathf.Clamp01((float)(Session.Health / boss.health));
            battleBossText.text = $"BOSS {Session.CurrentBossIndex + 1:00}  {boss.name}";
            battleHealthText.text = $"{Number(Session.Health)} / {Number(boss.health)}";
            battleAmmoText.text = $"弹药  {Session.AmmoRemaining} / {Session.BattleMaxAmmo}";
            battleCoinsText.text = Number(Session.Data.coins);
            healthFill.rectTransform.anchorMax = new Vector2(health, 1);
            armor75.SetActive(health > .75f);
            armor50.SetActive(health > .50f);
            armor25.SetActive(health > .25f);
            battleStatusText.text = Session.Phase == BattlePhase.Resolving ? "等待最后子弹命中……" :
                Session.Phase == BattlePhase.Fighting ? "点击任意战斗区域射击" : "";
            battleBossBody.color = bossColors[Session.CurrentBossIndex % bossColors.Length];
            RefreshGun(battleGunBarrel, battleGunBody, battleGunStock, Session.Data.currentWeaponIndex);
        }

        void RefreshGun(RectTransform barrel, RectTransform body, GameObject stock, int index)
        {
            barrel.sizeDelta = new Vector2(54 + index * 12, 10 + index * 2);
            body.sizeDelta = new Vector2(76 + index * 10, 35 + index * 3);
            stock.SetActive(index >= 2);
        }

        void SpawnShot()
        {
            var image = new GameObject("Projectile", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(projectileLayer, false);
            image.color = new Color(1f, .72f, .12f);
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = new Vector2(28, 7);
            Vector2 start = projectileLayer.InverseTransformPoint(shotOrigin.position);
            Vector2 end = (Vector2)projectileLayer.InverseTransformPoint(hitTarget.position) +
                new Vector2(UnityEngine.Random.Range(-20, 21), UnityEngine.Random.Range(-55, 56));
            image.rectTransform.anchoredPosition = start;
            shots.Add(new ShotFx { rect = image.rectTransform, start = start, end = end,
                duration = (float)balance.projectileSeconds });
        }

        void Update()
        {
            if (resetConfirmUntil > 0 && Time.unscaledTime > resetConfirmUntil)
            {
                resetConfirmUntil = 0;
                resetLabel.text = "重置进度";
            }
            AnimateShots();
            AnimateCoins();
            AnimateCharacters();
        }

        void AnimateShots()
        {
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                var fx = shots[i];
                fx.age += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(fx.age / fx.duration);
                fx.rect.anchoredPosition = Vector2.Lerp(fx.start, fx.end, t);
                if (t < 1) continue;
                Destroy(fx.rect.gameObject);
                shots.RemoveAt(i);
                if (Session == null || !Session.ResolveNextShot()) continue;
                bossKick = Mathf.Min(1.5f, bossKick + .9f);
                hitFade = 1;
                hitText.text = $"-{Number(Session.LastHitDamage)}   +{Number(Session.LastHitCoins)}";
                SpawnCoins(Session.LastHitCoins > 0 ? 2 : 0);
                if (Session.Phase == BattlePhase.Won) SpawnCoins(6);
                Save();
                RefreshBattle();
                if (Session.Phase == BattlePhase.Won || Session.Phase == BattlePhase.Lost)
                    ShowResult();
            }
        }

        void SpawnCoins(int count)
        {
            count = Mathf.Min(count, 12 - coins.Count);
            for (int i = 0; i < count; i++)
            {
                var text = new GameObject("Coin", typeof(RectTransform), typeof(TextMeshProUGUI))
                    .GetComponent<TextMeshProUGUI>();
                text.transform.SetParent(projectileLayer, false);
                text.font = battleCoinsText.font;
                text.text = "●";
                text.fontSize = 28;
                text.alignment = TextAlignmentOptions.Center;
                text.color = new Color(1f, .71f, .08f);
                text.raycastTarget = false;
                text.rectTransform.sizeDelta = new Vector2(34, 34);
                Vector2 start = (Vector2)projectileLayer.InverseTransformPoint(hitTarget.position) +
                    new Vector2(UnityEngine.Random.Range(-45, 46), UnityEngine.Random.Range(-60, 61));
                Vector2 end = projectileLayer.InverseTransformPoint(coinTarget.position);
                text.rectTransform.anchoredPosition = start;
                coins.Add(new CoinFx { rect = text.rectTransform, start = start, end = end,
                    duration = UnityEngine.Random.Range(.28f, .46f) });
            }
        }

        void AnimateCoins()
        {
            for (int i = coins.Count - 1; i >= 0; i--)
            {
                var fx = coins[i];
                fx.age += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(fx.age / fx.duration);
                Vector2 position = Vector2.Lerp(fx.start, fx.end, t);
                position.y += Mathf.Sin(t * Mathf.PI) * 70;
                fx.rect.anchoredPosition = position;
                fx.rect.localScale = Vector3.one * Mathf.Lerp(1, .55f, t);
                if (t < 1) continue;
                Destroy(fx.rect.gameObject);
                coins.RemoveAt(i);
            }
        }

        void AnimateCharacters()
        {
            recoil = Mathf.MoveTowards(recoil, 0, Time.unscaledDeltaTime * 10);
            flash = Mathf.MoveTowards(flash, 0, Time.unscaledDeltaTime * 18);
            bossKick = Mathf.MoveTowards(bossKick, 0, Time.unscaledDeltaTime * 5);
            hitFade = Mathf.MoveTowards(hitFade, 0, Time.unscaledDeltaTime * 2.8f);
            gunRoot.anchoredPosition = gunBase + Vector2.left * recoil * 13;
            gunRoot.localRotation = Quaternion.Euler(0, 0, recoil * 5);
            bossRoot.anchoredPosition = bossBase + Vector2.right * bossKick * 15;
            bossRoot.localRotation = Quaternion.Euler(0, 0, -bossKick * 5);
            muzzleFlash.color = new Color(1, .76f, .12f, flash);
            hitText.color = new Color(1, .84f, .18f, hitFade);
            if (Session != null && Session.CurrentBossIndex >= 0)
            {
                float target = Mathf.Clamp01((float)(Session.Health /
                    balance.bosses[Session.CurrentBossIndex].health));
                healthLag = Mathf.MoveTowards(healthLag, target, Time.unscaledDeltaTime * .55f);
                healthLagFill.rectTransform.anchorMax = new Vector2(healthLag, 1);
            }
        }

        void ShowResult()
        {
            resultPanel.SetActive(true);
            if (Session.Phase == BattlePhase.Won)
            {
                resultTitle.text = "BOSS 已击败";
                resultDetails.text = $"命中金币  +{Number(Session.BattleHitCoins)}\n" +
                    $"击败奖励  +{Number(Session.LastKillReward)}" +
                    (Session.LastFirstBonus > 0 ? $"\n首次通关  +{Number(Session.LastFirstBonus)}" : "");
            }
            else
            {
                resultTitle.text = "弹药耗尽";
                resultDetails.text = $"还差  {Number(Session.Health)}  伤害\n本局已保留  {Number(Session.BattleHitCoins)}  金币\n" +
                    "返回主菜单强化后继续挑战";
            }
        }

        void ResetProgress()
        {
            if (resetConfirmUntil <= 0 || Time.unscaledTime > resetConfirmUntil)
            {
                resetConfirmUntil = Time.unscaledTime + 4;
                resetLabel.text = "再次点击确认";
                return;
            }
            if (!store.TryReset(out var data, out string message))
            {
                saveText.text = message;
                saveReloadButton.gameObject.SetActive(true);
                return;
            }
            Session = new GameSession(balance, data);
            resetConfirmUntil = 0;
            resetLabel.text = "重置进度";
            saveText.text = "进度已重置";
            saveReloadButton.gameObject.SetActive(false);
            ShowMenu();
        }

        void Save()
        {
            if (Session == null) return;
            bool success = store.TrySave(Session.Data, out string message);
            saveText.text = success ? "进度已保存" : message;
            saveReloadButton.gameObject.SetActive(!success);
        }

        void ClearEffects()
        {
            foreach (var shot in shots) if (shot.rect) Destroy(shot.rect.gameObject);
            foreach (var coin in coins) if (coin.rect) Destroy(coin.rect.gameObject);
            shots.Clear();
            coins.Clear();
        }

        void OnApplicationPause(bool paused) { if (paused) Save(); }
        void OnApplicationFocus(bool focused) { if (!focused) Save(); }
        void OnApplicationQuit() => Save();

        public static string Number(double value)
        {
            string[] suffixes = { "", "K", "M", "B", "T" };
            int unit = 0;
            while (Math.Abs(value) >= 1000 && unit < suffixes.Length - 1)
            {
                value /= 1000;
                unit++;
            }
            return value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + suffixes[unit];
        }
    }
}
