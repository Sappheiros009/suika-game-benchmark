using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class FruitDropManagerSetup
{
    [MenuItem("Tools/Suika/Set Up Scene Drop Manager")]
    public static void ConfigureScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Configure the drop manager outside Play Mode.");
        var playfield = GameObject.Find("Playfield");
        var dropPoint = GameObject.Find("Playfield/DropPoint");
        if (playfield == null || dropPoint == null)
            throw new InvalidOperationException("Open the SuikaGame layout scene first.");

        var managerObject = GameObject.Find("GameManager");
        if (managerObject == null)
        {
            managerObject = new GameObject("GameManager");
            Undo.RegisterCreatedObjectUndo(managerObject, "Create GameManager");
        }
        var manager = managerObject.GetComponent<FruitDropManager>();
        if (manager == null) manager = Undo.AddComponent<FruitDropManager>(managerObject);

        var spawned = playfield.transform.Find("SpawnedFruits");
        if (spawned == null)
        {
            var container = new GameObject("SpawnedFruits");
            Undo.RegisterCreatedObjectUndo(container, "Create spawned fruit container");
            container.transform.SetParent(playfield.transform, false);
            spawned = container.transform;
        }

        var serialized = new SerializedObject(manager);
        var prefabs = serialized.FindProperty("fruitPrefabs");
        string[] names = { "cherry", "mandarin", "apple" };
        prefabs.arraySize = names.Length;
        for (int i = 0; i < names.Length; i++)
            prefabs.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Fruits/" + names[i] + ".prefab");
        serialized.FindProperty("dropPoint").objectReferenceValue = dropPoint.transform;
        serialized.FindProperty("spawnedFruitParent").objectReferenceValue = spawned;
        serialized.FindProperty("leftWall").objectReferenceValue = playfield.transform.Find("Basket/LeftWall").GetComponent<BoxCollider2D>();
        serialized.FindProperty("rightWall").objectReferenceValue = playfield.transform.Find("Basket/RightWall").GetComponent<BoxCollider2D>();
        serialized.FindProperty("dropGuide").objectReferenceValue = playfield.transform.Find("DropGuide");
        var preview = GameObject.Find("HUD/NextFruitPanel/NextFruitPreview");
        var nameLabel = GameObject.Find("HUD/NextFruitPanel/NextFruitName");
        if (preview != null) serialized.FindProperty("nextFruitImage").objectReferenceValue = preview.GetComponent<Image>();
        if (nameLabel != null) serialized.FindProperty("nextFruitName").objectReferenceValue = nameLabel.GetComponent<TMP_Text>();
        serialized.ApplyModifiedProperties();

        var help = GameObject.Find("HUD/HowToPanel/HelpBody");
        if (help != null)
        {
            var label = help.GetComponent<TMP_Text>();
            Undo.RecordObject(label, "Update drop instructions");
            label.text = "Left / Right or A / D\nSpace to drop";
        }
        EditorSceneManager.MarkSceneDirty(managerObject.scene);
        GameOverSceneSetup.ConfigureScene();
    }
}
