using Godot;
using System;
using System.Collections.Generic;

// Procedural sound: no audio files. Effects and the two music loops (calm jazz -> frantic ragtime) are synthesised at startup.
public partial class Sfx : Node
{
    const int Rate = 22050;
    const int PoolSize = 24;
    readonly List<AudioStreamPlayer> pool = new List<AudioStreamPlayer>();
    readonly Dictionary<string, AudioStreamWav> cache = new Dictionary<string, AudioStreamWav>();
    int poolNext;
    public bool Muted;

    string mode = "off";
    int step;
    double acc;
    readonly Random rnd = new Random(7);

    static readonly int[][] CalmBass = { new[] { 36, 39, 41, 43 }, new[] { 41, 44, 46, 44 }, new[] { 43, 47, 50, 47 }, new[] { 36, 43, 39, 43 } };
    static readonly int[] CalmMel = { 60, 0, 62, 63, 0, 67, 0, 0, 65, 0, 68, 65, 0, 63, 0, 0, 62, 0, 65, 67, 0, 71, 0, 67, 63, 0, 62, 60, 0, 0, 0, 0 };
    static readonly int[][] RagBass = { new[] { 48, 43 }, new[] { 43, 50 }, new[] { 48, 43 }, new[] { 41, 48 } };
    static readonly int[][] RagChord = { new[] { 64, 67, 72 }, new[] { 62, 65, 71 }, new[] { 64, 67, 72 }, new[] { 60, 65, 69 } };
    static readonly int[] RagMel = { 76, 0, 79, 76, 72, 0, 74, 76, 77, 0, 74, 71, 74, 77, 0, 74, 76, 79, 0, 84, 79, 76, 72, 0, 77, 0, 72, 69, 72, 0, 77, 0 };

    public override void _Ready()
    {
        Muted = AudioServer.IsBusMute(0);
        for (int i = 0; i < PoolSize; i++)
        {
            var p = new AudioStreamPlayer();
            AddChild(p);
            pool.Add(p);
        }
        // sound effects
        cache["swish"] = Wav(Noise(0.13f, 0.14f, "hp", 3500f));
        cache["splat"] = Wav(Mix(Noise(0.2f, 0.32f, "lp", 500f), Blip(320f, 70f, 0.18f, 0, 0.3f), 0));
        cache["bark"] = Wav(Mix(Blip(560f, 300f, 0.12f, 3, 0.13f), Blip(520f, 270f, 0.12f, 3, 0.13f), (int)(0.17f * Rate)));
        cache["pop"] = Wav(Blip(180f, 520f, 0.15f, 2, 0.12f));
        cache["scoop"] = Wav(Mix(Noise(0.12f, 0.2f, "lp", 900f), Blip(200f, 420f, 0.1f, 0, 0.15f), 0));
        cache["buzz"] = Wav(Mix(Blip(170f, 110f, 0.22f, 3, 0.14f), Blip(160f, 100f, 0.22f, 3, 0.14f), (int)(0.12f * Rate)));
        cache["slip"] = Wav(Mix(Blip(760f, 160f, 0.55f, 0, 0.2f), Noise(0.3f, 0.1f, "hp", 2500f), 0));
        cache["thud"] = Wav(Mix(Blip(130f, 45f, 0.22f, 0, 0.4f), Noise(0.1f, 0.18f, "lp", 400f), 0));
        cache["zap"] = Wav(Mix(Blip(950f, 90f, 0.32f, 3, 0.15f), Noise(0.25f, 0.14f, "hp", 4000f), 0));
        cache["scrub"] = Wav(Noise(0.14f, 0.13f, "bp", 1800f));
        cache["ding"] = Wav(Mix(Tone(88, 0.3f, 1, 0.18f), Tone(95, 0.4f, 1, 0.18f), (int)(0.1f * Rate)));
        float[] win = new float[0];
        int[] notes = { 60, 64, 67, 72, 76, 79, 84 };
        for (int i = 0; i < notes.Length; i++) win = Mix(win, Tone(notes[i], 0.4f, 1, 0.2f), (int)(i * 0.09f * Rate));
        cache["win"] = Wav(win);
    }

    // ---------------------------------------------------------------- synthesis
    static float Wave(int type, float cycles)
    {
        float f = cycles - Mathf.Floor(cycles);
        switch (type)
        {
            case 0: return Mathf.Sin(f * Mathf.Tau);                 // sine
            case 1: return 4f * Mathf.Abs(f - 0.5f) - 1f;            // triangle
            case 2: return f < 0.5f ? 1f : -1f;                      // square
            default: return 2f * f - 1f;                             // saw
        }
    }

    static float Mtof(int m) { return 440f * Mathf.Pow(2f, (m - 69) / 12f); }

    static float[] Blip(float f0, float f1, float dur, int wave, float vol)
    {
        int n = (int)(dur * Rate);
        var d = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float f = f0 * Mathf.Pow(f1 / f0, t / dur);
            phase += f / Rate;
            float env = vol * Mathf.Pow(0.0001f / vol, t / dur);
            d[i] = Wave(wave, phase) * env;
        }
        return d;
    }

    static float[] Tone(int midi, float dur, int wave, float vol)
    {
        int n = (int)(dur * Rate);
        var d = new float[n];
        float freq = Mtof(midi), att = 0.012f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float env = t < att ? vol * t / att : vol * Mathf.Pow(0.0001f / vol, (t - att) / Mathf.Max(0.001f, dur - att));
            d[i] = Wave(wave, freq * t) * env;
        }
        return d;
    }

    float[] Noise(float dur, float vol, string filter, float freq)
    {
        int n = (int)(dur * Rate);
        var d = new float[n];
        float a = 1f - Mathf.Exp(-Mathf.Tau * freq / Rate);
        float lp = 0f, lp2 = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float x = (float)(rnd.NextDouble() * 2.0 - 1.0);
            float y;
            lp += a * (x - lp);
            if (filter == "lp") y = lp;
            else if (filter == "hp") y = x - lp;
            else { lp2 += a * 0.5f * (x - lp2); y = lp - lp2; y *= 2f; }   // rough band-pass
            float env = vol * Mathf.Pow(0.0001f / vol, t / dur);
            d[i] = y * env;
        }
        return d;
    }

    static float[] Mix(float[] a, float[] b, int offset)
    {
        int n = Math.Max(a.Length, b.Length + offset);
        var d = new float[n];
        for (int i = 0; i < a.Length; i++) d[i] += a[i];
        for (int i = 0; i < b.Length; i++) d[i + offset] += b[i];
        return d;
    }

    static AudioStreamWav Wav(float[] samples)
    {
        var data = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            short s = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767f);
            data[i * 2] = (byte)(s & 0xff);
            data[i * 2 + 1] = (byte)((s >> 8) & 0xff);
        }
        var w = new AudioStreamWav();
        w.Format = AudioStreamWav.FormatEnum.Format16Bits;
        w.MixRate = Rate;
        w.Stereo = false;
        w.Data = data;
        return w;
    }

    // ---------------------------------------------------------------- playback
    void Play(AudioStreamWav w)
    {
        AudioStreamPlayer p = null;
        foreach (var c in pool) if (!c.Playing) { p = c; break; }
        if (p == null) { p = pool[poolNext]; poolNext = (poolNext + 1) % pool.Count; }
        p.Stream = w;
        p.Play();
    }

    void Fx(string name) { AudioStreamWav w; if (cache.TryGetValue(name, out w)) Play(w); }

    void Note(int midi, float dur, int wave, float vol)
    {
        string key = "n" + midi + "_" + (int)(dur * 1000f) + "_" + wave + "_" + (int)(vol * 1000f);
        AudioStreamWav w;
        if (!cache.TryGetValue(key, out w)) { w = Wav(Tone(midi, dur, wave, vol)); cache[key] = w; }
        Play(w);
    }

    void Hat(string key, float dur, float vol, string filter, float freq)
    {
        AudioStreamWav w;
        if (!cache.TryGetValue(key, out w)) { w = Wav(Noise(dur, vol, filter, freq)); cache[key] = w; }
        Play(w);
    }

    public void Swish() { Fx("swish"); }
    public void Splat() { Fx("splat"); }
    public void Bark() { Fx("bark"); }
    public void Pop() { Fx("pop"); }
    public void Scoop() { Fx("scoop"); }
    public void Buzz() { Fx("buzz"); }
    public void Slip() { Fx("slip"); }
    public void Thud() { Fx("thud"); }
    public void Zap() { Fx("zap"); }
    public void Scrub() { Fx("scrub"); }
    public void Ding() { Fx("ding"); }
    public void Win() { Fx("win"); }

    public void ToggleMute() { Muted = !Muted; AudioServer.SetBusMute(0, Muted); }

    public void SetMode(string m) { mode = m; step = 0; acc = 0.05; }   // "off", "calm" or "panic"

    public override void _Process(double delta)
    {
        if (mode == "off") return;
        acc -= delta;
        while (acc <= 0.0)
        {
            double dur;
            if (mode == "calm")
            {
                dur = 60.0 / 104.0 / 2.0;
                PlayCalm(step);
                acc += dur * (step % 2 == 0 ? 1.28 : 0.72);   // swing
            }
            else
            {
                dur = 60.0 / 185.0 / 2.0;
                PlayPanic(step);
                acc += dur;
            }
            step = (step + 1) % 32;
        }
    }

    void PlayCalm(int st)
    {
        int bar = st / 8, s = st % 8;
        if (s % 2 == 0) Note(CalmBass[bar][s / 2], 0.26f, 1, 0.32f);
        int m = CalmMel[st];
        if (m != 0) { Note(m, 0.2f, 0, 0.16f); Note(m + 12, 0.1f, 1, 0.03f); }
        if (s % 2 == 1) Hat("hat_c", 0.05f, 0.05f, "hp", 6000f);
        if (s == 2 || s == 6) Hat("snr_c", 0.04f, 0.09f, "bp", 2500f);
    }

    void PlayPanic(int st)
    {
        int bar = st / 8, s = st % 8;
        if (s == 0) Note(RagBass[bar][0], 0.18f, 1, 0.34f);
        if (s == 4) Note(RagBass[bar][1], 0.18f, 1, 0.34f);
        if (s == 2 || s == 6) foreach (int n in RagChord[bar]) Note(n, 0.11f, 2, 0.045f);
        int m = RagMel[st];
        if (m != 0) { Note(m, 0.13f, 2, 0.06f); Note(m + 12, 0.08f, 1, 0.04f); }
        if (s % 2 == 0) Hat("hat_p", 0.03f, 0.04f, "hp", 7000f);
    }
}
