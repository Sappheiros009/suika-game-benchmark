using UnityEngine;

// 대체과제 A: 다음에 떨어질 과일의 몸통을 낙하 위치에 반투명하게 미리 보여줍니다.
// SpriteRenderer만 사용하고 Collider·Rigidbody2D·FruitMerge가 없으므로
// 물리 충돌, 합체, 점수, 게임오버 판정에 참여하지 않습니다.
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class DropPreview : MonoBehaviour
{
    // 다음 과일 선택과 낙하 위치를 알려줄 매니저입니다.
    [SerializeField] private FruitDropManager dropManager;
    // 게임오버 시 숨기고 재시작 시 다시 보이게 할 때 사용합니다.
    [SerializeField] private GameOverManager gameOverManager;
    // 실제 과일과 구분되도록 적용할 투명도입니다.
    [SerializeField, Range(0, 1)] private float alpha = .45f;

    private SpriteRenderer previewRenderer;

    private void Awake()
    {
        previewRenderer = GetComponent<SpriteRenderer>();
        previewRenderer.enabled = false;
    }

    private void OnEnable()
    {
        if (dropManager != null) dropManager.NextFruitChanged += Show;
        if (gameOverManager != null)
        {
            gameOverManager.GameEnded += Hide;
            gameOverManager.Restarted += Refresh;
        }
    }

    private void OnDisable()
    {
        if (dropManager != null) dropManager.NextFruitChanged -= Show;
        if (gameOverManager != null)
        {
            gameOverManager.GameEnded -= Hide;
            gameOverManager.Restarted -= Refresh;
        }
    }

    private void Start()
    {
        // FruitDropManager.Start가 먼저 선택했을 수 있으므로 현재 선택으로 한 번 맞춥니다.
        Refresh();
    }

    private void LateUpdate()
    {
        // 낙하 위치의 자식이 아니어도 좌우 이동을 그대로 따라갑니다.
        if (dropManager != null && dropManager.DropPoint != null && previewRenderer.enabled)
            transform.position = dropManager.DropPoint.position + bodyOffset;
    }

    private Vector3 bodyOffset;

    private void Refresh()
    {
        if (dropManager != null && dropManager.NextFruitPrefab != null) Show(dropManager.NextFruitPrefab);
    }

    private void Show(GameObject prefab)
    {
        if (prefab == null) return;
        if (gameOverManager != null && gameOverManager.IsGameOver) return;

        var body = prefab.transform.Find("Body");
        var bodyRenderer = body != null ? body.GetComponent<SpriteRenderer>() : null;
        if (bodyRenderer == null) return;

        // 실제 과일과 같은 크기가 되도록 루트 배율과 몸통 배율을 곱합니다.
        Vector3 rootScale = prefab.transform.localScale;
        transform.localScale = Vector3.Scale(rootScale, body.localScale);
        // 몸통이 루트 중심에서 살짝 비켜 있으므로 같은 만큼 옮겨서 겹쳐 보이게 합니다.
        bodyOffset = Vector3.Scale(rootScale, body.localPosition);

        previewRenderer.sprite = bodyRenderer.sprite;
        var color = bodyRenderer.color;
        color.a = alpha;
        previewRenderer.color = color;
        previewRenderer.enabled = true;

        if (dropManager != null && dropManager.DropPoint != null)
            transform.position = dropManager.DropPoint.position + bodyOffset;
    }

    private void Hide(int finalScore)
    {
        previewRenderer.enabled = false;
    }
}
