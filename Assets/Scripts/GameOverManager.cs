using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(FruitDropManager))]
public sealed class GameOverManager : MonoBehaviour
{
    [SerializeField] private FruitDropManager dropManager;
    [SerializeField] private Transform spawnedFruitParent;
    [SerializeField] private Transform heightLimit;
    [SerializeField] private Transform dropGuide;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField, Min(0)] private float spawnGracePeriod = 1f;
    [SerializeField, Min(0)] private float overflowDuration = 1f;

    private sealed class FruitState
    {
        public Collider2D collider;
        public Rigidbody2D body;
        public float age;
        public float overflowTime;
    }

    private readonly Dictionary<FruitMerge, FruitState> states = new Dictionary<FruitMerge, FruitState>();
    private readonly List<FruitMerge> fruits = new List<FruitMerge>();
    private readonly List<FruitMerge> removedFruits = new List<FruitMerge>();
    private float previousTimeScale = 1f;

    public bool IsGameOver { get; private set; }

    private void Start()
    {
        if (dropManager == null || spawnedFruitParent == null || heightLimit == null)
        {
            Debug.LogError("GameOverManager에 낙하 매니저, 과일 부모와 높이 제한선을 지정해주세요.", this);
            enabled = false;
            return;
        }
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (IsGameOver || Time.timeScale <= 0 || spawnedFruitParent == null || heightLimit == null) return;

        spawnedFruitParent.GetComponentsInChildren(false, fruits);
        foreach (var fruit in fruits)
        {
            if (!states.TryGetValue(fruit, out var state))
            {
                state = new FruitState
                {
                    collider = fruit.GetComponent<Collider2D>(),
                    body = fruit.GetComponent<Rigidbody2D>()
                };
                states.Add(fruit, state);
            }
            if (fruit.IsMerging || state.collider == null || !state.collider.enabled
                || state.collider.isTrigger || state.body == null || !state.body.simulated)
            {
                state.overflowTime = 0;
                continue;
            }

            state.age += Time.fixedDeltaTime;
            // 낙하 위치가 선 위에 있으므로 새 과일이 내려올 시간을 준다.
            if (state.age < spawnGracePeriod)
            {
                state.overflowTime = 0;
                continue;
            }

            // 과일이 선 전체를 넘어가도 감지하도록 Trigger 겹침 대신 윗면 높이를 확인한다.
            if (state.collider.bounds.max.y > heightLimit.position.y + .01f)
            {
                state.overflowTime += Time.fixedDeltaTime;
                if (state.overflowTime >= overflowDuration)
                {
                    EndGame();
                    return;
                }
            }
            else state.overflowTime = 0;
        }

        removedFruits.Clear();
        foreach (var pair in states)
            if (pair.Key == null || !fruits.Contains(pair.Key)) removedFruits.Add(pair.Key);
        foreach (var fruit in removedFruits) states.Remove(fruit);
    }

    private void Update()
    {
        if (IsGameOver && Input.GetKeyDown(KeyCode.R))
            RestartGame();
    }

    private void EndGame()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        dropManager.enabled = false;
        if (dropGuide != null) dropGuide.gameObject.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        previousTimeScale = Time.timeScale > 0 ? Time.timeScale : 1f;
        Time.timeScale = 0;
        if (ScoreManager.Instance != null) ScoreManager.Instance.FinishRound();
    }

    public void RestartGame()
    {
        if (!IsGameOver) return;

        for (int i = spawnedFruitParent.childCount - 1; i >= 0; i--)
        {
            var fruit = spawnedFruitParent.GetChild(i).gameObject;
            fruit.SetActive(false);
            Destroy(fruit);
        }
        states.Clear();
        fruits.Clear();
        removedFruits.Clear();
        IsGameOver = false;
        Time.timeScale = previousTimeScale;
        dropManager.enabled = true;
        dropManager.ResetRound();
        if (dropGuide != null) dropGuide.gameObject.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (ScoreManager.Instance != null) ScoreManager.Instance.ResetScore();
    }

    private void OnDestroy()
    {
        if (IsGameOver) Time.timeScale = previousTimeScale;
    }
}
