using System.Collections.Generic;
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
    [SerializeField] private FruitType fruitType;
    [SerializeField] private GameObject nextFruitPrefab;
    [SerializeField, Min(0)] private float chainMergeDelay = .2f;

    private Rigidbody2D body;
    private CircleCollider2D fruitCollider;
    private float remainingMergeDelay;
    private readonly List<ContactPoint2D> contacts = new List<ContactPoint2D>(8);

    public FruitType Type => fruitType;
    public GameObject NextFruitPrefab => nextFruitPrefab;
    public bool IsMerging { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        fruitCollider = GetComponent<CircleCollider2D>();
    }

    private void FixedUpdate()
    {
        if (IsMerging || remainingMergeDelay <= 0) return;

        remainingMergeDelay = Mathf.Max(0, remainingMergeDelay - Time.fixedDeltaTime);
        if (remainingMergeDelay > 0) return;

        // 대기 중에 물리적으로 잠들어도, 대기 종료 시 현재 접촉을 다시 확인한다.
        int count = fruitCollider.GetContacts(contacts);
        for (int i = 0; i < count && !IsMerging; i++)
        {
            var contact = contacts[i];
            var other = contact.collider == fruitCollider ? contact.otherCollider : contact.collider;
            if (other != null) TryMerge(other.GetComponentInParent<FruitMerge>());
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryMerge(collision.collider.GetComponentInParent<FruitMerge>());
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryMerge(collision.collider.GetComponentInParent<FruitMerge>());
    }

    private void TryMerge(FruitMerge other)
    {
        if (!isActiveAndEnabled || IsMerging || nextFruitPrefab == null
            || other == null || other == this || !other.isActiveAndEnabled || other.IsMerging
            || fruitType != other.fruitType || nextFruitPrefab != other.nextFruitPrefab
            || remainingMergeDelay > 0 || other.remainingMergeDelay > 0)
            return;

        // 양쪽 충돌 콜백과 제3의 과일이 같은 두 과일을 중복 소비하지 못하게 한다.
        IsMerging = true;
        other.IsMerging = true;

        Vector3 position = (transform.position + other.transform.position) * .5f;
        Vector2 velocity = (body.linearVelocity + other.body.linearVelocity) * .5f;
        float angularVelocity = (body.angularVelocity + other.body.angularVelocity) * .5f;
        Transform parent = transform.parent;
        var scene = gameObject.scene;

        // Destroy는 프레임 끝에 처리되므로 원본은 즉시 숨기고 충돌에서 제외한다.
        gameObject.SetActive(false);
        other.gameObject.SetActive(false);

        var merged = Instantiate(nextFruitPrefab, position, Quaternion.identity, parent);
        if (merged.scene != scene)
            SceneManager.MoveGameObjectToScene(merged, scene);
        // 일반 낙하 과일은 즉시 합체하고, 합체 결과로 생긴 과일만 잠시 대기한다.
        var mergedFruit = merged.GetComponent<FruitMerge>();
        mergedFruit.remainingMergeDelay = mergedFruit.chainMergeDelay;
        var mergedBody = merged.GetComponent<Rigidbody2D>();
        mergedBody.simulated = true;
        mergedBody.linearVelocity = velocity;
        mergedBody.angularVelocity = angularVelocity;

        Destroy(gameObject);
        Destroy(other.gameObject);
        // 실제로 생성된 합체 결과의 보상을 한 번만 더한다.
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddMergeScore(mergedFruit.Type);
    }
}
