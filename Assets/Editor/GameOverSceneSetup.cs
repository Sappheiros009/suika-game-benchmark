using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class GameOverSceneSetup
{
    [MenuItem("Tools/Suika/Set Up Game Over")]
    public static void ConfigureScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Configure game over outside Play Mode.");
        var managerObject = GameObject.Find("GameManager");
        var playfield = GameObject.Find("Playfield");
        var hud = GameObject.Find("HUD");
        if (managerObject == null || playfield == null || hud == null)
            throw new InvalidOperationException("Open the SuikaGame scene with its drop manager first.");

        var gameOver = managerObject.GetComponent<GameOverManager>();
        if (gameOver == null) gameOver = Undo.AddComponent<GameOverManager>(managerObject);
        var existing = hud.transform.Find("GameOverPanel");
        var panel = existing != null ? existing.gameObject : CreatePanel(hud.transform);
        EnsureFinalScore(panel.transform);
        panel.transform.SetAsLastSibling();
        panel.SetActive(false);
        var serialized = new SerializedObject(gameOver);
        serialized.FindProperty("dropManager").objectReferenceValue = managerObject.GetComponent<FruitDropManager>();
        serialized.FindProperty("spawnedFruitParent").objectReferenceValue = playfield.transform.Find("SpawnedFruits");
        serialized.FindProperty("heightLimit").objectReferenceValue = playfield.transform.Find("GameOverLine");
        serialized.FindProperty("dropGuide").objectReferenceValue = playfield.transform.Find("DropGuide");
        serialized.FindProperty("gameOverPanel").objectReferenceValue = panel;
        serialized.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(managerObject.scene);
        ScoreSceneSetup.ConfigureScene();
    }

    static void EnsureFinalScore(Transform panel)
    {
        var card = panel.Find("Card");
        var message = card.Find("Message").GetComponent<TMP_Text>();
        Undo.RecordObject(message.rectTransform, "Make room for final score");
        Undo.RecordObject(message, "Update result message size");
        message.rectTransform.anchoredPosition = new Vector2(0, 27);
        message.fontSize = 18;
        var hint = card.Find("RestartHint").GetComponent<TMP_Text>();
        Undo.RecordObject(hint.rectTransform, "Move restart hint");
        Undo.RecordObject(hint, "Update restart hint size");
        hint.rectTransform.anchoredPosition = new Vector2(0, -88);
        hint.fontSize = 16;
        if (card.Find("FinalScore") == null)
            Label("FinalScore", card, "SCORE  0000", -20, 28, new Color(.20f, .31f, .26f), FontStyles.Bold);
    }

    static GameObject CreatePanel(Transform parent)
    {
        var panel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(panel, "Create game over screen");
        panel.transform.SetParent(parent, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(.12f, .18f, .14f, .65f);
        panel.GetComponent<Image>().raycastTarget = false;

        var card = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(panel.transform, false);
        card.GetComponent<RectTransform>().sizeDelta = new Vector2(520, 280);
        var image = card.GetComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Layout/Rounded.png");
        image.type = Image.Type.Sliced;
        image.color = new Color(1f, .996f, .973f);
        image.raycastTarget = false;
        var ink = new Color(.20f, .31f, .26f);
        var muted = new Color(.49f, .56f, .50f);
        Label("Title", card.transform, "GAME OVER", 70, 40, ink, FontStyles.Bold);
        Label("Message", card.transform, "Your basket is full.", 6, 21, muted);
        Label("RestartHint", card.transform, "PRESS R TO TRY AGAIN", -70, 18, ink, FontStyles.Bold);
        return panel;
    }

    static void Label(string name, Transform parent, string value, float y, int fontSize, Color color, FontStyles style = FontStyles.Normal)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(obj, "Create result label");
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(480, 60);
        rect.anchoredPosition = new Vector2(0, y);
        var text = obj.GetComponent<TMP_Text>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;
        text.raycastTarget = false;
    }
}
