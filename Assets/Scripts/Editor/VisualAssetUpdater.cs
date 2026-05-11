using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Player;
using WizardGrower.UI;

namespace WizardGrower.EditorTools
{
    public static class VisualAssetUpdater
    {
        private const string ArtPath = "Assets/Art/Generated/";
        private const string AnimPath = "Assets/Animations/";

        [MenuItem("Wizard Grower/Update Visual Assets")]
        public static void UpdateVisualAssetsMenu()
        {
            Debug.Log(UpdateVisualAssets());
        }

        [MenuItem("Wizard Grower/Generate Weapon Glyphs")]
        public static void GenerateWeaponGlyphsMenu()
        {
            Debug.Log(GenerateWeaponGlyphs());
        }

        [MenuItem("Wizard Grower/Generate Weapon Projectiles")]
        public static void GenerateWeaponProjectilesMenu()
        {
            Debug.Log(GenerateWeaponProjectiles());
        }

        [MenuItem("Wizard Grower/Generate Armor Icons")]
        public static void GenerateArmorIconsMenu()
        {
            Debug.Log(GenerateArmorIcons());
        }

        public static string UpdateVisualAssets()
        {
            Directory.CreateDirectory(ArtPath);
            Directory.CreateDirectory(AnimPath);

            Sprite wizard0 = SaveSprite("Wizard", DrawWizardFrame(0), 128f);
            Sprite wizardIdle0 = SaveSprite("Wizard_Idle_0", DrawWizardFrame(0), 128f);
            Sprite wizardIdle1 = SaveSprite("Wizard_Idle_1", DrawWizardFrame(1), 128f);
            Sprite wizardRun0 = SaveSprite("Wizard_Run_0", DrawWizardFrame(2), 128f);
            Sprite wizardRun1 = SaveSprite("Wizard_Run_1", DrawWizardFrame(3), 128f);
            Sprite slime = SaveSprite("Slime", DrawSlime(), 96f);
            Sprite boss = SaveSprite("Boss", DrawBoss(), 96f);
            Sprite background = SaveSprite("TopDownBackground", DrawBackground(), 96f);

            UpdatePrefabSprite("Assets/Prefabs/PlayerWizard.prefab", wizard0, Vector3.one * 1.05f);
            UpdatePrefabSprite("Assets/Prefabs/NormalEnemy.prefab", slime, Vector3.one * 1.25f);
            UpdatePrefabSprite("Assets/Prefabs/BossEnemy.prefab", boss, Vector3.one * 2.0f);
            UpdateSceneSprite("PlayerWizard", wizard0);
            UpdateSceneScale("PlayerWizard", Vector3.one * 1.05f);
            UpdateSceneSprite("TopDownRpgTrainingGround", background);

            AnimatorController controller = CreateWizardAnimator(wizardIdle0, wizardIdle1, wizardRun0, wizardRun1);
            ApplyWizardAnimator(controller);
            FixTmpFonts();
            FixHudCanvas();
            AdjustDamageText();
            AdjustCameraBackground();

            EditorSceneManagerShim.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "Updated Wizard/Slime/Boss/Background visual assets, wizard animator, TMP fonts, and camera/background scale.";
        }

        public static string GenerateWeaponGlyphs()
        {
            string folder = ArtPath + "WeaponGlyphs/";
            Directory.CreateDirectory(folder);
            for (int i = 0; i < V7WeaponIds.Length; i++)
                SaveSprite(folder, V7WeaponIds[i], DrawWeaponGlyph(V7WeaponColor(i), i), 96f);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "Generated 20 weapon glyph sprites.";
        }

        public static string GenerateWeaponProjectiles()
        {
            string folder = ArtPath + "WeaponProjectiles/";
            Directory.CreateDirectory(folder);
            for (int i = 0; i < V7WeaponIds.Length; i++)
                SaveSprite(folder, V7WeaponIds[i], DrawWeaponProjectile(V7WeaponColor(i), i), 96f);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "Generated 20 weapon projectile sprites.";
        }

        public static string GenerateArmorIcons()
        {
            string folder = ArtPath + "ArmorIcons/";
            Directory.CreateDirectory(folder);
            for (int i = 0; i < V13ArmorIds.Length; i++)
                SaveSprite(folder, V13ArmorIds[i], DrawArmorIcon(i / 4, i % 4), 96f);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "Generated 20 armor icon sprites.";
        }

        private static readonly string[] V13ArmorIds =
        {
            "helmet_common_beginner", "helmet_common_intermediate", "helmet_common_upper", "helmet_common_supreme",
            "chest_common_beginner", "chest_common_intermediate", "chest_common_upper", "chest_common_supreme",
            "legs_common_beginner", "legs_common_intermediate", "legs_common_upper", "legs_common_supreme",
            "gloves_common_beginner", "gloves_common_intermediate", "gloves_common_upper", "gloves_common_supreme",
            "boots_common_beginner", "boots_common_intermediate", "boots_common_upper", "boots_common_supreme"
        };

        private static Texture2D DrawArmorIcon(int slotIndex, int lowerIndex)
        {
            Color[] slotColors =
            {
                new Color(0.32f, 0.48f, 0.92f, 1f),
                new Color(0.48f, 0.32f, 0.88f, 1f),
                new Color(0.34f, 0.66f, 0.42f, 1f),
                new Color(0.88f, 0.56f, 0.24f, 1f),
                new Color(0.28f, 0.72f, 0.76f, 1f)
            };

            Texture2D t = NewTexture(128, 128, Color.clear);
            Color main = Color.Lerp(slotColors[Mathf.Clamp(slotIndex, 0, slotColors.Length - 1)], Color.white, lowerIndex * 0.08f);
            Color ink = new Color(0.04f, 0.035f, 0.05f, 1f);
            Ellipse(t, 64, 64, 46, 46, new Color(main.r, main.g, main.b, 0.28f));
            Ellipse(t, 64, 64, 40, 40, ink, true);

            switch (slotIndex)
            {
                case 0:
                    Poly(t, new[] { new Vector2(33, 65), new Vector2(48, 91), new Vector2(80, 91), new Vector2(95, 65), new Vector2(82, 50), new Vector2(46, 50) }, main);
                    break;
                case 1:
                    Poly(t, new[] { new Vector2(42, 94), new Vector2(32, 59), new Vector2(47, 34), new Vector2(64, 48), new Vector2(81, 34), new Vector2(96, 59), new Vector2(86, 94) }, main);
                    break;
                case 2:
                    Poly(t, new[] { new Vector2(45, 94), new Vector2(49, 42), new Vector2(61, 42), new Vector2(61, 96) }, main);
                    Poly(t, new[] { new Vector2(83, 94), new Vector2(79, 42), new Vector2(67, 42), new Vector2(67, 96) }, main);
                    break;
                case 3:
                    Ellipse(t, 64, 64, 28, 25, main);
                    Rect(t, 48, 57, 32, 18, Color.Lerp(main, Color.white, 0.12f));
                    break;
                default:
                    Poly(t, new[] { new Vector2(35, 48), new Vector2(91, 48), new Vector2(100, 71), new Vector2(76, 86), new Vector2(32, 76) }, main);
                    break;
            }

            Ellipse(t, 73, 78, 10, 4, new Color(1f, 1f, 1f, 0.35f));
            t.Apply();
            return t;
        }

        private static readonly string[] V7WeaponIds =
        {
            "common_beginner_staff", "common_intermediate_staff", "common_upper_staff", "common_supreme_staff",
            "normal_beginner_staff", "normal_intermediate_staff", "normal_upper_staff", "normal_supreme_staff",
            "advanced_beginner_staff", "advanced_intermediate_staff", "advanced_upper_staff", "advanced_supreme_staff",
            "epic_beginner_staff", "epic_intermediate_staff", "epic_upper_staff", "epic_supreme_staff",
            "unique_beginner_staff", "unique_intermediate_staff", "unique_upper_staff", "unique_supreme_staff"
        };

        private static Color V7WeaponColor(int ladderIndex)
        {
            Color[] upperColors =
            {
                new Color(0.82f, 0.82f, 0.78f, 1f),
                new Color(0.36f, 0.92f, 1f, 1f),
                new Color(0.38f, 0.58f, 1f, 1f),
                new Color(0.74f, 0.32f, 1f, 1f),
                new Color(1f, 0.58f, 0.16f, 1f)
            };
            Color baseColor = upperColors[Mathf.Clamp(ladderIndex / 4, 0, upperColors.Length - 1)];
            return Color.Lerp(baseColor, Color.white, (ladderIndex % 4) * 0.08f);
        }

        private static Sprite SaveSprite(string name, Texture2D texture, float ppu)
        {
            return SaveSprite(ArtPath, name, texture, ppu);
        }

        private static Sprite SaveSprite(string folder, string name, Texture2D texture, float ppu)
        {
            string path = folder + name + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Texture2D DrawWeaponGlyph(Color main, int variant)
        {
            Texture2D t = NewTexture(128, 128, Color.clear);
            Color ink = new Color(0.04f, 0.03f, 0.02f, 1f);
            Color glow = new Color(main.r, main.g, main.b, 0.35f);
            Ellipse(t, 64, 64, 39, 39, glow);
            Line(t, 64, 20, 64, 108, ink, 4);
            Line(t, 64, 20, 64, 108, main, 2);
            int points = 4 + variant % 3;
            for (int i = 0; i < points; i++)
            {
                float angle = i * Mathf.PI * 2f / points + variant * 0.18f;
                int x = 64 + Mathf.RoundToInt(Mathf.Cos(angle) * 34f);
                int y = 64 + Mathf.RoundToInt(Mathf.Sin(angle) * 34f);
                Line(t, 64, 64, x, y, ink, 5);
                Line(t, 64, 64, x, y, main, 3);
            }
            Ellipse(t, 64, 64, 18, 18, ink, true);
            Ellipse(t, 64, 64, 13, 13, main);
            Ellipse(t, 69, 71, 5, 4, Color.white);
            t.Apply();
            return t;
        }

        private static Texture2D DrawWeaponProjectile(Color main, int variant)
        {
            Texture2D t = NewTexture(128, 128, Color.clear);
            Color core = Color.Lerp(main, Color.white, 0.45f);
            Color trail = new Color(main.r, main.g, main.b, 0.45f);
            for (int i = 0; i < 5; i++)
                Ellipse(t, 30 + i * 8, 64 - i * 2, 30 - i * 4, 12 - i, trail);
            Ellipse(t, 76, 64, 25 + variant, 25 + variant / 2, new Color(main.r, main.g, main.b, 0.75f));
            Ellipse(t, 76, 64, 15, 15, core);
            if (variant >= 3)
            {
                Line(t, 77, 30, 87, 55, Color.white, 2);
                Line(t, 91, 73, 110, 85, main, 3);
            }
            if (variant == 4)
                Poly(t, new[] { new Vector2(79, 95), new Vector2(95, 70), new Vector2(78, 75), new Vector2(66, 36), new Vector2(54, 76), new Vector2(36, 71) }, new Color(1f, 0.64f, 0.10f, 0.9f));
            t.Apply();
            return t;
        }

        private static Texture2D NewTexture(int width, int height, Color clear)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = clear;
            texture.SetPixels(pixels);
            return texture;
        }

        private static Texture2D DrawWizardFrame(int frame)
        {
            Texture2D t = NewTexture(256, 256, Color.clear);
            int bob = frame == 1 ? 4 : 0;
            int run = frame == 2 ? 8 : frame == 3 ? -8 : 0;

            Color ink = new Color(0.035f, 0.028f, 0.024f, 1f);
            Color coat = new Color(0.035f, 0.045f, 0.070f, 1f);
            Color coatLight = new Color(0.105f, 0.145f, 0.210f, 1f);
            Color trim = new Color(0.985f, 0.590f, 0.115f, 1f);
            Color red = new Color(0.630f, 0.060f, 0.030f, 1f);
            Color redLight = new Color(0.920f, 0.210f, 0.075f, 1f);
            Color skin = new Color(1.000f, 0.720f, 0.500f, 1f);
            Color hair = new Color(0.235f, 0.105f, 0.040f, 1f);
            Color hairLight = new Color(0.510f, 0.245f, 0.095f, 1f);

            Ellipse(t, 128, 43 + bob, 47, 13, new Color(0f, 0f, 0f, 0.24f));
            Poly(t, new[] { new Vector2(85, 116 + bob), new Vector2(47, 92 + bob), new Vector2(50, 58 + bob), new Vector2(93, 77 + bob) }, ink);
            Poly(t, new[] { new Vector2(91, 112 + bob), new Vector2(52, 91 + bob), new Vector2(59, 67 + bob), new Vector2(101, 80 + bob) }, red);
            Line(t, 58, 74 + bob, 94, 85 + bob, trim, 3);
            Poly(t, new[] { new Vector2(172, 116 + bob), new Vector2(212, 91 + bob), new Vector2(206, 60 + bob), new Vector2(160, 78 + bob) }, ink);
            Poly(t, new[] { new Vector2(165, 112 + bob), new Vector2(204, 90 + bob), new Vector2(197, 68 + bob), new Vector2(156, 81 + bob) }, red);
            Line(t, 197, 76 + bob, 163, 86 + bob, trim, 3);

            Ellipse(t, 128, 91 + bob, 45, 46, ink, true);
            Ellipse(t, 128, 92 + bob, 38, 39, coat, false);
            Rect(t, 90, 60 + bob, 76, 51, ink);
            Rect(t, 96, 65 + bob, 64, 44, coatLight);
            Rect(t, 104, 64 + bob, 6, 46, trim);
            Rect(t, 146, 64 + bob, 6, 46, trim);
            Line(t, 101, 109 + bob, 155, 66 + bob, new Color(0.020f, 0.030f, 0.055f, 1f), 2);
            Line(t, 155, 109 + bob, 101, 66 + bob, new Color(0.020f, 0.030f, 0.055f, 1f), 2);
            Ellipse(t, 128, 83 + bob, 17, 17, trim, true);
            Ellipse(t, 128, 83 + bob, 10, 10, new Color(0.135f, 0.530f, 1f, 1f), false);
            Ellipse(t, 128, 83 + bob, 5, 5, new Color(0.760f, 0.950f, 1f, 1f), false);

            Line(t, 93, 88 + bob, 67, 74 + bob, ink, 10);
            Line(t, 166, 91 + bob, 196, 119 + bob, ink, 10);
            Line(t, 194, 74 + bob, 214, 155 + bob, new Color(0.425f, 0.250f, 0.070f, 1f), 6);
            Line(t, 195, 76 + bob, 215, 154 + bob, trim, 2);
            Ellipse(t, 217, 163 + bob, 18, 18, trim, true);
            Ellipse(t, 217, 163 + bob, 12, 12, new Color(0.080f, 0.610f, 1f, 1f), false);
            Ellipse(t, 217, 163 + bob, 7, 7, new Color(0.710f, 0.930f, 1f, 1f), false);
            Ellipse(t, 222, 170 + bob, 11, 4, new Color(0.820f, 0.970f, 1f, 0.65f), false);

            Ellipse(t, 128, 142 + bob, 44, 39, ink, true);
            Ellipse(t, 128, 139 + bob, 35, 32, skin, false);
            Ellipse(t, 91, 132 + bob, 8, 12, skin, false);
            Ellipse(t, 165, 132 + bob, 8, 12, skin, false);
            for (int i = 0; i < 10; i++)
            {
                int x = 84 + i * 9;
                int h = 20 + (i % 3) * 7;
                Poly(t, new[] { new Vector2(x, 161 + bob), new Vector2(x + 15, 188 + bob + h / 4), new Vector2(x + 28, 149 + bob) }, i % 2 == 0 ? hair : hairLight);
            }
            Poly(t, new[] { new Vector2(94, 154 + bob), new Vector2(107, 185 + bob), new Vector2(121, 152 + bob) }, hairLight);
            Poly(t, new[] { new Vector2(137, 154 + bob), new Vector2(154, 183 + bob), new Vector2(164, 150 + bob) }, hair);
            Ellipse(t, 112, 139 + bob, 5, 8, ink, false);
            Ellipse(t, 145, 139 + bob, 5, 8, ink, false);
            Ellipse(t, 114, 143 + bob, 2, 3, Color.white, false);
            Ellipse(t, 147, 143 + bob, 2, 3, Color.white, false);
            Line(t, 119, 124 + bob, 137, 122 + bob, new Color(0.390f, 0.085f, 0.055f, 1f), 2);
            Ellipse(t, 102, 130 + bob, 5, 3, new Color(1f, 0.430f, 0.360f, 0.35f), false);
            Ellipse(t, 154, 130 + bob, 5, 3, new Color(1f, 0.430f, 0.360f, 0.35f), false);

            Line(t, 111, 48 + bob, 103 + run, 22, ink, 11);
            Line(t, 145, 48 + bob, 150 - run, 22, ink, 11);
            Line(t, 111, 47 + bob, 103 + run, 24, coatLight, 5);
            Line(t, 145, 47 + bob, 150 - run, 24, coatLight, 5);
            t.Apply();
            return t;
        }

        private static Texture2D DrawSlime()
        {
            Texture2D t = NewTexture(192, 192, Color.clear);
            Ellipse(t, 96, 48, 46, 14, new Color(0f, 0f, 0f, 0.22f));
            Ellipse(t, 96, 86, 58, 44, new Color(0.02f, 0.18f, 0.08f, 1f), true);
            Ellipse(t, 96, 90, 50, 38, new Color(0.16f, 0.78f, 0.28f, 1f), false);
            Ellipse(t, 72, 94, 13, 18, Color.white, false);
            Ellipse(t, 121, 94, 13, 18, Color.white, false);
            Ellipse(t, 74, 91, 6, 9, Color.black, false);
            Ellipse(t, 119, 91, 6, 9, Color.black, false);
            Ellipse(t, 75, 96, 2, 3, Color.white, false);
            Ellipse(t, 120, 96, 2, 3, Color.white, false);
            Line(t, 80, 70, 113, 69, new Color(0.02f, 0.16f, 0.08f, 1f), 3);
            Ellipse(t, 73, 119, 15, 7, new Color(0.70f, 1f, 0.55f, 0.75f), false);
            t.Apply();
            return t;
        }

        private static Texture2D DrawBoss()
        {
            Texture2D t = NewTexture(256, 256, Color.clear);
            Ellipse(t, 128, 58, 74, 20, new Color(0f, 0f, 0f, 0.25f));
            Poly(t, new[] { new Vector2(67, 156), new Vector2(42, 216), new Vector2(100, 172) }, new Color(0.12f, 0.05f, 0.04f, 1f));
            Poly(t, new[] { new Vector2(189, 156), new Vector2(214, 216), new Vector2(156, 172) }, new Color(0.12f, 0.05f, 0.04f, 1f));
            Ellipse(t, 128, 112, 76, 58, new Color(0.13f, 0.02f, 0.03f, 1f), true);
            Ellipse(t, 128, 118, 65, 49, new Color(0.60f, 0.05f, 0.06f, 1f), false);
            Ellipse(t, 97, 125, 17, 14, new Color(1f, 0.78f, 0.15f, 1f), false);
            Ellipse(t, 159, 125, 17, 14, new Color(1f, 0.78f, 0.15f, 1f), false);
            Ellipse(t, 99, 123, 7, 8, Color.black, false);
            Ellipse(t, 157, 123, 7, 8, Color.black, false);
            Line(t, 94, 90, 160, 87, Color.black, 5);
            Line(t, 105, 80, 112, 72, Color.white, 4);
            Line(t, 139, 80, 146, 72, Color.white, 4);
            for (int i = 0; i < 5; i++)
                Poly(t, new[] { new Vector2(82 + i * 22, 174), new Vector2(93 + i * 22, 202), new Vector2(106 + i * 22, 174) }, new Color(0.96f, 0.52f, 0.08f, 1f));
            t.Apply();
            return t;
        }

        private static Texture2D DrawBackground()
        {
            Texture2D t = NewTexture(2048, 1536, new Color(0.21f, 0.46f, 0.23f, 1f));
            for (int y = 0; y < t.height; y += 32)
            {
                for (int x = 0; x < t.width; x += 32)
                {
                    float n = Mathf.PerlinNoise(x * 0.015f, y * 0.015f);
                    Rect(t, x, y, 34, 34, Color.Lerp(new Color(0.16f, 0.38f, 0.20f), new Color(0.32f, 0.56f, 0.26f), n));
                }
            }
            for (int i = 0; i < 80; i++)
            {
                int x = Random.Range(0, t.width);
                int y = Random.Range(0, t.height);
                Ellipse(t, x, y, Random.Range(8, 18), Random.Range(3, 7), new Color(0.38f, 0.66f, 0.30f, 0.7f));
            }
            for (int i = 0; i < 36; i++)
            {
                int x = Random.Range(0, t.width);
                int y = Random.Range(0, t.height);
                Ellipse(t, x, y, Random.Range(12, 26), Random.Range(8, 18), new Color(0.30f, 0.27f, 0.24f, 0.85f));
                Ellipse(t, x - 3, y + 4, 5, 3, new Color(0.56f, 0.52f, 0.44f, 0.75f));
            }
            for (int i = 0; i < 120; i++)
            {
                int x = Random.Range(0, t.width);
                int y = Random.Range(0, t.height);
                Ellipse(t, x, y, 3, 3, new Color(0.94f, 0.80f, 0.24f, 0.9f));
            }
            t.Apply();
            return t;
        }

        private static AnimatorController CreateWizardAnimator(Sprite idle0, Sprite idle1, Sprite run0, Sprite run1)
        {
            string controllerPath = AnimPath + "Wizard.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.layers[0].stateMachine.states = new ChildAnimatorState[0];
            AnimationClip idle = CreateSpriteClip("Wizard_Idle.anim", 0.55f, idle0, idle1, idle0);
            AnimationClip run = CreateSpriteClip("Wizard_Run.anim", 0.18f, run0, run1, run0);
            if (!HasParameter(controller, "Moving"))
                controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);
            AnimatorState idleState = controller.AddMotion(idle);
            idleState.name = "Idle";
            AnimatorState runState = controller.AddMotion(run);
            runState.name = "Run";
            controller.layers[0].stateMachine.defaultState = idleState;
            AnimatorStateTransition toRun = idleState.AddTransition(runState);
            toRun.hasExitTime = false;
            toRun.duration = 0.05f;
            toRun.AddCondition(AnimatorConditionMode.If, 0f, "Moving");
            AnimatorStateTransition toIdle = runState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.05f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Moving");
            return controller;
        }

        private static bool HasParameter(AnimatorController controller, string parameterName)
        {
            foreach (AnimatorControllerParameter parameter in controller.parameters)
            {
                if (parameter.name == parameterName)
                    return true;
            }
            return false;
        }

        private static AnimationClip CreateSpriteClip(string fileName, float frameTime, params Sprite[] sprites)
        {
            AnimationClip clip = new AnimationClip();
            clip.frameRate = 12f;
            EditorCurveBinding binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };
            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                keys[i] = new ObjectReferenceKeyframe { time = i * frameTime, value = sprites[i] };
            }
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            string path = AnimPath + fileName;
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static void ApplyWizardAnimator(AnimatorController controller)
        {
            ApplyAnimatorToObject("Assets/Prefabs/PlayerWizard.prefab", controller);
            GameObject sceneWizard = GameObject.Find("PlayerWizard");
            if (sceneWizard != null)
            {
                Animator animator = sceneWizard.GetComponent<Animator>();
                if (animator == null)
                    animator = sceneWizard.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                if (sceneWizard.GetComponent<WizardAnimationController>() == null)
                    sceneWizard.AddComponent<WizardAnimationController>();
                EditorUtility.SetDirty(sceneWizard);
            }
        }

        private static void ApplyAnimatorToObject(string prefabPath, AnimatorController controller)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                return;
            Animator animator = prefab.GetComponent<Animator>();
            if (animator == null)
                animator = prefab.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            if (prefab.GetComponent<WizardAnimationController>() == null)
                prefab.AddComponent<WizardAnimationController>();
            EditorUtility.SetDirty(prefab);
        }

        private static void UpdatePrefabSprite(string prefabPath, Sprite sprite, Vector3 scale)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                return;
            SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
            if (renderer != null)
                renderer.sprite = sprite;
            prefab.transform.localScale = scale;
            EditorUtility.SetDirty(prefab);
        }

        private static void UpdateSceneSprite(string objectName, Sprite sprite)
        {
            GameObject go = GameObject.Find(objectName);
            if (go == null)
                return;
            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            if (renderer != null)
                renderer.sprite = sprite;
            EditorUtility.SetDirty(go);
        }

        private static void UpdateSceneScale(string objectName, Vector3 scale)
        {
            GameObject go = GameObject.Find(objectName);
            if (go == null)
                return;
            go.transform.localScale = scale;
            EditorUtility.SetDirty(go);
        }

        private static void FixTmpFonts()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/AppleGothic_TMP.asset");
            if (font == null)
                return;
            foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
            {
                text.font = font;
                EditorUtility.SetDirty(text);
            }
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/UpgradeButton.prefab");
            if (prefab != null)
            {
                foreach (TMP_Text text in prefab.GetComponentsInChildren<TMP_Text>(true))
                    text.font = font;
                EditorUtility.SetDirty(prefab);
            }
        }

        private static void FixHudCanvas()
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
                return;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            if (canvas.GetComponent<HUDTextRenderFixer>() == null)
                canvas.gameObject.AddComponent<HUDTextRenderFixer>();
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 1f;
                EditorUtility.SetDirty(scaler);
            }
            EditorUtility.SetDirty(canvas);
        }

        private static void AdjustDamageText()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/DamageText.prefab");
            if (prefab == null)
                return;
            TextMeshProUGUI text = prefab.GetComponent<TextMeshProUGUI>();
            if (text != null)
                text.fontSize = 38f;
            EditorUtility.SetDirty(prefab);
        }

        private static void AdjustCameraBackground()
        {
            Camera camera = Camera.main;
            GameObject background = GameObject.Find("TopDownRpgTrainingGround");
            if (camera == null || background == null)
                return;
            MobileCameraFitterAccessor.Configure(camera.GetComponent<WizardGrower.UI.MobileCameraFitter>(), background.GetComponent<SpriteRenderer>());
        }

        private static void Rect(Texture2D t, int x, int y, int w, int h, Color c)
        {
            for (int py = y; py < y + h; py++)
                for (int px = x; px < x + w; px++)
                    Set(t, px, py, c);
        }

        private static void Ellipse(Texture2D t, int cx, int cy, int rx, int ry, Color c, bool outline = false)
        {
            int rxo = Mathf.Max(1, rx - 5);
            int ryo = Mathf.Max(1, ry - 5);
            for (int y = cy - ry; y <= cy + ry; y++)
                for (int x = cx - rx; x <= cx + rx; x++)
                {
                    float outer = ((x - cx) * (x - cx)) / (float)(rx * rx) + ((y - cy) * (y - cy)) / (float)(ry * ry);
                    float inner = ((x - cx) * (x - cx)) / (float)(rxo * rxo) + ((y - cy) * (y - cy)) / (float)(ryo * ryo);
                    if (outer <= 1f && (!outline || inner >= 1f))
                        Set(t, x, y, c);
                }
        }

        private static void Line(Texture2D t, int x0, int y0, int x1, int y1, Color c, int width)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            while (true)
            {
                Ellipse(t, x0, y0, width, width, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        private static void Poly(Texture2D t, Vector2[] points, Color c)
        {
            Rect bounds = new Rect(points[0], Vector2.zero);
            foreach (Vector2 p in points)
            {
                bounds.xMin = Mathf.Min(bounds.xMin, p.x);
                bounds.xMax = Mathf.Max(bounds.xMax, p.x);
                bounds.yMin = Mathf.Min(bounds.yMin, p.y);
                bounds.yMax = Mathf.Max(bounds.yMax, p.y);
            }
            for (int y = Mathf.FloorToInt(bounds.yMin); y <= Mathf.CeilToInt(bounds.yMax); y++)
                for (int x = Mathf.FloorToInt(bounds.xMin); x <= Mathf.CeilToInt(bounds.xMax); x++)
                    if (Inside(new Vector2(x, y), points))
                        Set(t, x, y, c);
        }

        private static bool Inside(Vector2 p, Vector2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                    (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                    inside = !inside;
            }
            return inside;
        }

        private static void Set(Texture2D t, int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= t.width || y >= t.height)
                return;
            t.SetPixel(x, y, c);
        }
    }

    internal static class MobileCameraFitterAccessor
    {
        public static void Configure(WizardGrower.UI.MobileCameraFitter fitter, SpriteRenderer background)
        {
            if (fitter == null)
                return;
            SerializedObject so = new SerializedObject(fitter);
            so.FindProperty("minVisibleWidth").floatValue = 7.2f;
            so.FindProperty("minVisibleHeight").floatValue = 12.4f;
            so.FindProperty("maxOrthographicSize").floatValue = 8.0f;
            so.FindProperty("fittedBackground").objectReferenceValue = background;
            so.FindProperty("mapSize").vector2Value = new Vector2(28f, 18f);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(fitter);
        }
    }

    internal static class EditorSceneManagerShim
    {
        public static void SaveOpenScenes()
        {
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        }
    }
}
