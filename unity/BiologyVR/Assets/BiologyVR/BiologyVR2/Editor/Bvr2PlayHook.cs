using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace BiologyVR.V2.Editor
{
    /// <summary>
    /// The project forces the working version as the Play Mode start scene. When the new scene is the
    /// active one, Play starts the new scene instead; leaving Play Mode restores the old behaviour.
    /// </summary>
    [InitializeOnLoad]
    public static class Bvr2PlayHook
    {
        static SceneAsset previous;
        static Bvr2PlayHook() { EditorApplication.playModeStateChanged += Changed; }

        static void Changed(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode)
            {
                var active = SceneManager.GetActiveScene();
                if (active.path.StartsWith(Bvr2SceneBuilder.Root + "/Scenes/")) { previous = EditorSceneManager.playModeStartScene; EditorSceneManager.playModeStartScene = null; }
            }
            else if (change == PlayModeStateChange.EnteredEditMode && previous)
            {
                EditorSceneManager.playModeStartScene = previous; previous = null;
            }
        }

        [MenuItem("Biology VR 2/Open Scene")]
        public static void Open()
        {
            if (EditorApplication.isPlaying) return;
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(Bvr2SceneBuilder.ScenePath);
        }

        [MenuItem("Biology VR 2/Play (chapter choice)")]
        public static void Play() => PlayChapter(-1);
        [MenuItem("Biology VR 2/Play Prelude (lab)")]
        public static void Play0() => PlayChapter(0);
        [MenuItem("Biology VR 2/Play Chapter 1")]
        public static void Play1() => PlayChapter(1);
        [MenuItem("Biology VR 2/Play Chapter 2")]
        public static void Play2() => PlayChapter(2);
        [MenuItem("Biology VR 2/Play Chapter 3")]
        public static void Play3() => PlayChapter(3);
        [MenuItem("Biology VR 2/Play Chapter 4")]
        public static void Play4() => PlayChapter(4);
        [MenuItem("Biology VR 2/Play Chapter 5")]
        public static void Play5() => PlayChapter(5);
        [MenuItem("Biology VR 2/Play Chapter 6")]
        public static void Play6() => PlayChapter(6);

        static void PlayChapter(int n)
        {
            if (EditorApplication.isPlaying) return;
            if (n >= 0) { UnityEngine.PlayerPrefs.SetInt(Bvr2Campaign.StartChapterKey, n); UnityEngine.PlayerPrefs.Save(); }
            if (SceneManager.GetActiveScene().path != Bvr2SceneBuilder.ScenePath) EditorSceneManager.OpenScene(Bvr2SceneBuilder.ScenePath);
            EditorApplication.isPlaying = true;
        }
    }
}
