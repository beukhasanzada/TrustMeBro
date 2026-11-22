using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Plays the ending cutscene for EndingLevel:
/// 1) Smooth fly-through over the shattered chaotic dream office,
/// 2) Fade to black,
/// 3) Slow eyelid blinks waking up with our head resting on our desk/chair in a spotless sunlit office,
///    looking left and right around the tidy workspace, then pulling back to "THE END".
/// </summary>
public sealed class EndingCutsceneController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private UIDocument endingUiDoc;
    [SerializeField] private Light sunLight;
    [SerializeField] private GameObject chaosSetRoot;
    [SerializeField] private GameObject neatOfficeSetRoot;

    [Header("Act 1 — Chaotic Office Fly-Through")]
    [SerializeField] private Transform[] chaosWaypoints = System.Array.Empty<Transform>();
    [SerializeField] private Transform[] chaosLookTargets = System.Array.Empty<Transform>();
    [SerializeField, Min(2f)] private float chaosDuration = 8.5f;

    [Header("Act 2 & 3 — Wake Up & Pullback")]
    [SerializeField] private Transform sleepPose;
    [SerializeField] private Transform awakePose;
    [SerializeField] private Transform lookLeftTarget;
    [SerializeField] private Transform lookRightTarget;
    [SerializeField] private Transform finalDeskTarget;
    [SerializeField] private Transform heroPullbackPose;

    [Header("Atmosphere")]
    [SerializeField] private Material chaosSkybox;
    [SerializeField] private Material neatSkybox;

    [Header("Transitions")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string loadingSceneName = "Loading";

    private VisualElement eyelidTop;
    private VisualElement eyelidBottom;
    private VisualElement fadeCurtain;
    private VisualElement endingCardRoot;
    private Label captionLabel;
    private Label skipHintLabel;
    private Label crystalsLabel;
    private Button menuButton;
    private Button quitButton;

    private Coroutine sequenceRoutine;
    private bool cardShown;
    private bool loadingScene;

    private void OnEnable()
    {
        Time.timeScale = 1f;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        if (mainCamera == null) mainCamera = Camera.main;

        if (endingUiDoc != null && endingUiDoc.rootVisualElement != null)
        {
            VisualElement root = endingUiDoc.rootVisualElement;
            eyelidTop = root.Q<VisualElement>("eyelid-top");
            eyelidBottom = root.Q<VisualElement>("eyelid-bottom");
            fadeCurtain = root.Q<VisualElement>("fade-curtain");
            endingCardRoot = root.Q<VisualElement>("ending-card-root");
            captionLabel = root.Q<Label>("ending-caption");
            skipHintLabel = root.Q<Label>("skip-hint");
            crystalsLabel = root.Q<Label>("ending-crystals");
            menuButton = root.Q<Button>("btn-ending-menu");
            quitButton = root.Q<Button>("btn-ending-quit");

            if (menuButton != null) menuButton.clicked += HandleMainMenu;
            if (quitButton != null) quitButton.clicked += HandleQuit;
        }

        if (sequenceRoutine != null) StopCoroutine(sequenceRoutine);
        sequenceRoutine = StartCoroutine(PlayEndingSequence());
    }

    private void OnDisable()
    {
        if (menuButton != null) menuButton.clicked -= HandleMainMenu;
        if (quitButton != null) quitButton.clicked -= HandleQuit;
    }

    private void Update()
    {
        if (cardShown) return;

        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame))
            SkipToFinalCard();
    }

    private IEnumerator PlayEndingSequence()
    {
        ApplyChaosAtmosphere();
        if (chaosSetRoot != null) chaosSetRoot.SetActive(true);
        if (neatOfficeSetRoot != null) neatOfficeSetRoot.SetActive(false);

        SetEyelids(0f);
        SetCurtain(1f);
        SetCaption("", visible: false);

        if (chaosWaypoints.Length > 0 && mainCamera != null)
        {
            mainCamera.transform.position = chaosWaypoints[0].position;
            mainCamera.fieldOfView = 64f;
            if (chaosLookTargets.Length > 0 && chaosLookTargets[0] != null)
            {
                Vector3 dir = chaosLookTargets[0].position - mainCamera.transform.position;
                if (dir.sqrMagnitude > 0.001f)
                    mainCamera.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }
        }

        // Act 1: Fade in & smoothly tour the shattered chaotic office
        float t = 0f;
        bool captionShown = false;
        while (t < chaosDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / chaosDuration);

            if (t < 1.0f)
                SetCurtain(1f - Mathf.SmoothStep(0f, 1f, t / 1.0f));
            else
                SetCurtain(0f);

            if (!captionShown && t >= 1.2f)
            {
                captionShown = true;
                SetCaption("Everything fell apart...", visible: true);
            }
            else if (t >= chaosDuration - 1.6f)
            {
                SetCaption("", visible: false);
            }

            UpdateChaosCamera(Mathf.SmoothStep(0f, 1f, u), Time.deltaTime);
            yield return null;
        }

        // Fade to black & close eyelids before reality transition
        SetCaption("", visible: false);
        yield return FadeCurtain(0f, 1f, 1.0f);
        SetEyelids(52f);

        if (chaosSetRoot != null) chaosSetRoot.SetActive(false);
        if (neatOfficeSetRoot != null) neatOfficeSetRoot.SetActive(true);
        ApplyNeatMorningAtmosphere();

        if (mainCamera != null && sleepPose != null)
        {
            mainCamera.transform.SetPositionAndRotation(sleepPose.position, sleepPose.rotation);
            mainCamera.fieldOfView = 54f;
        }

        yield return new WaitForSeconds(0.25f);
        yield return FadeCurtain(1f, 0f, 0.45f);

        // Act 2: Slow drowsy blinks at our desk, waking up & looking around
        yield return AnimateEyelids(52f, 26f, 0.95f);
        yield return new WaitForSeconds(0.42f);
        yield return AnimateEyelids(26f, 50f, 0.42f);
        yield return new WaitForSeconds(0.18f);

        // Lift head from sleepPose to seated awakePose while opening eyelids cleanly
        if (mainCamera != null && sleepPose != null && awakePose != null)
        {
            Vector3 startPos = sleepPose.position;
            Quaternion startRot = sleepPose.rotation;
            Quaternion wakeLook = finalDeskTarget != null
                ? Quaternion.LookRotation((finalDeskTarget.position - awakePose.position).normalized, Vector3.up)
                : awakePose.rotation;

            float liftDur = 2.2f;
            float liftElapsed = 0f;
            while (liftElapsed < liftDur)
            {
                liftElapsed += Time.deltaTime;
                float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(liftElapsed / liftDur));
                float eyeOpen = Mathf.SmoothStep(50f, 0f, Mathf.Clamp01(liftElapsed / 1.15f));
                SetEyelids(eyeOpen);

                mainCamera.transform.position = Vector3.Lerp(startPos, awakePose.position, s);
                mainCamera.transform.rotation = Quaternion.Slerp(startRot, wakeLook, s);
                mainCamera.fieldOfView = Mathf.Lerp(54f, 68f, s);
                yield return null;
            }
            SetEyelids(0f);
        }
        else
        {
            yield return AnimateEyelids(50f, 0f, 1.0f);
        }

        SetCaption("Monday, 09:02 AM. Just a dream.", visible: true);
        yield return new WaitForSeconds(0.35f);

        // Look around the clean, orderly sunlit office
        yield return TurnCameraToward(lookLeftTarget, 1.55f);
        yield return new WaitForSeconds(0.25f);
        yield return TurnCameraToward(lookRightTarget, 1.75f);
        yield return new WaitForSeconds(0.25f);
        yield return TurnCameraToward(finalDeskTarget, 1.20f);

        SetCaption("", visible: false);

        // Act 3: Gentle pullback framing our tidy workstation & sunlit office
        if (mainCamera != null && heroPullbackPose != null)
        {
            Vector3 p0 = mainCamera.transform.position;
            Quaternion r0 = mainCamera.transform.rotation;
            float fov0 = mainCamera.fieldOfView;
            Vector3 lookPt = finalDeskTarget != null ? finalDeskTarget.position : p0 + Vector3.forward * 2f;
            Quaternion r1 = Quaternion.LookRotation((lookPt - heroPullbackPose.position).normalized, Vector3.up);

            float pullDur = 2.6f;
            float pe = 0f;
            while (pe < pullDur)
            {
                pe += Time.deltaTime;
                float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(pe / pullDur));
                mainCamera.transform.position = Vector3.Lerp(p0, heroPullbackPose.position, s);
                mainCamera.transform.rotation = Quaternion.Slerp(r0, r1, s);
                mainCamera.fieldOfView = Mathf.Lerp(fov0, 62f, s);
                yield return null;
            }
        }

        RevealEndingCard();
    }

    private void SkipToFinalCard()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        if (chaosSetRoot != null) chaosSetRoot.SetActive(false);
        if (neatOfficeSetRoot != null) neatOfficeSetRoot.SetActive(true);
        ApplyNeatMorningAtmosphere();

        SetEyelids(0f);
        SetCurtain(0f);
        SetCaption("", visible: false);

        if (mainCamera != null && heroPullbackPose != null)
        {
            mainCamera.transform.position = heroPullbackPose.position;
            if (finalDeskTarget != null)
            {
                Vector3 dir = (finalDeskTarget.position - heroPullbackPose.position).normalized;
                mainCamera.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
            mainCamera.fieldOfView = 62f;
        }

        RevealEndingCard();
    }

    private void RevealEndingCard()
    {
        if (cardShown) return;
        cardShown = true;

        if (skipHintLabel != null)
            skipHintLabel.style.display = DisplayStyle.None;

        if (crystalsLabel != null)
        {
            int count = CrystalCurrencyManager.TotalCrystals;
            if (count > 0)
            {
                crystalsLabel.text = $"Crystals collected: {count}";
                crystalsLabel.RemoveFromClassList("hidden-display");
            }
            else
            {
                crystalsLabel.AddToClassList("hidden-display");
            }
        }

        if (endingCardRoot != null)
            endingCardRoot.RemoveFromClassList("screen--hidden");

        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        menuButton?.schedule.Execute(() => menuButton.Focus()).StartingIn(40);
    }

    private void ApplyChaosAtmosphere()
    {
        if (chaosSkybox != null) RenderSettings.skybox = chaosSkybox;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.42f, 0.48f, 0.68f);
        RenderSettings.ambientEquatorColor = new Color(0.32f, 0.34f, 0.44f);
        RenderSettings.ambientGroundColor = new Color(0.18f, 0.17f, 0.24f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.06f, 0.08f, 0.14f);
        RenderSettings.fogStartDistance = 28f;
        RenderSettings.fogEndDistance = 120f;

        if (sunLight != null)
        {
            sunLight.color = new Color(0.86f, 0.90f, 1.0f);
            sunLight.intensity = 1.15f;
            sunLight.transform.rotation = Quaternion.Euler(36f, -35f, 0f);
        }
    }

    private void ApplyNeatMorningAtmosphere()
    {
        if (neatSkybox != null) RenderSettings.skybox = neatSkybox;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.86f, 0.91f, 0.98f);
        RenderSettings.ambientEquatorColor = new Color(0.76f, 0.72f, 0.66f);
        RenderSettings.ambientGroundColor = new Color(0.45f, 0.40f, 0.36f);
        RenderSettings.fog = false;

        if (sunLight != null)
        {
            sunLight.color = new Color(1.0f, 0.95f, 0.86f);
            sunLight.intensity = 1.75f;
            sunLight.transform.rotation = Quaternion.Euler(46f, 38f, 0f);
        }
    }

    private void UpdateChaosCamera(float u, float dt)
    {
        if (mainCamera == null || chaosWaypoints.Length < 2) return;

        float scaled = u * (chaosWaypoints.Length - 1);
        int i1 = Mathf.Min(chaosWaypoints.Length - 2, Mathf.FloorToInt(scaled));
        int i0 = Mathf.Max(0, i1 - 1);
        int i2 = i1 + 1;
        int i3 = Mathf.Min(chaosWaypoints.Length - 1, i2 + 1);
        float segT = scaled - i1;

        Vector3 pos = CatmullRom(
            chaosWaypoints[i0].position,
            chaosWaypoints[i1].position,
            chaosWaypoints[i2].position,
            chaosWaypoints[i3].position,
            segT);
        mainCamera.transform.position = pos;

        if (chaosLookTargets.Length >= 2)
        {
            float lookScaled = u * (chaosLookTargets.Length - 1);
            int li1 = Mathf.Min(chaosLookTargets.Length - 2, Mathf.FloorToInt(lookScaled));
            int li2 = li1 + 1;
            Vector3 lookPos = Vector3.Lerp(chaosLookTargets[li1].position, chaosLookTargets[li2].position, lookScaled - li1);
            Vector3 dir = lookPos - pos;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion desired = Quaternion.LookRotation(dir.normalized, Vector3.up);
                mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, desired, 1f - Mathf.Exp(-3.5f * dt));
            }
        }
    }

    private IEnumerator TurnCameraToward(Transform target, float duration)
    {
        if (mainCamera == null || target == null)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        Quaternion r0 = mainCamera.transform.rotation;
        Quaternion r1 = Quaternion.LookRotation((target.position - mainCamera.transform.position).normalized, Vector3.up);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            mainCamera.transform.rotation = Quaternion.Slerp(r0, r1, s);
            yield return null;
        }
    }

    private IEnumerator FadeCurtain(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            SetCurtain(Mathf.Lerp(from, to, s));
            yield return null;
        }
        SetCurtain(to);
    }

    private IEnumerator AnimateEyelids(float fromPercent, float toPercent, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            SetEyelids(Mathf.Lerp(fromPercent, toPercent, s));
            yield return null;
        }
        SetEyelids(toPercent);
    }

    private void SetEyelids(float percent)
    {
        Length h = Length.Percent(Mathf.Clamp(percent, 0f, 55f));
        if (eyelidTop != null) eyelidTop.style.height = h;
        if (eyelidBottom != null) eyelidBottom.style.height = h;
    }

    private void SetCurtain(float alpha)
    {
        if (fadeCurtain != null)
            fadeCurtain.style.opacity = Mathf.Clamp01(alpha);
    }

    private void SetCaption(string text, bool visible)
    {
        if (captionLabel == null) return;
        if (!string.IsNullOrEmpty(text))
            captionLabel.text = text;
        captionLabel.EnableInClassList("ending-caption--hidden", !visible);
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    private void HandleMainMenu()
    {
        if (loadingScene) return;
        loadingScene = true;
        Time.timeScale = 1f;

        if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName) && Application.CanStreamedLevelBeLoaded(loadingSceneName))
        {
            LoadingData.nextScene = mainMenuSceneName;
            SceneManager.LoadScene(loadingSceneName);
        }
        else if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogWarning($"[EndingCutsceneController] Scene '{mainMenuSceneName}' not in Build Settings.");
            loadingScene = false;
        }
    }

    private void HandleQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
