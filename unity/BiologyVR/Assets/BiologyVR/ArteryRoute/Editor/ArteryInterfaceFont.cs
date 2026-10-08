using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ArteryInterfaceFont
    {
        [MenuItem("Biology VR/Embed Cyrillic Interface Font")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            const string path="Assets/VRTemplateAssets/Fonts/Inter/Inter-Regular.ttf";
            var font=AssetDatabase.LoadAssetAtPath<Font>(path);
            if(!font)throw new InvalidOperationException("Inter font is missing from the transferred scene dependencies");
            var importer=(TrueTypeFontImporter)AssetImporter.GetAtPath(path);
            if(!importer.includeFontData){importer.includeFontData=true;importer.SaveAndReimport();}
            foreach(var text in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {text.font=font;EditorUtility.SetDirty(text);}
            foreach(var onboarding in UnityEngine.Object.FindObjectsByType<JourneyDesktopOnboarding>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {onboarding.uiFont=font;EditorUtility.SetDirty(onboarding);}
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();
        }
    }
}
