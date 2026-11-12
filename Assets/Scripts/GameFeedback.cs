using System.Collections;
using UnityEngine;

// 효과음과 시각 연출을 한곳에서 담당합니다.
// 효과음과 이펙트 텍스처는 Kenney의 CC0 에셋(Assets/ThirdParty/Kenney)을 사용합니다.
// 게임 규칙(합체·점수·게임오버 판정)에는 관여하지 않고 이벤트를 듣기만 합니다.
[DisallowMultipleComponent]
public sealed class GameFeedback : MonoBehaviour
{
    public static GameFeedback Instance { get; private set; }

    [Header("Sources")]
    [SerializeField] private FruitDropManager dropManager;
    [SerializeField] private GameOverManager gameOverManager;
    // 겹쳐 재생해도 서로 음높이를 바꾸지 않도록 여러 AudioSource를 돌려 씁니다.
    [SerializeField] private AudioSource[] voices;

    [Header("Kenney CC0 sounds")]
    [SerializeField] private AudioClip dropClip;
    [SerializeField] private AudioClip[] landClips;
    [SerializeField] private AudioClip mergeClip;
    [SerializeField] private AudioClip melonClip;
    [SerializeField] private AudioClip watermelonClip;
    [SerializeField] private AudioClip gameOverClip;
    [SerializeField] private AudioClip newBestClip;
    [SerializeField] private AudioClip restartClip;
    [SerializeField, Range(0, 1)] private float volume = .8f;

    [Header("Kenney CC0 particle effects")]
    // 합체 지점에서 반짝이는 별 입자입니다.
    [SerializeField] private ParticleSystem mergeSparkle;
    // 합체 결과 크기만큼 퍼지는 빛 고리입니다.
    [SerializeField] private ParticleSystem mergeRing;
    // 수박이 만들어졌을 때만 추가로 터지는 큰 효과입니다.
    [SerializeField] private ParticleSystem watermelonBurst;

    [Header("UI motion")]
    [SerializeField] private RectTransform scoreLabel;
    [SerializeField] private RectTransform gameOverCard;
    [SerializeField] private GameObject newBestBadge;

    private int voiceIndex;
    private float lastLandTime = -1;
    private int lastScore;
    private int bestAtRoundStart;
    private Coroutine scorePunch;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        FruitMerge.Merged += OnMerged;
        if (dropManager != null) dropManager.FruitDropped += OnDropped;
        if (gameOverManager != null)
        {
            gameOverManager.GameEnded += OnGameEnded;
            gameOverManager.Restarted += OnRestarted;
        }
    }

    private void OnDisable()
    {
        FruitMerge.Merged -= OnMerged;
        if (dropManager != null) dropManager.FruitDropped -= OnDropped;
        if (gameOverManager != null)
        {
            gameOverManager.GameEnded -= OnGameEnded;
            gameOverManager.Restarted -= OnRestarted;
        }
    }

    private void Start()
    {
        if (newBestBadge != null) newBestBadge.SetActive(false);
        bestAtRoundStart = ScoreManager.Instance != null ? ScoreManager.Instance.BestScore : 0;
    }

    private void LateUpdate()
    {
        // ScoreManager는 완성 코드라 이벤트가 없으므로 점수 변화를 관찰해 반응합니다.
        int score = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
        if (score > lastScore && scoreLabel != null)
        {
            if (scorePunch != null) StopCoroutine(scorePunch);
            scorePunch = StartCoroutine(Punch(scoreLabel, 1.25f, .22f));
        }
        lastScore = score;
    }

    public void PlayLanding(float intensity)
    {
        // 과일이 여러 개 동시에 닿아도 소리가 뭉개지지 않게 최소 간격을 둡니다.
        if (Time.unscaledTime - lastLandTime < .06f || landClips == null || landClips.Length == 0) return;
        lastLandTime = Time.unscaledTime;
        Play(landClips[Random.Range(0, landClips.Length)], Mathf.Lerp(.35f, 1f, intensity), Random.Range(.92f, 1.08f));
    }

    private void OnDropped(GameObject fruit)
    {
        Play(dropClip, .6f, Random.Range(.95f, 1.05f));
    }

    private void OnMerged(FruitMerge fruit)
    {
        if (fruit == null) return;

        // 단계가 높을수록 낮고 묵직한 소리를 냅니다.
        switch (fruit.Type)
        {
            case FruitType.Mandarin: Play(mergeClip, .9f, 1.25f); break;
            case FruitType.Apple: Play(mergeClip, .95f, 1.05f); break;
            case FruitType.Melon: Play(melonClip, 1f, 1f); break;
            case FruitType.Watermelon: Play(watermelonClip, 1f, 1f); break;
        }

        float radius = GetWorldRadius(fruit);
        Vector3 position = fruit.transform.position;

        Emit(mergeSparkle, position, radius, 6 + (int)fruit.Type * 3, radius * .55f);
        Emit(mergeRing, position, radius, 1, radius * 2.6f);
        if (fruit.Type == FruitType.Watermelon) Emit(watermelonBurst, position, radius, 24, radius * .8f);

        StartCoroutine(Pop(fruit.transform));
    }

    private void OnGameEnded(int finalScore)
    {
        Play(gameOverClip, 1f, 1f);

        // 이번 판에서 최고 기록을 넘었고, 그 값이 현재 최고 기록 자체일 때만 배지를 띄웁니다.
        int best = ScoreManager.Instance != null ? ScoreManager.Instance.BestScore : finalScore;
        bool newBest = finalScore > 0 && finalScore > bestAtRoundStart && finalScore >= best;
        if (newBestBadge != null) newBestBadge.SetActive(newBest);
        if (newBest) StartCoroutine(PlayDelayed(newBestClip, .45f));

        // 게임은 Time.timeScale = 0으로 멈춰 있으므로 UI 연출은 실제 시간으로 진행합니다.
        if (gameOverCard != null) StartCoroutine(CardIn(gameOverCard));
    }

    private void OnRestarted()
    {
        Play(restartClip, .9f, 1f);
        if (newBestBadge != null) newBestBadge.SetActive(false);
        bestAtRoundStart = ScoreManager.Instance != null ? ScoreManager.Instance.BestScore : 0;
        lastScore = 0;

        // 멈춰 있던 이전 판의 입자를 지워서 새 판에 남지 않게 합니다.
        if (mergeSparkle != null) mergeSparkle.Clear();
        if (mergeRing != null) mergeRing.Clear();
        if (watermelonBurst != null) watermelonBurst.Clear();
    }

    private void Play(AudioClip clip, float clipVolume, float pitch)
    {
        if (clip == null || voices == null || voices.Length == 0) return;
        var source = voices[voiceIndex];
        voiceIndex = (voiceIndex + 1) % voices.Length;
        if (source == null) return;
        source.pitch = pitch;
        source.PlayOneShot(clip, clipVolume * volume);
    }

    private IEnumerator PlayDelayed(AudioClip clip, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        Play(clip, 1f, 1f);
    }

    private static float GetWorldRadius(FruitMerge fruit)
    {
        var circle = fruit.GetComponent<CircleCollider2D>();
        return circle != null ? circle.radius * Mathf.Abs(fruit.transform.lossyScale.x) : .4f;
    }

    private static void Emit(ParticleSystem system, Vector3 position, float radius, int count, float size)
    {
        if (system == null) return;
        var shape = system.shape;
        shape.radius = Mathf.Max(.01f, radius * .6f);

        var emitParams = new ParticleSystem.EmitParams
        {
            position = position,
            applyShapeToPosition = true,
            startSize = size
        };
        system.Emit(emitParams, count);
    }

    // 합체 결과가 "톡" 하고 부풀어 나타나는 연출입니다.
    // 크기 변화는 외부 에셋으로 대신할 수 없어 코드로 처리하고, 물리 크기에 영향을 주지 않도록
    // Collider가 있는 루트가 아닌 Body와 Face만 키웁니다.
    private static IEnumerator Pop(Transform fruit)
    {
        var body = fruit.Find("Body");
        var face = fruit.Find("Face");
        Vector3 bodyScale = body != null ? body.localScale : Vector3.one;
        Vector3 faceScale = face != null ? face.localScale : Vector3.one;

        const float duration = .2f;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            if (fruit == null) yield break;
            float k = EaseOutBack(Mathf.Clamp01(t / duration), .7f);
            float s = Mathf.LerpUnclamped(.65f, 1f, k);
            if (body != null) body.localScale = bodyScale * s;
            if (face != null) face.localScale = faceScale * s;
            yield return null;
        }
        if (fruit == null) yield break;
        if (body != null) body.localScale = bodyScale;
        if (face != null) face.localScale = faceScale;
    }

    private static IEnumerator Punch(RectTransform target, float peak, float duration)
    {
        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
            target.localScale = Vector3.one * Mathf.Lerp(1f, peak, k);
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    private static IEnumerator CardIn(RectTransform card)
    {
        const float duration = .32f;
        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = EaseOutBack(Mathf.Clamp01(t / duration), 1.4f);
            card.localScale = Vector3.one * Mathf.LerpUnclamped(.6f, 1f, k);
            yield return null;
        }
        card.localScale = Vector3.one;
    }

    private static float EaseOutBack(float x, float overshoot)
    {
        float c3 = overshoot + 1f;
        float p = x - 1f;
        return 1f + c3 * p * p * p + overshoot * p * p;
    }
}
