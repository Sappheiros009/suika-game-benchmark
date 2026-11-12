using UnityEngine;

// 과일이 처음 바닥이나 다른 과일에 떨어졌을 때 한 번만 Kenney CC0 충돌음을 냅니다.
// 소리만 재생하며 물리나 합체 판정에는 영향을 주지 않습니다.
[DisallowMultipleComponent]
public sealed class FruitImpactSound : MonoBehaviour
{
    // 이 속도보다 약하게 닿으면 소리를 내지 않습니다. 쌓여 있는 과일끼리 비비는 소리를 막습니다.
    [SerializeField, Min(0)] private float minImpactSpeed = 1.2f;
    // 이 속도 이상이면 가장 큰 소리로 냅니다.
    [SerializeField, Min(0)] private float maxImpactSpeed = 8f;

    private bool landed;

    private void OnEnable()
    {
        landed = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (landed) return;
        landed = true;

        float speed = collision.relativeVelocity.magnitude;
        if (speed < minImpactSpeed || GameFeedback.Instance == null) return;
        GameFeedback.Instance.PlayLanding(Mathf.InverseLerp(minImpactSpeed, maxImpactSpeed, speed));
    }
}
