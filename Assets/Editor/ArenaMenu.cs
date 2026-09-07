using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BigMonster.Editor
{
    public static class ArenaMenu
    {
        public const string ScenePath = "Assets/Scenes/Arena.unity";

        // Editor Play otherwise uses the last inspected scene, regardless of Build Settings.
        internal static bool ConfigurePlayScene()
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (scene == null) return false;
            EditorSceneManager.playModeStartScene = scene;
            Debug.Log("[Big Monster] Editor Play start scene: " + AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            return true;
        }

        [MenuItem("Big Monster Game/Open Arena")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
            ConfigurePlayScene();
        }

        [MenuItem("Big Monster Game/Open Arena", true)]
        static bool CanOpen() => !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
