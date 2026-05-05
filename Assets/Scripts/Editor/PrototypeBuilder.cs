using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WizardGrower.Combat;
using WizardGrower.Core;
using WizardGrower.Economy;
using WizardGrower.Enemies;
using WizardGrower.Player;
using WizardGrower.Stages;
using WizardGrower.UI;
using WizardGrower.Upgrades;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace WizardGrower.EditorTools
{
    public static class PrototypeBuilder
    {
        private const string ArtPath = "Assets/Art/Generated/";
        private const string PrefabPath = "Assets/Prefabs/";
        private const string ScenePath = "Assets/Scenes/MainScene.unity";

        [MenuItem("Wizard Grower/Build Prototype Scene")]
        public static void BuildAllMenu()
        {
            Debug.Log(BuildAll());
        }

        public static string BuildAll()
        {
            Directory.CreateDirectory(ArtPath);
            Directory.CreateDirectory(PrefabPath);

            Sprite wizard = CreateSprite("Wizard", DrawWizard);
            Sprite autoProjectile = CreateSprite("NormalProjectile", t => DrawOrb(t, new Color(0.2f, 0.85f, 1f), 22, false));
            Sprite clickProjectile = CreateSprite("ClickProjectile", t => DrawOrb(t, new Color(0.5f, 1f, 1f), 30, true));
            Sprite skillProjectile = CreateSprite("SkillProjectile", DrawSkillOrb);
            Sprite slime = CreateSprite("Slime", DrawSlime);
            Sprite boss = CreateSprite("Boss", DrawBoss);
            Sprite background = CreateSprite("TopDownBackground", DrawBackground, 512, 1024, 100f);
            Sprite goldIcon = CreateSprite("GoldIcon", DrawGoldIcon, 96, 96, 100f);
            Sprite manaIcon = CreateSprite("ManaIcon", t => DrawOrb(t, new Color(0.15f, 0.45f, 1f), 28, true), 96, 96, 100f);
            Sprite attackIcon = CreateSprite("AttackUpgradeIcon", DrawAttackIcon, 96, 96, 100f);
            Sprite critIcon = CreateSprite("CriticalUpgradeIcon", DrawCritIcon, 96, 96, 100f);
            Sprite skillIcon = CreateSprite("SkillIcon", DrawSkillOrb, 96, 96, 100f);

            GameObject wizardPrefab = CreateWizardPrefab(wizard);
            NormalEnemy normalPrefab = CreateEnemyPrefab<NormalEnemy>("NormalEnemy", slime, Vector3.one * 1.25f);
            BossEnemy bossPrefab = CreateEnemyPrefab<BossEnemy>("BossEnemy", boss, Vector3.one * 2.0f);
            Projectile autoPrefab = CreateProjectilePrefab("AutoProjectile", autoProjectile, Vector3.one * 0.45f, new Color(0.1f, 0.8f, 1f));
            Projectile clickPrefab = CreateProjectilePrefab("ClickProjectile", clickProjectile, Vector3.one * 0.58f, new Color(0.45f, 1f, 1f));
            Projectile skillPrefab = CreateProjectilePrefab("SkillProjectile", skillProjectile, Vector3.one * 0.9f, new Color(0.85f, 0.45f, 1f));
            DamageTextView damageTextPrefab = CreateDamageTextPrefab();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MainScene";

            Camera camera = CreateCamera();
            SpriteRenderer backgroundRenderer = CreateBackground(background);
            camera.GetComponent<MobileCameraFitter>().SetBackground(backgroundRenderer);

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(wizardPrefab);
            player.name = "PlayerWizard";
            player.transform.position = new Vector3(0f, -2.75f, 0f);

            GameObject roots = new GameObject("Runtime");
            GameContext context = roots.AddComponent<GameContext>();
            GameManager manager = roots.AddComponent<GameManager>();
            CurrencyWallet wallet = roots.AddComponent<CurrencyWallet>();
            BossStageController bossStage = roots.AddComponent<BossStageController>();
            StageManager stage = roots.AddComponent<StageManager>();
            UpgradeSystem upgrades = roots.AddComponent<UpgradeSystem>();
            AutoAttackController autoAttack = roots.AddComponent<AutoAttackController>();
            ClickAttackController clickAttack = roots.AddComponent<ClickAttackController>();
            ActiveSkillController activeSkill = roots.AddComponent<ActiveSkillController>();

            GameObject projectileRoot = new GameObject("Projectiles");
            ProjectileFactory factory = projectileRoot.AddComponent<ProjectileFactory>();
            SetField(factory, "autoProjectilePrefab", autoPrefab);
            SetField(factory, "manualProjectilePrefab", clickPrefab);
            SetField(factory, "skillProjectilePrefab", skillPrefab);
            SetField(factory, "projectileRoot", projectileRoot.transform);

            GameObject enemyRoot = new GameObject("EnemySpawner");
            enemyRoot.transform.position = Vector3.zero;
            Transform spawnPoint = new GameObject("MonsterSpawnPoint").transform;
            spawnPoint.position = new Vector3(0f, 2.15f, 0f);
            spawnPoint.SetParent(enemyRoot.transform);
            EnemySpawner spawner = enemyRoot.AddComponent<EnemySpawner>();
            SetField(spawner, "normalEnemyPrefab", normalPrefab);
            SetField(spawner, "bossEnemyPrefab", bossPrefab);
            SetField(spawner, "spawnPoint", spawnPoint);

            Canvas canvas = CreateCanvas();
            HUDParts hudParts = CreateHud(canvas, attackIcon, manaIcon, critIcon, skillIcon);
            FloatingTextSpawner floating = roots.AddComponent<FloatingTextSpawner>();
            SetField(floating, "damageTextPrefab", damageTextPrefab);
            SetField(floating, "canvas", canvas);
            SetField(floating, "mainCamera", camera);

            PlayerWizard wizardComponent = player.GetComponent<PlayerWizard>();
            PlayerMovementController movement = player.GetComponent<PlayerMovementController>();
            PlayerMana mana = player.GetComponent<PlayerMana>();
            PlayerProgression progression = player.GetComponent<PlayerProgression>();

            SetField(context, "Wizard", wizardComponent);
            SetField(context, "Movement", movement);
            SetField(context, "Mana", mana);
            SetField(context, "Progression", progression);
            SetField(context, "Wallet", wallet);
            SetField(context, "EnemySpawner", spawner);
            SetField(context, "ProjectileFactory", factory);
            SetField(context, "AutoAttack", autoAttack);
            SetField(context, "ClickAttack", clickAttack);
            SetField(context, "ActiveSkill", activeSkill);
            SetField(context, "StageManager", stage);
            SetField(context, "BossStage", bossStage);
            SetField(context, "UpgradeSystem", upgrades);
            SetField(context, "HUD", hudParts.Hud);
            SetField(context, "FloatingText", floating);
            SetField(manager, "context", context);

            SetField(hudParts.Hud, "stageLabel", hudParts.Stage);
            SetField(hudParts.Hud, "goldLabel", hudParts.Gold);
            SetField(hudParts.Hud, "attackLabel", hudParts.Attack);
            SetField(hudParts.Hud, "feedbackLabel", hudParts.Feedback);
            SetField(hudParts.Hud, "manaBar", hudParts.ManaBar);
            SetField(hudParts.Hud, "healthBar", hudParts.HealthBar);
            SetField(hudParts.Hud, "bossTimer", hudParts.BossTimer);
            SetField(hudParts.Hud, "dpsView", hudParts.Dps);
            SetField(hudParts.Hud, "joystickIndicator", hudParts.Joystick);
            SetField(hudParts.Hud, "skillButton", hudParts.SkillButton);
            SetField(hudParts.Hud, "skillButtonLabel", hudParts.SkillLabel);
            SetField(hudParts.Hud, "manualAttackButton", hudParts.ManualButton);
            SetField(hudParts.Hud, "manualAttackButtonLabel", hudParts.ManualLabel);
            SetField(hudParts.Hud, "autoToggleButton", hudParts.AutoToggleButton);
            SetField(hudParts.Hud, "autoToggleButtonLabel", hudParts.AutoToggleLabel);
            SetField(hudParts.Hud, "upgradeButtons", hudParts.Upgrades);
            SetField(hudParts.Hud, "upgradeIcons", new[] { attackIcon, manaIcon, critIcon });

            PrefabUtility.SaveAsPrefabAsset(player, PrefabPath + "PlayerWizard.prefab");
            PrefabUtility.SaveAsPrefabAsset(canvas.gameObject, PrefabPath + "HUD.prefab");

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "Built Wizard Grower prototype: sprites, prefabs, and Assets/Scenes/MainScene.unity";
        }

        private static Camera CreateCamera()
        {
            GameObject go = new GameObject("Main Camera");
            Camera camera = go.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.18f, 0.16f);
            go.transform.position = new Vector3(0f, 0f, -10f);
            go.AddComponent<MobileCameraFitter>();
            return camera;
        }

        private static SpriteRenderer CreateBackground(Sprite sprite)
        {
            GameObject go = new GameObject("TopDownRpgTrainingGround");
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -10;
            return renderer;
        }

        private static GameObject CreateWizardPrefab(Sprite sprite)
        {
            GameObject go = new GameObject("PlayerWizard");
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 5;
            go.transform.localScale = Vector3.one * 1.25f;
            PlayerWizard wizard = go.AddComponent<PlayerWizard>();
            go.AddComponent<PlayerMovementController>();
            go.AddComponent<PlayerMana>();
            go.AddComponent<PlayerProgression>();
            Transform cast = new GameObject("CastPoint").transform;
            cast.SetParent(go.transform);
            cast.localPosition = new Vector3(0f, 0.6f, 0f);
            wizard.ConfigureCastPoint(cast);
            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath + "PlayerWizard.prefab");
            AssetDatabase.ImportAsset(PrefabPath + "PlayerWizard.prefab", ImportAssetOptions.ForceUpdate);
            UnityEngine.Object.DestroyImmediate(go);
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath + "PlayerWizard.prefab");
        }

        private static T CreateEnemyPrefab<T>(string name, Sprite sprite, Vector3 scale) where T : EnemyBase
        {
            GameObject go = new GameObject(name);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 4;
            go.transform.localScale = scale;
            T enemy = go.AddComponent<T>();
            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath + name + ".prefab");
            UnityEngine.Object.DestroyImmediate(go);
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath + name + ".prefab").GetComponent<T>();
        }

        private static Projectile CreateProjectilePrefab(string name, Sprite sprite, Vector3 scale, Color trailColor)
        {
            GameObject go = new GameObject(name);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 8;
            go.transform.localScale = scale;
            TrailRenderer trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.18f;
            trail.startWidth = 0.24f;
            trail.endWidth = 0f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = trailColor;
            trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
            Projectile projectile = go.AddComponent<Projectile>();
            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath + name + ".prefab");
            UnityEngine.Object.DestroyImmediate(go);
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath + name + ".prefab").GetComponent<Projectile>();
        }

        private static DamageTextView CreateDamageTextPrefab()
        {
            GameObject go = new GameObject("DamageText", typeof(RectTransform));
            DamageTextView view = go.AddComponent<DamageTextView>();
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 26f;
            text.raycastTarget = false;
            SetField(view, "label", text);
            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath + "DamageText.prefab");
            UnityEngine.Object.DestroyImmediate(go);
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath + "DamageText.prefab").GetComponent<DamageTextView>();
        }

        private static Canvas CreateCanvas()
        {
            GameObject go = new GameObject("HUD", typeof(RectTransform));
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();
            go.AddComponent<HUDController>();

            GameObject safeArea = new GameObject("SafeArea", typeof(RectTransform));
            safeArea.transform.SetParent(go.transform, false);
            RectTransform safeRect = safeArea.GetComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.offsetMin = Vector2.zero;
            safeRect.offsetMax = Vector2.zero;
            safeArea.AddComponent<SafeAreaFitter>();

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
            return canvas;
        }

        private static HUDParts CreateHud(Canvas canvas, Sprite attackIcon, Sprite manaIcon, Sprite critIcon, Sprite skillIcon)
        {
            HUDParts parts = new HUDParts();
            parts.Hud = canvas.GetComponent<HUDController>();
            Transform parent = canvas.transform.Find("SafeArea") ?? canvas.transform;
            parts.Stage = CreateText(parent, "StageLabel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(155, -52), new Vector2(270, 54), 28, "Stage 1");
            parts.Gold = CreateText(parent, "GoldLabel", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-155, -52), new Vector2(270, 54), 28, "Gold 0");
            parts.Attack = CreateText(parent, "AttackLabel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -112), new Vector2(430, 50), 24, "ATK 10");
            parts.Dps = CreateText(parent, "DPSLabel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -52), new Vector2(260, 50), 28, "DPS 0").gameObject.AddComponent<DPSView>();
            SetField(parts.Dps, "label", parts.Dps.GetComponent<TMP_Text>());
            parts.Feedback = CreateText(parent, "FeedbackLabel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(500, 60), 42, "");

            parts.BossTimer = CreateText(parent, "BossTimer", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -166), new Vector2(280, 52), 30, "").gameObject.AddComponent<BossTimerView>();
            SetField(parts.BossTimer, "label", parts.BossTimer.GetComponent<TMP_Text>());
            parts.BossTimer.gameObject.SetActive(false);

            parts.ManaBar = CreateSliderWithLabel<ManaBarView>(parent, "ManaBar", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 188), new Vector2(620, 46), new Color(0.14f, 0.36f, 1f), "Mana 100 / 100");
            parts.HealthBar = CreateSliderWithLabel<HealthBarView>(parent, "EnemyHealthBar", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(330, 38), new Color(0.88f, 0.15f, 0.15f), "");
            parts.Joystick = CreateJoystickIndicator(parent);

            parts.Upgrades = new UpgradeButtonView[3];
            parts.Upgrades[0] = CreateUpgradeButton(parent, "AttackUpgrade", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-250, 98), attackIcon);
            parts.Upgrades[1] = CreateUpgradeButton(parent, "ManaUpgrade", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 98), manaIcon);
            parts.Upgrades[2] = CreateUpgradeButton(parent, "CritUpgrade", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(250, 98), critIcon);

            GameObject manual = CreateButton(parent, "ManualAttackButton", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(130, 34), new Vector2(210, 76), "Shot", skillIcon, out parts.ManualLabel);
            parts.ManualButton = manual.GetComponent<Button>();

            GameObject skill = CreateButton(parent, "ActiveSkillButton", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-130, 34), new Vector2(210, 76), "Skill", skillIcon, out parts.SkillLabel);
            parts.SkillButton = skill.GetComponent<Button>();

            GameObject auto = CreateButton(parent, "AutoToggleButton", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-92, -112), new Vector2(150, 48), "Auto On", null, out parts.AutoToggleLabel);
            parts.AutoToggleButton = auto.GetComponent<Button>();
            return parts;
        }

        private static JoystickIndicatorView CreateJoystickIndicator(Transform parent)
        {
            GameObject root = new GameObject("FloatingJoystick", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(132, 132);
            Image ring = root.AddComponent<Image>();
            ring.color = new Color(0.1f, 0.16f, 0.22f, 0.28f);

            GameObject knob = new GameObject("Knob", typeof(RectTransform));
            knob.transform.SetParent(root.transform, false);
            RectTransform knobRect = knob.GetComponent<RectTransform>();
            knobRect.anchorMin = new Vector2(0.5f, 0.5f);
            knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobRect.sizeDelta = new Vector2(56, 56);
            Image knobImage = knob.AddComponent<Image>();
            knobImage.color = new Color(0.25f, 0.85f, 1f, 0.55f);

            JoystickIndicatorView view = root.AddComponent<JoystickIndicatorView>();
            SetField(view, "root", rootRect);
            SetField(view, "knob", knobRect);
            root.SetActive(false);
            return view;
        }

        private static TMP_Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, float fontSize, string text)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static T CreateSliderWithLabel<T>(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color fillColor, string text) where T : Component
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image background = root.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.55f);
            Slider slider = root.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            GameObject fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(root.transform, false);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(4, 4);
            fillRect.offsetMax = new Vector2(-4, -4);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = fillColor;
            slider.fillRect = fillRect;
            slider.targetGraphic = fillImage;

            TMP_Text label = CreateText(root.transform, "Label", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 22, text);
            T view = root.AddComponent<T>();
            SetField(view, "slider", slider);
            SetField(view, "label", label);
            return view;
        }

        private static UpgradeButtonView CreateUpgradeButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Sprite icon)
        {
            TMP_Text label;
            GameObject go = CreateButton(parent, name, anchorMin, anchorMax, position, new Vector2(220, 76), name, icon, out label);
            UpgradeButtonView view = go.AddComponent<UpgradeButtonView>();
            SetField(view, "button", go.GetComponent<Button>());
            SetField(view, "label", label);
            SetField(view, "icon", go.transform.Find("Icon").GetComponent<Image>());
            return view;
        }

        private static GameObject CreateButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, string text, Sprite icon, out TMP_Text label)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = go.AddComponent<Image>();
            image.color = new Color(0.12f, 0.13f, 0.18f, 0.92f);
            go.AddComponent<Button>();

            GameObject iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(go.transform, false);
            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(32, 0);
            iconRect.sizeDelta = new Vector2(42, 42);
            Image iconImage = iconGo.AddComponent<Image>();
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;

            label = CreateText(go.transform, "Label", Vector2.zero, Vector2.one, new Vector2(28, 0), new Vector2(-36, 0), 20, text);
            return go;
        }

        private static Sprite CreateSprite(string name, Action<Texture2D> drawer, int width = 128, int height = 128, float pixelsPerUnit = 64f)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Clear(texture, new Color(0, 0, 0, 0));
            drawer(texture);
            texture.Apply();
            string path = ArtPath + name + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void Clear(Texture2D texture, Color color)
        {
            Color[] pixels = new Color[texture.width * texture.height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            texture.SetPixels(pixels);
        }

        private static void Rect(Texture2D t, int x0, int y0, int x1, int y1, Color c)
        {
            for (int y = Mathf.Max(0, y0); y < Mathf.Min(t.height, y1); y++)
                for (int x = Mathf.Max(0, x0); x < Mathf.Min(t.width, x1); x++)
                    t.SetPixel(x, y, c);
        }

        private static void Circle(Texture2D t, int cx, int cy, int r, Color c)
        {
            int rr = r * r;
            for (int y = cy - r; y <= cy + r; y++)
                for (int x = cx - r; x <= cx + r; x++)
                    if (x >= 0 && y >= 0 && x < t.width && y < t.height && (x - cx) * (x - cx) + (y - cy) * (y - cy) <= rr)
                        t.SetPixel(x, y, c);
        }

        private static void Triangle(Texture2D t, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            for (int y = 0; y < t.height; y++)
            for (int x = 0; x < t.width; x++)
            {
                Vector2 p = new Vector2(x, y);
                float w1 = (a.x * (c.y - a.y) + (p.y - a.y) * (c.x - a.x) - p.x * (c.y - a.y)) / ((b.y - a.y) * (c.x - a.x) - (b.x - a.x) * (c.y - a.y));
                float w2 = (p.y - a.y - w1 * (b.y - a.y)) / (c.y - a.y);
                if (w1 >= 0 && w2 >= 0 && w1 + w2 <= 1)
                    t.SetPixel(x, y, color);
            }
        }

        private static void DrawWizard(Texture2D t)
        {
            Triangle(t, new Vector2(42, 78), new Vector2(66, 120), new Vector2(88, 78), new Color(0.23f, 0.2f, 0.82f));
            Rect(t, 46, 42, 84, 82, new Color(0.18f, 0.32f, 0.95f));
            Circle(t, 64, 76, 14, new Color(1f, 0.78f, 0.58f));
            Rect(t, 35, 38, 93, 50, new Color(0.13f, 0.17f, 0.5f));
            Rect(t, 88, 44, 93, 90, new Color(0.62f, 0.36f, 0.1f));
            Circle(t, 92, 92, 5, new Color(0.1f, 0.9f, 1f));
            Circle(t, 58, 78, 2, Color.black);
            Circle(t, 70, 78, 2, Color.black);
        }

        private static void DrawOrb(Texture2D t, Color color, int radius, bool star)
        {
            Circle(t, t.width / 2, t.height / 2, radius + 8, new Color(color.r, color.g, color.b, 0.18f));
            Circle(t, t.width / 2, t.height / 2, radius, color);
            Circle(t, t.width / 2 - radius / 3, t.height / 2 + radius / 3, radius / 3, Color.white);
            if (star)
            {
                Rect(t, t.width / 2 - 4, 16, t.width / 2 + 4, t.height - 16, new Color(1f, 1f, 1f, 0.45f));
                Rect(t, 16, t.height / 2 - 4, t.width - 16, t.height / 2 + 4, new Color(1f, 1f, 1f, 0.45f));
            }
        }

        private static void DrawSkillOrb(Texture2D t)
        {
            DrawOrb(t, new Color(0.64f, 0.2f, 1f), 32, true);
            Triangle(t, new Vector2(62, 6), new Vector2(78, 54), new Vector2(55, 50), new Color(1f, 0.82f, 0.18f));
            Triangle(t, new Vector2(70, 122), new Vector2(50, 72), new Vector2(75, 76), new Color(0.15f, 1f, 0.95f));
        }

        private static void DrawSlime(Texture2D t)
        {
            Circle(t, 64, 54, 38, new Color(0.16f, 0.78f, 0.22f));
            Circle(t, 64, 66, 34, new Color(0.25f, 0.95f, 0.32f));
            Circle(t, 50, 70, 5, Color.black);
            Circle(t, 78, 70, 5, Color.black);
            Rect(t, 50, 47, 78, 51, new Color(0.05f, 0.33f, 0.08f));
            Circle(t, 45, 83, 9, new Color(0.7f, 1f, 0.68f, 0.65f));
        }

        private static void DrawBoss(Texture2D t)
        {
            Circle(t, 64, 58, 42, new Color(0.36f, 0.04f, 0.05f));
            Circle(t, 64, 68, 38, new Color(0.72f, 0.08f, 0.08f));
            Triangle(t, new Vector2(30, 95), new Vector2(12, 122), new Vector2(48, 100), new Color(0.08f, 0.02f, 0.02f));
            Triangle(t, new Vector2(98, 95), new Vector2(116, 122), new Vector2(80, 100), new Color(0.08f, 0.02f, 0.02f));
            Circle(t, 49, 75, 7, Color.yellow);
            Circle(t, 79, 75, 7, Color.yellow);
            Rect(t, 42, 43, 86, 49, Color.black);
            Rect(t, 48, 43, 54, 35, Color.white);
            Rect(t, 74, 43, 80, 35, Color.white);
        }

        private static void DrawBackground(Texture2D t)
        {
            Clear(t, new Color(0.16f, 0.34f, 0.2f));
            for (int y = 0; y < t.height; y += 64)
            for (int x = 0; x < t.width; x += 64)
            {
                Color tile = ((x + y) / 64) % 2 == 0 ? new Color(0.17f, 0.38f, 0.22f) : new Color(0.14f, 0.31f, 0.19f);
                Rect(t, x, y, x + 64, y + 64, tile);
            }
            Rect(t, t.width / 2 - 72, 0, t.width / 2 + 72, t.height, new Color(0.43f, 0.35f, 0.21f));
            Rect(t, 0, t.height / 2 - 64, t.width, t.height / 2 + 64, new Color(0.36f, 0.30f, 0.2f));
            Circle(t, t.width / 2, 210, 34, new Color(0.25f, 0.55f, 0.28f));
            Circle(t, t.width / 2, 775, 38, new Color(0.22f, 0.48f, 0.25f));
            for (int i = 0; i < 16; i++)
            {
                int x = 28 + (i * 97) % (t.width - 56);
                int y = 60 + (i * 151) % (t.height - 120);
                Circle(t, x, y, 8 + (i % 3) * 3, new Color(0.09f, 0.25f, 0.14f));
            }
            for (int i = 0; i < 18; i++)
            {
                int x = 20 + (i * 53) % (t.width - 40);
                int y = 20 + (i * 89) % (t.height - 40);
                Circle(t, x, y, 4, new Color(0.55f, 0.55f, 0.47f));
            }
        }

        private static void DrawGoldIcon(Texture2D t)
        {
            Circle(t, 48, 48, 32, new Color(1f, 0.72f, 0.12f));
            Circle(t, 48, 48, 22, new Color(1f, 0.9f, 0.24f));
            Rect(t, 43, 24, 53, 72, new Color(0.72f, 0.44f, 0.04f));
            Rect(t, 30, 43, 66, 53, new Color(0.72f, 0.44f, 0.04f));
        }

        private static void DrawAttackIcon(Texture2D t)
        {
            Rect(t, 43, 18, 53, 70, new Color(0.46f, 0.25f, 0.1f));
            Triangle(t, new Vector2(48, 82), new Vector2(20, 48), new Vector2(76, 48), new Color(0.1f, 0.85f, 1f));
            Circle(t, 48, 48, 12, Color.white);
        }

        private static void DrawCritIcon(Texture2D t)
        {
            Triangle(t, new Vector2(48, 12), new Vector2(58, 40), new Vector2(88, 40), new Color(1f, 0.18f, 0.1f));
            Triangle(t, new Vector2(88, 40), new Vector2(62, 58), new Vector2(72, 88), new Color(1f, 0.72f, 0.08f));
            Triangle(t, new Vector2(72, 88), new Vector2(48, 68), new Vector2(24, 88), new Color(1f, 0.18f, 0.1f));
            Triangle(t, new Vector2(24, 88), new Vector2(34, 58), new Vector2(8, 40), new Color(1f, 0.72f, 0.08f));
            Triangle(t, new Vector2(8, 40), new Vector2(38, 40), new Vector2(48, 12), new Color(1f, 0.18f, 0.1f));
        }

        private static void SetField(object target, string name, object value)
        {
            Type type = target.GetType();
            PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
            {
                property.SetValue(target, value, null);
                return;
            }

            while (type != null)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                field = field ?? type.GetField("<" + name + ">k__BackingField", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
                type = type.BaseType;
            }
            throw new MissingFieldException(target.GetType().Name, name);
        }

        private class HUDParts
        {
            public HUDController Hud;
            public TMP_Text Stage;
            public TMP_Text Gold;
            public TMP_Text Attack;
            public TMP_Text Feedback;
            public ManaBarView ManaBar;
            public HealthBarView HealthBar;
            public BossTimerView BossTimer;
            public DPSView Dps;
            public JoystickIndicatorView Joystick;
            public UpgradeButtonView[] Upgrades;
            public Button SkillButton;
            public TMP_Text SkillLabel;
            public Button ManualButton;
            public TMP_Text ManualLabel;
            public Button AutoToggleButton;
            public TMP_Text AutoToggleLabel;
        }
    }
}
