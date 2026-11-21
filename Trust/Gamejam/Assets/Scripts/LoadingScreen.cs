using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    [Serializable]
    public struct MessageThreshold
    {
        [Range(0f, 100f)] public float minPercent;
        public string message;
    }

    [Header("Scene Flow")]
    [Tooltip("Used if LoadingData.nextScene is empty (e.g. when testing the Loading scene directly).")]
    [SerializeField] private string fallbackScene = "SampleScene";
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    [Header("Fake Sequence Timing (~5s total)")]
    [SerializeField, Range(1f, 98f)] private float fastPhaseEndPercent = 60f;
    [SerializeField] private float fastPhaseDuration = 1.2f;
    [SerializeField, Range(1f, 99f)] private float slowPhaseEndPercent = 99f;
    [SerializeField] private float slowPhaseDuration = 1.9f;
    [SerializeField] private float holdAt99Duration = 1.5f;
    [SerializeField] private float holdAt100Duration = 0.25f;

    [Header("Messages")]
    [SerializeField] private MessageThreshold[] messageThresholds = new MessageThreshold[]
    {
        new MessageThreshold { minPercent = 0f,  message = "Loading gravity..." },
        new MessageThreshold { minPercent = 25f, message = "Waking up drones..." },
        new MessageThreshold { minPercent = 55f, message = "Looking for the exit..." },
        new MessageThreshold { minPercent = 80f, message = "Calculating escape route..." }
    };

    [Header("UI References")]
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private Transform loadingBarRoot;
    [SerializeField] private Text percentageText;
    [SerializeField] private Text messageText;
    [SerializeField] private Text hintsText;
    [SerializeField] private RectTransform cornerSpinner;
    [SerializeField] private Image fadeOverlay;

    [Header("Bar & Spinner Style")]
    [SerializeField] private Color filledBlockColor = Color.white;
    [SerializeField] private Color emptyBlockColor = new Color(0.18f, 0.18f, 0.18f, 1f);
    [SerializeField] private float spinnerDegreesPerSecond = -180f;

    [Header("Glitch Effect (1-frame shake or flicker)")]
    [SerializeField] private bool enableGlitches = true;
    [SerializeField] private float glitchMinInterval = 0.6f;
    [SerializeField] private float glitchMaxInterval = 1.6f;
    [SerializeField] private float shakeMagnitude = 16f;
    [SerializeField] private Color flickerColor = new Color(0.25f, 1f, 0.85f, 1f);

    private readonly List<Image> barBlocks = new List<Image>();
    private Vector2 contentBasePosition;
    private Color percentageBaseColor = Color.white;
    private Color messageBaseColor = Color.white;
    private float currentPercent;
    private int currentFilledBlocks = -1;
    private bool glitchActive;
    private bool sequenceRunning;

    private void Awake()
    {
        CollectBarBlocks();

        if (contentRoot != null)
            contentBasePosition = contentRoot.anchoredPosition;
        if (percentageText != null)
            percentageBaseColor = percentageText.color;
        if (messageText != null)
            messageBaseColor = messageText.color;

        if (fadeOverlay != null)
        {
            Color c = fadeOverlay.color;
            c.a = 1f;
            fadeOverlay.color = c;
        }

        ApplyProgress(0f);
    }

    private void Start()
    {
        StartCoroutine(RunLoadingFlow());
    }

    private void Update()
    {
        if (cornerSpinner != null)
        {
            cornerSpinner.Rotate(0f, 0f, spinnerDegreesPerSecond * Time.unscaledDeltaTime);
        }
    }

    private void CollectBarBlocks()
    {
        barBlocks.Clear();
        if (loadingBarRoot == null) return;

        for (int i = 0; i < loadingBarRoot.childCount; i++)
        {
            Image img = loadingBarRoot.GetChild(i).GetComponent<Image>();
            if (img != null)
            {
                barBlocks.Add(img);
            }
        }
    }

    private IEnumerator RunLoadingFlow()
    {
        string targetScene = string.IsNullOrEmpty(LoadingData.nextScene) ? fallbackScene : LoadingData.nextScene;
        AsyncOperation loadOp = null;

        if (!string.IsNullOrEmpty(targetScene) && Application.CanStreamedLevelBeLoaded(targetScene))
        {
            loadOp = SceneManager.LoadSceneAsync(targetScene);
            if (loadOp != null)
                loadOp.allowSceneActivation = false;
        }
        else
        {
            Debug.LogWarning($"[LoadingScreen] Target scene '{targetScene}' is not in Build Settings. Running visual sequence only.");
        }

        // 1. Fade in from white (0.3s)
        yield return Fade(1f, 0f, fadeInDuration);

        // 2. Start fake loading sequence + random glitches
        sequenceRunning = true;
        Coroutine glitchRoutine = enableGlitches ? StartCoroutine(RunRandomGlitches()) : null;

        // Phase A: 0% -> 60% fast
        yield return AnimatePercent(0f, fastPhaseEndPercent, fastPhaseDuration, t => t);

        // Phase B: 60% -> 99% slow down (ease-out)
        yield return AnimatePercent(fastPhaseEndPercent, slowPhaseEndPercent, slowPhaseDuration, t => 1f - (1f - t) * (1f - t));

        // Phase C: Hold at 99% (~1.5s)
        ApplyProgress(slowPhaseEndPercent);
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, holdAt99Duration));

        // Wait if the real async load hasn't reached 0.9 yet
        if (loadOp != null)
        {
            while (loadOp.progress < 0.9f)
                yield return null;
        }

        // Phase D: Jump to 100%
        ApplyProgress(100f);
        sequenceRunning = false;
        if (glitchRoutine != null)
        {
            StopCoroutine(glitchRoutine);
            RestoreVisualsAfterGlitch();
        }

        if (holdAt100Duration > 0f)
            yield return new WaitForSecondsRealtime(holdAt100Duration);

        // 3. Fade to white
        yield return Fade(0f, 1f, fadeOutDuration);

        // 4. Activate target scene
        if (loadOp != null)
        {
            loadOp.allowSceneActivation = true;
        }
    }

    private IEnumerator AnimatePercent(float from, float to, float duration, Func<float, float> ease)
    {
        if (duration <= 0f)
        {
            ApplyProgress(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float value = Mathf.Lerp(from, to, ease(t));
            ApplyProgress(value);
            yield return null;
        }

        ApplyProgress(to);
    }

    private void ApplyProgress(float percent)
    {
        currentPercent = Mathf.Clamp(percent, 0f, 100f);

        if (percentageText != null)
            percentageText.text = $"{Mathf.FloorToInt(currentPercent)}%";

        UpdateSegmentedBar(currentPercent);
        UpdateMessage(currentPercent);
    }

    private void UpdateSegmentedBar(float percent)
    {
        int total = barBlocks.Count;
        if (total == 0) return;

        int filledCount = percent >= 100f
            ? total
            : Mathf.Clamp(Mathf.FloorToInt((percent / 100f) * total), 0, total - 1);

        if (filledCount == currentFilledBlocks && !glitchActive)
            return;

        currentFilledBlocks = filledCount;
        if (glitchActive) return;

        for (int i = 0; i < total; i++)
        {
            barBlocks[i].color = i < filledCount ? filledBlockColor : emptyBlockColor;
        }
    }

    private void UpdateMessage(float percent)
    {
        if (messageText == null || messageThresholds == null || messageThresholds.Length == 0)
            return;

        string chosen = messageThresholds[0].message;
        float bestThreshold = float.NegativeInfinity;

        for (int i = 0; i < messageThresholds.Length; i++)
        {
            if (percent >= messageThresholds[i].minPercent && messageThresholds[i].minPercent >= bestThreshold)
            {
                bestThreshold = messageThresholds[i].minPercent;
                chosen = messageThresholds[i].message;
            }
        }

        if (messageText.text != chosen)
            messageText.text = chosen;
    }

    private IEnumerator RunRandomGlitches()
    {
        while (sequenceRunning)
        {
            float wait = UnityEngine.Random.Range(Mathf.Max(0.1f, glitchMinInterval), Mathf.Max(glitchMinInterval, glitchMaxInterval));
            yield return new WaitForSecondsRealtime(wait);

            if (!sequenceRunning) yield break;

            bool doShake = UnityEngine.Random.value < 0.5f;
            glitchActive = true;

            if (doShake && contentRoot != null)
            {
                Vector2 offset = new Vector2(
                    UnityEngine.Random.Range(-shakeMagnitude, shakeMagnitude),
                    UnityEngine.Random.Range(-shakeMagnitude * 0.5f, shakeMagnitude * 0.5f)
                );
                contentRoot.anchoredPosition = contentBasePosition + offset;
            }
            else
            {
                for (int i = 0; i < barBlocks.Count; i++)
                {
                    if (i < currentFilledBlocks)
                        barBlocks[i].color = flickerColor;
                }
                if (percentageText != null) percentageText.color = flickerColor;
                if (messageText != null) messageText.color = flickerColor;
            }

            // Hold glitch for exactly one frame
            yield return null;

            RestoreVisualsAfterGlitch();
        }
    }

    private void RestoreVisualsAfterGlitch()
    {
        glitchActive = false;

        if (contentRoot != null)
            contentRoot.anchoredPosition = contentBasePosition;

        if (percentageText != null)
            percentageText.color = percentageBaseColor;

        if (messageText != null)
            messageText.color = messageBaseColor;

        for (int i = 0; i < barBlocks.Count; i++)
        {
            barBlocks[i].color = i < currentFilledBlocks ? filledBlockColor : emptyBlockColor;
        }
    }

    private IEnumerator Fade(float fromAlpha, float toAlpha, float duration)
    {
        if (fadeOverlay == null) yield break;

        Color c = fadeOverlay.color;
        if (duration <= 0f)
        {
            c.a = toAlpha;
            fadeOverlay.color = c;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(fromAlpha, toAlpha, Mathf.Clamp01(elapsed / duration));
            fadeOverlay.color = c;
            yield return null;
        }

        c.a = toAlpha;
        fadeOverlay.color = c;
    }
}
