using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Editor-only layout builder. The saved scene contains ordinary, editable objects.
public static class SuikaSceneLayout
{
    const string ScenePath = "Assets/Scenes/SuikaGame.unity";
    const string ArtPath = "Assets/Art/Layout";
    static readonly string[] Fruits = { "cherry", "mandarin", "apple", "melon", "watermelon" };
    static readonly float[] BodyOffsets = { .105f, .065f, .055f, .065f, .04f };
    static readonly Color Ink = Hex("344F43");
    static readonly Color Muted = Hex("819084");
    static readonly Color Green = Hex("4D7959");
    static Sprite rounded, circle, face;
    static TMP_FontAsset font;
    static Material unlit;

    [MenuItem("Tools/Suika/Create Layout Scene")]
    public static void Build()
    {
        if (File.Exists(ScenePath))
            throw new System.InvalidOperationException("SuikaGame.unity already exists. Edit the saved scene directly.");
        if (SceneManager.GetActiveScene().isDirty)
            throw new System.InvalidOperationException("Save the current scene before creating the layout.");

        Directory.CreateDirectory(ArtPath);
        Directory.CreateDirectory("Assets/Prefabs/Fruits");
        AssetDatabase.Refresh();
        foreach (string name in Fruits) ImportSprite("Assets/Art/Fruits/" + name + ".png");
        ImportSprite("Assets/Art/Faces/face_idle.png");
        ImportSprite("Assets/Art/Faces/face_contact.png");
        rounded = ShapeSprite("Rounded", false);
        circle = ShapeSprite("Circle", true);
        face = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Faces/face_idle.png");
        font = TMP_Settings.defaultFontAsset;
        unlit = AssetDatabase.LoadAssetAtPath<Material>(ArtPath + "/SpriteUnlit.mat");
        if (unlit == null)
        {
            unlit = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            AssetDatabase.CreateAsset(unlit, ArtPath + "/SpriteUnlit.mat");
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        Undo.RegisterCreatedObjectUndo(cameraObject, "Create Suika camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0, 0, -10);
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Hex("F6F2E8");
        camera.nearClipPlane = .1f;
        camera.farClipPlane = 50;

        var stage = new GameObject("Playfield").transform;
        var basket = new GameObject("Basket").transform;
        basket.SetParent(stage);
        WorldShape("BasketShadow", basket, new Vector2(.06f, -2.69f), new Vector2(5.1f, .26f), Hex("E2DDCE"), -5);
        WorldShape("BasketInterior", basket, new Vector2(0, -.40f), new Vector2(4.8f, 4.7f), Hex("E8EEDC"), -4);
        WorldShape("BasketInnerLight", basket, new Vector2(0, -.30f), new Vector2(4.45f, 4.35f), Hex("F1F4E6"), -3);
        Wall("LeftWall", basket, new Vector2(-2.38f, -.42f), new Vector2(.18f, 4.65f));
        Wall("RightWall", basket, new Vector2(2.38f, -.42f), new Vector2(.18f, 4.65f));
        Wall("Floor", basket, new Vector2(0, -2.67f), new Vector2(4.93f, .22f));
        ApplyBasketSprite(basket);

        var limit = new GameObject("GameOverLine").transform;
        limit.SetParent(stage);
        limit.localPosition = new Vector3(0, 1.45f, 0);
        for (int i = 0; i < 17; i++)
            WorldShape("Dash_" + i, limit, new Vector2(-2.1f + i * .26f, 0), new Vector2(.13f, .025f), Hex("D3A397"), 2);

        var spawn = new GameObject("DropPoint").transform;
        spawn.SetParent(stage);
        spawn.position = new Vector3(0, 2.4f, 0);
        var guide = new GameObject("DropGuide").transform;
        guide.SetParent(stage);
        for (int i = 0; i < 10; i++)
            WorldShape("GuideDot_" + i, guide, new Vector2(0, 2.2f - i * .31f), new Vector2(.035f, .035f), Hex("BDCBB2"), -1, true);

        FruitPrefabSetup.Configure();

        var canvasObject = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 5;
        canvas.sortingOrder = 100;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var ui = canvasObject.transform;

        Text("Eyebrow", ui, "A LITTLE FRUIT, A BIG WATERMELON", 800, 842, 660, 22, 13, Muted);
        Text("Title", ui, "FRUIT BASKET", 800, 796, 650, 58, 43, Ink, FontStyles.Bold);
        Text("Subtitle", ui, "Same fruit. One sweet upgrade.", 800, 755, 600, 28, 16, Muted);

        var score = Card("ScorePanel", ui, 345, 555, 265, 280);
        Text("ScoreLabel", score, "SCORE", 0, 94, 220, 26, 15, Green, FontStyles.Bold);
        Text("ScoreValue", score, "0000", 0, 35, 235, 70, 55, Ink, FontStyles.Bold);
        Image("Divider", score, rounded, 0, -25, 205, 2, Hex("E9EBDD"));
        Text("BestLabel", score, "BEST", 0, -61, 200, 24, 13, Muted);
        Text("BestValue", score, "0000", 0, -98, 200, 38, 25, Ink, FontStyles.Bold);

        var next = Card("NextFruitPanel", ui, 1255, 570, 265, 250);
        Text("NextLabel", next, "NEXT FRUIT", 0, 85, 230, 26, 15, Green, FontStyles.Bold);
        Image("NextFruitBackdrop", next, circle, 0, -5, 145, 145, Hex("F5F0E5"));
        FruitIcon("NextFruitPreview", next, 1, 0, -5, 135);
        Text("NextFruitName", next, "MANDARIN", 0, -98, 220, 22, 12, Muted);

        var help = Card("HowToPanel", ui, 1255, 325, 265, 185);
        Text("HelpLabel", help, "HOW TO PLAY", 0, 54, 225, 24, 13, Green, FontStyles.Bold);
        Text("HelpBody", help, "Left / Right or A / D\nSpace to drop", 0, -8, 225, 90, 17, Ink);
        Text("LimitLabel", ui, "KEEP BELOW THE LINE", 800, 618, 340, 22, 11, Hex("B9897C"));

        var progression = Card("EvolutionPanel", ui, 800, 82, 820, 115);
        Text("EvolutionLabel", progression, "GROW YOUR FRUIT", 0, 35, 550, 20, 11, Muted, FontStyles.Bold);
        string[] labels = { "CHERRY", "MANDARIN", "APPLE", "MELON", "WATERMELON" };
        for (int i = 0; i < 5; i++)
        {
            float x = -300 + i * 150;
            FruitIcon("Stage_" + (i + 1), progression, i, x, -2, 59);
            Text("Name_" + (i + 1), progression, labels[i], x, -41, 140, 16, 9, Muted);
            if (i < 4) Text("Arrow_" + i, progression, ">", x + 75, -5, 20, 24, 17, Hex("B6C4AE"));
        }
        Text("Footer", ui, "UNITY BASIC STUDY  /  SUIKA GAME", 800, 12, 800, 16, 10, Hex("A1AA9B"));

        FruitDropManagerSetup.ConfigureScene();

        // Leave a clean 16:9 desktop layout with the shared fruit drop manager.
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = basket.gameObject;
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.in2DMode = true;
            SceneView.lastActiveSceneView.LookAt(Vector3.zero, Quaternion.identity, 5f);
        }
        Debug.Log("Created " + ScenePath + " with fruit spawning, merging, scoring and game-over controls.");
    }

    static void ImportSprite(string path)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 1254;
        importer.spritePivot = new Vector2(.5f, .5f);
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        importer.SetTextureSettings(settings);
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    static Sprite ShapeSprite(string name, bool isCircle)
    {
        string path = ArtPath + "/" + name + ".png";
        if (!File.Exists(path))
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                var p = new Vector2(x + .5f, y + .5f);
                Vector2 center = isCircle ? new Vector2(32, 32) : new Vector2(Mathf.Clamp(p.x, 16, 48), Mathf.Clamp(p.y, 16, 48));
                float alpha = Mathf.Clamp01((isCircle ? 32 : 16) - Vector2.Distance(p, center));
                texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
        }
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.spriteBorder = isCircle ? Vector4.zero : new Vector4(16, 16, 16, 16);
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void ApplyBasketSprite(Transform basket)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Basket/basket_frame.png");
        if (sprite == null) return;
        var obj = new GameObject("BasketSprite", typeof(SpriteRenderer));
        obj.transform.SetParent(basket, false);
        obj.transform.localPosition = new Vector3(.010088f, -.360313f, 0);
        obj.transform.localScale = new Vector3(1.008811f, .922301f, 1);
        var renderer = obj.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = unlit;
        renderer.sortingOrder = 10;
    }

    static void Wall(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var obj = new GameObject(name, typeof(BoxCollider2D));
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.GetComponent<BoxCollider2D>().size = size;
    }

    static GameObject WorldShape(string name, Transform parent, Vector2 position, Vector2 size, Color color, int order, bool isCircle = false)
    {
        var obj = new GameObject(name, typeof(SpriteRenderer));
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        var renderer = obj.GetComponent<SpriteRenderer>();
        renderer.sprite = isCircle ? circle : rounded;
        renderer.sharedMaterial = unlit;
        renderer.color = color;
        renderer.sortingOrder = order;
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = size;
        return obj;
    }

    static Transform Card(string name, Transform parent, float x, float y, float width, float height)
    {
        Image(name + "Shadow", parent, rounded, x + 2, y - 5, width, height, Hex("E9E5D9"));
        return Image(name, parent, rounded, x, y, width, height, Hex("FFFEF8")).transform;
    }

    static Image Image(string name, Transform parent, Sprite sprite, float x, float y, float width, float height, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = parent.GetComponent<Canvas>() != null ? new Vector2(x - 800, y - 450) : new Vector2(x, y);
        var image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.type = sprite == rounded ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static void Text(string name, Transform parent, string content, float x, float y, float width, float height, int size, Color color, FontStyles style = FontStyles.Normal)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = parent.GetComponent<Canvas>() != null ? new Vector2(x - 800, y - 450) : new Vector2(x, y);
        var text = obj.GetComponent<TMP_Text>();
        text.font = font;
        text.text = content;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;
        text.raycastTarget = false;
    }

    static void FruitIcon(string name, Transform parent, int index, float x, float y, float size)
    {
        var icon = Image(name, parent, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Fruits/" + Fruits[index] + ".png"), x, y, size, size, Color.white);
        Image("Face", icon.transform, face, 0, -size * BodyOffsets[index], size * .65f, size * .65f, Color.white);
    }

    static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var color); return color; }
}
