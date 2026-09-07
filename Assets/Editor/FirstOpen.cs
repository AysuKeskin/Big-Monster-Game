using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BigMonster.Editor
{
    [InitializeOnLoad]
    public static class FirstOpen
    {
        static readonly string Marker = "Library/BigMonsterOpened";
        static FirstOpen()
        {
            if (!Application.isBatchMode) EditorApplication.update += ConfigureWhenReady;
        }
        static void ConfigureWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!ArenaMenu.ConfigurePlayScene()) return;
            EditorApplication.update -= ConfigureWhenReady;
            if (File.Exists(Marker) || SceneManager.GetActiveScene().isDirty) return;
            EditorSceneManager.OpenScene(ArenaMenu.ScenePath);
            File.WriteAllText(Marker, "Arena scene opened once.");
        }
    }
}
