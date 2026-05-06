using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;
using WizardGrower.Player;

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

        public static string UpdateVisualAssets()
        {
            Directory.CreateDirectory(ArtPath);
            Directory.CreateDirectory(AnimPath);

            Sprite wizard0 = SaveSprite("Wizard", DrawWizardFrame(0), 96f);
            Sprite wizardIdle0 = SaveSprite("Wizard_Idle_0", DrawWizardFrame(0), 96f);
            Sprite wizardIdle1 = SaveSprite("Wizard_Idle_1", DrawWizardFrame(1), 96f);
            Sprite wizardRun0 = SaveSprite("Wizard_Run_0", DrawWizardFrame(2), 96f);
            Sprite wizardRun1 = SaveSprite("Wizard_Run_1", DrawWizardFrame(3), 96f);
            Sprite slime = SaveSprite("Slime", DrawSlime(), 96f);
            Sprite boss = SaveSprite("Boss", DrawBoss(), 96f);
            Sprite background = SaveSprite("TopDownBackground", DrawBackground(), 96f);

            UpdatePrefabSprite("Assets/Prefabs/PlayerWizard.prefab", wizard0, Vector3.one * 1.35f);
            UpdatePrefabSprite("Assets/Prefabs/NormalEnemy.prefab", slime, Vector3.one * 1.25f);
            UpdatePrefabSprite("Assets/Prefabs/BossEnemy.prefab", boss, Vector3.one * 2.0f);
            UpdateSceneSprite("PlayerWizard", wizard0);
            UpdateSceneSprite("TopDownRpgTrainingGround", background);

            AnimatorController controller = CreateWizardAnimator(wizardIdle0, wizardIdle1, wizardRun0, wizardRun1);
            ApplyWizardAnimator(controller);
            FixTmpFonts();
            AdjustDamageText();
            AdjustCameraBackground();

            EditorSceneManagerShim.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "Updated Wizard/Slime/Boss/Background visual assets, wizard animator, TMP fonts, and camera/background scale.";
        }

        private static Sprite SaveSprite(string name, Texture2D texture, float ppu)
        {
            string path = ArtPath + name + ".png";
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
            Texture2D t = NewTexture(192, 192, Color.clear);
            int bob = frame == 1 ? 3 : 0;
            int step = frame == 2 ? 7 : frame == 3 ? -7 : 0;

            Ellipse(t, 92, 44 + bob, 34, 14, new Color(0f, 0f, 0f, 0.22f));
            Ellipse(t, 96, 88 + bob, 38, 42, new Color(0.05f, 0.06f, 0.08f, 1f), true);
            Ellipse(t, 96, 88 + bob, 31, 35, new Color(0.10f, 0.13f, 0.18f, 1f), false);
            Rect(t, 69, 63 + bob, 54, 38, new Color(0.07f, 0.08f, 0.12f, 1f));
            Rect(t, 72, 65 + bob, 48, 32, new Color(0.13f, 0.16f, 0.23f, 1f));
            Rect(t, 80, 66 + bob, 8, 29, new Color(0.94f, 0.50f, 0.13f, 1f));
            Rect(t, 105, 66 + bob, 8, 29, new Color(0.94f, 0.50f, 0.13f, 1f));
            Ellipse(t, 96, 80 + bob, 13, 13, new Color(0.96f, 0.42f, 0.08f, 1f), true);
            Ellipse(t, 96, 80 + bob, 7, 7, new Color(0.16f, 0.44f, 0.95f, 1f), false);
            Poly(t, new[] { new Vector2(66, 100 + bob), new Vector2(34, 78 + bob), new Vector2(54, 54 + bob), new Vector2(76, 62 + bob) }, new Color(0.48f, 0.08f, 0.03f, 1f));
            Poly(t, new[] { new Vector2(126, 100 + bob), new Vector2(155, 78 + bob), new Vector2(138, 54 + bob), new Vector2(116, 62 + bob) }, new Color(0.56f, 0.11f, 0.03f, 1f));
            Line(t, 39, 76 + bob, 62, 57 + bob, new Color(0.93f, 0.46f, 0.10f, 1f), 2);
            Line(t, 153, 77 + bob, 132, 56 + bob, new Color(0.93f, 0.46f, 0.10f, 1f), 2);

            Ellipse(t, 96, 120 + bob, 35, 32, new Color(0.10f, 0.05f, 0.02f, 1f), true);
            Ellipse(t, 96, 118 + bob, 28, 27, new Color(0.98f, 0.72f, 0.47f, 1f), false);
            for (int i = 0; i < 8; i++)
            {
                int x = 66 + i * 8;
                Poly(t, new[] { new Vector2(x, 137 + bob), new Vector2(x + 14, 160 + bob + (i % 2) * 5), new Vector2(x + 22, 132 + bob) }, new Color(0.26f, 0.12f, 0.04f, 1f));
            }
            Ellipse(t, 83, 119 + bob, 4, 6, Color.black, false);
            Ellipse(t, 110, 119 + bob, 4, 6, Color.black, false);
            Ellipse(t, 84, 121 + bob, 1, 2, Color.white, false);
            Ellipse(t, 111, 121 + bob, 1, 2, Color.white, false);
            Line(t, 89, 106 + bob, 103, 103 + bob, new Color(0.38f, 0.12f, 0.07f, 1f), 2);

            Line(t, 72, 87 + bob, 52, 73 + bob, new Color(0.05f, 0.06f, 0.08f, 1f), 8);
            Line(t, 120, 91 + bob, 146, 117 + bob, new Color(0.05f, 0.06f, 0.08f, 1f), 8);
            Line(t, 139, 82 + bob, 154, 139 + bob, new Color(0.52f, 0.30f, 0.08f, 1f), 5);
            Ellipse(t, 157, 147 + bob, 14, 14, new Color(0.94f, 0.58f, 0.10f, 1f), true);
            Ellipse(t, 157, 147 + bob, 9, 9, new Color(0.16f, 0.65f, 1f, 1f), false);
            Ellipse(t, 157, 147 + bob, 5, 5, new Color(0.72f, 0.94f, 1f, 1f), false);
            Line(t, 82, 47 + bob, 84 + step, 26, new Color(0.08f, 0.06f, 0.05f, 1f), 9);
            Line(t, 111, 47 + bob, 111 - step, 26, new Color(0.08f, 0.06f, 0.05f, 1f), 9);
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
