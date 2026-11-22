using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent music controller auto-spawned at runtime.
/// Plays menu music in hub/menu scenes, keeps playing through Loading,
/// plays level music in gameplay/tutorial scenes, and persists user choices in PlayerPrefs.
/// </summary>
public sealed class MusicManager : MonoBehaviour
{
    public const string DefaultChoice = "Default";

    private const string PrefMenuTrack = "MusicManager.MenuTrack";
    private const string PrefLevelTrack = "MusicManager.LevelTrack";

    private enum SceneMusicMode { Menu, Level, KeepCurrent }

    private static readonly string[] MenuKeywords =
    {
        "menu", "ambient", "calm", "chill", "lobby", "hub", "title", "soft", "quiet", "slow"
    };

    private static readonly string[] ActionKeywords =
    {
        "action", "battle", "combat", "fight", "boss", "fast", "intense", "chase", "run", "rock", "metal"
    };

    public static MusicManager Instance { get; private set; }

    [SerializeField, Min(0.1f)] private float crossfadeDuration = 1.0f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.55f;
    [SerializeField, Min(1f)] private float minMusicLengthSeconds = 45f;

    private readonly List<AudioClip> tracks = new List<AudioClip>();
    private AudioSource audioSource;
    private AudioClip defaultMenuTrack;
    private AudioClip defaultLevelTrack;
    private SceneMusicMode currentMode = SceneMusicMode.Menu;
    private Coroutine fadeRoutine;

    public string SelectedMenuChoice => GetSavedChoice(PrefMenuTrack);
    public string SelectedLevelChoice => GetSavedChoice(PrefLevelTrack);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeOnBoot()
    {
        EnsureCreated();
    }

    public static MusicManager EnsureCreated()
    {
        if (Instance != null)
            return Instance;

        MusicManager existing = FindFirstObjectByType<MusicManager>();
        if (existing != null)
        {
            Instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            return existing;
        }

        GameObject go = new GameObject(nameof(MusicManager));
        DontDestroyOnLoad(go);
        return go.AddComponent<MusicManager>();
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
        GameSettings.EnsureLoaded();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
        audioSource.priority = 0;
        audioSource.volume = 0f;

        LoadTracks();
        ResolveDefaults();

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        ApplyForScene(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (Instance == this)
            Instance = null;
    }

    public List<string> GetDropdownChoices()
    {
        List<string> choices = new List<string>(tracks.Count + 1) { DefaultChoice };
        for (int i = 0; i < tracks.Count; i++)
        {
            if (tracks[i] != null && !string.IsNullOrEmpty(tracks[i].name))
                choices.Add(tracks[i].name);
        }
        return choices;
    }

    public void SetMenuTrackChoice(string choice)
    {
        string normalized = NormalizeChoice(choice);
        PlayerPrefs.SetString(PrefMenuTrack, normalized);
        PlayerPrefs.Save();

        if (currentMode == SceneMusicMode.Menu)
            PlayTrack(ResolveTrack(normalized, defaultMenuTrack));
    }

    public void SetLevelTrackChoice(string choice)
    {
        string normalized = NormalizeChoice(choice);
        PlayerPrefs.SetString(PrefLevelTrack, normalized);
        PlayerPrefs.Save();

        if (currentMode == SceneMusicMode.Level)
            PlayTrack(ResolveTrack(normalized, defaultLevelTrack));
    }

    private void LoadTracks()
    {
        tracks.Clear();

        MusicCatalog catalog = Resources.Load<MusicCatalog>("MusicCatalog");
        if (catalog != null && catalog.tracks != null)
        {
            for (int i = 0; i < catalog.tracks.Length; i++)
                AddUniqueClip(catalog.tracks[i]);
        }

        AudioClip[] folderClips = Resources.LoadAll<AudioClip>("Music");
        for (int i = 0; i < folderClips.Length; i++)
        {
            AudioClip clip = folderClips[i];
            if (clip != null && clip.length >= minMusicLengthSeconds)
                AddUniqueClip(clip);
        }
    }

    private void AddUniqueClip(AudioClip clip)
    {
        if (clip != null && !tracks.Contains(clip))
            tracks.Add(clip);
    }

    private void ResolveDefaults()
    {
        if (tracks.Count == 0)
        {
            defaultMenuTrack = null;
            defaultLevelTrack = null;
            Debug.LogWarning("[MusicManager] No music tracks found in MusicCatalog or Resources/Music.");
            return;
        }

        defaultMenuTrack = FindByKeywords(MenuKeywords, exclude: null) ?? tracks[0];
        defaultLevelTrack = FindByKeywords(ActionKeywords, exclude: defaultMenuTrack)
            ?? (tracks.Count > 1 ? FirstDifferentFrom(defaultMenuTrack) : null)
            ?? FindByKeywords(ActionKeywords, exclude: null)
            ?? tracks[0];
    }

    private AudioClip FindByKeywords(string[] keywords, AudioClip exclude)
    {
        for (int i = 0; i < tracks.Count; i++)
        {
            AudioClip clip = tracks[i];
            if (clip == null || clip == exclude)
                continue;

            string lower = clip.name.ToLowerInvariant();
            for (int k = 0; k < keywords.Length; k++)
            {
                if (lower.Contains(keywords[k]))
                    return clip;
            }
        }
        return null;
    }

    private AudioClip FirstDifferentFrom(AudioClip exclude)
    {
        for (int i = 0; i < tracks.Count; i++)
        {
            if (tracks[i] != null && tracks[i] != exclude)
                return tracks[i];
        }
        return null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyForScene(scene);
    }

    private void ApplyForScene(Scene scene)
    {
        SceneMusicMode mode = ClassifyScene(scene);
        if (mode == SceneMusicMode.KeepCurrent)
            return;

        currentMode = mode;
        AudioClip target = mode == SceneMusicMode.Menu
            ? ResolveTrack(SelectedMenuChoice, defaultMenuTrack)
            : ResolveTrack(SelectedLevelChoice, defaultLevelTrack);

        PlayTrack(target);
    }

    private static SceneMusicMode ClassifyScene(Scene scene)
    {
        if (!scene.IsValid())
            return SceneMusicMode.KeepCurrent;

        string name = scene.name;
        if (string.Equals(name, "Loading", StringComparison.OrdinalIgnoreCase))
            return SceneMusicMode.KeepCurrent;

        if (string.Equals(name, "MainMenu", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "SampleScene", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "EndingLevel", StringComparison.OrdinalIgnoreCase)
            || scene.buildIndex == 0)
            return SceneMusicMode.Menu;

        return SceneMusicMode.Level;
    }

    private string GetSavedChoice(string prefKey)
    {
        return NormalizeChoice(PlayerPrefs.GetString(prefKey, DefaultChoice));
    }

    private string NormalizeChoice(string choice)
    {
        if (string.IsNullOrEmpty(choice) || string.Equals(choice, DefaultChoice, StringComparison.Ordinal))
            return DefaultChoice;

        for (int i = 0; i < tracks.Count; i++)
        {
            if (tracks[i] != null && string.Equals(tracks[i].name, choice, StringComparison.Ordinal))
                return tracks[i].name;
        }

        return DefaultChoice;
    }

    private AudioClip ResolveTrack(string choice, AudioClip fallbackDefault)
    {
        if (!string.IsNullOrEmpty(choice) && !string.Equals(choice, DefaultChoice, StringComparison.Ordinal))
        {
            for (int i = 0; i < tracks.Count; i++)
            {
                if (tracks[i] != null && string.Equals(tracks[i].name, choice, StringComparison.Ordinal))
                    return tracks[i];
            }
        }
        return fallbackDefault;
    }

    private void PlayTrack(AudioClip targetClip)
    {
        if (audioSource == null)
            return;

        // Same clip already playing across a scene transition must continue uninterrupted
        if (targetClip != null && audioSource.clip == targetClip && audioSource.isPlaying)
        {
            if (!Mathf.Approximately(audioSource.volume, musicVolume))
            {
                if (fadeRoutine != null) StopCoroutine(fadeRoutine);
                fadeRoutine = StartCoroutine(FadeVolumeRoutine(musicVolume, crossfadeDuration * 0.5f));
            }
            return;
        }

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(CrossfadeRoutine(targetClip));
    }

    private IEnumerator CrossfadeRoutine(AudioClip targetClip)
    {
        float halfFade = Mathf.Max(0.05f, crossfadeDuration * 0.5f);

        if (audioSource.isPlaying && audioSource.clip != null && audioSource.volume > 0.001f)
        {
            float startVol = audioSource.volume;
            float t = 0f;
            while (t < halfFade)
            {
                t += Time.unscaledDeltaTime;
                audioSource.volume = Mathf.Lerp(startVol, 0f, Mathf.Clamp01(t / halfFade));
                yield return null;
            }
            audioSource.volume = 0f;
            audioSource.Stop();
        }

        if (targetClip == null)
        {
            audioSource.clip = null;
            fadeRoutine = null;
            yield break;
        }

        if (targetClip.loadState == AudioDataLoadState.Unloaded)
            targetClip.LoadAudioData();

        audioSource.clip = targetClip;
        audioSource.volume = 0f;
        audioSource.Play();

        float fadeIn = 0f;
        while (fadeIn < halfFade)
        {
            fadeIn += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(0f, musicVolume, Mathf.Clamp01(fadeIn / halfFade));
            yield return null;
        }

        audioSource.volume = musicVolume;
        fadeRoutine = null;
    }

    private IEnumerator FadeVolumeRoutine(float targetVolume, float duration)
    {
        float dur = Mathf.Max(0.05f, duration);
        float startVol = audioSource.volume;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(startVol, targetVolume, Mathf.Clamp01(t / dur));
            yield return null;
        }
        audioSource.volume = targetVolume;
        fadeRoutine = null;
    }
}
