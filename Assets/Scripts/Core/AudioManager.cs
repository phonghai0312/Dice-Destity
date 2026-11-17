using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// AudioManager — Quản lý toàn bộ âm thanh (BGM & SFX) và cài đặt người chơi (Volume, Mute, Screen Shake).
/// Tự động tạo AudioClip dự phòng bằng tổng hợp âm thanh (procedural audio) nếu chưa có file âm thanh trong Resources/Audio.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string PrefMasterVol = "DD_MasterVolume";
    private const string PrefBgmVol = "DD_BgmVolume";
    private const string PrefSfxVol = "DD_SfxVolume";
    private const string PrefMuted = "DD_Muted";
    private const string PrefScreenShake = "DD_ScreenShake";

    public static float MasterVolume { get; private set; } = 0.85f;
    public static float BgmVolume { get; private set; } = 0.60f;
    public static float SfxVolume { get; private set; } = 0.90f;
    public static bool IsMuted { get; private set; } = false;
    public static bool IsScreenShakeEnabled { get; private set; } = true;

    public static event Action OnSettingsChanged;

    private AudioSource bgmSource;
    private AudioSource sfxSource;

    private readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();
    private string currentBgmKey = string.Empty;

    public static AudioManager ResolveInstance()
    {
        if (Instance != null) return Instance;
        Instance = FindAnyObjectByType<AudioManager>();
        if (Instance != null) return Instance;

        GameObject go = new GameObject("AudioManager");
        Instance = go.AddComponent<AudioManager>();
        DontDestroyOnLoad(go);
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupAudioSources();
        LoadSettings();
        ApplyVolumes();

        SceneManager.sceneLoaded += OnSceneLoaded;
        PlayBgmForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void SetupAudioSources()
    {
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.spatialBlend = 0f;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
    }

    private void LoadSettings()
    {
        MasterVolume = PlayerPrefs.GetFloat(PrefMasterVol, 0.85f);
        BgmVolume = PlayerPrefs.GetFloat(PrefBgmVol, 0.60f);
        SfxVolume = PlayerPrefs.GetFloat(PrefSfxVol, 0.90f);
        IsMuted = PlayerPrefs.GetInt(PrefMuted, 0) == 1;
        IsScreenShakeEnabled = PlayerPrefs.GetInt(PrefScreenShake, 1) == 1;
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetFloat(PrefMasterVol, MasterVolume);
        PlayerPrefs.SetFloat(PrefBgmVol, BgmVolume);
        PlayerPrefs.SetFloat(PrefSfxVol, SfxVolume);
        PlayerPrefs.SetInt(PrefMuted, IsMuted ? 1 : 0);
        PlayerPrefs.SetInt(PrefScreenShake, IsScreenShakeEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyVolumes()
    {
        float effectiveMaster = IsMuted ? 0f : MasterVolume;
        if (bgmSource != null)
        {
            bgmSource.volume = effectiveMaster * BgmVolume * 0.45f;
        }
        if (sfxSource != null)
        {
            sfxSource.volume = effectiveMaster * SfxVolume;
        }
        AudioListener.volume = IsMuted ? 0f : 1f;
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        ApplyVolumes();
        SaveSettings();
        OnSettingsChanged?.Invoke();
    }

    public void SetBgmVolume(float value)
    {
        BgmVolume = Mathf.Clamp01(value);
        ApplyVolumes();
        SaveSettings();
        OnSettingsChanged?.Invoke();
    }

    public void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        ApplyVolumes();
        SaveSettings();
        OnSettingsChanged?.Invoke();
    }

    public void SetMuted(bool muted)
    {
        IsMuted = muted;
        ApplyVolumes();
        SaveSettings();
        OnSettingsChanged?.Invoke();
    }

    public void SetScreenShake(bool enabled)
    {
        IsScreenShakeEnabled = enabled;
        SaveSettings();
        OnSettingsChanged?.Invoke();
    }

    public void ResetToDefaults()
    {
        MasterVolume = 0.85f;
        BgmVolume = 0.60f;
        SfxVolume = 0.90f;
        IsMuted = false;
        IsScreenShakeEnabled = true;
        ApplyVolumes();
        SaveSettings();
        OnSettingsChanged?.Invoke();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayBgmForScene(scene.name);
    }

    public void PlayBgmForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        string key = "bgm_menu";
        if (sceneName.StartsWith("CombatScene", StringComparison.OrdinalIgnoreCase))
            key = "bgm_combat";
        else if (sceneName.Equals("TheForge", StringComparison.OrdinalIgnoreCase))
            key = "bgm_forge";
        else if (sceneName.Equals("StageMap", StringComparison.OrdinalIgnoreCase))
            key = "bgm_map";

        if (currentBgmKey == key && bgmSource != null && bgmSource.isPlaying) return;
        currentBgmKey = key;

        AudioClip clip = GetOrCreateClip(key);
        if (bgmSource != null && clip != null)
        {
            bgmSource.clip = clip;
            bgmSource.Play();
        }
    }

    // ─── SFX Public API ───────────────────────────────────────────────────────────

    public void PlayButtonClick() => PlaySfx("sfx_click", 0.7f);
    public void PlayDiceRoll() => PlaySfx("sfx_dice_roll", 0.9f);
    public void PlayAttack() => PlaySfx("sfx_attack", 0.95f);
    public void PlayShield() => PlaySfx("sfx_shield", 0.85f);
    public void PlayFire() => PlaySfx("sfx_fire", 0.9f);
    public void PlayPoison() => PlaySfx("sfx_poison", 0.85f);
    public void PlayLightning() => PlaySfx("sfx_lightning", 0.95f);
    public void PlayHeal() => PlaySfx("sfx_heal", 0.85f);
    public void PlayHit() => PlaySfx("sfx_hit", 0.85f);
    public void PlayForge() => PlaySfx("sfx_forge", 0.95f);
    public void PlayVictory() => PlaySfx("sfx_victory", 1.0f);
    public void PlayDefeat() => PlaySfx("sfx_defeat", 0.95f);

    public void PlaySfx(string key, float volumeScale = 1f)
    {
        if (IsMuted || MasterVolume <= 0.001f || SfxVolume <= 0.001f) return;
        if (sfxSource == null) return;

        AudioClip clip = GetOrCreateClip(key);
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }
    }

    // ─── Clip Loading & Procedural Fallback ───────────────────────────────────────

    private AudioClip GetOrCreateClip(string key)
    {
        if (clipCache.TryGetValue(key, out AudioClip cached) && cached != null)
            return cached;

        // Ưu tiên load file âm thanh thật nếu có trong Assets/Resources/Audio/
        AudioClip resClip = Resources.Load<AudioClip>("Audio/" + key);
        if (resClip != null)
        {
            clipCache[key] = resClip;
            return resClip;
        }

        AudioClip generated = GenerateProceduralClip(key);
        if (generated != null)
            clipCache[key] = generated;
        return generated;
    }

    private AudioClip GenerateProceduralClip(string key)
    {
        const int sampleRate = 22050;
        switch (key)
        {
            case "bgm_menu":
            case "bgm_map":
                return BuildDarkFantasyLoop("BGM_Menu", sampleRate, new float[] { 146.83f, 174.61f, 220.00f, 196.00f, 164.81f, 220.00f, 174.61f, 146.83f }, 0.55f, false);
            case "bgm_combat":
                return BuildDarkFantasyLoop("BGM_Combat", sampleRate, new float[] { 110.00f, 130.81f, 146.83f, 130.81f, 110.00f, 164.81f, 155.56f, 130.81f }, 0.34f, true);
            case "bgm_forge":
                return BuildDarkFantasyLoop("BGM_Forge", sampleRate, new float[] { 130.81f, 196.00f, 164.81f, 196.00f, 146.83f, 220.00f, 196.00f, 164.81f }, 0.50f, false);

            case "sfx_click":
                return BuildToneBurst("SFX_Click", sampleRate, 0.055f, 720f, 480f, 0.35f, false);
            case "sfx_dice_roll":
                return BuildDiceRattle("SFX_DiceRoll", sampleRate, 0.32f);
            case "sfx_attack":
                return BuildSlash("SFX_Attack", sampleRate, 0.18f);
            case "sfx_shield":
                return BuildToneBurst("SFX_Shield", sampleRate, 0.22f, 320f, 540f, 0.40f, false);
            case "sfx_fire":
                return BuildNoiseBurst("SFX_Fire", sampleRate, 0.28f, 0.42f, 0.25f);
            case "sfx_poison":
                return BuildToneBurst("SFX_Poison", sampleRate, 0.24f, 480f, 210f, 0.35f, true);
            case "sfx_lightning":
                return BuildNoiseBurst("SFX_Lightning", sampleRate, 0.22f, 0.55f, 0.75f);
            case "sfx_heal":
                return BuildArpeggio("SFX_Heal", sampleRate, new float[] { 523.25f, 659.25f, 783.99f }, 0.09f, 0.35f);
            case "sfx_hit":
                return BuildToneBurst("SFX_Hit", sampleRate, 0.14f, 180f, 75f, 0.45f, true);
            case "sfx_forge":
                return BuildAnvilStrike("SFX_Forge", sampleRate, 0.42f);
            case "sfx_victory":
                return BuildArpeggio("SFX_Victory", sampleRate, new float[] { 392.00f, 493.88f, 587.33f, 783.99f }, 0.13f, 0.42f);
            case "sfx_defeat":
                return BuildArpeggio("SFX_Defeat", sampleRate, new float[] { 329.63f, 293.66f, 261.63f, 196.00f }, 0.18f, 0.40f);
            default:
                return null;
        }
    }

    private AudioClip BuildDarkFantasyLoop(string name, int sr, float[] notes, float noteDur, bool withPulse)
    {
        int noteSamples = Mathf.RoundToInt(sr * noteDur);
        int totalSamples = noteSamples * notes.Length;
        float[] data = new float[totalSamples];

        for (int n = 0; n < notes.Length; n++)
        {
            float freq = notes[n];
            for (int i = 0; i < noteSamples; i++)
            {
                float t = (float)i / sr;
                float env = Mathf.Sin(Mathf.PI * i / noteSamples);
                float pad = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.55f
                          + Mathf.Sin(2f * Mathf.PI * (freq * 1.5f) * t) * 0.22f
                          + Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t) * 0.25f;
                float pulse = withPulse ? (Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t) * Mathf.Exp(-6f * t) * 0.35f) : 0f;
                data[n * noteSamples + i] = Mathf.Clamp((pad * env + pulse) * 0.28f, -0.9f, 0.9f);
            }
        }

        AudioClip clip = AudioClip.Create(name, totalSamples, 1, sr, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip BuildToneBurst(string name, int sr, float duration, float startFreq, float endFreq, float amp, bool gritty)
    {
        int samples = Mathf.RoundToInt(sr * duration);
        float[] data = new float[samples];
        float phase = 0f;

        for (int i = 0; i < samples; i++)
        {
            float p = (float)i / samples;
            float freq = Mathf.Lerp(startFreq, endFreq, p);
            phase += 2f * Mathf.PI * freq / sr;
            float wave = gritty ? Mathf.Sign(Mathf.Sin(phase)) * 0.5f + Mathf.Sin(phase) * 0.5f : Mathf.Sin(phase);
            float env = (1f - p) * Mathf.Min(1f, p * 12f);
            data[i] = wave * env * amp;
        }

        AudioClip clip = AudioClip.Create(name, samples, 1, sr, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip BuildDiceRattle(string name, int sr, float duration)
    {
        int samples = Mathf.RoundToInt(sr * duration);
        float[] data = new float[samples];
        System.Random rng = new System.Random(42);
        int clicks = 5;
        int spacing = samples / clicks;

        for (int c = 0; c < clicks; c++)
        {
            int start = c * spacing + rng.Next(0, Mathf.Max(1, spacing / 4));
            int len = Mathf.Min(samples - start, sr / 35);
            float f = 450f + c * 75f;
            for (int i = 0; i < len; i++)
            {
                float p = (float)i / len;
                float env = 1f - p;
                float val = (Mathf.Sin(2f * Mathf.PI * f * i / sr) * 0.6f + ((float)rng.NextDouble() * 2f - 1f) * 0.4f) * env * 0.4f;
                data[start + i] += val;
            }
        }

        AudioClip clip = AudioClip.Create(name, samples, 1, sr, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip BuildSlash(string name, int sr, float duration)
    {
        int samples = Mathf.RoundToInt(sr * duration);
        float[] data = new float[samples];
        System.Random rng = new System.Random(101);
        float phase = 0f;

        for (int i = 0; i < samples; i++)
        {
            float p = (float)i / samples;
            float freq = Mathf.Lerp(900f, 160f, p);
            phase += 2f * Mathf.PI * freq / sr;
            float noise = (float)rng.NextDouble() * 2f - 1f;
            float env = Mathf.Pow(1f - p, 1.5f);
            data[i] = (noise * 0.55f + Mathf.Sin(phase) * 0.45f) * env * 0.5f;
        }

        AudioClip clip = AudioClip.Create(name, samples, 1, sr, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip BuildNoiseBurst(string name, int sr, float duration, float amp, float crackle)
    {
        int samples = Mathf.RoundToInt(sr * duration);
        float[] data = new float[samples];
        System.Random rng = new System.Random(77);
        float prev = 0f;

        for (int i = 0; i < samples; i++)
        {
            float p = (float)i / samples;
            float white = (float)rng.NextDouble() * 2f - 1f;
            prev = Mathf.Lerp(prev, white, 0.25f + crackle * 0.65f);
            float env = (1f - p) * Mathf.Min(1f, p * 16f);
            data[i] = prev * env * amp;
        }

        AudioClip clip = AudioClip.Create(name, samples, 1, sr, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip BuildArpeggio(string name, int sr, float[] notes, float noteDur, float amp)
    {
        int noteSamples = Mathf.RoundToInt(sr * noteDur);
        int totalSamples = noteSamples * notes.Length;
        float[] data = new float[totalSamples];

        for (int n = 0; n < notes.Length; n++)
        {
            float f = notes[n];
            for (int i = 0; i < noteSamples; i++)
            {
                float p = (float)i / noteSamples;
                float env = (1f - p) * Mathf.Min(1f, p * 15f);
                float t = (float)i / sr;
                data[n * noteSamples + i] = (Mathf.Sin(2f * Mathf.PI * f * t) * 0.75f + Mathf.Sin(4f * Mathf.PI * f * t) * 0.25f) * env * amp;
            }
        }

        AudioClip clip = AudioClip.Create(name, totalSamples, 1, sr, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip BuildAnvilStrike(string name, int sr, float duration)
    {
        int samples = Mathf.RoundToInt(sr * duration);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sr;
            float p = (float)i / samples;
            float env = Mathf.Exp(-7f * t);
            float ring = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.45f
                       + Mathf.Sin(2f * Mathf.PI * 1318.5f * t) * 0.35f
                       + Mathf.Sin(2f * Mathf.PI * 1760f * t) * 0.20f;
            data[i] = ring * env * 0.45f * Mathf.Min(1f, p * 40f);
        }

        AudioClip clip = AudioClip.Create(name, samples, 1, sr, false);
        clip.SetData(data, 0);
        return clip;
    }
}
