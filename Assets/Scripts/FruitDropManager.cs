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

        // TODO 1: Input.GetKey로 direction을 설정하세요.

        // TODO 2: 낙하 위치를 이동하세요.
        // 이동 거리는 direction, moveSpeed, Time.deltaTime을 곱해 계산하세요.

        // TODO 3: Input.GetKeyDown으로 Space를 누른 순간을 확인하고 DropFruit를 호출하세요.
        // 키를 계속 누르고 있어도 과일이 반복 생성되지 않아야 합니다.
    }

    public GameObject DropFruit()
    {
        if (!isActiveAndEnabled || NextFruitPrefab == null || dropPoint == null
            || Time.timeScale <= 0 || Time.time < nextDropTime)
            return null;

        nextDropTime = Time.time + dropCooldown;

        // TODO: dropPoint를 이용해 과일을 생성하세요.
        GameObject fruit = null;

        // 제공 코드: 빈칸을 채우기 전에는 아래 물리 설정을 건너뜁니다.
        if (fruit == null) return null;
        fruit.GetComponent<Rigidbody2D>().simulated = true;

        return fruit;
    }

    private void SelectNextFruit()
    {
        // TODO: fruitPrefabs에서 무작위로 과일을 선택해 NextFruitPrefab에 저장하고,
        // ClampDropPosition을 호출해 낙하 위치를 보정하세요. 다음 과일 UI도 갱신하세요.

        // 이미지와 이름 설정 예시:
        // if (nextFruitImage != null)
        // {
        //     var body = NextFruitPrefab.transform.Find("Body");
        //     if (body != null)
        //         nextFruitImage.sprite = body.GetComponent<SpriteRenderer>().sprite;
        // }
        // if (nextFruitName != null)
        //     nextFruitName.text = NextFruitPrefab.name.ToUpperInvariant();
    }

    public void ResetRound()
    {
        // TODO: 낙하 대기 시간을 초기화하고, dropPoint를 initialDropPosition으로 되돌리세요.
        // SelectNextFruit를 호출해 다음 과일을 다시 선택하세요.
    }

    public void MoveDropPoint(float distance)
    {
        if (!isActiveAndEnabled || dropPoint == null || Time.timeScale <= 0) return;

        // TODO: dropPoint의 현재 위치에서 x 좌표에 distance를 더하고, 변경한 위치를 적용하세요.
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
