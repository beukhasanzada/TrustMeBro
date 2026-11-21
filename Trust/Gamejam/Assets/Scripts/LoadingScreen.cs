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
    [SerializeField] private string fallbackScene = "MainMenu";
    [SerializeField, Min(0f)] private float fadeInDuration = 0.18f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.18f;

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
        string targetScene = LoadingData.nextScene;
        LoadingData.nextScene = "";

        if (!CanLoadDestination(targetScene))
        {
            if (!string.IsNullOrWhiteSpace(targetScene))
                Debug.LogWarning($"[LoadingScreen] Destination '{targetScene}' is unavailable. Trying '{fallbackScene}'.", this);

            targetScene = fallbackScene;
        }

        AsyncOperation loadOp = CanLoadDestination(targetScene) ? SceneManager.LoadSceneAsync(targetScene) : null;
        if (loadOp == null)
        {
            Debug.LogError($"[LoadingScreen] Could not load scene '{targetScene}'.", this);
            yield return Fade(1f, 0f, fadeInDuration);
            if (messageText != null)
                messageText.text = "Unable to load the next scene.";
            yield break;
        }

        loadOp.allowSceneActivation = false;
        yield return Fade(1f, 0f, fadeInDuration);

        sequenceRunning = true;
        Coroutine glitchRoutine = enableGlitches ? StartCoroutine(RunRandomGlitches()) : null;

        // Unity stops at 0.9 until the scene is allowed to activate.
        while (loadOp.progress < 0.9f)
        {
            ApplyProgress(loadOp.progress / 0.9f * 100f);
            yield return null;
        }

        ApplyProgress(100f);
        sequenceRunning = false;
        if (glitchRoutine != null)
        {
            StopCoroutine(glitchRoutine);
            RestoreVisualsAfterGlitch();
        }

        yield return Fade(0f, 1f, fadeOutDuration);
        loadOp.allowSceneActivation = true;
    }

    private bool CanLoadDestination(string sceneName) => !string.IsNullOrWhiteSpace(sceneName)
        && !string.Equals(sceneName, gameObject.scene.name, StringComparison.OrdinalIgnoreCase)
        && !string.Equals(sceneName, gameObject.scene.path, StringComparison.OrdinalIgnoreCase)
        && Application.CanStreamedLevelBeLoaded(sceneName);

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
