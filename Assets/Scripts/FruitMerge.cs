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
        // TODO: 충돌 상대 Collider에서 FruitMerge를 찾아 TryMerge에 전달하세요.
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // TODO: 계속 닿아 있는 상대의 FruitMerge를 찾아 TryMerge에 전달하세요.
        // 대기 시간 중 합체하지 못한 과일도 대기가 끝나면 합체할 수 있어야 합니다.
    }

    private void TryMerge(FruitMerge other)
    {
        if (!isActiveAndEnabled || IsMerging || nextFruitPrefab == null
            || other == null || other == this || !other.isActiveAndEnabled || other.IsMerging
            || fruitType != other.fruitType || nextFruitPrefab != other.nextFruitPrefab
            || remainingMergeDelay > 0 || other.remainingMergeDelay > 0)
            return;

        // TODO 1: 자신과 상대의 IsMerging을 true로 설정하세요.
        // 다른 충돌 콜백에서 같은 과일을 중복으로 합체하지 못하게 합니다.

        // TODO 2: 두 과일 위치의 중간 지점을 계산하세요.
        // 두 Rigidbody2D의 linearVelocity와 angularVelocity도 각각 평균을 구하세요.
        // 생성할 과일이 사용할 부모 Transform을 보관하세요.
        // Vector3 position = _____;
        // Vector2 linearVelocity = _____;
        // float angularVelocity = _____;
        // Transform parent = _____;

        // 제공 코드: 생성한 과일을 현재 과일과 같은 Scene에 배치하기 위해 보관합니다.
        Scene scene = gameObject.scene;

        // TODO 3: 자신과 상대 GameObject를 비활성화하세요.
        // Destroy는 프레임 끝에 처리되므로 충돌에서 먼저 제외해야 합니다.

        // TODO 4: Instantiate를 사용해 nextFruitPrefab을 중간 위치에 Quaternion.identity로 생성하세요.
        // 자신의 부모 아래에 생성하고, 생성한 오브젝트를 merged에 대입하세요.
        GameObject merged = null;

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

        // TODO 6: 생성한 Rigidbody2D의 linearVelocity와 angularVelocity에
        // 앞에서 계산한 두 과일의 평균 속도와 평균 각속도를 적용하세요.

        // TODO 7: 자신과 상대 GameObject를 Destroy로 제거하세요.

        // TODO 8: ScoreManager.Instance가 있으면 AddMergeScore를 호출하세요.
        // 생성된 과일의 Type을 전달하여 합체 결과 점수를 한 번만 더하세요.
    }
}
