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

    // 야시장 테마 그림(neon-watermelon-assets.zip)입니다.
    private const string BackgroundSpritePath = "Assets/Art/Background/background_night_market.png";
    private const string BasketSpritePath = "Assets/Art/Basket/basket_glass.png";
    private const string LimitLineSpritePath = "Assets/Art/Guides/limit_line_dashed.png";
    private const string DropGuideSpritePath = "Assets/Art/Guides/drop_guide_dotted.png";
    private const string ScorePanelPath = "Assets/Art/Layout/panel_score.png";
    private const string BestPanelPath = "Assets/Art/Layout/panel_best.png";
    private const string NextPanelPath = "Assets/Art/Layout/panel_next.png";
    private const string Top5PanelPath = "Assets/Art/Layout/panel_top5.png";
    // BEST 판에서 제목 칸과 구분선을 잘라 내고 이어 붙인 진화 순서 띠용 판입니다.
    private const string StripPanelPath = "Assets/Art/Layout/panel_strip.png";
    private const string FredokaFontPath = "Assets/ThirdParty/Fredoka/Fredoka-SemiBold.ttf";
    private const string FredokaAssetPath = "Assets/ThirdParty/Fredoka/Fredoka SDF.asset";
    // 바구니·안내선·제한선 그림의 Pixels Per Unit입니다. 아래 측정값은 이 값 기준입니다.
    private const float WorldPixelsPerUnit = 200f;

    // 유리 바구니 그림(1254px, PPU 200)에서 측정한 안쪽 경계입니다. 바구니 중심 기준 단위입니다.
    // 측정: 가운데에서 바깥으로 가며 불투명도 50%가 넘는 첫 픽셀(x 188·1066, 바닥 y 1141)을 경계로 봄.
    private const float BasketInnerLeft = -2.195f;
    private const float BasketInnerRight = 2.195f;
    private const float BasketInnerFloor = -2.57f;

    // 화면 배치 값입니다.
    private const float BasketY = -1.15f;
    private const float DropY = 2.75f;
    private const float GameOverLineY = 1.35f;
    private const float CameraSize = 4.6f;

    // 배경 이미지를 넣기 전까지 보이는 카메라 배경색(야시장 그림의 어두운 보라)입니다.
    private static readonly Color Background = new Color32(24, 16, 40, 255);
    // 참고 이미지의 글자 색: 제목은 민트, 숫자·이름은 크림색, 강조는 판 테두리와 같은 분홍입니다.
    private static readonly Color Mint = new Color32(127, 230, 190, 255);
    private static readonly Color Cream = new Color32(247, 231, 198, 255);
    private static readonly Color Pink = new Color32(255, 111, 145, 255);

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

        // 배경 이미지: BackgroundFitter가 화면 비율에 맞춰 화면 전체를 덮도록 크기를 맞춥니다.
        // 이미지가 없으면 비어 있는 채로 두고 카메라 배경색(남색)만 보입니다.
        var background = new GameObject("Background");
        background.transform.position = new Vector3(0, 0, 5);
        var backgroundRenderer = background.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundSpritePath);
        backgroundRenderer.sortingOrder = -20;
        if (unlit != null) backgroundRenderer.sharedMaterial = unlit;
        var fitter = background.AddComponent<BackgroundFitter>();
        SetRef(fitter, "targetCamera", cam);

        // 바구니 그림과 실제 충돌 벽입니다. 벽은 그림 안쪽 경계에 맞추고 위로 길게 늘려 과일이 넘어가지 않게 합니다.
        var basket = Sprite("Basket", Load<Sprite>(BasketSpritePath), new Vector3(0, BasketY, 0), 3, unlit);
        var walls = new GameObject("BasketColliders").transform;
        float floorY = BasketY + BasketInnerFloor;
        const float thickness = 1f;
        const float wallTop = 6f;
        var leftWall = Box("LeftWall", walls, new Vector2(BasketInnerLeft - thickness * .5f, (floorY + wallTop) * .5f), new Vector2(thickness, wallTop - floorY + thickness));
        var rightWall = Box("RightWall", walls, new Vector2(BasketInnerRight + thickness * .5f, (floorY + wallTop) * .5f), new Vector2(thickness, wallTop - floorY + thickness));
        Box("Floor", walls, new Vector2((BasketInnerLeft + BasketInnerRight) * .5f, floorY - thickness * .5f), new Vector2(BasketInnerRight - BasketInnerLeft + thickness * 2, thickness));

        var dropPoint = new GameObject("DropPoint").transform;
        dropPoint.position = new Vector3(0, DropY, 0);

        // 낙하 위치 안내선(흰 점선): 낙하 높이부터 바닥까지 세로로 늘립니다. Collider는 넣지 않습니다.
        var guideSprite = Load<Sprite>(DropGuideSpritePath);
        var guide = Sprite("DropGuide", guideSprite, new Vector3(0, (DropY + floorY) * .5f, 0), -1, unlit);
        if (guideSprite != null)
            guide.transform.localScale = new Vector3(1, (DropY - floorY) / guideSprite.bounds.size.y, 1);

        // 게임오버 제한선: 분홍 점선입니다. Transform y가 실제 판정 높이이며 Collider는 넣지 않습니다.
        var lineSprite = Load<Sprite>(LimitLineSpritePath);
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
        // 리더보드 판 그림의 칸 간격(약 56px)과 게임오버 카드의 줄 간격(38px)에 맞춥니다. 강조색은 판 테두리와 같은 분홍입니다.
        SetString(leaderboard, "hudTextPrefix", "<line-height=56>");
        SetString(leaderboard, "resultTextPrefix", "<line-height=38>");
        SetColor(leaderboard, "highlightColor", Pink);

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

        // 야시장 테마 판. 화면의 판은 원래 비율 그대로(Simple) 쓰고, NEXT 판만 게임오버 카드에서 9-slice로 늘립니다.
        // NEXT 판의 테두리 값은 제목 칸(위 30%)과 안쪽 틀 모서리가 늘어나지 않도록 잰 값입니다. (왼쪽, 아래, 오른쪽, 위)
        SpriteImport(ScorePanelPath, Vector4.zero);
        SpriteImport(BestPanelPath, Vector4.zero);
        SpriteImport(NextPanelPath, new Vector4(180, 200, 180, 380));
        SpriteImport(Top5PanelPath, Vector4.zero);
        // 진화 순서 판은 세로로 길게 9-slice 합니다. 위 290px(둥근 모서리와 마름모 장식), 아래 240px(아래 모서리 홈),
        // 좌우 280px(마름모 가로 범위)을 테두리로 두어 장식은 그대로 두고 곧은 선 부분만 늘어나게 합니다.
        SpriteImport(StripPanelPath, new Vector4(280, 240, 280, 290));
        SpriteImport(BasketSpritePath, Vector4.zero, WorldPixelsPerUnit);
        SpriteImport(LimitLineSpritePath, Vector4.zero, WorldPixelsPerUnit);
        SpriteImport(DropGuideSpritePath, Vector4.zero, WorldPixelsPerUnit);
        SpriteImport(BackgroundSpritePath, Vector4.zero);

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

    private static void SpriteImport(string path, Vector4 border, float pixelsPerUnit = 100f)
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
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.spriteBorder = border;
        importer.SaveAndReimport();
    }

    private static TMP_FontAsset CreateFontAsset()
    {
        // Fredoka SemiBold(OFL, 고정 굵기 600)로 SDF 폰트 에셋을 만듭니다.
        const string fontPath = FredokaFontPath;
        const string assetPath = FredokaAssetPath;

        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null) return existing;

        var source = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        var fallback = TMP_Settings.defaultFontAsset;
        if (source == null) return fallback;

        try
        {
            var asset = TMP_FontAsset.CreateFontAsset(source);
            if (asset == null) return fallback;
            asset.name = "Fredoka SDF";
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
            // Fredoka에 없는 글자는 TMP 기본 폰트로 대신 그립니다.
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
            Debug.LogWarning("Fredoka 폰트 에셋을 만들지 못해 기본 폰트를 사용합니다: " + exception.Message);
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

    private static TMP_Text Tint(TMP_Text label, Color color)
    {
        label.color = color;
        return label;
    }

    // 과일 프리팹의 몸통(Body) 그림입니다. 진화 순서 띠에 씁니다.
    private static Sprite FruitBody(string fruit)
    {
        var prefab = Load<GameObject>("Assets/Prefabs/Fruits/" + fruit + ".prefab");
        var body = prefab != null ? prefab.transform.Find("Body") : null;
        var renderer = body != null ? body.GetComponent<SpriteRenderer>() : null;
        return renderer != null ? renderer.sprite : null;
    }

    // ---------- UI ----------

    private struct UiRefs
    {
        public TMP_Text score, best, finalScore, nextName, resultList, hudList;
        public Image nextImage;
        public GameObject gameOverPanel, newBest;
        public RectTransform card;
    }

    // 판 배치는 참고 이미지(1672x941)의 비율을 1920x1080 캔버스로 옮긴 값입니다.
    // 판 안 글자 위치는 각 판 그림에서 잰 칸 비율(예: SCORE 제목 칸 10~39%, 숫자 칸 39~79%)로 정했습니다.
    private static GameObject BuildUI(TMP_FontAsset font, out UiRefs refs)
    {
        refs = new UiRefs();
        var scorePanelSprite = Load<Sprite>(ScorePanelPath);
        var bestPanelSprite = Load<Sprite>(BestPanelPath);
        var nextPanelSprite = Load<Sprite>(NextPanelPath);
        var top5PanelSprite = Load<Sprite>(Top5PanelPath);
        var stripPanelSprite = Load<Sprite>(StripPanelPath);
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

        // 왼쪽: 현재 점수(300x190), 최고 점수(270x124). 배경 그림의 네온 수박 간판 아래에 둡니다.
        var scorePanel = Panel("ScorePanel", root, scorePanelSprite, new Vector2(0, 1), new Vector2(190, -262), new Vector2(300, 190));
        Tint(Label(scorePanel, "Label", "SCORE", font, 30, new Vector2(0, -24), new Vector2(260, 44), TextAlignmentOptions.Center, top: true), Mint);
        refs.score = Tint(Label(scorePanel, "ScoreText", "0000", font, 58, new Vector2(0, -72), new Vector2(240, 80), TextAlignmentOptions.Center, top: true), Cream);

        var bestPanel = Panel("BestPanel", root, bestPanelSprite, new Vector2(0, 1), new Vector2(205, -470), new Vector2(270, 124));
        Tint(Label(bestPanel, "Label", "BEST", font, 24, new Vector2(0, -13), new Vector2(230, 34), TextAlignmentOptions.Center, top: true), Mint);
        refs.best = Tint(Label(bestPanel, "BestScoreText", "0000", font, 42, new Vector2(0, -51), new Vector2(230, 54), TextAlignmentOptions.Center, top: true), Cream);

        // 왼쪽 아래: 리더보드(300x417, TOP 5 판 비율 그대로). 참고한 수박게임처럼 점수 판 아래에 둡니다.
        // 판 그림의 다섯 칸 간격은 높이의 13.55%(약 56px)이고, 첫 칸 가운데는 위에서 24.8%(약 103px)입니다.
        var rankPanel = Panel("LeaderboardPanel", root, top5PanelSprite, new Vector2(0, 1), new Vector2(190, -610), new Vector2(300, 417));
        // 제목이 양옆 마름모 장식에 닿지 않도록 20pt로 둡니다.
        Tint(Label(rankPanel, "Label", "LEADERBOARD", font, 20, new Vector2(0, -28), new Vector2(200, 34), TextAlignmentOptions.Center, top: true), Mint);
        // 안쪽 틀 왼쪽 선과 띄우려고 글자 칸을 오른쪽으로 10px 옮깁니다.
        refs.hudList = Tint(Label(rankPanel, "RankList", "", font, 26, new Vector2(10, -85), new Vector2(210, 300), TextAlignmentOptions.TopLeft, top: true), Cream);

        // 오른쪽(가운데 x=1600): 다음 과일(260x250), 그 아래 세로 진화 순서 판(200x605). 두 열의 아래 끝을 약 1000px로 맞춥니다.
        var nextPanel = Panel("NextPanel", root, nextPanelSprite, new Vector2(1, 1), new Vector2(-190, -125), new Vector2(260, 250));
        Tint(Label(nextPanel, "Label", "NEXT", font, 30, new Vector2(0, -25), new Vector2(220, 40), TextAlignmentOptions.Center, top: true), Mint);
        var imageRect = Rect("NextFruitImage", nextPanel, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -80), new Vector2(96, 96));
        refs.nextImage = imageRect.gameObject.AddComponent<Image>();
        refs.nextImage.preserveAspect = true;
        refs.nextImage.raycastTarget = false;
        // 채점 5번(이미지와 이름 함께 갱신)을 위해 참고 이미지에는 없는 이름을 안쪽 틀 아래에 둡니다.
        refs.nextName = Tint(Label(nextPanel, "NextFruitName", "", font, 22, new Vector2(0, -180), new Vector2(200, 30), TextAlignmentOptions.Center, top: true), Cream);

        // 세로 진화 순서 판: 9-slice 테두리를 1/6로 줄여 그려 모서리·마름모가 약 47px 크기로 보입니다.
        // NEXT 판과 가운데(x=1600)를 맞춥니다: 오른쪽 끝 1700px.
        var stripPanel = Panel("EvolutionStrip", root, stripPanelSprite, new Vector2(1, 1), new Vector2(-220, -395), new Vector2(200, 605), sliced: true, ppuMultiplier: 6f);
        string[] order = { "cherry", "mandarin", "apple", "melon", "watermelon" };
        const float arrowGap = 24f;
        // 체리부터 수박까지 위에서 아래로, 진화할수록 커지게(60~100px) 놓고 사이에 아래 방향 화살표를 둡니다.
        // 과일 그림에 투명 여백이 있어 칸 크기보다 작아 보이므로 판 안쪽 높이를 거의 다 씁니다.
        float[] sizes = { 60, 70, 80, 90, 100 };
        float total = arrowGap * (order.Length - 1);
        foreach (var s in sizes) total += s;
        // 판 안쪽(위 테두리 약 48px 아래 ~ 아래 테두리 약 40px 위)의 가운데에 세로로 모읍니다.
        float y = 48 + (605 - 48 - 40 - total) * .5f;
        for (int i = 0; i < order.Length; i++)
        {
            var icon = Rect("Fruit_" + order[i], stripPanel, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -y), new Vector2(sizes[i], sizes[i]));
            var iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.sprite = FruitBody(order[i]);
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            y += sizes[i];
            if (i < order.Length - 1)
            {
                // 기존 ">" 글자를 시계 방향으로 90도 돌려 아래를 가리키게 합니다.
                var arrow = Tint(Label(stripPanel, "Arrow" + i, ">", font, 24, new Vector2(0, -y), new Vector2(arrowGap, arrowGap), TextAlignmentOptions.Center, top: true), Mint);
                // 칸 가운데를 기준으로 돌려야 제자리에서 회전합니다.
                arrow.rectTransform.pivot = new Vector2(.5f, .5f);
                arrow.rectTransform.anchoredPosition = new Vector2(0, -(y + arrowGap * .5f));
                arrow.rectTransform.localEulerAngles = new Vector3(0, 0, -90);
                y += arrowGap;
            }
        }

        // 게임오버 패널
        var overlay = Rect("GameOverPanel", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        var dim = overlay.gameObject.AddComponent<Image>();
        // 게임오버 순간 제한선 위로 넘친 과일이 보이도록 어둡게 하는 정도를 낮춥니다.
        dim.color = new Color(.08f, .05f, .1f, .5f);
        dim.raycastTarget = false;
        refs.gameOverPanel = overlay.gameObject;

        // 카드는 제한선(화면 위에서 약 380px) 아래에 두어, 게임오버 순간 선을 넘은 과일과 제한선이 가려지지 않게 합니다.
        // NEXT 판을 9-slice로 늘려 쓰며, 테두리를 1/2.7로 줄여 제목 칸 약 141px, 옆 67px, 아래 74px이 됩니다.
        var card = Panel("Card", overlay, nextPanelSprite, new Vector2(.5f, .5f), new Vector2(0, -200), new Vector2(720, 640), pivot: new Vector2(.5f, .5f), sliced: true, ppuMultiplier: 2.7f);
        refs.card = card;
        Tint(Label(card, "Title", "GAME OVER", font, 56, new Vector2(0, -35), new Vector2(600, 70), TextAlignmentOptions.Center, top: true), Pink);
        refs.finalScore = Tint(Label(card, "FinalScoreText", "SCORE  0000", font, 44, new Vector2(0, -157), new Vector2(600, 56), TextAlignmentOptions.Center, top: true), Cream);

        var badge = Rect("NewBest", card, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -208), new Vector2(360, 60));
        var starRect = Rect("Star", badge, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(30, 0), new Vector2(50, 46));
        var starImage = starRect.gameObject.AddComponent<Image>();
        starImage.sprite = star;
        starImage.preserveAspect = true;
        starImage.raycastTarget = false;
        Tint(Label(badge, "Text", "NEW BEST!", font, 34, new Vector2(30, 0), new Vector2(260, 60), TextAlignmentOptions.Center), Pink);
        refs.newBest = badge.gameObject;

        Tint(Label(card, "RankingTitle", "RANKING", font, 26, new Vector2(0, -267), new Vector2(400, 36), TextAlignmentOptions.Center, top: true), Mint);
        refs.resultList = Tint(Label(card, "ResultList", "", font, 26, new Vector2(0, -305), new Vector2(300, 200), TextAlignmentOptions.TopLeft, top: true), Cream);

        // 아이콘과 글자가 붙어 보이도록 줄 폭을 380px로 줄였습니다.
        var restartRow = Rect("RestartHint", card, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 80), new Vector2(380, 50));
        var iconRect = Rect("Icon", restartRow, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, 0), new Vector2(40, 36));
        var restartIcon = iconRect.gameObject.AddComponent<Image>();
        restartIcon.sprite = repeat;
        restartIcon.color = Mint;
        restartIcon.preserveAspect = true;
        restartIcon.raycastTarget = false;
        Tint(Label(restartRow, "Text", "PRESS  R  TO RESTART", font, 24, new Vector2(24, 0), new Vector2(330, 50), TextAlignmentOptions.Center), Cream);

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

    // sliced가 false면 그림을 원래 비율 그대로(Simple) 그리고, true면 스프라이트 테두리 값으로 9-slice 합니다.
    // ppuMultiplier가 클수록 9-slice 테두리가 작게 그려집니다.
    private static RectTransform Panel(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null, bool sliced = false, float ppuMultiplier = 1f)
    {
        var rect = Rect(name, parent, anchor, pivot ?? anchor, position, size);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        image.preserveAspect = !sliced;
        image.pixelsPerUnitMultiplier = ppuMultiplier;
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
        label.color = Cream;
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

    private static void SetString(Object target, string field, string value)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(field);
        if (property == null)
        {
            Debug.LogError($"[Suika] {target.GetType().Name}.{field} 필드를 찾지 못했습니다.");
            return;
        }
        property.stringValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetColor(Object target, string field, Color value)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(field);
        if (property == null)
        {
            Debug.LogError($"[Suika] {target.GetType().Name}.{field} 필드를 찾지 못했습니다.");
            return;
        }
        property.colorValue = value;
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
