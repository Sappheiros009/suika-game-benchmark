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
    // 마지막으로 읽은 마우스 화면 좌표입니다. 마우스가 실제로 움직였을 때만 낙하 위치를 바꾸는 데 씁니다.
    private Vector3 lastMousePosition;

    // 다음에 생성할 프리팹입니다. 외부에서는 읽기만 가능합니다.
    public GameObject NextFruitPrefab { get; private set; }

    // 낙하 위치 Transform입니다. 미리보기처럼 낙하 위치를 따라가야 하는 연출에서 읽습니다.
    public Transform DropPoint => dropPoint;

    // 과일 하나가 실제로 생성되어 떨어지기 시작할 때 알립니다. 효과음 재생에 사용합니다.
    public event System.Action<GameObject> FruitDropped;
    // 다음 과일 선택이 바뀔 때 알립니다. 낙하 위치 미리보기 갱신에 사용합니다.
    public event System.Action<GameObject> NextFruitChanged;

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
        // 시작 직후 커서 위치로 튀지 않게, 지금 위치를 기준으로 잡고 다음 움직임부터 따라갑니다.
        lastMousePosition = Input.mousePosition;
    }

    private void Update()
    {
        if (Time.timeScale <= 0) return;

        float direction = 0;

        // 왼쪽과 오른쪽을 동시에 누르면 -1과 +1이 상쇄되어 이동하지 않습니다.
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) direction -= 1;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) direction += 1;

        // 누르고 있는 동안만 이동하고, 키를 놓으면 direction이 0이 되어 멈춥니다.
        if (direction != 0) MoveDropPoint(direction * moveSpeed * Time.deltaTime);

        // 마우스는 실제로 움직였을 때만 낙하 위치를 커서 x로 바로 옮깁니다.
        // 가만히 있는 마우스가 키보드 이동을 덮어쓰지 않으므로 키보드 조작은 그대로 유지됩니다.
        Vector3 mouse = Input.mousePosition;
        if ((mouse - lastMousePosition).sqrMagnitude > .01f)
        {
            lastMousePosition = mouse;
            FollowMouse(mouse);
        }

        // GetKeyDown·GetMouseButtonDown은 누른 순간 한 프레임만 true이므로 길게 눌러도 반복 생성되지 않습니다.
        // 좌클릭도 Space와 같은 DropFruit을 거치므로 0.4초 간격이 똑같이 적용됩니다.
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) DropFruit();
    }

    private void FollowMouse(Vector3 screenPosition)
    {
        var cam = Camera.main;
        if (cam == null || dropPoint == null) return;

        // 화면 좌표를 낙하 높이 평면의 월드 좌표로 바꾼 뒤, x 차이만큼 옮깁니다. 바구니 안 제한과 가이드 이동은 MoveDropPoint가 처리합니다.
        screenPosition.z = dropPoint.position.z - cam.transform.position.z;
        float targetX = cam.ScreenToWorldPoint(screenPosition).x;
        MoveDropPoint(targetX - dropPoint.position.x);
    }

    public GameObject DropFruit()
    {
        if (!isActiveAndEnabled || NextFruitPrefab == null || dropPoint == null
            || Time.timeScale <= 0 || Time.time < nextDropTime)
            return null;

        nextDropTime = Time.time + dropCooldown;

        // 낙하 위치와 회전에서 생성하고, 게임오버 판정과 재시작 정리를 위해 과일 부모 아래에 둡니다.
        GameObject fruit = Instantiate(NextFruitPrefab, dropPoint.position, dropPoint.rotation, spawnedFruitParent);

        // 제공 코드: 빈칸을 채우기 전에는 아래 물리 설정을 건너뜁니다.
        if (fruit == null) return null;
        fruit.GetComponent<Rigidbody2D>().simulated = true;

        // 낙하에 성공했으므로 다음 과일을 다시 고릅니다.
        SelectNextFruit();
        FruitDropped?.Invoke(fruit);

        return fruit;
    }

    private void SelectNextFruit()
    {
        if (fruitPrefabs == null || fruitPrefabs.Length == 0) return;

        // 무작위 후보(체리·귤·사과) 중 하나를 고릅니다. 같은 과일이 연속으로 나와도 됩니다.
        NextFruitPrefab = fruitPrefabs[Random.Range(0, fruitPrefabs.Length)];

        // 작은 과일 다음에 큰 과일이 골라져도 벽과 겹치지 않도록 낙하 위치를 다시 보정합니다.
        ClampDropPosition();

        if (nextFruitImage != null)
        {
            var body = NextFruitPrefab.transform.Find("Body");
            if (body != null)
            {
                nextFruitImage.sprite = body.GetComponent<SpriteRenderer>().sprite;
                nextFruitImage.enabled = nextFruitImage.sprite != null;
            }
        }
        if (nextFruitName != null)
            nextFruitName.text = NextFruitPrefab.name.ToUpperInvariant();

        NextFruitChanged?.Invoke(NextFruitPrefab);
    }

    public void ResetRound()
    {
        // 재시작 직후 바로 낙하할 수 있도록 대기 시간을 지웁니다.
        nextDropTime = 0;

        if (dropPoint != null) dropPoint.position = initialDropPosition;
        // 게임오버 중에 움직인 마우스 때문에 재시작 첫 프레임에 커서 쪽으로 튀지 않게 기준점을 다시 잡습니다.
        lastMousePosition = Input.mousePosition;

        // 다음 과일을 다시 고르면 ClampDropPosition이 가이드도 시작 위치로 맞춥니다.
        SelectNextFruit();
    }

    public void MoveDropPoint(float distance)
    {
        if (!isActiveAndEnabled || dropPoint == null || Time.timeScale <= 0) return;

        // x만 바꾸므로 이동 중에도 낙하 높이(y)는 유지됩니다.
        var position = dropPoint.position;
        position.x += distance;
        dropPoint.position = position;

        // 바구니 안으로 제한하고 가이드를 같은 x로 옮깁니다.
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
