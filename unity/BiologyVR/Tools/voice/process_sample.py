"""Professor voice: Piper ru_RU-ruslan (mature male) -> a touch deeper and warmer, calm pace."""
import sys, numpy as np, parselmouth
from parselmouth.praat import call
from scipy.io import wavfile
from scipy.signal import butter, sosfilt
def process(src, dst, formant=0.965, median=104.0, rng=1.08):
    snd = parselmouth.Sound(src)
    out = call(snd, "Change gender", 65, 300, formant, median, rng, 1.0)
    x = out.values[0].astype(np.float64); sr = int(out.sampling_frequency)
    # warmth: gentle low shelf (+2.5 dB under ~180 Hz) and a soft presence dip of harshness above 7 kHz
    lo = sosfilt(butter(2, 180, 'lowpass', fs=sr, output='sos'), x); x = x + 0.33 * lo
    hi = sosfilt(butter(2, 7000, 'highpass', fs=sr, output='sos'), x); x = x - 0.35 * hi
    x = x / max(1e-6, np.max(np.abs(x))) * 0.89
    wavfile.write(dst, sr, (x * 32767).astype(np.int16))
if __name__ == "__main__":
    process(sys.argv[1], sys.argv[2])
