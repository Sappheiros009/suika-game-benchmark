using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public sealed class FruitDropManager : MonoBehaviour
{
    [Header("Fruit spawning")]
    // 무작위 낙하 대상으로 사용할 체리·귤·사과 프리팹입니다.
    [SerializeField] private GameObject[] fruitPrefabs;
    // 과일이 생성될 위치와 회전입니다. 좌우 입력으로 이동합니다.
    [SerializeField] private Transform dropPoint;
    // 생성된 과일들을 모아둘 부모 Transform입니다.
    [SerializeField] private Transform spawnedFruitParent;
    // 과일을 연속으로 떨어뜨릴 수 있는 최소 시간 간격입니다.
    [SerializeField, Min(0)] private float dropCooldown = .4f;

    [Header("Keyboard aiming")]
    // 낙하 위치의 초당 좌우 이동 거리입니다.
    [SerializeField, Min(0)] private float moveSpeed = 4f;
    // 낙하 위치의 왼쪽 한계를 계산할 바구니 벽입니다.
    [SerializeField] private BoxCollider2D leftWall;
    // 낙하 위치의 오른쪽 한계를 계산할 바구니 벽입니다.
    [SerializeField] private BoxCollider2D rightWall;
    // 낙하 위치를 따라 좌우로 움직이는 안내선입니다.
    [SerializeField] private Transform dropGuide;
    // 과일과 바구니 벽 사이에 확보할 여유 거리입니다.
    [SerializeField, Min(0)] private float wallPadding = .02f;

    [Header("Next fruit display")]
    // 다음에 떨어뜨릴 과일의 몸통 이미지를 표시합니다.
    [SerializeField] private Image nextFruitImage;
    // 다음에 떨어뜨릴 과일의 이름을 표시합니다.
    [SerializeField] private TMP_Text nextFruitName;

    // 다음 낙하가 허용되는 게임 시간입니다. Time.time과 비교합니다.
    private float nextDropTime;
    // 재시작할 때 복원할 낙하 위치입니다. Awake에서 저장합니다.
    private Vector3 initialDropPosition;

    // 다음에 생성할 프리팹입니다. 외부에서는 읽기만 가능합니다.
    public GameObject NextFruitPrefab { get; private set; }

    private void Awake()
    {
        if (dropPoint != null) initialDropPosition = dropPoint.position;
    }

    private void Start()
    {
        if (dropPoint == null || fruitPrefabs == null || fruitPrefabs.Length == 0
            || leftWall == null || rightWall == null)
        {
            Debug.LogError("FruitDropManager에 과일 프리팹, 낙하 위치와 바구니 양쪽 벽을 지정해주세요.", this);
            enabled = false;
            return;
        }

        foreach (var prefab in fruitPrefabs)
        {
            if (prefab == null || prefab.GetComponent<Rigidbody2D>() == null
                || prefab.GetComponent<CircleCollider2D>() == null)
            {
                Debug.LogError("과일 프리팹에는 루트 Rigidbody2D와 CircleCollider2D가 필요합니다.", this);
                enabled = false;
                return;
            }
        }

        SelectNextFruit();
    }

    private void Update()
    {
        if (Time.timeScale <= 0) return;

        float direction = 0;

        // 양쪽을 동시에 누르면 -1 + 1 = 0이 되어 움직이지 않는다.
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) direction -= 1;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) direction += 1;

        if (direction != 0) MoveDropPoint(direction * moveSpeed * Time.deltaTime);

        // GetKeyDown은 누른 순간 한 프레임만 true라서 길게 눌러도 반복 생성되지 않는다.
        if (Input.GetKeyDown(KeyCode.Space)) DropFruit();
    }

    public GameObject DropFruit()
    {
        if (!isActiveAndEnabled || NextFruitPrefab == null || dropPoint == null
            || Time.timeScale <= 0 || Time.time < nextDropTime)
            return null;

        nextDropTime = Time.time + dropCooldown;

        GameObject fruit = Instantiate(NextFruitPrefab, dropPoint.position, dropPoint.rotation, spawnedFruitParent);

        // 제공 코드: 빈칸을 채우기 전에는 아래 물리 설정을 건너뜁니다.
        if (fruit == null) return null;
        fruit.GetComponent<Rigidbody2D>().simulated = true;

        // 낙하에 성공했을 때만 다음 과일을 다시 고른다.
        SelectNextFruit();

        return fruit;
    }

    private void SelectNextFruit()
    {
        NextFruitPrefab = fruitPrefabs[Random.Range(0, fruitPrefabs.Length)];

        // 작은 과일 다음에 큰 과일이 골라져도 벽과 겹치지 않도록 위치를 다시 보정한다.
        ClampDropPosition();

        if (nextFruitImage != null)
        {
            var body = NextFruitPrefab.transform.Find("Body");
            if (body != null)
                nextFruitImage.sprite = body.GetComponent<SpriteRenderer>().sprite;
        }
        if (nextFruitName != null)
        {
            var merge = NextFruitPrefab.GetComponent<FruitMerge>();
            nextFruitName.text = merge != null ? GetDisplayName(merge.Type) : NextFruitPrefab.name;
        }
    }

    // 다음 과일 UI에 표시할 한국어 이름입니다. (한글이 들어간 TMP 폰트 에셋이 필요합니다.)
    public static string GetDisplayName(FruitType type)
    {
        switch (type)
        {
            case FruitType.Cherry: return "체리";
            case FruitType.Mandarin: return "귤";
            case FruitType.Apple: return "사과";
            case FruitType.Melon: return "멜론";
            case FruitType.Watermelon: return "수박";
            default: return type.ToString();
        }
    }

    public void ResetRound()
    {
        nextDropTime = 0;
        if (dropPoint != null) dropPoint.position = initialDropPosition;
        // SelectNextFruit 안의 ClampDropPosition이 가이드 위치도 함께 맞춘다.
        SelectNextFruit();
    }

    public void MoveDropPoint(float distance)
    {
        if (!isActiveAndEnabled || dropPoint == null || Time.timeScale <= 0) return;

        var position = dropPoint.position;
        position.x += distance;
        dropPoint.position = position;

        // 바구니 밖으로 나가지 않게 보정하고 가이드도 함께 이동시킨다.
        ClampDropPosition();
    }

    private void ClampDropPosition()
    {
        if (NextFruitPrefab == null || dropPoint == null || leftWall == null || rightWall == null) return;

        var collider = NextFruitPrefab.GetComponent<CircleCollider2D>();
        float parentScale = spawnedFruitParent != null ? Mathf.Abs(spawnedFruitParent.lossyScale.x) : 1;
        float radius = collider.radius * Mathf.Abs(NextFruitPrefab.transform.localScale.x) * parentScale;
        float minX = leftWall.bounds.max.x + radius + wallPadding;
        float maxX = rightWall.bounds.min.x - radius - wallPadding;
        var position = dropPoint.position;
        position.x = minX <= maxX ? Mathf.Clamp(position.x, minX, maxX) : (minX + maxX) * .5f;
        dropPoint.position = position;

        if (dropGuide != null)
        {
            var guidePosition = dropGuide.position;
            guidePosition.x = position.x;
            dropGuide.position = guidePosition;
        }
    }
}
