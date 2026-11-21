using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Controls all in-game UI Toolkit screens (HUD, Pause, Out of Time, Escaped)
/// and exposes the public gameplay API.
/// </summary>
[RequireComponent(typeof(UIDocument))]
[RequireComponent(typeof(HUDController))]
[RequireComponent(typeof(PauseController))]
public class UIManager : MonoBehaviour
{
    private enum ScreenState { Playing, Paused, Dead, Escaped }

    [Header("References (auto-found if empty)")]
    [SerializeField] private RigidbodyFpsController fpsController;
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private FpsCameraRig cameraRig;
    [SerializeField] private PlayerSlow playerSlow;
    [SerializeField] private TimeManager timeManager;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [Tooltip("Optional. If set, shows a Next Level button on the Escape screen.")]
    [SerializeField] private string nextLevelSceneName = "";

    [Header("Escape Screen Fade-In")]
    [SerializeField] private float escapeLineDelay = 0.7f;

    private const string ScreenHiddenClass = "screen--hidden";
    private const string HiddenDisplayClass = "hidden-display";

    private UIDocument uiDocument;
    private HUDController hudController;
    private PauseController pauseController;

    private VisualElement hudRoot;
    private VisualElement pauseRoot;
    private VisualElement deathRoot;
    private VisualElement escapeRoot;

    private Button btnResume;
    private Button btnRetry;
    private Button btnEscapeNext;
    private Button btnEscapeMenu;
    private Label escapeLine1;
    private Label escapeLine2;
    private VisualElement escapeActions;

    private ScreenState state = ScreenState.Playing;
    private Coroutine escapeRoutine;

    public bool CanTogglePause => state == ScreenState.Playing || state == ScreenState.Paused;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        hudController = GetComponent<HUDController>();
        pauseController = GetComponent<PauseController>();

        if (fpsController == null) fpsController = FindFirstObjectByType<RigidbodyFpsController>();
        if (weaponController == null) weaponController = FindFirstObjectByType<WeaponController>();
        if (cameraRig == null) cameraRig = FindFirstObjectByType<FpsCameraRig>();
        if (playerSlow == null) playerSlow = FindFirstObjectByType<PlayerSlow>();
        if (timeManager == null) timeManager = GetComponent<TimeManager>() ?? FindFirstObjectByType<TimeManager>();
    }

    private void OnEnable()
    {
        Time.timeScale = 1f;

        VisualElement root = uiDocument.rootVisualElement;
        if (root == null) return;

        hudRoot = root.Q<VisualElement>("hud-root");
        pauseRoot = root.Q<VisualElement>("pause-root");
        deathRoot = root.Q<VisualElement>("death-root");
        escapeRoot = root.Q<VisualElement>("escape-root");

        escapeLine1 = root.Q<Label>("escape-line-1");
        escapeLine2 = root.Q<Label>("escape-line-2");
        escapeActions = root.Q<VisualElement>("escape-actions");

        // Pause buttons
        btnResume = root.Q<Button>("btn-resume");
        if (btnResume != null) btnResume.clicked += () => pauseController.Resume();

        Button btnRestart = root.Q<Button>("btn-restart");
        if (btnRestart != null) btnRestart.clicked += RestartActiveScene;

        Button btnPauseMenu = root.Q<Button>("btn-pause-mainmenu");
        if (btnPauseMenu != null) btnPauseMenu.clicked += () => LoadSceneSafe(mainMenuSceneName);

        Button btnQuit = root.Q<Button>("btn-quit");
        if (btnQuit != null) btnQuit.clicked += QuitGame;

        // Death buttons
        btnRetry = root.Q<Button>("btn-retry");
        if (btnRetry != null) btnRetry.clicked += RestartActiveScene;

        Button btnDeathMenu = root.Q<Button>("btn-death-mainmenu");
        if (btnDeathMenu != null) btnDeathMenu.clicked += () => LoadSceneSafe(mainMenuSceneName);

        // Escape buttons
        btnEscapeNext = root.Q<Button>("btn-escape-nextlevel");
        if (btnEscapeNext != null)
        {
            bool hasNext = !string.IsNullOrEmpty(nextLevelSceneName);
            btnEscapeNext.EnableInClassList(HiddenDisplayClass, !hasNext);
            btnEscapeNext.clicked += () => LoadSceneSafe(nextLevelSceneName);
        }

        btnEscapeMenu = root.Q<Button>("btn-escape-mainmenu");
        if (btnEscapeMenu != null) btnEscapeMenu.clicked += () => LoadSceneSafe(mainMenuSceneName);

        hudController.Initialize(root, weaponController, playerSlow, timeManager);
        pauseController.Initialize(this, fpsController, weaponController, cameraRig, timeManager);

        if (timeManager != null)
        {
            timeManager.OnTimeDepleted -= ShowDeath;
            timeManager.OnTimeDepleted += ShowDeath;
        }

        SetState(ScreenState.Playing);
    }

    private void OnDisable()
    {
        if (timeManager != null)
            timeManager.OnTimeDepleted -= ShowDeath;
    }

    // ------------------------------------------------------------------ Public API

    public void AddBuff(string buffName, Sprite icon = null) => hudController.AddBuff(buffName, icon);
    public void RemoveBuff(string buffName) => hudController.RemoveBuff(buffName);
    public void SetCrosshairEnemy(bool isEnemy) => hudController.SetCrosshairEnemy(isEnemy);

    public void ShowDeath()
    {
        if (state == ScreenState.Dead || state == ScreenState.Escaped) return;

        Time.timeScale = 0f;
        timeManager?.StopTimer();
        pauseController.SetGameplayControlActive(false);
        SetState(ScreenState.Dead);
        FocusElement(btnRetry);
    }

    public void ShowEscape()
    {
        if (state == ScreenState.Escaped) return;

        Time.timeScale = 0f;
        timeManager?.StopTimer();
        pauseController.SetGameplayControlActive(false);
        SetState(ScreenState.Escaped);

        if (escapeRoutine != null) StopCoroutine(escapeRoutine);
        escapeRoutine = StartCoroutine(PlayEscapeFadeSequence());
    }

    internal void SetPauseVisible(bool paused)
    {
        if (state == ScreenState.Dead || state == ScreenState.Escaped) return;

        SetState(paused ? ScreenState.Paused : ScreenState.Playing);
        if (paused) FocusElement(btnResume);
    }

    // ------------------------------------------------------------------ Internals

    private void SetState(ScreenState newState)
    {
        state = newState;

        SetScreenVisible(hudRoot, newState == ScreenState.Playing || newState == ScreenState.Paused, interactive: false);
        SetScreenVisible(pauseRoot, newState == ScreenState.Paused, interactive: true);
        SetScreenVisible(deathRoot, newState == ScreenState.Dead, interactive: true);
        SetScreenVisible(escapeRoot, newState == ScreenState.Escaped, interactive: true);
    }

    private static void SetScreenVisible(VisualElement element, bool visible, bool interactive)
    {
        if (element == null) return;
        element.EnableInClassList(ScreenHiddenClass, !visible);
        element.pickingMode = (visible && interactive) ? PickingMode.Position : PickingMode.Ignore;
    }

    private static void FocusElement(VisualElement element)
    {
        if (element != null)
            element.schedule.Execute(() => element.Focus()).StartingIn(30);
    }

    private IEnumerator PlayEscapeFadeSequence()
    {
        escapeLine1?.RemoveFromClassList("escape-line--visible");
        escapeLine2?.RemoveFromClassList("escape-line--visible");
        escapeActions?.RemoveFromClassList("escape-actions--visible");

        yield return new WaitForSecondsRealtime(0.25f);
        escapeLine1?.AddToClassList("escape-line--visible");

        yield return new WaitForSecondsRealtime(escapeLineDelay);
        escapeLine2?.AddToClassList("escape-line--visible");

        yield return new WaitForSecondsRealtime(escapeLineDelay);
        escapeActions?.AddToClassList("escape-actions--visible");

        bool hasNext = !string.IsNullOrEmpty(nextLevelSceneName);
        FocusElement(hasNext ? btnEscapeNext : btnEscapeMenu);
    }

    private void RestartActiveScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void LoadSceneSafe(string sceneName)
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName))
            SceneManager.LoadScene(sceneName);
        else
            Debug.LogWarning($"[UIManager] Scene '{sceneName}' is not in Build Settings.");
    }

    private void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    [ContextMenu("Test: Add Time (+5s)")]     private void DebugAddTime() => timeManager?.AddTime(5f);
    [ContextMenu("Test: Remove Time (-10s)")] private void DebugRemoveTime() => timeManager?.RemoveTime(10f);
    [ContextMenu("Test: Trigger Time's Up")]  private void DebugShowDeath() => ShowDeath();
    [ContextMenu("Test: Show Escape")]        private void DebugShowEscape() => ShowEscape();
    [ContextMenu("Test: Add Sample Buff")]    private void DebugAddBuff() => AddBuff("Gold Tooth");
    [ContextMenu("Test: Apply Slow (2s)")]    private void DebugApplySlow() => playerSlow?.ApplySlow(0.55f, 2f);
}
