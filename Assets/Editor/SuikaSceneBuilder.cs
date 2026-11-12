using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 메뉴 Suika > Build Play Scene 한 번으로 플레이 씬 전체를 만듭니다.
// 바구니·벽·가이드·제한선·UI·매니저·효과음·이펙트를 배치하고 Inspector 참조를 모두 연결합니다.
// 같은 메뉴를 다시 실행하면 SampleScene을 기반으로 씬을 처음부터 다시 만듭니다.
public static class SuikaSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/SuikaGame.unity";
    private const string BaseScenePath = "Assets/Scenes/SampleScene.unity";
    private const string GeneratedFolder = "Assets/Art/Generated";
    private const string Kenney = "Assets/ThirdParty/Kenney";
    private const string UnlitMaterialPath = "Assets/Art/Layout/SpriteUnlit.mat";

    // 바구니 그림(1254px, Pixels Per Unit 200)에서 측정한 안쪽 경계입니다. 바구니 중심 기준 단위입니다.
    private const float BasketInnerLeft = -2.28f;
    private const float BasketInnerRight = 2.255f;
    private const float BasketInnerFloor = -2.385f;

    // 화면 배치 값입니다.
    private const float BasketY = -1.15f;
    private const float DropY = 2.75f;
    private const float GameOverLineY = 1.35f;
    private const float CameraSize = 4.6f;

    private static readonly Color Ink = new Color(.2f, .2f, .27f);
    private static readonly Color Background = new Color(.965f, .906f, .784f);

    [MenuItem("Suika/Build Play Scene")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("플레이 중에는 씬을 만들 수 없습니다. 플레이를 멈춘 뒤 다시 실행하세요.");
            return;
        }

        ConfigureImports();
        var font = CreateFontAsset();
        AddImpactSoundToFruitBase();

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (File.Exists(ScenePath)) AssetDatabase.DeleteAsset(ScenePath);
        if (!AssetDatabase.CopyAsset(BaseScenePath, ScenePath))
        {
            Debug.LogError("SampleScene을 복사하지 못했습니다: " + BaseScenePath);
            return;
        }
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);
        var cam = Object.FindAnyObjectByType<Camera>();
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = CameraSize;
            cam.backgroundColor = Background;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(0, 0, -10);
        }

        // 배경 이미지 자리: 스프라이트는 비워 두고 나중에 넣을 수 있게 합니다.
        var background = new GameObject("Background (이미지 필요)");
        background.transform.position = new Vector3(0, 0, 5);
        var backgroundRenderer = background.AddComponent<SpriteRenderer>();
        backgroundRenderer.sortingOrder = -20;
        if (unlit != null) backgroundRenderer.sharedMaterial = unlit;
        var fitter = background.AddComponent<BackgroundFitter>();
        SetRef(fitter, "targetCamera", cam);

        // 바구니 그림과 실제 충돌 벽입니다. 벽은 그림 안쪽 경계에 맞추고 위로 길게 늘려 과일이 넘어가지 않게 합니다.
        var basket = Sprite("Basket", Load<Sprite>("Assets/Art/Basket/basket_frame.png"), new Vector3(0, BasketY, 0), 3, unlit);
        var walls = new GameObject("BasketColliders").transform;
        float floorY = BasketY + BasketInnerFloor;
        const float thickness = 1f;
        const float wallTop = 6f;
        var leftWall = Box("LeftWall", walls, new Vector2(BasketInnerLeft - thickness * .5f, (floorY + wallTop) * .5f), new Vector2(thickness, wallTop - floorY + thickness));
        var rightWall = Box("RightWall", walls, new Vector2(BasketInnerRight + thickness * .5f, (floorY + wallTop) * .5f), new Vector2(thickness, wallTop - floorY + thickness));
        Box("Floor", walls, new Vector2((BasketInnerLeft + BasketInnerRight) * .5f, floorY - thickness * .5f), new Vector2(BasketInnerRight - BasketInnerLeft + thickness * 2, thickness));

        var dropPoint = new GameObject("DropPoint").transform;
        dropPoint.position = new Vector3(0, DropY, 0);

        // 낙하 위치 안내선: 낙하 높이부터 바닥까지 세로로 늘립니다. Collider는 넣지 않습니다.
        var guideSprite = Load<Sprite>("Assets/Art/Guides/drop_guide.png");
        var guide = Sprite("DropGuide", guideSprite, new Vector3(0, (DropY + floorY) * .5f, 0), -1, unlit);
        if (guideSprite != null)
            guide.transform.localScale = new Vector3(1, (DropY - floorY) / guideSprite.bounds.size.y, 1);
        guide.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, .85f);

        // 게임오버 제한선: Transform y가 실제 판정 높이입니다. Collider는 넣지 않습니다.
        var lineSprite = Load<Sprite>("Assets/Art/Guides/game_over_line.png");
        var line = Sprite("GameOverLine", lineSprite, new Vector3((BasketInnerLeft + BasketInnerRight) * .5f, GameOverLineY, 0), 2, unlit);
        if (lineSprite != null)
            line.transform.localScale = new Vector3((BasketInnerRight - BasketInnerLeft) / lineSprite.bounds.size.x, 1, 1);

        var spawned = new GameObject("SpawnedFruits").transform;

        // 대체과제 A: 낙하 위치 미리보기(충돌·합체·판정 없음).
        var preview = new GameObject("DropPreview");
        var previewRenderer = preview.AddComponent<SpriteRenderer>();
        previewRenderer.sortingOrder = 4;
        if (unlit != null) previewRenderer.sharedMaterial = unlit;
        var dropPreview = preview.AddComponent<DropPreview>();

        // 매니저
        var manager = new GameObject("GameManager");
        var dropManager = manager.AddComponent<FruitDropManager>();
        var gameOver = manager.AddComponent<GameOverManager>();
        var score = manager.AddComponent<ScoreManager>();
        var leaderboard = manager.AddComponent<LeaderboardManager>();
        var feedback = manager.AddComponent<GameFeedback>();
        var voices = new AudioSource[4];
        for (int i = 0; i < voices.Length; i++)
        {
            var voice = new GameObject("Voice" + i).AddComponent<AudioSource>();
            voice.transform.SetParent(manager.transform, false);
            voice.playOnAwake = false;
            voice.spatialBlend = 0;
            voices[i] = voice;
        }

        // 합체 이펙트(Kenney Particle Pack 텍스처)
        var fx = new GameObject("FX").transform;
        var sparkle = Particles("MergeSparkle", fx, "star_06", ConfigureSparkle);
        var ring = Particles("MergeRing", fx, "light_02", ConfigureRing);
        var burst = Particles("WatermelonBurst", fx, "magic_02", ConfigureBurst);

        // UI
        var ui = BuildUI(font, out var refs);

        // 참조 연결
        SetArray(dropManager, "fruitPrefabs", new Object[]
        {
            Load<GameObject>("Assets/Prefabs/Fruits/cherry.prefab"),
            Load<GameObject>("Assets/Prefabs/Fruits/mandarin.prefab"),
            Load<GameObject>("Assets/Prefabs/Fruits/apple.prefab"),
        });
        SetRef(dropManager, "dropPoint", dropPoint);
        SetRef(dropManager, "spawnedFruitParent", spawned);
        SetRef(dropManager, "leftWall", leftWall);
        SetRef(dropManager, "rightWall", rightWall);
        SetRef(dropManager, "dropGuide", guide.transform);
        SetRef(dropManager, "nextFruitImage", refs.nextImage);
        SetRef(dropManager, "nextFruitName", refs.nextName);

        SetRef(gameOver, "dropManager", dropManager);
        SetRef(gameOver, "spawnedFruitParent", spawned);
        SetRef(gameOver, "heightLimit", line.transform);
        SetRef(gameOver, "dropGuide", guide.transform);
        SetRef(gameOver, "gameOverPanel", refs.gameOverPanel);

        SetRef(score, "scoreText", refs.score);
        SetRef(score, "bestScoreText", refs.best);
        SetRef(score, "finalScoreText", refs.finalScore);

        SetRef(leaderboard, "gameOverManager", gameOver);
        SetRef(leaderboard, "resultListText", refs.resultList);
        SetRef(leaderboard, "hudListText", refs.hudList);

        SetRef(dropPreview, "dropManager", dropManager);
        SetRef(dropPreview, "gameOverManager", gameOver);

        SetRef(feedback, "dropManager", dropManager);
        SetRef(feedback, "gameOverManager", gameOver);
        SetArray(feedback, "voices", voices);
        SetRef(feedback, "dropClip", Clip("drop_002.wav"));
        SetArray(feedback, "landClips", new Object[] { Clip("impact_soft_medium_000.ogg"), Clip("impact_soft_medium_001.ogg"), Clip("impact_soft_medium_002.ogg") });
        SetRef(feedback, "mergeClip", Clip("pluck_002.wav"));
        SetRef(feedback, "melonClip", Clip("confirmation_001.wav"));
        SetRef(feedback, "watermelonClip", Clip("confirmation_004.wav"));
        SetRef(feedback, "gameOverClip", Clip("error_006.wav"));
        SetRef(feedback, "newBestClip", Clip("confirmation_002.wav"));
        SetRef(feedback, "restartClip", Clip("maximize_006.wav"));
        SetRef(feedback, "mergeSparkle", sparkle);
        SetRef(feedback, "mergeRing", ring);
        SetRef(feedback, "watermelonBurst", burst);
        SetRef(feedback, "scoreLabel", refs.score.rectTransform);
        SetRef(feedback, "gameOverCard", refs.card);
        SetRef(feedback, "newBestBadge", refs.newBest);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = basket;
        Debug.Log("[Suika] 플레이 씬을 만들었습니다: " + ScenePath);
    }

    // ---------- 임포트 설정 ----------

    private static void ConfigureImports()
    {
        // UI 패널은 9-slice로 늘려 쓰므로 테두리 값을 지정합니다. (왼쪽, 아래, 오른쪽, 위)
        SpriteImport(Kenney + "/UI/panel_grey.png", new Vector4(14, 14, 14, 14));
        SpriteImport(Kenney + "/UI/panel_yellow.png", new Vector4(14, 18, 14, 14));
        SpriteImport(Kenney + "/UI/bar_green.png", new Vector4(14, 18, 14, 14));
        SpriteImport(Kenney + "/UI/bar_blue.png", new Vector4(14, 18, 14, 14));
        SpriteImport(Kenney + "/UI/star.png", Vector4.zero);
        SpriteImport(Kenney + "/UI/icon_repeat.png", Vector4.zero);

        foreach (var name in new[] { "star_06", "light_02", "circle_05", "magic_02" })
        {
            var path = Kenney + "/Particles/" + name + ".png";
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }
        }
    }

    private static void SpriteImport(string path, Vector4 border)
    {
        if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
        {
            Debug.LogWarning("임포트 설정을 찾지 못했습니다: " + path);
            return;
        }
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.spriteBorder = border;
        importer.SaveAndReimport();
    }

    private static TMP_FontAsset CreateFontAsset()
    {
        const string fontPath = Kenney + "/Fonts/KenneyFuture.ttf";
        const string assetPath = Kenney + "/Fonts/KenneyFuture SDF.asset";

        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null) return existing;

        var source = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        var fallback = TMP_Settings.defaultFontAsset;
        if (source == null) return fallback;

        try
        {
            var asset = TMP_FontAsset.CreateFontAsset(source);
            if (asset == null) return fallback;
            asset.name = "KenneyFuture SDF";
            AssetDatabase.CreateAsset(asset, assetPath);
            if (asset.material != null)
            {
                asset.material.name = asset.name + " Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }
            if (asset.atlasTextures != null)
            {
                for (int i = 0; i < asset.atlasTextures.Length; i++)
                {
                    var texture = asset.atlasTextures[i];
                    if (texture == null) continue;
                    texture.name = asset.name + " Atlas " + i;
                    AssetDatabase.AddObjectToAsset(texture, asset);
                }
            }
            // Kenney Future에 없는 글자는 TMP 기본 폰트로 대신 그립니다.
            if (fallback != null)
            {
                if (asset.fallbackFontAssetTable == null) asset.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
                asset.fallbackFontAssetTable.Add(fallback);
            }
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning("Kenney Future 폰트 에셋을 만들지 못해 기본 폰트를 사용합니다: " + exception.Message);
            return fallback;
        }
    }

    private static void AddImpactSoundToFruitBase()
    {
        const string path = "Assets/Prefabs/FruitBase.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            if (root.GetComponent<FruitImpactSound>() == null)
            {
                root.AddComponent<FruitImpactSound>();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ---------- 월드 오브젝트 ----------

    private static GameObject Sprite(string name, Sprite sprite, Vector3 position, int order, Material material)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        if (material != null) renderer.sharedMaterial = material;
        return go;
    }

    private static BoxCollider2D Box(string name, Transform parent, Vector2 center, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = size;
        return box;
    }

    private static ParticleSystem Particles(string name, Transform parent, string texture, System.Action<ParticleSystem> configure)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = system.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;
        main.gravityModifier = 0;

        var emission = system.emission;
        emission.rateOverTime = 0;

        configure(system);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = ParticleMaterial(texture);
        renderer.sortingOrder = 8;
        return system;
    }

    private static void ConfigureSparkle(ParticleSystem system)
    {
        var main = system.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(.35f, .6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        main.startSize = .3f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(1f, .92f, .55f));

        var shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = .3f;
        shape.radiusThickness = 1f;

        FadeAndShrink(system, 1f, 0f);
    }

    private static void ConfigureRing(ParticleSystem system)
    {
        var main = system.main;
        main.startLifetime = .35f;
        main.startSpeed = 0;
        main.startSize = 1f;
        main.startColor = new Color(1f, .97f, .85f, .9f);

        var shape = system.shape;
        shape.enabled = false;

        FadeAndShrink(system, .45f, 1.1f);
    }

    private static void ConfigureBurst(ParticleSystem system)
    {
        var main = system.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(.6f, .95f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSize = .6f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(.45f, .9f, .4f), new Color(1f, .45f, .45f));
        main.gravityModifier = .35f;

        var shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = .5f;
        shape.radiusThickness = 1f;

        FadeAndShrink(system, 1f, .2f);
    }

    // 수명 동안 투명해지고 크기가 start에서 end 배율로 바뀌게 합니다.
    private static void FadeAndShrink(ParticleSystem system, float startScale, float endScale)
    {
        var color = system.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
            new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, .45f), new GradientAlphaKey(0, 1) });
        color.color = new ParticleSystem.MinMaxGradient(gradient);

        var size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, startScale, 1, endScale));
    }

    private static Material ParticleMaterial(string texture)
    {
        if (!AssetDatabase.IsValidFolder(GeneratedFolder))
            AssetDatabase.CreateFolder("Assets/Art", "Generated");

        string path = GeneratedFolder + "/FX_" + texture + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null)
            {
                var unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);
                shader = unlit != null ? unlit.shader : Shader.Find("Sprites/Default");
            }
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Kenney + "/Particles/" + texture + ".png");
        EditorUtility.SetDirty(material);
        return material;
    }

    // ---------- UI ----------

    private struct UiRefs
    {
        public TMP_Text score, best, finalScore, nextName, resultList, hudList;
        public Image nextImage;
        public GameObject gameOverPanel, newBest;
        public RectTransform card;
    }

    private static GameObject BuildUI(TMP_FontAsset font, out UiRefs refs)
    {
        refs = new UiRefs();
        var panelGrey = Load<Sprite>(Kenney + "/UI/panel_grey.png");
        var panelYellow = Load<Sprite>(Kenney + "/UI/panel_yellow.png");
        var star = Load<Sprite>(Kenney + "/UI/star.png");
        var repeat = Load<Sprite>(Kenney + "/UI/icon_repeat.png");

        var canvasGo = new GameObject("Canvas", typeof(RectTransform));
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;
        var root = canvasGo.transform;

        // 왼쪽: 현재 점수, 최고 점수, 조작법
        var scorePanel = Panel("ScorePanel", root, panelGrey, new Vector2(0, 1), new Vector2(110, -90), new Vector2(400, 230));
        Label(scorePanel, "Label", "SCORE", font, 34, new Vector2(0, -24), new Vector2(360, 50), TextAlignmentOptions.Center, top: true);
        refs.score = Label(scorePanel, "ScoreText", "0000", font, 100, new Vector2(0, -22), new Vector2(360, 130), TextAlignmentOptions.Center);

        var bestPanel = Panel("BestPanel", root, panelGrey, new Vector2(0, 1), new Vector2(110, -345), new Vector2(400, 170));
        Label(bestPanel, "Label", "BEST", font, 30, new Vector2(0, -20), new Vector2(360, 44), TextAlignmentOptions.Center, top: true);
        refs.best = Label(bestPanel, "BestScoreText", "0000", font, 64, new Vector2(0, -18), new Vector2(360, 90), TextAlignmentOptions.Center);

        var howTo = Panel("HowToPanel", root, panelGrey, new Vector2(0, 0), new Vector2(110, 90), new Vector2(400, 210));
        var howText = Label(howTo, "Text", "MOVE   A D / ARROWS\nDROP   SPACE\nRESTART   R", font, 28, Vector2.zero, new Vector2(350, 170), TextAlignmentOptions.Left);
        howText.lineSpacing = 22;

        // 오른쪽: 다음 과일, 상위 5개 기록
        var nextPanel = Panel("NextPanel", root, panelGrey, new Vector2(1, 1), new Vector2(-110, -90), new Vector2(400, 330));
        Label(nextPanel, "Label", "NEXT", font, 34, new Vector2(0, -24), new Vector2(360, 50), TextAlignmentOptions.Center, top: true);
        var imageRect = Rect("NextFruitImage", nextPanel, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -2), new Vector2(180, 180));
        refs.nextImage = imageRect.gameObject.AddComponent<Image>();
        refs.nextImage.preserveAspect = true;
        refs.nextImage.raycastTarget = false;
        refs.nextName = Label(nextPanel, "NextFruitName", "", font, 32, new Vector2(0, 30), new Vector2(360, 48), TextAlignmentOptions.Center, bottom: true);

        var rankPanel = Panel("RankPanel", root, panelGrey, new Vector2(1, 1), new Vector2(-110, -445), new Vector2(400, 360));
        Label(rankPanel, "Label", "TOP 5", font, 34, new Vector2(0, -24), new Vector2(360, 50), TextAlignmentOptions.Center, top: true);
        refs.hudList = Label(rankPanel, "RankList", "", font, 34, new Vector2(0, -84), new Vector2(300, 260), TextAlignmentOptions.TopLeft, top: true);

        // 게임오버 패널
        var overlay = Rect("GameOverPanel", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        var dim = overlay.gameObject.AddComponent<Image>();
        dim.color = new Color(.08f, .07f, .12f, .68f);
        dim.raycastTarget = false;
        refs.gameOverPanel = overlay.gameObject;

        var card = Panel("Card", overlay, panelYellow, new Vector2(.5f, .5f), Vector2.zero, new Vector2(720, 800), pivot: new Vector2(.5f, .5f));
        refs.card = card;
        Label(card, "Title", "GAME OVER", font, 84, new Vector2(0, -70), new Vector2(660, 110), TextAlignmentOptions.Center, top: true);
        refs.finalScore = Label(card, "FinalScoreText", "SCORE  0000", font, 54, new Vector2(0, -170), new Vector2(660, 80), TextAlignmentOptions.Center, top: true);

        var badge = Rect("NewBest", card, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -240), new Vector2(420, 70));
        var starRect = Rect("Star", badge, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(20, 0), new Vector2(64, 60));
        var starImage = starRect.gameObject.AddComponent<Image>();
        starImage.sprite = star;
        starImage.preserveAspect = true;
        starImage.raycastTarget = false;
        var badgeText = Label(badge, "Text", "NEW BEST!", font, 42, new Vector2(40, 0), new Vector2(320, 70), TextAlignmentOptions.Center);
        badgeText.color = new Color(.85f, .25f, .15f);
        refs.newBest = badge.gameObject;

        Label(card, "RankingTitle", "RANKING", font, 34, new Vector2(0, -320), new Vector2(560, 50), TextAlignmentOptions.Center, top: true);
        var listPanel = Panel("ListPanel", card, panelGrey, new Vector2(.5f, 1), new Vector2(0, -360), new Vector2(520, 270), pivot: new Vector2(.5f, 1));
        refs.resultList = Label(listPanel, "ResultList", "", font, 36, new Vector2(0, -22), new Vector2(380, 240), TextAlignmentOptions.TopLeft, top: true);

        var restartRow = Rect("RestartHint", card, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 75), new Vector2(560, 60));
        var iconRect = Rect("Icon", restartRow, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(10, 0), new Vector2(46, 40));
        var icon = iconRect.gameObject.AddComponent<Image>();
        icon.sprite = repeat;
        icon.color = Ink;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        Label(restartRow, "Text", "PRESS  R  TO RESTART", font, 30, new Vector2(40, 0), new Vector2(460, 60), TextAlignmentOptions.Center);

        overlay.gameObject.SetActive(false);
        return canvasGo;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static RectTransform Panel(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
    {
        var rect = Rect(name, parent, anchor, pivot ?? anchor, position, size);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        image.pixelsPerUnitMultiplier = .45f;
        image.raycastTarget = false;
        return rect;
    }

    private static TextMeshProUGUI Label(Transform parent, string name, string text, TMP_FontAsset font, float size, Vector2 position, Vector2 box, TextAlignmentOptions alignment, bool top = false, bool bottom = false)
    {
        var anchor = top ? new Vector2(.5f, 1) : bottom ? new Vector2(.5f, 0) : new Vector2(.5f, .5f);
        var rect = Rect(name, parent, anchor, anchor, position, box);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.text = text;
        label.fontSize = size;
        label.color = Ink;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        return label;
    }

    // ---------- 공통 ----------

    private static T Load<T>(string path) where T : Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) Debug.LogWarning("에셋을 찾지 못했습니다: " + path);
        return asset;
    }

    private static AudioClip Clip(string file) => Load<AudioClip>(Kenney + "/Audio/" + file);

    private static void SetRef(Object target, string field, Object value)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(field);
        if (property == null)
        {
            Debug.LogError($"[Suika] {target.GetType().Name}.{field} 필드를 찾지 못했습니다.");
            return;
        }
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray(Object target, string field, Object[] values)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(field);
        if (property == null || !property.isArray)
        {
            Debug.LogError($"[Suika] {target.GetType().Name}.{field} 배열 필드를 찾지 못했습니다.");
            return;
        }
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}

// 프로젝트를 처음 열었을 때 플레이 씬이 없으면 자동으로 한 번 만들고 엽니다.
[InitializeOnLoad]
internal static class SuikaAutoBuild
{
    private const string SessionKey = "Suika.AutoBuild.Done";

    static SuikaAutoBuild()
    {
        if (SessionState.GetBool(SessionKey, false)) return;
        EditorApplication.delayCall += () =>
        {
            SessionState.SetBool(SessionKey, true);
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!File.Exists(SuikaSceneBuilder.ScenePath))
            {
                SuikaSceneBuilder.Build();
                if (File.Exists(SuikaSceneBuilder.ScenePath))
                    EditorSceneManager.OpenScene(SuikaSceneBuilder.ScenePath, OpenSceneMode.Single);
            }
        };
    }
}
