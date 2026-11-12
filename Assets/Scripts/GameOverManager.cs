using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(FruitDropManager))]
public sealed class GameOverManager : MonoBehaviour
{
    // 게임오버 시 낙하 입력을 끄고, 재시작 시 낙하 상태를 초기화할 매니저입니다.
    [SerializeField] private FruitDropManager dropManager;
    // 게임오버 판정과 재시작 시 제거할 과일들이 모여 있는 부모입니다.
    [SerializeField] private Transform spawnedFruitParent;
    // 과일 Collider의 윗면과 비교할 게임오버 제한선입니다.
    [SerializeField] private Transform heightLimit;
    // 게임오버 시 숨기고 재시작 시 표시할 낙하 안내선입니다.
    [SerializeField] private Transform dropGuide;
    // 게임오버 결과를 표시할 UI 패널입니다.
    [SerializeField] private GameObject gameOverPanel;
    // 새 과일이 낙하할 수 있도록 높이 판정을 유예하는 시간입니다.
    [SerializeField, Min(0)] private float spawnGracePeriod = 1f;
    // 유예 시간이 지난 과일이 제한선 위에 연속으로 머물 수 있는 시간입니다.
    [SerializeField, Min(0)] private float overflowDuration = 1f;

    // 과일 하나의 물리 참조와 게임오버 판정 시간을 묶어 보관합니다.
    private sealed class FruitState
    {
        // 과일 윗면의 높이를 확인할 Collider입니다.
        public Collider2D collider;
        // 물리 시뮬레이션 중인 과일인지 확인할 Rigidbody입니다.
        public Rigidbody2D body;
        // 이 과일이 추적되기 시작한 후 누적된 물리 진행 시간입니다.
        public float age;
        // 생성 유예 후 제한선 위에 연속으로 머문 시간입니다. 선 아래로 내려오면 0으로 초기화합니다.
        public float overflowTime;
    }

    // 각 과일을 해당 과일의 추적 정보와 연결합니다.
    private readonly Dictionary<FruitMerge, FruitState> states = new Dictionary<FruitMerge, FruitState>();
    // 현재 생성된 과일 목록입니다. FixedUpdate에서 갱신합니다.
    private readonly List<FruitMerge> fruits = new List<FruitMerge>();
    // 추적 정보에서 제거할 과일을 임시로 모읍니다. Dictionary 순회가 끝난 뒤 제거합니다.
    private readonly List<FruitMerge> removedFruits = new List<FruitMerge>();
    // 게임을 멈추기 전의 진행 속도입니다. 재시작하거나 매니저가 제거될 때 복원합니다.
    private float previousTimeScale = 1f;

    // 현재 게임오버 상태인지 나타냅니다. 외부에서는 읽기만 가능합니다.
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
        // 진행 중에는 R을 무시한다. timeScale이 0이어도 Update와 GetKeyDown은 동작한다.
        if (IsGameOver && Input.GetKeyDown(KeyCode.R)) RestartGame();
    }

    private void EndGame()
    {
        IsGameOver = true;
        // 컴포넌트를 끄면 좌우 이동과 Space 입력(Update, DropFruit)이 모두 막힌다.
        if (dropManager != null) dropManager.enabled = false;
        if (dropGuide != null) dropGuide.gameObject.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        // 제공 코드: 재시작할 때 복원할 시간을 저장하고 게임을 멈춥니다.
        previousTimeScale = Time.timeScale > 0 ? Time.timeScale : 1f;
        Time.timeScale = 0;

        // 이후 점수 가산을 막고 결과 패널의 최종 점수를 갱신한다.
        if (ScoreManager.Instance != null) ScoreManager.Instance.FinishRound();
    }

    public void RestartGame()
    {
        // 뒤에서부터 지워야 순회 중 인덱스가 밀리지 않는다.
        for (int i = spawnedFruitParent.childCount - 1; i >= 0; i--)
        {
            var child = spawnedFruitParent.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }

        states.Clear();
        fruits.Clear();
        removedFruits.Clear();
        IsGameOver = false;

        // 제공 코드: 게임 진행 속도를 복원합니다.
        Time.timeScale = previousTimeScale;

        if (dropManager != null)
        {
            dropManager.enabled = true;
            dropManager.ResetRound();
        }
        if (dropGuide != null) dropGuide.gameObject.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // 현재 점수만 0으로 돌리고 최고 점수는 유지된다.
        if (ScoreManager.Instance != null) ScoreManager.Instance.ResetScore();
    }

    private void OnDestroy()
    {
        if (IsGameOver) Time.timeScale = previousTimeScale;
    }
}
