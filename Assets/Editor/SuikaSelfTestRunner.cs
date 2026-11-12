using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 메뉴 Suika > Run Self Test: 플레이 씬을 열고 플레이 모드에서 채점 시나리오 검사를 자동으로 돌립니다.
// 결과는 프로젝트 폴더의 SelfTestReport.txt, 스크린샷은 Screenshots 폴더에 저장됩니다.
[InitializeOnLoad]
internal static class SuikaSelfTestRunner
{
    private const string PendingKey = "Suika.SelfTest.Pending";

    static SuikaSelfTestRunner()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem("Suika/Run Self Test")]
    private static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!System.IO.File.Exists(SuikaSceneBuilder.ScenePath)) SuikaSceneBuilder.Build();
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(SuikaSceneBuilder.ScenePath, OpenSceneMode.Single);

        SessionState.SetBool(PendingKey, true);
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
        SessionState.SetBool(PendingKey, false);
        new GameObject("SuikaSelfTest").AddComponent<SuikaSelfTest>();
    }
}
