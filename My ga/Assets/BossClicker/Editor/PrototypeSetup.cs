using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace BossClicker.Editor
{
    public static class PrototypeSetup
    {
        const string Root = "Assets/BossClicker";
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        static TMP_FontAsset font;
        static Color Ink => new Color(.055f, .06f, .08f);
        static Color Paper => new Color(.96f, .92f, .80f);
        static Color Sky => new Color(.42f, .77f, .93f);
        static Color Orange => new Color(.98f, .48f, .10f);
        static Color Teal => new Color(.18f, .66f, .68f);
        static Color Gold => new Color(1f, .69f, .08f);
        static Color Red => new Color(.88f, .20f, .16f);
        static Color Muted => new Color(.28f, .31f, .34f);

        [MenuItem("Boss Clicker/Apply V3 Balance")]
        public static void ApplyV3Balance()
        {
            var defaults = ScriptableObject.CreateInstance<GameBalance>();
            var existing = AssetDatabase.LoadAssetAtPath<GameBalance>(Root + "/Data/PrototypeBalance.asset");
            if (existing)
            {
                EditorUtility.CopySerialized(defaults, existing);
                EditorUtility.SetDirty(existing);
            }
            else
            {
                AssetDatabase.CreateAsset(UnityEngine.Object.Instantiate(defaults),
                    Root + "/Data/PrototypeBalance.asset");
            }
            UnityEngine.Object.DestroyImmediate(defaults);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Boss Clicker/Create V3 WebGL Prototype")]
        public static void CreateV3Prototype()
        {
            ApplyV3Balance();
            AssetDatabase.DeleteAsset(Root + "/Art/PrototypeChinese.asset");
            CreateScene();
        }

        [MenuItem("Boss Clicker/Create Prototype Scene")]
        public static void CreateScene()
        {
            Directory.CreateDirectory(Root + "/Data");
            Directory.CreateDirectory(Root + "/Art");
            EnsureTmpResources();
            font = CreateFont();
            var balance = AssetDatabase.LoadAssetAtPath<GameBalance>(Root + "/Data/PrototypeBalance.asset");
            if (!balance)
            {
                balance = ScriptableObject.CreateInstance<GameBalance>();
                AssetDatabase.CreateAsset(balance, Root + "/Data/PrototypeBalance.asset");
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var obj in scene.GetRootGameObjects())
                if (obj.name == "LootshotUI" || obj.name == "BossForgeUI")
                    UnityEngine.Object.DestroyImmediate(obj);
            if (!UnityEngine.Object.FindObjectOfType<EventSystem>())
                new GameObject("LootshotEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var canvasObject = new GameObject("LootshotUI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;

            var background = Box(canvasObject.transform, "Background", 0, 0, 1280, 720, Ink);
            background.rectTransform.anchorMin = Vector2.zero;
            background.rectTransform.anchorMax = Vector2.one;
            background.rectTransform.offsetMin = background.rectTransform.offsetMax = Vector2.zero;
            var root = NewRect(canvasObject.transform, "GameRoot", 0, 0, 1280, 720);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, .5f);
            root.anchoredPosition = Vector2.zero;
            var view = root.gameObject.AddComponent<GameView>();
            view.balance = balance;

            BuildMenu(root, view, balance);
            BuildBattle(root, view);

            PlayerSettings.companyName = "LootshotStudio";
            PlayerSettings.productName = "LOOTSHOT Prototype";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = false;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("LOOTSHOT_V3_SETUP_COMPLETE");
        }

        static void BuildMenu(RectTransform root, GameView view, GameBalance balance)
        {
            var screen = NewRect(root, "MenuScreen", 0, 0, 1280, 720);
            view.menuScreen = screen.gameObject;
            Box(screen, "Sky", 0, 0, 1280, 720, Sky);
            AddCity(screen, 420);
            Box(screen, "Roof", 0, 590, 1280, 130, new Color(.31f, .27f, .25f));

            var title = Card(screen, "TitleCard", 20, 16, 370, 72, Paper);
            Label(title, "Title", "LOOTSHOT", 18, 4, 334, 52, 39, Ink, TextAlignmentOptions.Center);
            Label(title, "Sub", "点击 · 破坏 · 变强", 20, 48, 330, 18, 12, Muted,
                TextAlignmentOptions.Center);
            var coins = Card(screen, "Coins", 1010, 18, 250, 62, Ink);
            Label(coins, "Icon", "●", 15, 9, 42, 42, 31, Gold, TextAlignmentOptions.Center);
            view.menuCoinsText = Label(coins, "Value", "0", 58, 7, 172, 45, 31, Color.white,
                TextAlignmentOptions.Center);

            var character = Card(screen, "CharacterCard", 20, 106, 300, 478,
                new Color(.94f, .84f, .65f));
            Label(character, "Heading", "城市赏金猎人", 16, 14, 260, 30, 21, Ink,
                TextAlignmentOptions.Center);
            BuildGunner(character, 22, 62, 1.02f);
            Label(character, "Quote", "“小枪，大计划。”", 26, 420, 240, 30, 17, Muted,
                TextAlignmentOptions.Center);

            var weapon = Card(screen, "WeaponCard", 340, 106, 520, 405, Paper);
            view.weaponNameText = Label(weapon, "WeaponName", "W1  制式手枪", 20, 14, 480, 42,
                29, Ink, TextAlignmentOptions.Center);
            var gun = NewRect(weapon, "MenuGun", 135, 67, 250, 104);
            BuildGun(gun, out view.menuGunBarrel, out view.menuGunBody,
                out view.menuGunStock, out _, out _);
            view.weaponStatsText = Label(weapon, "Stats", "单发火力 10     弹量 12", 20, 169,
                480, 35, 20, Muted, TextAlignmentOptions.Center);

            var power = Card(weapon, "Power", 16, 215, 236, 166, new Color(.91f, .86f, .74f));
            view.powerInfoText = Label(power, "Info", "火力", 12, 10, 204, 86, 18, Ink,
                TextAlignmentOptions.Center);
            view.powerButton = Button(power, "Buy", "升级", 12, 108, 204, 42,
                out view.powerPriceText, Orange, 16);
            var ammo = Card(weapon, "Ammo", 268, 215, 236, 166, new Color(.91f, .86f, .74f));
            view.ammoInfoText = Label(ammo, "Info", "弹量", 12, 10, 204, 86, 18, Ink,
                TextAlignmentOptions.Center);
            view.ammoButton = Button(ammo, "Buy", "升级", 12, 108, 204, 42,
                out view.ammoPriceText, Teal, 16);

            var boss = Card(screen, "BossCard", 880, 106, 380, 405, Paper);
            Label(boss, "Heading", "目标", 20, 12, 340, 28, 17, Muted,
                TextAlignmentOptions.Center);
            var preview = NewRect(boss, "BossPreview", 105, 48, 170, 190);
            BuildBoss(preview, out view.menuBossBody, out _, out _, out _);
            view.bossNameText = Label(boss, "BossName", "BOSS 01", 36, 235, 308, 34, 23, Ink,
                TextAlignmentOptions.Center);
            view.bossInfoText = Label(boss, "BossInfo", "生命 / 奖励", 36, 271, 308, 82, 15, Muted,
                TextAlignmentOptions.Center);
            view.bossPreviousButton = Button(boss, "Previous", "<", 12, 174, 58, 54,
                out _, Teal, 25);
            view.bossNextButton = Button(boss, "Next", ">", 302, 174, 58, 54,
                out _, Teal, 25);

            var gold = Card(screen, "GoldBonus", 340, 527, 520, 82, Paper);
            view.goldInfoText = Label(gold, "Info", "金币加成", 18, 8, 310, 58, 17, Ink,
                TextAlignmentOptions.Center);
            view.goldButton = Button(gold, "Buy", "升级", 344, 14, 156, 46,
                out view.goldPriceText, Gold, 16);

            var weapons = Card(screen, "WeaponSelect", 340, 624, 520, 76, Ink);
            view.weaponButtons = new Button[balance.weapons.Length];
            view.weaponButtonLabels = new TMP_Text[balance.weapons.Length];
            float buttonWidth = 119;
            for (int i = 0; i < balance.weapons.Length; i++)
                view.weaponButtons[i] = Button(weapons, "Weapon" + (i + 1), "W" + (i + 1),
                    10 + i * 126, 10, buttonWidth, 48, out view.weaponButtonLabels[i], Paper, 13);

            view.fightButton = Button(screen, "Fight", "开始战斗", 880, 530, 380, 88,
                out _, Orange, 30);
            var next = Card(screen, "NextWeapon", 20, 600, 300, 100, Paper);
            view.nextWeaponInfoText = Label(next, "Info", "下一把武器", 10, 6, 174, 78, 13, Ink,
                TextAlignmentOptions.Center);
            view.nextWeaponButton = Button(next, "Buy", "购买", 190, 18, 96, 50,
                out view.nextWeaponPriceText, Gold, 13);

            view.progressText = Label(screen, "Progress", "0 / 12 已通关", 880, 630, 170, 24,
                14, Color.white);
            view.saveText = Label(screen, "Save", "进度自动保存", 880, 661, 170, 22,
                12, Color.white);
            view.resetButton = Button(screen, "Reset", "重置进度", 1060, 632, 196, 30,
                out view.resetLabel, new Color(.82f, .29f, .23f), 13);
            view.saveReloadButton = Button(screen, "ReloadSave", "重新读取", 1060, 668, 196, 28,
                out _, Teal, 12);
            view.saveReloadButton.gameObject.SetActive(false);
        }

        static void BuildBattle(RectTransform root, GameView view)
        {
            var screen = NewRect(root, "BattleScreen", 0, 0, 1280, 720);
            view.battleScreen = screen.gameObject;
            Box(screen, "Sky", 0, 0, 1280, 720, Sky);
            AddCity(screen, 350);
            Box(screen, "RoofBack", 0, 510, 1280, 210, new Color(.33f, .29f, .27f));
            Box(screen, "RoofLine", 0, 510, 1280, 12, Ink);

            var click = Box(screen, "FireArea", 0, 0, 1280, 720, new Color(1, 1, 1, .001f));
            click.raycastTarget = true;
            var input = click.gameObject.AddComponent<FireInput>();
            input.view = view;

            var player = NewRect(screen, "Player", 72, 330, 300, 250);
            BuildBattleGunner(player);
            view.gunRoot = NewRect(player, "Gun", 150, 88, 235, 100);
            BuildGun(view.gunRoot, out view.battleGunBarrel, out view.battleGunBody,
                out view.battleGunStock, out view.muzzleFlash, out view.shotOrigin);

            view.bossRoot = NewRect(screen, "Boss", 895, 246, 300, 340);
            BuildBoss(view.bossRoot, out view.battleBossBody, out view.armor75,
                out view.armor50, out view.armor25);
            view.hitTarget = NewRect(view.bossRoot, "HitTarget", 120, 125, 1, 1);

            view.projectileLayer = NewRect(screen, "Effects", 0, 0, 1280, 720);

            var exit = Button(screen, "Exit", "返回菜单", 20, 18, 142, 50,
                out _, Teal, 15);
            view.exitButton = exit;
            var health = Card(screen, "BossHealth", 330, 16, 620, 86, Paper);
            view.battleBossText = Label(health, "BossName", "BOSS 01", 15, 5, 590, 30,
                21, Ink, TextAlignmentOptions.Center);
            var healthBar = Box(health, "HealthBar", 28, 42, 564, 25, Ink);
            view.healthLagFill = Box(healthBar.transform, "Lag", 3, 3, 558, 19, Gold);
            view.healthFill = Box(healthBar.transform, "Fill", 3, 3, 558, 19, Red);
            StretchFill(view.healthLagFill.rectTransform);
            StretchFill(view.healthFill.rectTransform);
            view.battleHealthText = Label(health, "HealthText", "60 / 60", 150, 41, 320, 25,
                14, Color.white, TextAlignmentOptions.Center);

            var coin = Card(screen, "BattleCoins", 1030, 18, 230, 58, Ink);
            Label(coin, "Coin", "●", 12, 6, 44, 40, 30, Gold, TextAlignmentOptions.Center);
            view.battleCoinsText = Label(coin, "Value", "0", 52, 5, 160, 42, 28, Color.white,
                TextAlignmentOptions.Center);
            view.coinTarget = view.battleCoinsText.rectTransform;
            var ammo = Card(screen, "Ammo", 20, 100, 190, 90, Paper);
            Label(ammo, "Bullets", "● ● ●", 10, 8, 170, 27, 18, Gold,
                TextAlignmentOptions.Center);
            view.battleAmmoText = Label(ammo, "Value", "弹药 12 / 12", 8, 39, 174, 35,
                17, Ink, TextAlignmentOptions.Center);

            view.hitText = Label(screen, "Hit", "", 720, 250, 260, 50, 28, Gold,
                TextAlignmentOptions.Center);
            view.battleStatusText = Label(screen, "Status", "点击任意战斗区域射击", 390, 642,
                500, 42, 21, Color.white, TextAlignmentOptions.Center);

            var result = Card(screen, "Result", 390, 190, 500, 330, Paper);
            view.resultPanel = result.parent.gameObject;
            view.resultTitle = Label(result, "Title", "战斗结束", 24, 28, 452, 52, 32, Ink,
                TextAlignmentOptions.Center);
            view.resultDetails = Label(result, "Details", "", 40, 96, 420, 132, 19, Muted,
                TextAlignmentOptions.Center);
            view.resultBackButton = Button(result, "Back", "返回主菜单", 70, 248, 360, 58,
                out _, Orange, 22);
            view.resultPanel.SetActive(false);
            screen.gameObject.SetActive(false);
        }

        static void BuildGunner(Transform parent, float x, float y, float scale)
        {
            var root = NewRect(parent, "Gunner", x, y, 250, 340);
            root.localScale = Vector3.one * scale;
            Box(root, "BootL", 48, 290, 72, 35, Ink).rectTransform.localRotation = Quaternion.Euler(0, 0, 3);
            Box(root, "BootR", 145, 290, 72, 35, Ink).rectTransform.localRotation = Quaternion.Euler(0, 0, -4);
            Box(root, "LegL", 76, 220, 47, 86, new Color(.13f, .15f, .18f));
            Box(root, "LegR", 140, 220, 47, 86, new Color(.13f, .15f, .18f));
            Box(root, "TorsoOutline", 55, 105, 150, 142, Ink);
            Box(root, "Jacket", 63, 112, 134, 128, Orange);
            Box(root, "Scarf", 46, 103, 172, 24, Teal).rectTransform.localRotation = Quaternion.Euler(0, 0, -7);
            Box(root, "HeadOutline", 81, 22, 108, 102, Ink);
            Box(root, "Face", 88, 29, 94, 88, new Color(.92f, .62f, .43f));
            Box(root, "Hair1", 76, 12, 125, 28, Ink).rectTransform.localRotation = Quaternion.Euler(0, 0, -6);
            Box(root, "Hair2", 152, 3, 42, 48, Ink).rectTransform.localRotation = Quaternion.Euler(0, 0, 35);
            Box(root, "Eye", 142, 58, 24, 8, Ink);
            Box(root, "Smile", 139, 86, 28, 6, Ink).rectTransform.localRotation = Quaternion.Euler(0, 0, 10);
            Box(root, "Arm", 178, 132, 40, 108, Orange).rectTransform.localRotation = Quaternion.Euler(0, 0, -14);
            Box(root, "Pistol", 181, 222, 62, 22, Ink);
        }

        static void BuildBattleGunner(Transform root)
        {
            Box(root, "BootL", 35, 198, 70, 30, Ink);
            Box(root, "BootR", 118, 198, 70, 30, Ink);
            Box(root, "LegL", 61, 139, 38, 72, new Color(.13f, .15f, .18f));
            Box(root, "LegR", 119, 139, 38, 72, new Color(.13f, .15f, .18f));
            Box(root, "TorsoOutline", 45, 65, 130, 96, Ink);
            Box(root, "Jacket", 52, 72, 116, 82, Orange);
            Box(root, "Scarf", 30, 57, 150, 20, Teal).rectTransform.localRotation = Quaternion.Euler(0, 0, -6);
            Box(root, "HeadOutline", 64, 3, 92, 78, Ink);
            Box(root, "Face", 71, 10, 78, 64, new Color(.92f, .62f, .43f));
            Box(root, "Hair", 57, 0, 107, 24, Ink).rectTransform.localRotation = Quaternion.Euler(0, 0, -5);
        }

        static void BuildGun(Transform root, out RectTransform barrel, out RectTransform body,
            out GameObject stock, out Image muzzle, out RectTransform origin)
        {
            stock = Box(root, "Stock", 25, 36, 54, 31, Ink).gameObject;
            body = Box(root, "Body", 63, 20, 76, 35, new Color(.18f, .23f, .27f)).rectTransform;
            Box(root, "Grip", 91, 49, 23, 42, Ink).rectTransform.localRotation = Quaternion.Euler(0, 0, -12);
            barrel = Box(root, "Barrel", 132, 28, 54, 10, Ink).rectTransform;
            Box(root, "Accent", 73, 25, 38, 8, Orange);
            muzzle = Box(root, "Muzzle", 202, 18, 28, 30, Color.clear);
            origin = NewRect(root, "ShotOrigin", 220, 33, 1, 1);
        }

        static void BuildBoss(Transform root, out Image body, out GameObject armor75,
            out GameObject armor50, out GameObject armor25)
        {
            Box(root, "BootL", 20, 285, 105, 42, Ink);
            Box(root, "BootR", 165, 285, 105, 42, Ink);
            Box(root, "LegL", 65, 220, 62, 78, new Color(.18f, .19f, .17f));
            Box(root, "LegR", 164, 220, 62, 78, new Color(.18f, .19f, .17f));
            Box(root, "BodyOutline", 38, 80, 230, 166, Ink);
            body = Box(root, "Body", 48, 90, 210, 146, Red);
            Box(root, "HeadOutline", 91, 13, 128, 105, Ink);
            Box(root, "Face", 100, 22, 110, 87, new Color(.88f, .57f, .39f));
            Box(root, "Mohawk", 138, 0, 38, 45, Red);
            Box(root, "Eyes", 126, 55, 64, 9, Ink).rectTransform.localRotation = Quaternion.Euler(0, 0, -5);
            armor75 = Box(root, "Armor75", 16, 90, 74, 72, new Color(.12f, .13f, .15f)).gameObject;
            armor50 = Box(root, "Armor50", 218, 89, 74, 72, new Color(.12f, .13f, .15f)).gameObject;
            armor25 = Box(root, "Armor25", 102, 106, 102, 58, new Color(.18f, .20f, .22f)).gameObject;
            Box(armor75.transform, "Spike", -10, -15, 24, 40, Ink).rectTransform.localRotation = Quaternion.Euler(0, 0, -25);
            Box(armor50.transform, "Spike", 59, -15, 24, 40, Ink).rectTransform.localRotation = Quaternion.Euler(0, 0, 25);
        }

        static void AddCity(Transform parent, float groundY)
        {
            Color[] colors = {
                new Color(.73f,.55f,.48f), new Color(.82f,.67f,.51f),
                new Color(.48f,.59f,.65f), new Color(.69f,.48f,.56f)
            };
            for (int i = 0; i < 12; i++)
            {
                float width = 90 + i % 3 * 22;
                float height = 110 + i % 4 * 38;
                float x = i * 112 - 25;
                Box(parent, "Building" + i, x, groundY - height, width, height, colors[i % colors.Length]);
                for (int w = 0; w < 3; w++)
                    Box(parent, "Window" + i + "_" + w, x + 15 + w * 25, groundY - height + 28,
                        12, 20, new Color(1f, .84f, .48f));
            }
        }

        static void EnsureTmpResources()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
                return;
            string package = Path.GetFullPath(
                "Library/PackageCache/com.unity.textmeshpro@3.0.7/Package Resources/TMP Essential Resources.unitypackage");
            AssetDatabase.ImportPackage(package, false);
        }

        static TMP_FontAsset CreateFont()
        {
            const string path = Root + "/Art/PrototypeChinese.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing) return existing;
            const string sourcePath = Root + "/Art/PrototypeSource.ttf";
            File.Copy(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
                "NotoSansSC-VF.ttf"), sourcePath, true);
            AssetDatabase.ImportAsset(sourcePath, ImportAssetOptions.ForceSynchronousImport);
            var source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            var asset = TMP_FontAsset.CreateFontAsset(source, 48, 6, GlyphRenderMode.SDFAA,
                2048, 2048, AtlasPopulationMode.Dynamic, false);
            if (!asset) throw new InvalidOperationException("Could not load the installed Chinese font.");
            string sources = string.Concat(Directory.GetFiles(Root, "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));
            string characters = new string((new string(Enumerable.Range(32, 95)
                .Select(i => (char)i).ToArray()) + sources).Where(c => !char.IsControl(c)).Distinct().ToArray());
            if (!asset.TryAddCharacters(characters, out string missing))
                throw new InvalidOperationException("Font lacks required characters: " + missing);
            asset.name = "PrototypeChinese";
            asset.atlasPopulationMode = AtlasPopulationMode.Static;
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("m_SourceFontFile_EditorRef").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(asset, path);
            asset.material.name = "PrototypeChinese Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            foreach (var texture in asset.atlasTextures)
            {
                texture.name = "PrototypeChinese Atlas";
                AssetDatabase.AddObjectToAsset(texture, asset);
            }
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.DeleteAsset(sourcePath);
            return asset;
        }

        static Transform Card(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            var outline = Box(parent, name, x, y, w, h, Ink);
            return Box(outline.transform, "Fill", 4, 4, w - 8, h - 8, color).transform;
        }

        static void StretchFill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static RectTransform NewRect(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        static Image Box(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            var image = NewRect(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static TMP_Text Label(Transform parent, string name, string value, float x, float y,
            float w, float h, float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            var text = NewRect(parent, name, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Truncate;
            text.richText = false;
            text.raycastTarget = false;
            return text;
        }

        static Button Button(Transform parent, string name, string value, float x, float y,
            float w, float h, out TMP_Text label, Color color, float size)
        {
            var image = Box(parent, name, x, y, w, h, color);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f);
            colors.pressedColor = new Color(.72f, .72f, .72f);
            colors.disabledColor = new Color(.42f, .43f, .43f);
            button.colors = colors;
            label = Label(image.transform, "Label", value, 3, 0, w - 6, h, size, Ink,
                TextAlignmentOptions.Center);
            return button;
        }

        [MenuItem("Boss Clicker/Build WebGL Prototype")]
        public static void BuildWebGL()
        {
            BuildWebGL(BuildOptions.None);
        }

        [MenuItem("Boss Clicker/Build And Run WebGL Prototype")]
        public static void BuildAndRunWebGL()
        {
            BuildWebGL(BuildOptions.AutoRunPlayer);
        }

        static void BuildWebGL(BuildOptions options)
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = false;
            string output = Path.GetFullPath("Builds/WebGL");
            Directory.CreateDirectory(output);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = output,
                target = BuildTarget.WebGL, options = options
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("WebGL build failed: " + report.summary.result);
            Debug.Log("LOOTSHOT_WEBGL_BUILD_COMPLETE " + output);
        }
    }
}
