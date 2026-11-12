using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

// 대체과제 B: 같은 실행 안에서 각 판의 최종 점수를 모아 상위 5개를 보여주는 로컬 리더보드입니다.
// 파일이나 PlayerPrefs에 저장하지 않으므로 실행을 끝내면 기록이 사라집니다.
[DisallowMultipleComponent]
public sealed class LeaderboardManager : MonoBehaviour
{
    // 한 판의 기록입니다. order는 등록 순서이며 동점일 때 먼저 등록된 기록을 앞에 두는 데 씁니다.
    public readonly struct Entry
    {
        public readonly int score;
        public readonly int order;

        public Entry(int score, int order)
        {
            this.score = score;
            this.order = order;
        }
    }

    // 게임오버 확정과 재시작 시점을 알려줄 매니저입니다.
    [SerializeField] private GameOverManager gameOverManager;
    // 게임오버 패널 안에 표시할 순위 텍스트입니다.
    [SerializeField] private TMP_Text resultListText;
    // 플레이 중에도 보이는 화면 옆 순위 텍스트입니다. 첫 판 이전의 빈 상태를 여기서 확인할 수 있습니다.
    [SerializeField] private TMP_Text hudListText;
    // 유지할 최대 기록 수입니다.
    [SerializeField, Min(1)] private int maxEntries = 5;
    // 방금 등록된 기록을 강조할 색입니다.
    [SerializeField] private Color highlightColor = new Color(.88f, .36f, .08f);
    // 각 순위 텍스트 앞에 붙일 TMP 서식입니다. 판 그림의 칸 간격에 줄 높이를 맞출 때 씁니다(예: "<line-height=62>").
    [SerializeField] private string hudTextPrefix = "";
    [SerializeField] private string resultTextPrefix = "";

    private readonly List<Entry> entries = new List<Entry>();
    private int nextOrder;
    // 현재 판의 기록을 이미 등록했는지 나타냅니다. 같은 판이 두 번 등록되는 것을 막습니다.
    private bool registeredThisRound;
    // 방금 등록된 기록의 order입니다. 상위 5위 밖으로 밀렸다면 강조하지 않습니다.
    private int lastOrder = -1;

    public IReadOnlyList<Entry> Entries => entries;

    private void OnEnable()
    {
        if (gameOverManager != null)
        {
            gameOverManager.GameEnded += Register;
            gameOverManager.Restarted += OnRestarted;
        }
    }

    private void OnDisable()
    {
        if (gameOverManager != null)
        {
            gameOverManager.GameEnded -= Register;
            gameOverManager.Restarted -= OnRestarted;
        }
    }

    private void Start()
    {
        RefreshDisplay();
    }

    public void Register(int score)
    {
        if (registeredThisRound) return;
        registeredThisRound = true;

        lastOrder = nextOrder++;
        entries.Add(new Entry(score, lastOrder));

        // 점수 내림차순, 동점이면 먼저 등록된 기록(order가 작은 기록)이 앞입니다.
        entries.Sort((a, b) => a.score != b.score ? b.score.CompareTo(a.score) : a.order.CompareTo(b.order));
        if (entries.Count > maxEntries) entries.RemoveRange(maxEntries, entries.Count - maxEntries);

        RefreshDisplay();
    }

    private void OnRestarted()
    {
        // 기존 기록은 유지하고 다음 판에서 새 기록을 받을 수 있게만 합니다.
        registeredThisRound = false;
        lastOrder = -1;
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        string text = BuildText();
        if (resultListText != null) resultListText.text = resultTextPrefix + text;
        if (hudListText != null) hudListText.text = hudTextPrefix + text;
    }

    private string BuildText()
    {
        if (entries.Count == 0) return "<size=70%><alpha=#88>NO RECORDS YET";

        var builder = new StringBuilder();
        string hex = ColorUtility.ToHtmlStringRGB(highlightColor);
        for (int i = 0; i < entries.Count; i++)
        {
            if (i > 0) builder.Append('\n');
            bool isLatest = entries[i].order == lastOrder;
            if (isLatest) builder.Append("<color=#").Append(hex).Append('>');
            builder.Append(i + 1).Append(".  ").Append(entries[i].score.ToString("D4"));
            if (isLatest) builder.Append("  NEW</color>");
        }
        return builder.ToString();
    }
}
