using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace BiologyVR.V2.Editor
{
    /// <summary>
    /// Imports the professor's voice: Audio/Voice/voice_manifest.json + VO_*.wav (made offline by
    /// Tools/voice/gen_voice.py from the lines in the code) → Generated/Voice/ProfessorVoice.asset.
    /// Clips: mono, Vorbis, compressed in memory (≈16 min of speech stays a few MB).
    /// </summary>
    public static class Bvr2VoiceImport
    {
        public const string VoiceDir = Bvr2SceneBuilder.Root + "/Audio/Voice";
        public const string BankPath = Bvr2SceneBuilder.Root + "/Generated/Voice/ProfessorVoice.asset";

        [MenuItem("Biology VR 2/Voice/Import Voice Bank", false, 320)]
        static void Menu() => Debug.Log("[BVR2] " + Import());

        public static Bvr2VoiceBank Load() => AssetDatabase.LoadAssetAtPath<Bvr2VoiceBank>(BankPath);

        public static string Import()
        {
            string manifestPath = VoiceDir + "/voice_manifest.json";
            if (!File.Exists(manifestPath)) return "no voice manifest";
            var root = JObject.Parse(File.ReadAllText(manifestPath));
            Directory.CreateDirectory(Path.GetDirectoryName(BankPath));
            var bank = Load();
            if (!bank) { bank = ScriptableObject.CreateInstance<Bvr2VoiceBank>(); AssetDatabase.CreateAsset(bank, BankPath); }
            bank.voiceInfo = (string)root["voice"];
            bank.lines = new List<Bvr2VoiceBank.Line>();
            int missing = 0, reimported = 0; double seconds = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var item in root["lines"])
                {
                    string path = VoiceDir + "/" + (string)item["file"];
                    if (AssetImporter.GetAtPath(path) is AudioImporter ai)
                    {
                        var s = ai.defaultSampleSettings;
                        if (s.loadType != AudioClipLoadType.CompressedInMemory || s.compressionFormat != AudioCompressionFormat.Vorbis || !ai.forceToMono || Mathf.Abs(s.quality - 0.42f) > 0.01f)
                        {
                            s.loadType = AudioClipLoadType.CompressedInMemory; s.compressionFormat = AudioCompressionFormat.Vorbis; s.quality = 0.42f;
                            s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                            ai.defaultSampleSettings = s; ai.forceToMono = true; ai.loadInBackground = false; ai.SaveAndReimport(); reimported++;
                        }
                    }
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            foreach (var item in root["lines"])
            {
                string path = VoiceDir + "/" + (string)item["file"];
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (!clip) { missing++; continue; }
                byte[] B(string k) => item[k].Select(t => (byte)Mathf.Clamp((int)t, 0, 255)).ToArray();
                bank.lines.Add(new Bvr2VoiceBank.Line { text = (string)item["text"], clip = clip, fps = (float?)item["fps"] ?? 50, open = B("open"), round = B("round"), wide = B("wide") });
                seconds += clip.length;
            }
            EditorUtility.SetDirty(bank); AssetDatabase.SaveAssets();
            return $"voice bank: {bank.lines.Count} lines, {seconds / 60:F1} min, {missing} missing, {reimported} clips reimported";
        }
    }
}
