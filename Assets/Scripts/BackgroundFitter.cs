using UnityEngine;

// 배경 이미지를 화면에 맞춥니다. 스프라이트가 비어 있으면 카메라 배경색만 보입니다.
// Sprite에 이미지를 넣으면 화면 비율과 상관없이 화면 전체를 덮도록 크기를 자동으로 맞춥니다.
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class BackgroundFitter : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    private SpriteRenderer spriteRenderer;

    private void OnEnable()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        Fit();
    }

    private void LateUpdate()
    {
        Fit();
    }

    private void Fit()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;
        var cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null || !cam.orthographic) return;

        float viewHeight = cam.orthographicSize * 2f;
        float viewWidth = viewHeight * cam.aspect;
        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
        if (spriteSize.x <= 0 || spriteSize.y <= 0) return;

        // 긴 쪽을 기준으로 덮어서 빈 가장자리가 생기지 않게 합니다.
        float scale = Mathf.Max(viewWidth / spriteSize.x, viewHeight / spriteSize.y);
        transform.localScale = new Vector3(scale, scale, 1f);

        var position = cam.transform.position;
        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }
}
