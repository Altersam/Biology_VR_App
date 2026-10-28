"""Professor voice bank (usage: python gen_voice.py [lines.json] [out_dir]): every line from lines.json -> Piper ru_RU-ruslan (mature male) -> a touch deeper and
warmer (Praat), trimmed, loudness-matched, + viseme curves (mouth open / round / wide at 50 fps) for lip sync.
Writes out/voice/VO_*.wav and out/voice/voice_manifest.json."""
import json, re, wave, hashlib, sys, os, io, numpy as np, parselmouth
from parselmouth.praat import call
from piper import PiperVoice, SynthesisConfig
from scipy.io import wavfile
from scipy.signal import butter, sosfilt, sosfiltfilt
HERE = os.path.dirname(os.path.abspath(__file__))
LINES = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, "out", "lines.json")
OUT = sys.argv[2] if len(sys.argv) > 2 else os.path.join(HERE, "out", "voice"); os.makedirs(OUT, exist_ok=True); FPS = 50
voice = PiperVoice.load(os.path.join(HERE, "models", "vits-piper-ru_RU-ruslan-medium", "ru_RU-ruslan-medium.onnx"))
import shutil, subprocess
FFMPEG = shutil.which("ffmpeg")
SR = voice.config.sample_rate
CFG = SynthesisConfig(length_scale=1.06, noise_scale=0.62, noise_w_scale=0.75)
SPOKEN = [  # subtitle text stays as written; only what is said changes
    ("пульс 72, давление 120 на 80, кислород 98 процентов, температура 36,6", "пульс семьдесят два, давление сто двадцать на восемьдесят, кислород девяносто восемь процентов, температура тридцать шесть и шесть"),
    ("3,3–5,5 ммоль/л", "от трёх и трёх десятых до пяти и пяти десятых миллимоля на литр"),
    ("Температура 38 и растёт", "Температура тридцать восемь и растёт"),
    ("Это глава 2.", "Это глава вторая."),
    ("Импульс — B / Q.", "Импульс — кнопка Би или Кью."),
    ("правая кнопка мыши или F", "правая кнопка мыши или Эф"),
    ("буквы Y", "буквы игрек"),
    ("BioTool", "Био-тул"),
    ("Т-киллер", "Тэ-киллер"),
    ("«", ""), ("»", ""),
]
def spoken(t):
    for a, b in SPOKEN: t = t.replace(a, b)
    assert not re.search(r"[0-9A-Za-z]", t), t
    return t
def synth(text):
    chunks = []
    for ch in voice.synthesize(text, syn_config=CFG):
        a = ch.audio_float_array.astype(np.float64)
        chunks.append(a); chunks.append(np.zeros(int(SR * 0.26)))
    return np.concatenate(chunks[:-1]) if chunks else np.zeros(SR // 2)
def process(x):
    snd = parselmouth.Sound(x, sampling_frequency=SR)
    out = call(snd, "Change gender", 65, 300, 0.965, 104.0, 1.08, 1.0)
    y = out.values[0].astype(np.float64)
    y = y + 0.33 * sosfilt(butter(2, 180, 'lowpass', fs=SR, output='sos'), y)
    y = y - 0.35 * sosfilt(butter(2, 7000, 'highpass', fs=SR, output='sos'), y)
    # trim silence, pad, level
    env = np.sqrt(np.convolve(y * y, np.ones(256) / 256, 'same')); th = env.max() * 10 ** (-42 / 20)
    idx = np.where(env > th)[0]; a, b = (idx[0], idx[-1]) if len(idx) else (0, len(y))
    y = y[max(0, a - int(0.02 * SR)):min(len(y), b + int(0.04 * SR))]
    y = np.concatenate([np.zeros(int(0.06 * SR)), y, np.zeros(int(0.12 * SR))])
    voiced = env[a:b]; rms = np.sqrt(np.mean(y[np.abs(y) > 0.02] ** 2)) if np.any(np.abs(y) > 0.02) else 0.1
    y *= (10 ** (-17 / 20)) / max(rms, 1e-4)
    peak = np.max(np.abs(y)); y = y / peak * 0.95 if peak > 0.95 else y
    return y
def visemes(y):
    n = int(np.ceil(len(y) / SR * FPS)); hop = SR // FPS
    band = sosfiltfilt(butter(3, [250, 3500], 'bandpass', fs=SR, output='sos'), y)
    rms = np.array([np.sqrt(np.mean(band[i * hop:(i + 1) * hop] ** 2) + 1e-12) for i in range(n)])
    db = 20 * np.log10(rms + 1e-9); top = np.percentile(db, 95); floor = top - 30
    o = np.clip((db - floor) / (top - floor), 0, 1) ** 1.3
    sm = np.zeros(n); v = 0
    for i in range(n): k = 0.55 if o[i] > v else 0.28; v += (o[i] - v) * k; sm[i] = v
    snd = parselmouth.Sound(y, sampling_frequency=SR)
    fm = snd.to_formant_burg(time_step=1 / FPS, max_number_of_formants=5, maximum_formant=5000)
    pitch = snd.to_pitch(time_step=1 / FPS, pitch_floor=60, pitch_ceiling=300)
    rnd, wid = np.zeros(n), np.zeros(n)
    for i in range(n):
        t = (i + 0.5) / FPS
        f2 = fm.get_value_at_time(2, t); f1 = fm.get_value_at_time(1, t); f0 = pitch.get_value_at_time(t)
        if not (f0 and f0 == f0) or not (f2 and f2 == f2): continue
        rnd[i] = np.clip((1250 - f2) / 450, 0, 1); wid[i] = np.clip((f2 - 1650) / 450, 0, 1)
        if f1 and f1 == f1: sm[i] = min(1, sm[i] * (0.85 + 0.35 * np.clip((f1 - 420) / 300, 0, 1)))
    k = np.ones(3) / 3; rnd = np.convolve(rnd, k, 'same') * (sm > 0.08); wid = np.convolve(wid, k, 'same') * (sm > 0.08)
    q = lambda a: [int(round(float(x) * 255)) for x in np.clip(a, 0, 1)]
    return q(sm), q(rnd), q(wid)
lines = json.load(open(LINES, encoding='utf-8'))
man = []; counts = {}
for k, item in enumerate(lines):
    text = item["text"]; src = item["src"].split(":")[0].replace("Bvr2", "").replace(".cs", "")
    counts[src] = counts.get(src, 0) + 1
    h = hashlib.md5(text.encode("utf-8")).hexdigest()[:8]
    name = f"VO_{src}_{counts[src]:03d}_{h}.wav"
    y = process(synth(spoken(text)))
    wavfile.write(f"{OUT}/{name}", SR, (np.clip(y, -1, 1) * 32767).astype(np.int16))
    if FFMPEG:  # Unity re-encodes anyway; OGG keeps the project small
        ogg = name[:-4] + ".ogg"
        subprocess.run([FFMPEG, "-y", "-loglevel", "error", "-i", f"{OUT}/{name}", "-c:a", "libvorbis", "-q:a", "5", "-ac", "1", f"{OUT}/{ogg}"], check=True)
        os.remove(f"{OUT}/{name}"); name = ogg
    o, r, w = visemes(y)
    man.append({"text": text, "spoken": spoken(text), "file": name, "seconds": round(len(y) / SR, 3), "fps": FPS, "open": o, "round": r, "wide": w})
    if k % 20 == 0: print(k, name, round(len(y) / SR, 2), flush=True)
json.dump({"voice": "piper ru_RU-ruslan-medium + Praat (formants 0.965, F0 104 Hz)", "lines": man}, open(os.path.join(OUT, "voice_manifest.json"), "w", encoding="utf-8"), ensure_ascii=False)
print("done", len(man), "lines", round(sum(m["seconds"] for m in man) / 60, 1), "min")
