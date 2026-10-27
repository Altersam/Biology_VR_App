using System.Collections.Generic;
using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>Sound of the organism: heart thump on every beat (tempo from the model), blood rush that
    /// follows your speed over the wall, music moods with crossfade, pooled one-shots.</summary>
    public sealed class Bvr2Audio : MonoBehaviour
    {
        public Bvr2Organism organism;
        public Bvr2Swimmer swimmer;
        public AudioClip heartThump, heartLoop, flowLoop, musicCalm, musicTension, musicRecovery, scanLoop, fieldLoop, thrustLoop, ambienceLoop;
        public List<AudioClip> clips = new List<AudioClip>();
        [Range(0, 1)] public float master = 1f, musicVolume = 0.42f;

        AudioSource heart, flow, scan, field, musicA, musicB, thrust, ambience; readonly List<AudioSource> pool = new List<AudioSource>();
        readonly Dictionary<string, AudioClip> byName = new Dictionary<string, AudioClip>();
        string mood = ""; float musicFade = 1f; bool aIsCurrent = true;
        public float ScanLevel { get; set; }
        public float FieldLevel { get; set; }

        AudioSource Make(string name, bool loop, float spatial = 0f)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>(); s.loop = loop; s.playOnAwake = false; s.spatialBlend = spatial; s.dopplerLevel = 0; return s;
        }

        void Awake()
        {
            foreach (var c in clips) if (c) byName[c.name] = c;
            heart = Make("Heart", false); flow = Make("Flow", true); thrust = Make("Thrust", true); ambience = Make("Ambience", true); scan = Make("Scan", true); field = Make("Field", true);
            musicA = Make("Music A", true); musicB = Make("Music B", true);
            for (int i = 0; i < 10; i++) pool.Add(Make("SFX " + i, false));
        }

        void Start()
        {
            if (organism) organism.OnBeat += Beat;
            if (flowLoop) { flow.clip = flowLoop; flow.volume = 0.25f; flow.Play(); }
            if (thrustLoop) { thrust.clip = thrustLoop; thrust.volume = 0; thrust.Play(); }
            if (ambienceLoop) { ambience.clip = ambienceLoop; ambience.volume = 0.35f * master; ambience.Play(); }
            if (swimmer) { swimmer.OnBoost += () => Play("BVR2_Boost", 0.9f); swimmer.OnWallBump += () => Play("BVR2_Bump", 0.8f); swimmer.OnAnchored += () => Play("BVR2_Anchor", 0.9f); }
            if (scanLoop) { scan.clip = scanLoop; scan.volume = 0; scan.Play(); }
            if (fieldLoop) { field.clip = fieldLoop; field.volume = 0; field.Play(); }
            if (!heartThump && heartLoop) { heart.clip = heartLoop; heart.loop = true; heart.volume = 0.6f; heart.Play(); }
            Mood("calm");
        }

        void OnDestroy() { if (organism) organism.OnBeat -= Beat; }

        void Beat()
        {
            if (!heartThump) return;
            float stress = organism ? Mathf.InverseLerp(75, 140, organism.heartRate) : 0;
            heart.pitch = Mathf.Lerp(0.98f, 1.08f, stress) * Mathf.Lerp(1f, 0.85f, 1f - Bvr2Time.Scale);
            heart.PlayOneShot(heartThump, master * Mathf.Lerp(0.7f, 1f, stress));
            if (stress > 0.35f) Bvr2Haptics.Both(0.08f + 0.15f * stress, 0.05f);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (!heartThump && heartLoop && organism) heart.pitch = organism.heartRate / 72f;
            float speed = swimmer ? swimmer.SpeedOverWall : 2f;
            float pulse = organism ? organism.Pulse : 0f;
            flow.volume = master * Mathf.Clamp01(0.14f + speed / 14f + pulse * 0.12f);
            flow.pitch = Mathf.Lerp(0.85f, 1.25f, Mathf.InverseLerp(1f, 12f, speed)) * Mathf.Lerp(0.7f, 1f, Bvr2Time.Scale);
            float push = swimmer && swimmer.input ? Mathf.Clamp01(swimmer.input.Move.magnitude + Mathf.Abs(swimmer.input.Vertical)) : 0f;
            thrust.volume = Mathf.MoveTowards(thrust.volume, push * 0.32f * master + (swimmer && swimmer.Boosting ? 0.3f : 0), dt * 2.5f);
            thrust.pitch = 0.9f + push * 0.25f + (swimmer && swimmer.Boosting ? 0.35f : 0);
            scan.volume = Mathf.MoveTowards(scan.volume, ScanLevel > 0 ? 0.35f * master : 0, dt * 3f); scan.pitch = 0.8f + ScanLevel * 0.7f;
            field.volume = Mathf.MoveTowards(field.volume, FieldLevel > 0 ? 0.4f * master : 0, dt * 3f); field.pitch = 0.75f + FieldLevel * 0.5f;
            // crossfade music
            musicFade = Mathf.MoveTowards(musicFade, 1f, dt / 2.5f);
            var cur = aIsCurrent ? musicA : musicB; var old = aIsCurrent ? musicB : musicA;
            cur.volume = musicVolume * master * musicFade; old.volume = musicVolume * master * (1f - musicFade);
            if (old.volume <= 0.001f && old.isPlaying) old.Stop();
        }

        public void Mood(string name)
        {
            if (name == mood) return; mood = name;
            var clip = name == "tension" ? musicTension : name == "recovery" ? musicRecovery : name == "none" ? null : musicCalm;
            aIsCurrent = !aIsCurrent; musicFade = 0;
            var cur = aIsCurrent ? musicA : musicB;
            if (clip) { cur.clip = clip; cur.volume = 0; cur.Play(); } else cur.Stop();
        }

        public void Play(string clipName, float volume = 1f, float pitch = 1f)
        {
            if (!byName.TryGetValue(clipName, out var clip) || !clip) return;
            foreach (var s in pool)
                if (!s.isPlaying) { s.pitch = pitch; s.PlayOneShot(clip, volume * master); return; }
            pool[0].PlayOneShot(clip, volume * master);
        }

        public void PlayAt(string clipName, Vector3 position, float volume = 1f)
        {
            if (!byName.TryGetValue(clipName, out var clip) || !clip) return;
            AudioSource.PlayClipAtPoint(clip, position, volume * master);
        }
    }
}
