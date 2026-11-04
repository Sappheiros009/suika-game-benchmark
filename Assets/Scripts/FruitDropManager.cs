using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public sealed class FruitDropManager : MonoBehaviour
{
    [Header("Fruit spawning")]
    [SerializeField] private GameObject[] fruitPrefabs;
    [SerializeField] private Transform dropPoint;
    [SerializeField] private Transform spawnedFruitParent;
    [SerializeField, Min(0)] private float dropCooldown = .4f;

    [Header("Keyboard aiming")]
    [SerializeField, Min(0)] private float moveSpeed = 4f;
    [SerializeField] private BoxCollider2D leftWall;
    [SerializeField] private BoxCollider2D rightWall;
    [SerializeField] private Transform dropGuide;
    [SerializeField, Min(0)] private float wallPadding = .02f;

    [Header("Next fruit display")]
    [SerializeField] private Image nextFruitImage;
    [SerializeField] private TMP_Text nextFruitName;

    private float nextDropTime;
    private Vector3 initialDropPosition;

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
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) direction -= 1;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) direction += 1;
        MoveDropPoint(direction * moveSpeed * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.Space))
            DropFruit();
    }

    public GameObject DropFruit()
    {
        if (!isActiveAndEnabled || NextFruitPrefab == null || dropPoint == null
            || Time.timeScale <= 0 || Time.time < nextDropTime)
            return null;

        nextDropTime = Time.time + dropCooldown;
        ClampDropPosition();
        var fruit = Instantiate(NextFruitPrefab, dropPoint.position, dropPoint.rotation, spawnedFruitParent);
        fruit.GetComponent<Rigidbody2D>().simulated = true;
        SelectNextFruit();
        return fruit;
    }

    private void SelectNextFruit()
    {
        int index = Random.Range(0, fruitPrefabs.Length);
        NextFruitPrefab = fruitPrefabs[index];
        ClampDropPosition();

        if (nextFruitImage != null)
        {
            var body = NextFruitPrefab.transform.Find("Body");
            if (body != null)
                nextFruitImage.sprite = body.GetComponent<SpriteRenderer>().sprite;
        }
        if (nextFruitName != null)
            nextFruitName.text = NextFruitPrefab.name.ToUpperInvariant();
    }

    public void ResetRound()
    {
        nextDropTime = 0;
        dropPoint.position = initialDropPosition;
        SelectNextFruit();
    }

    public void MoveDropPoint(float distance)
    {
        if (!isActiveAndEnabled || dropPoint == null || Time.timeScale <= 0) return;

        var position = dropPoint.position;
        position.x += distance;
        dropPoint.position = position;
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
