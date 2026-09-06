using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RollablePlus.Editor
{
    [InitializeOnLoad]
    public static class FirstOpen
    {
        static readonly string Marker = "Library/RollableEnhancedOpened";
        static FirstOpen()
        {
            if (!Application.isBatchMode) EditorApplication.update += ConfigureWhenReady;
        }
        static void ConfigureWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!UpgradeArena.ConfigurePlayScene()) return;
            EditorApplication.update -= ConfigureWhenReady;
            if (File.Exists(Marker) || SceneManager.GetActiveScene().isDirty) return;
            EditorSceneManager.OpenScene(UpgradeArena.ScenePath);
            File.WriteAllText(Marker, "Enhanced scene opened once. Original scenes are preserved.");
        }
    }
}
