using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Creates shared face animation assets and migrates the five fruits to variants.</summary>
public static class FruitPrefabSetup
{
    public const string BasePath = "Assets/Prefabs/FruitBase.prefab";
    public const string ControllerPath = "Assets/Animations/FruitFace/FruitFace.controller";
    static readonly string[] Names = { "cherry", "mandarin", "apple", "melon", "watermelon" };
    static readonly float[] Diameters = { .54f, .76f, 1f, 1.26f, 1.56f };
    static readonly float[] BodyOffsets = { .105f, .065f, .055f, .065f, .04f };

    [MenuItem("Tools/Suika/Set Up Fruit Variants")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Configure fruit prefabs outside Play Mode.");

        GetOrCreateBase();
        for (int i = 0; i < Names.Length; i++) EnsureVariant(i, Diameters[i], BodyOffsets[i]);
        ConfigureMergeChain();
        AssetDatabase.SaveAssets();
        Debug.Log("FruitBase and five fruit variants are ready. Edit FruitBase/Face to change shared animation.");
    }

    public static GameObject EnsureVariant(int index, float diameter, float bodyOffset)
    {
        var basePrefab = GetOrCreateBase();
        string path = "Assets/Prefabs/Fruits/" + Names[index] + ".prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null && PrefabUtility.GetPrefabAssetType(existing) == PrefabAssetType.Variant)
        {
            var source = PrefabUtility.GetCorrespondingObjectFromSource(existing);
            if (AssetDatabase.GetAssetPath(source) != BasePath)
                throw new InvalidOperationException(path + " already derives from a different base.");
            return existing;
        }

        // Preserve the current fruit settings, rather than resetting them during migration.
        var scale = existing != null ? existing.transform.localScale : Vector3.one * (diameter / .74f);
        var bodyPosition = existing != null ? existing.transform.Find("Body").localPosition : new Vector3(.005f, bodyOffset, 0);
        var sprite = existing != null ? existing.transform.Find("Body").GetComponent<SpriteRenderer>().sprite
            : AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Fruits/" + Names[index] + ".png");
        float mass = existing != null ? existing.GetComponent<Rigidbody2D>().mass : .5f + index * .25f;
        var sceneOverrides = CaptureSceneOverrides(existing, path);

        var previewScene = EditorSceneManager.NewPreviewScene();
        try
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab, previewScene);
            instance.name = Names[index];
            instance.transform.localScale = scale;
            var body = instance.transform.Find("Body");
            body.localPosition = bodyPosition;
            body.GetComponent<SpriteRenderer>().sprite = sprite;
            instance.GetComponent<Rigidbody2D>().mass = mass;
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(body);
            PrefabUtility.RecordPrefabInstancePropertyModifications(body.GetComponent<SpriteRenderer>());
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.GetComponent<Rigidbody2D>());
            // The GUID stays the same, but converting to a variant changes local file IDs.
            var variant = PrefabUtility.SaveAsPrefabAsset(instance, path, out bool success);
            if (!success || variant == null || PrefabUtility.GetPrefabAssetType(variant) != PrefabAssetType.Variant)
                throw new InvalidOperationException("Could not save fruit variant: " + path);
            RestoreSceneOverrides(sceneOverrides, variant);
            return variant;
        }
        finally { EditorSceneManager.ClosePreviewScene(previewScene); }
    }

    sealed class SceneOverrides
    {
        public GameObject instance;
        public PropertyModification[] modifications;
        public string[] paths;
        public Type[] types;
    }

    static List<SceneOverrides> CaptureSceneOverrides(GameObject source, string path)
    {
        var snapshots = new List<SceneOverrides>();
        if (source == null) return snapshots;
        foreach (var obj in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!PrefabUtility.IsAnyPrefabInstanceRoot(obj) || PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj) != path) continue;
            var mods = PrefabUtility.GetPropertyModifications(obj) ?? Array.Empty<PropertyModification>();
            var snapshot = new SceneOverrides
            {
                instance = obj, modifications = mods,
                paths = new string[mods.Length], types = new Type[mods.Length]
            };
            for (int i = 0; i < mods.Length; i++)
            {
                var target = mods[i].target as Component;
                var go = mods[i].target as GameObject;
                Transform transform = target != null ? target.transform : go != null ? go.transform : null;
                if (transform == null || !transform.IsChildOf(source.transform)) continue;
                snapshot.paths[i] = AnimationUtility.CalculateTransformPath(transform, source.transform);
                snapshot.types[i] = mods[i].target.GetType();
            }
            snapshots.Add(snapshot);
        }
        return snapshots;
    }

    static void RestoreSceneOverrides(List<SceneOverrides> snapshots, GameObject variant)
    {
        foreach (var snapshot in snapshots)
        {
            if (snapshot.instance == null) throw new InvalidOperationException("A scene fruit instance was lost during conversion.");
            for (int i = 0; i < snapshot.modifications.Length; i++)
            {
                if (snapshot.types[i] == null) continue;
                var transform = snapshot.paths[i].Length == 0 ? variant.transform : variant.transform.Find(snapshot.paths[i]);
                if (transform == null) throw new InvalidOperationException("Missing variant child: " + snapshot.paths[i]);
                snapshot.modifications[i].target = snapshot.types[i] == typeof(GameObject)
                    ? (UnityEngine.Object)transform.gameObject : transform.GetComponent(snapshot.types[i]);
            }
            PrefabUtility.SetPropertyModifications(snapshot.instance, snapshot.modifications);
            EditorSceneManager.MarkSceneDirty(snapshot.instance.scene);
        }
    }

    static GameObject GetOrCreateBase()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(BasePath);
        if (existing != null)
        {
            if (existing.GetComponent<FruitContactExpression>() == null || existing.GetComponent<FruitMerge>() == null)
            {
                var contents = PrefabUtility.LoadPrefabContents(BasePath);
                try
                {
                    AddContactExpression(contents);
                    if (contents.GetComponent<FruitMerge>() == null) contents.AddComponent<FruitMerge>();
                    PrefabUtility.SaveAsPrefabAsset(contents, BasePath);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(BasePath);
        }

        Directory.CreateDirectory("Assets/Prefabs/Fruits");
        Directory.CreateDirectory("Assets/Animations/FruitFace");
        AssetDatabase.Refresh();
        var controller = GetOrCreateController();
        string cherryPath = "Assets/Prefabs/Fruits/cherry.prefab";
        var cherry = AssetDatabase.LoadAssetAtPath<GameObject>(cherryPath);
        GameObject root;
        if (cherry != null)
            root = PrefabUtility.LoadPrefabContents(cherryPath);
        else
        {
            root = new GameObject("FruitBase");
            root.AddComponent<CircleCollider2D>().radius = .37f;
            var rb = root.AddComponent<Rigidbody2D>();
            rb.mass = .5f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Layout/SpriteUnlit.mat");
            var body = new GameObject("Body", typeof(SpriteRenderer));
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(.005f, .105f, 0);
            body.GetComponent<SpriteRenderer>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Fruits/cherry.png");
            body.GetComponent<SpriteRenderer>().sharedMaterial = material;
            body.GetComponent<SpriteRenderer>().sortingOrder = 5;
            var face = new GameObject("Face", typeof(SpriteRenderer));
            face.transform.SetParent(root.transform, false);
            face.transform.localPosition = new Vector3(0, .01f, 0);
            face.transform.localScale = Vector3.one * .65f;
            face.GetComponent<SpriteRenderer>().sharedMaterial = material;
            face.GetComponent<SpriteRenderer>().sortingOrder = 6;
        }

        try
        {
            root.name = "FruitBase";
            root.transform.localScale = Vector3.one;
            var face = root.transform.Find("Face").gameObject;
            face.GetComponent<SpriteRenderer>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Faces/face_idle.png");
            var animator = face.GetComponent<Animator>();
            if (animator == null) animator = face.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            AddContactExpression(root);
            if (root.GetComponent<FruitMerge>() == null) root.AddComponent<FruitMerge>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, BasePath);
            if (prefab == null) throw new InvalidOperationException("Could not save FruitBase.");
            return prefab;
        }
        finally
        {
            if (cherry != null) PrefabUtility.UnloadPrefabContents(root);
            else UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static void AddContactExpression(GameObject root)
    {
        var expression = root.GetComponent<FruitContactExpression>();
        if (expression == null) expression = root.AddComponent<FruitContactExpression>();
        var serialized = new SerializedObject(expression);
        serialized.FindProperty("fruitCollider").objectReferenceValue = root.GetComponent<Collider2D>();
        serialized.FindProperty("faceAnimator").objectReferenceValue = root.transform.Find("Face").GetComponent<Animator>();
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ConfigureMergeChain()
    {
        for (int i = Names.Length - 1; i >= 0; i--)
        {
            string path = "Assets/Prefabs/Fruits/" + Names[i] + ".prefab";
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var merger = contents.GetComponent<FruitMerge>();
                var serialized = new SerializedObject(merger);
                serialized.FindProperty("fruitType").enumValueIndex = i;
                serialized.FindProperty("nextFruitPrefab").objectReferenceValue = i + 1 < Names.Length
                    ? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Fruits/" + Names[i + 1] + ".prefab") : null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }
    }

    static AnimatorController GetOrCreateController()
    {
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (existing != null) return existing;
        var idle = CreateClip("Idle", "idle");
        var contact = CreateClip("Contact", "contact");
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("IsTouching", AnimatorControllerParameterType.Bool);
        var machine = controller.layers[0].stateMachine;
        var idleState = machine.AddState("Idle", new Vector3(250, 80, 0));
        idleState.motion = idle;
        idleState.writeDefaultValues = false;
        var contactState = machine.AddState("Contact", new Vector3(500, 80, 0));
        contactState.motion = contact;
        contactState.writeDefaultValues = false;
        machine.defaultState = idleState;
        Transition(idleState, contactState, AnimatorConditionMode.If);
        Transition(contactState, idleState, AnimatorConditionMode.IfNot);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    static AnimationClip CreateClip(string name, string spriteName)
    {
        string path = "Assets/Animations/FruitFace/" + name + ".anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing != null) return existing;
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Faces/face_" + spriteName + ".png");
        if (sprite == null) throw new InvalidOperationException("Missing face sprite: " + spriteName);
        var clip = new AnimationClip { name = name, frameRate = 12 };
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, new[]
        {
            new ObjectReferenceKeyframe { time = 0, value = sprite },
            new ObjectReferenceKeyframe { time = 1f / 12f, value = sprite }
        });
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    static void Transition(AnimatorState from, AnimatorState to, AnimatorConditionMode condition)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0;
        transition.AddCondition(condition, 0, "IsTouching");
    }
}
