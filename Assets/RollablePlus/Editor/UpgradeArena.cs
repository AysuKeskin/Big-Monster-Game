using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RollablePlus.Editor
{
    public static class UpgradeArena
    {
        public const string ScenePath = "Assets/RollablePlus/Scenes/Rollable Enhanced.unity";
        public const string OriginalScenePath = "Assets/Scenes/Mini Game.unity";
        static string OriginalPreviewKey => "RollablePlus.OriginalPreview:" + Application.dataPath;

        // Editor Play otherwise uses the last inspected scene, regardless of Build Settings.
        // Keep the enhanced scene as this copy's default, with an explicit original preview.
        internal static bool ConfigurePlayScene()
        {
            string path = EditorPrefs.GetBool(OriginalPreviewKey, false) ? OriginalScenePath : ScenePath;
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            if (scene == null) return false;
            EditorSceneManager.playModeStartScene = scene;
            Debug.Log("[Rollable] Editor Play start scene: " + AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            return true;
        }

        [MenuItem("Rollable/Open Enhanced Arena")]
        public static void Open() { OpenScene(ScenePath, false); }

        [MenuItem("Rollable/Open Original Mini Game")]
        public static void OpenOriginal() { OpenScene(OriginalScenePath, true); }

        [MenuItem("Rollable/Open Enhanced Arena", true)]
        [MenuItem("Rollable/Open Original Mini Game", true)]
        static bool CanOpen() => !EditorApplication.isPlayingOrWillChangePlaymode;

        static void OpenScene(string path, bool original)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(path);
            EditorPrefs.SetBool(OriginalPreviewKey, original);
            ConfigurePlayScene();
        }
    }
}
