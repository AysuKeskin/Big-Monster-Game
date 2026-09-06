using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace RollablePlus.Editor
{
    [InitializeOnLoad]
    public static class VerifyArena
    {
        static VerifyArena() { EditorApplication.playModeStateChanged += OnState; }
        public static void Run()
        {
            EditorSceneManager.OpenScene(UpgradeArena.ScenePath);
            SessionState.SetBool("Rollable.Verifying", true);
            EditorApplication.isPlaying = true;
        }
        static void OnState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("Rollable.Verifying", false))
                new GameObject("Automated gameplay verification").AddComponent<RollableVerification>();
        }
    }
}
