using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public sealed class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text bestScoreText;
    [SerializeField] private TMP_Text finalScoreText;

    [Header("Points for the fruit created by a merge")]
    [SerializeField, Min(0)] private int mandarinPoints = 10;
    [SerializeField, Min(0)] private int applePoints = 30;
    [SerializeField, Min(0)] private int melonPoints = 80;
    [SerializeField, Min(0)] private int watermelonPoints = 200;

    private bool acceptingScore = true;

    public int CurrentScore { get; private set; }
    public int BestScore { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // GameManager에 함께 붙은 다른 컴포넌트는 유지한다.
            Destroy(this);
            return;
        }
        Instance = this;
        RefreshDisplay();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void AddMergeScore(FruitType type)
    {
        if (Instance != this || !isActiveAndEnabled || !acceptingScore) return;

        int points = GetMergePoints(type);
        if (points <= 0) return;
        CurrentScore += points;
        BestScore = Mathf.Max(BestScore, CurrentScore);
        RefreshDisplay();
    }

    private int GetMergePoints(FruitType type)
    {
        switch (type)
        {
            case FruitType.Mandarin: return mandarinPoints;
            case FruitType.Apple: return applePoints;
            case FruitType.Melon: return melonPoints;
            case FruitType.Watermelon: return watermelonPoints;
            default: return 0;
        }
    }

    public void FinishRound()
    {
        acceptingScore = false;
        RefreshDisplay();
    }

    public void ResetScore()
    {
        CurrentScore = 0;
        acceptingScore = true;
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (scoreText != null) scoreText.text = CurrentScore.ToString("D4");
        if (bestScoreText != null) bestScoreText.text = BestScore.ToString("D4");
        if (finalScoreText != null) finalScoreText.text = "SCORE  " + CurrentScore.ToString("D4");
    }

}
