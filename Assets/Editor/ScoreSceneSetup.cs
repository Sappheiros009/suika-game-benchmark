using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

public static class ScoreSceneSetup
{
    [MenuItem("Tools/Suika/Set Up Score")]
    public static void ConfigureScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Configure score outside Play Mode.");
        var manager = GameObject.Find("GameManager");
        var hud = GameObject.Find("HUD");
        if (manager == null || hud == null)
            throw new InvalidOperationException("Open the SuikaGame scene first.");
        var score = manager.GetComponent<ScoreManager>();
        if (score == null) score = Undo.AddComponent<ScoreManager>(manager);
        var settings = new SerializedObject(score);
        settings.FindProperty("scoreText").objectReferenceValue = hud.transform.Find("ScorePanel/ScoreValue").GetComponent<TMP_Text>();
        settings.FindProperty("bestScoreText").objectReferenceValue = hud.transform.Find("ScorePanel/BestValue").GetComponent<TMP_Text>();
        settings.FindProperty("finalScoreText").objectReferenceValue = hud.transform.Find("GameOverPanel/Card/FinalScore").GetComponent<TMP_Text>();
        settings.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(manager.scene);
    }
}
