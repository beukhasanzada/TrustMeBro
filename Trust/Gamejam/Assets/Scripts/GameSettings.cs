using System;
using UnityEngine;

/// <summary>
/// Persistent player settings (PlayerPrefs). Loaded and applied automatically before every scene loads.
/// </summary>
public static class GameSettings
{
    private const string KeyMouseSens = "GameSettings.MouseSensitivity";
    private const string KeyAimMult = "GameSettings.AimSensitivityMultiplier";
    private const string KeyFov = "GameSettings.Fov";
    private const string KeyVolume = "GameSettings.MasterVolume";

    public const float DefaultMouseSensitivity = 1.0f;
    public const float DefaultAimSensitivityMultiplier = 0.6f;
    public const float DefaultFov = 78f;
    public const float DefaultMasterVolume = 1.0f;

    public static float MouseSensitivity { get; private set; } = DefaultMouseSensitivity;
    public static float AimSensitivityMultiplier { get; private set; } = DefaultAimSensitivityMultiplier;
    public static float Fov { get; private set; } = DefaultFov;
    public static float MasterVolume { get; private set; } = DefaultMasterVolume;

    public static event Action Changed;

    private static bool loaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeOnBoot()
    {
        Load();
        ApplyAudio();
    }

    public static void EnsureLoaded()
    {
        if (!loaded)
        {
            Load();
            ApplyAudio();
        }
    }

    public static void Load()
    {
        MouseSensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(KeyMouseSens, DefaultMouseSensitivity), 0.2f, 3.0f);
        AimSensitivityMultiplier = Mathf.Clamp(PlayerPrefs.GetFloat(KeyAimMult, DefaultAimSensitivityMultiplier), 0.2f, 1.0f);
        Fov = Mathf.Clamp(PlayerPrefs.GetFloat(KeyFov, DefaultFov), 70f, 110f);
        MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyVolume, DefaultMasterVolume));
        loaded = true;
    }

    public static void SetMouseSensitivity(float value)
    {
        EnsureLoaded();
        MouseSensitivity = Mathf.Clamp(value, 0.2f, 3.0f);
        PlayerPrefs.SetFloat(KeyMouseSens, MouseSensitivity);
        Changed?.Invoke();
    }

    public static void SetAimSensitivityMultiplier(float value)
    {
        EnsureLoaded();
        AimSensitivityMultiplier = Mathf.Clamp(value, 0.2f, 1.0f);
        PlayerPrefs.SetFloat(KeyAimMult, AimSensitivityMultiplier);
        Changed?.Invoke();
    }

    public static void SetFov(float value)
    {
        EnsureLoaded();
        Fov = Mathf.Clamp(value, 70f, 110f);
        PlayerPrefs.SetFloat(KeyFov, Fov);
        Changed?.Invoke();
    }

    public static void SetMasterVolume(float value)
    {
        EnsureLoaded();
        MasterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(KeyVolume, MasterVolume);
        ApplyAudio();
        Changed?.Invoke();
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    private static void ApplyAudio()
    {
        AudioListener.volume = MasterVolume;
    }
}
