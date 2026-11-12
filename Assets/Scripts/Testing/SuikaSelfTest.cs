#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// 메뉴 Suika > Run Self Test로만 생성되는 검사용 컴포넌트입니다. 빌드에는 포함되지 않습니다.
// 채점 기준의 동작을 실제 물리로 재현하고, 결과를 프로젝트 폴더의 SelfTestReport.txt에 기록합니다.
// 확인용 스크린샷은 Screenshots 폴더에 저장합니다.
public sealed class SuikaSelfTest : MonoBehaviour
{
    private readonly StringBuilder report = new StringBuilder();
    private int passed;
    private int failed;

    private FruitDropManager drop;
    private GameOverManager over;
    private LeaderboardManager board;
    private DropPreview preview;
    private Transform spawned;
    private Transform guide;
    private Transform line;
    private GameObject panel;
    private BoxCollider2D leftWall;
    private BoxCollider2D rightWall;
    private float floorY;

    private GameObject cherry, mandarin, apple, melon, watermelon;

    public static string ReportPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "SelfTestReport.txt"));
    public static string ShotFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Screenshots"));

    private IEnumerator Start()
    {
        report.AppendLine("Suika self test  " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        report.AppendLine("Unity " + Application.unityVersion);
        Directory.CreateDirectory(ShotFolder);

        yield return null;
        yield return null;

        if (!Setup())
        {
            Finish();
            yield break;
        }

        yield return Run("4) 낙하 간격과 다음 과일", TestDropAndNext());
        yield return Run("4) 이동 제한과 가이드", TestMoveClamp());
        yield return Run("6) 합체 4단계와 점수", TestMergeChain());
        yield return Run("6) 수박끼리는 합체하지 않음", TestWatermelonStays());
        yield return Run("6) 같은 과일 3개 동시 접촉", TestTriple());
        yield return Run("6·7) 연쇄 합체 40점", TestChainScore());
        yield return Run("8) 잠깐 넘었다 내려오면 계속", TestBriefOverflow());
        yield return Run("화면 확인용 장면", Showcase());
        yield return Run("8) 게임오버와 재시작, B) 리더보드", TestGameOverAndRestart());

        Finish();
    }

    // ---------- 준비 ----------

    private bool Setup()
    {
        drop = FindAnyObjectByType<FruitDropManager>();
        over = FindAnyObjectByType<GameOverManager>();
        board = FindAnyObjectByType<LeaderboardManager>();
        preview = FindAnyObjectByType<DropPreview>();
        spawned = GameObject.Find("SpawnedFruits")?.transform;
        guide = GameObject.Find("DropGuide")?.transform;
        line = GameObject.Find("GameOverLine")?.transform;
        leftWall = GameObject.Find("LeftWall")?.GetComponent<BoxCollider2D>();
        rightWall = GameObject.Find("RightWall")?.GetComponent<BoxCollider2D>();
        var floor = GameObject.Find("Floor")?.GetComponent<BoxCollider2D>();

        var canvas = GameObject.Find("Canvas");
        panel = canvas != null ? canvas.transform.Find("GameOverPanel")?.gameObject : null;

        cherry = LoadPrefab("cherry");
        mandarin = LoadPrefab("mandarin");
        apple = LoadPrefab("apple");
        melon = LoadPrefab("melon");
        watermelon = LoadPrefab("watermelon");

        bool ok = drop != null && over != null && board != null && preview != null && spawned != null
            && guide != null && line != null && panel != null && leftWall != null && rightWall != null && floor != null
            && cherry != null && mandarin != null && apple != null && melon != null && watermelon != null
            && ScoreManager.Instance != null;
        if (!ok)
        {
            Fail("준비", "씬 구성 요소를 찾지 못했습니다. Suika > Build Play Scene을 먼저 실행하세요.");
            return false;
        }
        floorY = floor.bounds.max.y;
        return true;
    }

    private static GameObject LoadPrefab(string name)
    {
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Fruits/" + name + ".prefab");
    }

    private IEnumerator Run(string title, IEnumerator body)
    {
        report.AppendLine();
        report.AppendLine("## " + title);
        yield return body;
    }

    private void Check(string name, bool condition, string detail = "")
    {
        if (condition)
        {
            passed++;
            report.AppendLine("PASS  " + name + (detail.Length > 0 ? "  (" + detail + ")" : ""));
        }
        else Fail(name, detail);
    }

    private void Fail(string name, string detail)
    {
        failed++;
        report.AppendLine("FAIL  " + name + (detail.Length > 0 ? "  (" + detail + ")" : ""));
        Debug.LogError("[SelfTest] FAIL " + name + " " + detail);
    }

    private void Finish()
    {
        report.AppendLine();
        report.AppendLine($"RESULT  passed={passed}  failed={failed}");
        File.WriteAllText(ReportPath, report.ToString());
        Debug.Log($"[SelfTest] passed={passed} failed={failed}  report: {ReportPath}");
        Time.timeScale = 1;
        UnityEditor.EditorApplication.ExitPlaymode();
    }

    // ---------- 도우미 ----------

    private float Radius(GameObject prefab)
    {
        return prefab.GetComponent<CircleCollider2D>().radius * Mathf.Abs(prefab.transform.localScale.x);
    }

    private FruitMerge Spawn(GameObject prefab, Vector2 position)
    {
        var go = Instantiate(prefab, position, Quaternion.identity, spawned);
        go.GetComponent<Rigidbody2D>().simulated = true;
        return go.GetComponent<FruitMerge>();
    }

    private IEnumerator ClearBoard()
    {
        if (over.IsGameOver) over.RestartGame();
        for (int i = spawned.childCount - 1; i >= 0; i--)
        {
            var child = spawned.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
        yield return new WaitForFixedUpdate();
        yield return null;
    }

    private List<FruitMerge> Fruits()
    {
        var list = new List<FruitMerge>();
        spawned.GetComponentsInChildren(false, list);
        return list;
    }

    private int Count(FruitType type)
    {
        int count = 0;
        foreach (var fruit in Fruits()) if (fruit.Type == type) count++;
        return count;
    }

    private static IEnumerator Wait(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }

    private static void Shot(string name)
    {
        ScreenCapture.CaptureScreenshot(Path.Combine(ShotFolder, name + ".png"));
    }

    // ---------- 시나리오 ----------

    private IEnumerator TestDropAndNext()
    {
        yield return ClearBoard();
        yield return Wait(.45f);

        var candidates = new[] { cherry, mandarin, apple };
        var shown = drop.NextFruitPrefab;
        var first = drop.DropFruit();
        Check("Space 낙하로 과일 1개 생성", first != null);
        Check("표시된 다음 과일과 실제 낙하 과일 일치", first != null && shown != null && first.name.StartsWith(shown.name), shown != null ? shown.name : "null");
        Check("낙하 직후 다음 과일을 다시 선택", drop.NextFruitPrefab != null && System.Array.IndexOf(candidates, drop.NextFruitPrefab) >= 0, drop.NextFruitPrefab != null ? drop.NextFruitPrefab.name : "null");

        var second = drop.DropFruit();
        Check("0.4초 안의 반복 입력은 생성하지 않음", second == null);

        var seen = new HashSet<string>();
        for (int i = 0; i < 40; i++) seen.Add(PickName());
        Check("무작위 후보는 체리·귤·사과만", seen.IsSubsetOf(new[] { "cherry", "mandarin", "apple" }) && seen.Count == 3, string.Join(",", seen));

        var previewRenderer = preview.GetComponent<SpriteRenderer>();
        var body = drop.NextFruitPrefab.transform.Find("Body").GetComponent<SpriteRenderer>();
        Check("A) 미리보기가 다음 과일 몸통과 같음", previewRenderer.enabled && previewRenderer.sprite == body.sprite);
        Check("A) 미리보기에는 Collider가 없음", preview.GetComponent<Collider2D>() == null && preview.GetComponent<Rigidbody2D>() == null);

        yield return Wait(.45f);
        var third = drop.DropFruit();
        Check("0.4초가 지나면 다시 낙하", third != null);
        yield return Wait(1f);
    }

    private string PickName()
    {
        drop.ResetRound();
        return drop.NextFruitPrefab != null ? drop.NextFruitPrefab.name : "null";
    }

    private IEnumerator TestMoveClamp()
    {
        yield return ClearBoard();
        float y = drop.DropPoint.position.y;
        drop.MoveDropPoint(-50f);
        float r = Radius(drop.NextFruitPrefab);
        float minX = leftWall.bounds.max.x + r;
        Check("왼쪽 끝에서 바구니 안으로 제한", drop.DropPoint.position.x >= minX - .001f, $"x={drop.DropPoint.position.x:F3} min={minX:F3}");
        Check("가이드가 낙하 위치를 따라감", Mathf.Abs(guide.position.x - drop.DropPoint.position.x) < .001f);
        Check("이동 중 낙하 높이 유지", Mathf.Abs(drop.DropPoint.position.y - y) < .0001f);
        // 미리보기는 LateUpdate에서 따라가므로 한 프레임 뒤에 확인합니다.
        yield return null;
        Check("A) 미리보기가 좌우 이동을 따라감", Mathf.Abs(preview.transform.position.x - drop.DropPoint.position.x) < .2f);

        drop.MoveDropPoint(50f);
        float maxX = rightWall.bounds.min.x - r;
        Check("오른쪽 끝에서 바구니 안으로 제한", drop.DropPoint.position.x <= maxX + .001f, $"x={drop.DropPoint.position.x:F3} max={maxX:F3}");

        yield return Wait(.45f);
        var fruit = drop.DropFruit();
        yield return Wait(1.5f);
        Check("가장자리에서 떨어진 과일이 벽 안에 머묾", fruit != null && fruit.transform.position.x <= rightWall.bounds.min.x && fruit.transform.position.y > floorY - .05f);
        drop.ResetRound();
    }

    private IEnumerator TestMergeChain()
    {
        var pairs = new[]
        {
            (cherry, FruitType.Mandarin, 10),
            (mandarin, FruitType.Apple, 30),
            (apple, FruitType.Melon, 80),
            (melon, FruitType.Watermelon, 200),
        };

        foreach (var (prefab, result, points) in pairs)
        {
            yield return ClearBoard();
            ScoreManager.Instance.ResetScore();
            float r = Radius(prefab);
            Spawn(prefab, new Vector2(-r * .95f, floorY + r));
            Spawn(prefab, new Vector2(r * .95f, floorY + r));
            yield return Wait(.6f);

            var fruits = Fruits();
            Check($"{prefab.name} 2개 → {result} 1개", fruits.Count == 1 && fruits[0].Type == result, $"count={fruits.Count}");
            Check($"{result} 생성 점수 {points}", ScoreManager.Instance.CurrentScore == points, $"score={ScoreManager.Instance.CurrentScore}");
            Check($"{result}이 바구니 안에 자연스럽게 있음", fruits.Count == 1 && fruits[0].transform.position.y > floorY - .05f && fruits[0].transform.position.y < floorY + 3f);
        }
        Shot("01_watermelon_merged");
        yield return Wait(.3f);
    }

    private IEnumerator TestWatermelonStays()
    {
        yield return ClearBoard();
        ScoreManager.Instance.ResetScore();
        float r = Radius(watermelon);
        Spawn(watermelon, new Vector2(-r * .98f, floorY + r));
        Spawn(watermelon, new Vector2(r * .98f, floorY + r));
        yield return Wait(.8f);
        Check("수박 2개가 그대로 남음", Count(FruitType.Watermelon) == 2, $"count={Count(FruitType.Watermelon)}");
        Check("수박끼리 닿아도 점수 없음", ScoreManager.Instance.CurrentScore == 0);
    }

    private IEnumerator TestTriple()
    {
        yield return ClearBoard();
        ScoreManager.Instance.ResetScore();
        float r = Radius(cherry);
        Spawn(cherry, new Vector2(-r * .95f, floorY + r));
        Spawn(cherry, new Vector2(r * .95f, floorY + r));
        Spawn(cherry, new Vector2(0, floorY + r * 2.6f));
        yield return Wait(.8f);
        Check("귤 1개와 체리 1개가 남음", Count(FruitType.Mandarin) == 1 && Count(FruitType.Cherry) == 1, $"mandarin={Count(FruitType.Mandarin)} cherry={Count(FruitType.Cherry)}");
        Check("중복 합체 없이 10점", ScoreManager.Instance.CurrentScore == 10, $"score={ScoreManager.Instance.CurrentScore}");
    }

    private IEnumerator TestChainScore()
    {
        yield return ClearBoard();
        ScoreManager.Instance.ResetScore();
        float rc = Radius(cherry);
        float rm = Radius(mandarin);
        // 바닥의 귤 두 개 사이 홈에 체리 두 개를 얹습니다. 체리가 합쳐진 귤은 아래 귤 위로 떨어져
        // 계속 닿아 있게 되므로, 0.2초 대기가 끝난 뒤 연쇄 합체가 일어나야 합니다.
        Spawn(mandarin, new Vector2(-rm, floorY + rm));
        Spawn(apple, new Vector2(rm + Radius(apple) + .02f, floorY + Radius(apple)));
        float top = floorY + rm * 2f;
        Spawn(cherry, new Vector2(-rm, top + rc));
        Spawn(cherry, new Vector2(-rm + .05f, top + rc * 3f - .05f));
        yield return Wait(1.5f);
        // 옆에 둔 사과는 받침 역할이며, 연쇄로 생긴 사과와 만나면 멜론까지 이어질 수 있습니다.
        int apples = Count(FruitType.Apple);
        int melons = Count(FruitType.Melon);
        int score = ScoreManager.Instance.CurrentScore;
        bool chained = Count(FruitType.Mandarin) == 0 && Count(FruitType.Cherry) == 0
            && ((apples == 2 && melons == 0 && score == 40) || (apples == 0 && melons == 1 && score == 120));
        Check("체리→귤 후 기존 귤과 연쇄 합체해 사과", chained, $"apple={apples} melon={melons} mandarin={Count(FruitType.Mandarin)}");
        Check("연쇄 합체 점수(귤 10 + 사과 30 = 40)", score == 40 || score == 120, $"score={score}");
    }

    private IEnumerator TestBriefOverflow()
    {
        yield return ClearBoard();
        var fruit = Spawn(apple, new Vector2(0, line.position.y + 1.2f));
        var body = fruit.GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        // 생성 유예(1초)가 지난 뒤 0.6초만 선 위에 머물다 아래로 내립니다.
        yield return Wait(1.6f);
        fruit.transform.position = new Vector2(0, floorY + Radius(apple));
        body.bodyType = RigidbodyType2D.Dynamic;
        yield return Wait(1.3f);
        Check("1초 미만으로 넘었다 내려오면 게임 계속", !over.IsGameOver);
    }

    private IEnumerator Showcase()
    {
        yield return ClearBoard();
        ScoreManager.Instance.ResetScore();
        // 여러 단계 과일이 쌓인 장면을 만들고 실제 낙하 입력 경로로 몇 개 더 떨어뜨립니다.
        Spawn(watermelon, new Vector2(-1.4f, floorY + Radius(watermelon)));
        Spawn(melon, new Vector2(1.3f, floorY + Radius(melon)));
        Spawn(apple, new Vector2(.1f, floorY + Radius(apple)));
        Spawn(mandarin, new Vector2(1.7f, floorY + 2f));
        Spawn(cherry, new Vector2(-.6f, floorY + 2.4f));
        yield return Wait(1.2f);
        float[] xs = { -1.2f, .4f, 1.4f };
        foreach (float x in xs)
        {
            drop.MoveDropPoint(x - drop.DropPoint.position.x);
            yield return Wait(.45f);
            drop.DropFruit();
        }
        yield return Wait(1.5f);
        drop.MoveDropPoint(-drop.DropPoint.position.x);
        yield return Wait(.2f);
        Shot("02_gameplay");
        yield return Wait(.3f);
    }

    private IEnumerator TestGameOverAndRestart()
    {
        int recordsBefore = board.Entries.Count;
        int best = ScoreManager.Instance.BestScore;
        int scoreAtEnd = ScoreManager.Instance.CurrentScore;

        // 제한선 위에 고정된 과일로 게임오버를 만듭니다(생성 유예 1초 + 초과 1초).
        var fruit = Spawn(watermelon, new Vector2(-1.2f, line.position.y + 1f));
        fruit.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        float waited = 0;
        while (!over.IsGameOver && waited < 4f)
        {
            yield return new WaitForFixedUpdate();
            waited += Time.fixedDeltaTime;
        }
        Check("제한선 위 1초 유지 시 게임오버", over.IsGameOver, $"after {waited:F2}s");
        Check("게임오버 판정이 유예+초과 시간 이후", waited >= 1.9f, $"{waited:F2}s");
        Check("결과 패널 표시", panel.activeSelf);
        Check("물리 정지(Time.timeScale = 0)", Time.timeScale == 0);
        Check("가이드 숨김", !guide.gameObject.activeSelf);
        Check("게임오버 중 낙하 불가", drop.DropFruit() == null);
        Check("A) 게임오버 시 미리보기 숨김", !preview.GetComponent<SpriteRenderer>().enabled);
        Check("B) 기록 1개 등록", board.Entries.Count == Mathf.Min(recordsBefore + 1, 5), $"records={board.Entries.Count}");
        var badge = panel.transform.Find("Card/NewBest");
        // 이 시점까지 재시작이 없었으므로 판 시작 최고 기록은 0입니다. 최종 점수가 현재 최고 기록일 때만 배지가 떠야 합니다.
        bool expectBadge = scoreAtEnd > 0 && scoreAtEnd >= ScoreManager.Instance.BestScore;
        Check("최고 기록 배지는 실제 갱신 때만 표시", badge != null && badge.gameObject.activeSelf == expectBadge, $"score={scoreAtEnd} best={ScoreManager.Instance.BestScore} badge={(badge != null && badge.gameObject.activeSelf)}");

        yield return new WaitForSecondsRealtime(.5f);
        Shot("03_game_over");
        yield return new WaitForSecondsRealtime(.3f);

        var dropStart = drop.DropPoint.position;
        drop.enabled = true;
        drop.MoveDropPoint(1f);
        drop.enabled = false;
        Check("게임오버 중 좌우 이동 불가", drop.DropPoint.position == dropStart);

        over.RestartGame();
        yield return null;
        yield return new WaitForFixedUpdate();
        Check("재시작 시 과일 모두 제거", spawned.childCount == 0, $"children={spawned.childCount}");
        Check("재시작 시 패널 숨김", !panel.activeSelf);
        Check("재시작 시 가이드 표시", guide.gameObject.activeSelf);
        Check("재시작 시 시간 복원", Time.timeScale == 1);
        Check("재시작 시 현재 점수 0", ScoreManager.Instance.CurrentScore == 0);
        Check("최고 점수 유지", ScoreManager.Instance.BestScore == Mathf.Max(best, scoreAtEnd));
        Check("A) 재시작 시 미리보기 복원", preview.GetComponent<SpriteRenderer>().enabled);

        yield return Wait(.45f);
        Check("재시작 후 다시 낙하", drop.DropFruit() != null);

        // 두 번째 판: 점수를 만든 뒤 다시 게임오버 → 기록이 정렬되어 추가되는지 확인합니다.
        ScoreManager.Instance.AddMergeScore(FruitType.Melon);
        var second = Spawn(watermelon, new Vector2(-1.2f, line.position.y + 1f));
        second.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        waited = 0;
        while (!over.IsGameOver && waited < 4f)
        {
            yield return new WaitForFixedUpdate();
            waited += Time.fixedDeltaTime;
        }
        Check("두 번째 판에서도 게임오버", over.IsGameOver);
        bool sorted = true;
        for (int i = 1; i < board.Entries.Count; i++)
            if (board.Entries[i - 1].score < board.Entries[i].score) sorted = false;
        Check("B) 기록이 점수 내림차순", sorted && board.Entries.Count == Mathf.Min(recordsBefore + 2, 5), $"records={board.Entries.Count}");

        board.Register(9999);
        Check("B) 같은 판 중복 등록 방지", board.Entries.Count == Mathf.Min(recordsBefore + 2, 5));

        yield return new WaitForSecondsRealtime(.6f);
        Shot("04_game_over_leaderboard");
        yield return new WaitForSecondsRealtime(.3f);

        over.RestartGame();
        yield return null;
        Check("두 번째 재시작", !over.IsGameOver && Time.timeScale == 1);
    }
}
#endif
