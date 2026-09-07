using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace BigMonster.Editor
{
    [InitializeOnLoad]
    public static class VerifyArena
    {
        static VerifyArena() { EditorApplication.playModeStateChanged += OnState; }
        public static void Run()
        {
            EditorSceneManager.OpenScene(ArenaMenu.ScenePath);
            SessionState.SetBool("BigMonster.Verifying", true);
            EditorApplication.isPlaying = true;
        }
        static void OnState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("BigMonster.Verifying", false))
                new GameObject("Automated gameplay verification").AddComponent<GameplayVerification>();
        }
    }
}
