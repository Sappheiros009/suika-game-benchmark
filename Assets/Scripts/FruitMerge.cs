using UnityEngine;
using UnityEngine.SceneManagement;

public enum FruitType
{
    Cherry,
    Mandarin,
    Apple,
    Melon,
    Watermelon
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class FruitMerge : MonoBehaviour
{
    // 이 과일의 종류입니다. 같은 종류의 과일끼리만 합체할 수 있습니다.
    [SerializeField] private FruitType fruitType;
    // 합체 결과로 생성할 다음 단계 프리팹입니다. 마지막 단계인 수박은 비워둡니다.
    [SerializeField] private GameObject nextFruitPrefab;
    // 합체로 생성된 과일이 다시 합체할 수 있을 때까지 기다리는 시간입니다.
    [SerializeField, Min(0)] private float chainMergeDelay = .2f;

    // 이 과일의 Rigidbody2D입니다. 합체 결과에 전달할 속도를 구할 때 사용합니다.
    private Rigidbody2D body;
    // 연쇄 합체까지 남은 대기 시간입니다. FixedUpdate에서 줄이며 0이면 합체할 수 있습니다.
    private float remainingMergeDelay;

    // 외부에서 읽을 수 있는 현재 과일 종류입니다. 합체 점수를 계산할 때 사용합니다.
    public FruitType Type => fruitType;
    // 외부에서 읽을 수 있는 다음 단계 프리팹입니다.
    public GameObject NextFruitPrefab => nextFruitPrefab;
    // 이미 합체에 사용 중인지 나타냅니다. 외부에서는 읽기만 가능하며 중복 합체를 막습니다.
    public bool IsMerging { get; private set; }

    // 합체 결과 과일이 생성될 때 알립니다. 효과음과 합체 이펙트에 사용합니다.
    public static event System.Action<FruitMerge> Merged;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (IsMerging || remainingMergeDelay <= 0) return;

        remainingMergeDelay = Mathf.Max(0, remainingMergeDelay - Time.fixedDeltaTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Collision2D.collider는 상대 Collider입니다. 벽·바닥에는 FruitMerge가 없어 null이 전달됩니다.
        TryMerge(collision.collider.GetComponent<FruitMerge>());
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // 접촉이 유지되는 동안 매 물리 단계 호출되므로, 연쇄 대기가 끝난 뒤에도 합체할 수 있습니다.
        TryMerge(collision.collider.GetComponent<FruitMerge>());
    }

    private void TryMerge(FruitMerge other)
    {
        if (!isActiveAndEnabled || IsMerging || nextFruitPrefab == null
            || other == null || other == this || !other.isActiveAndEnabled || other.IsMerging
            || fruitType != other.fruitType || nextFruitPrefab != other.nextFruitPrefab
            || remainingMergeDelay > 0 || other.remainingMergeDelay > 0)
            return;

        // 다른 충돌 콜백에서 같은 과일을 중복으로 합체하지 못하게 먼저 표시합니다.
        IsMerging = true;
        other.IsMerging = true;

        var otherBody = other.body != null ? other.body : other.GetComponent<Rigidbody2D>();
        Vector3 position = (transform.position + other.transform.position) * .5f;
        Vector2 linearVelocity = (body.linearVelocity + otherBody.linearVelocity) * .5f;
        float angularVelocity = (body.angularVelocity + otherBody.angularVelocity) * .5f;
        Transform parent = transform.parent;

        // 제공 코드: 생성한 과일을 현재 과일과 같은 Scene에 배치하기 위해 보관합니다.
        Scene scene = gameObject.scene;

        // Destroy는 프레임 끝에 처리되므로, 비활성화로 두 원본을 충돌 계산에서 즉시 뺍니다.
        gameObject.SetActive(false);
        other.gameObject.SetActive(false);

        GameObject merged = Instantiate(nextFruitPrefab, position, Quaternion.identity, parent);

        // 제공 코드: 빈칸을 채우기 전에도 아래 코드에서 NullReferenceException이 나지 않게 합니다.
        if (merged == null) return;

        // 제공 코드: 생성한 과일을 원본과 같은 Scene에 배치합니다.
        if (merged.scene != scene)
            SceneManager.MoveGameObjectToScene(merged, scene);

        // 제공 코드: 합체 결과로 생긴 과일만 연쇄 합체 대기 시간을 적용합니다.
        var mergedFruit = merged.GetComponent<FruitMerge>();
        mergedFruit.remainingMergeDelay = mergedFruit.chainMergeDelay;
        var mergedBody = merged.GetComponent<Rigidbody2D>();
        mergedBody.simulated = true;

        // 두 원본의 평균 속도를 넘겨 합체 직후에도 자연스럽게 떨어지거나 구르게 합니다.
        mergedBody.linearVelocity = linearVelocity;
        mergedBody.angularVelocity = angularVelocity;

        Destroy(gameObject);
        Destroy(other.gameObject);

        // 합체 한 번에 결과 과일 기준 점수를 한 번만 더합니다.
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddMergeScore(mergedFruit.Type);

        Merged?.Invoke(mergedFruit);
    }
}
