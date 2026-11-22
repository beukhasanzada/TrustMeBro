using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent combat hitstop / micro-slowdown, camera FOV punch, and headshot/kill audio feedback.
/// Auto-created at runtime so zero scene setup is required.
/// </summary>
public sealed class CombatFeelManager : MonoBehaviour
{
    public static CombatFeelManager Instance { get; private set; }

    private const float BodyHitSlowCooldown = 0.11f;

    private AudioSource audioSource;
    private AudioClip headshotSfx;
    private AudioClip killPunchSfx;
    private FpsCameraRig cameraRig;
    private Coroutine slowmoRoutine;

    private float baseFixedDeltaTime = 0.02f;
    private float lastBodySlowAt = float.NegativeInfinity;
    private bool slowmoActive;
    private int activePriority;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeOnBoot()
    {
        EnsureCreated();
    }

    public static CombatFeelManager EnsureCreated()
    {
        if (Instance != null)
            return Instance;

        CombatFeelManager existing = FindFirstObjectByType<CombatFeelManager>();
        if (existing != null)
        {
            Instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            return existing;
        }

        GameObject go = new GameObject(nameof(CombatFeelManager));
        DontDestroyOnLoad(go);
        return go.AddComponent<CombatFeelManager>();
    }

    public static void TriggerHit(bool isHeadshot, bool isKill)
    {
        CombatFeelManager mgr = EnsureCreated();
        mgr.HandleTriggerHit(isHeadshot, isKill);
    }

    public static void CancelSlowmo()
    {
        if (Instance == null) return;
        Instance.StopSlowmoInternal();
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

        baseFixedDeltaTime = Time.fixedDeltaTime > 0.001f
            ? Mathf.Clamp(Time.fixedDeltaTime, 0.005f, 0.05f)
            : 0.02f;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.priority = 10;

        headshotSfx = CreateHeadshotClip();
        killPunchSfx = CreateKillPunchClip();

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (slowmoActive)
        {
            if (Time.timeScale > 0.001f)
                Time.timeScale = 1f;
            Time.fixedDeltaTime = baseFixedDeltaTime;
        }

        if (headshotSfx != null) Destroy(headshotSfx);
        if (killPunchSfx != null) Destroy(killPunchSfx);

        if (Instance == this)
            Instance = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopSlowmoInternal();
        Time.timeScale = 1f;
        Time.fixedDeltaTime = baseFixedDeltaTime;
        cameraRig = null;
    }

    private void HandleTriggerHit(bool isHeadshot, bool isKill)
    {
        // Never unpause when the game is paused or on end-of-level modals
        if (Time.timeScale <= 0.001f && !slowmoActive)
            return;

        int priority;
        float targetScale;
        float holdSeconds;
        float recoverSeconds;
        float fovPunch;

        if (isHeadshot && isKill)
        {
            priority = 3;
            targetScale = 0.22f;
            holdSeconds = 0.11f;
            recoverSeconds = 0.12f;
            fovPunch = -2.6f;
        }
        else if (isKill || isHeadshot)
        {
            priority = 2;
            targetScale = 0.42f;
            holdSeconds = 0.085f;
            recoverSeconds = 0.09f;
            fovPunch = -1.7f;
        }
        else
        {
            priority = 1;
            targetScale = 0.72f;
            holdSeconds = 0.045f;
            recoverSeconds = 0.055f;
            fovPunch = -0.8f;
        }

        if (isHeadshot && headshotSfx != null)
            audioSource.PlayOneShot(headshotSfx, isKill ? 0.55f : 0.42f);
        else if (isKill && killPunchSfx != null)
            audioSource.PlayOneShot(killPunchSfx, 0.42f);

        if (cameraRig == null)
            cameraRig = FindFirstObjectByType<FpsCameraRig>();
        cameraRig?.AddHitFovPunch(fovPunch);

        if (priority == 1)
        {
            if (slowmoActive && activePriority > 1)
                return;
            if (Time.unscaledTime - lastBodySlowAt < BodyHitSlowCooldown)
                return;
            lastBodySlowAt = Time.unscaledTime;
        }

        if (slowmoRoutine != null)
            StopCoroutine(slowmoRoutine);

        activePriority = priority;
        slowmoActive = true;
        slowmoRoutine = StartCoroutine(SlowmoRoutine(targetScale, holdSeconds, recoverSeconds));
    }

    private void StopSlowmoInternal()
    {
        if (slowmoRoutine != null)
        {
            StopCoroutine(slowmoRoutine);
            slowmoRoutine = null;
        }
        slowmoActive = false;
        activePriority = 0;
        Time.fixedDeltaTime = baseFixedDeltaTime;
    }

    private IEnumerator SlowmoRoutine(float targetScale, float holdSeconds, float recoverSeconds)
    {
        Time.timeScale = targetScale;
        Time.fixedDeltaTime = baseFixedDeltaTime * Mathf.Max(0.05f, targetScale);

        float elapsed = 0f;
        while (elapsed < holdSeconds)
        {
            yield return null;
            if (Time.timeScale <= 0.001f)
            {
                AbortExternalPause();
                yield break;
            }
            elapsed += Time.unscaledDeltaTime;
        }

        float recoverElapsed = 0f;
        float invRecover = 1f / Mathf.Max(0.01f, recoverSeconds);
        while (recoverElapsed < recoverSeconds)
        {
            yield return null;
            if (Time.timeScale <= 0.001f)
            {
                AbortExternalPause();
                yield break;
            }

            recoverElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(recoverElapsed * invRecover);
            float scale = Mathf.SmoothStep(targetScale, 1f, t);
            Time.timeScale = scale;
            Time.fixedDeltaTime = baseFixedDeltaTime * Mathf.Max(0.05f, scale);
        }

        Time.timeScale = 1f;
        Time.fixedDeltaTime = baseFixedDeltaTime;
        slowmoActive = false;
        activePriority = 0;
        slowmoRoutine = null;
    }

    private void AbortExternalPause()
    {
        Time.fixedDeltaTime = baseFixedDeltaTime;
        slowmoActive = false;
        activePriority = 0;
        slowmoRoutine = null;
    }

    private static AudioClip CreateHeadshotClip()
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.RoundToInt(sampleRate * 0.13f);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envHigh = Mathf.Exp(-38f * t);
            float envLow = Mathf.Exp(-28f * t);
            float dink = (Mathf.Sin(2f * Mathf.PI * 1850f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 2780f * t) * 0.4f) * envHigh;
            float thump = Mathf.Sin(2f * Mathf.PI * 115f * t) * 0.45f * envLow;
            samples[i] = Mathf.Clamp((dink + thump) * 0.9f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("HeadshotDink", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateKillPunchClip()
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.RoundToInt(sampleRate * 0.09f);
        float[] samples = new float[sampleCount];
        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float freq = Mathf.Lerp(140f, 70f, Mathf.Clamp01(t / 0.09f));
            phase += 2f * Mathf.PI * freq / sampleRate;
            float env = Mathf.Exp(-34f * t);
            samples[i] = Mathf.Clamp(Mathf.Sin(phase) * env * 0.85f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("KillPunch", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
