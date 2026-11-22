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
    private enum ScreenState { Playing, Paused, Settings, Dead, Escaped }

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
    private VisualElement settingsRoot;
    private VisualElement deathRoot;
    private VisualElement escapeRoot;

    private Button btnResume;
    private Button btnSettingsBack;
    private Button btnRetry;
    private Button btnEscapeNext;
    private Button btnEscapeMenu;
    private Slider sliderMouseSens;
    private Slider sliderAimSens;
    private Slider sliderFov;
    private Slider sliderVolume;
    private Label valMouseSens;
    private Label valAimSens;
    private Label valFov;
    private Label valVolume;
    private Label escapeLine1;
    private Label escapeLine2;
    private VisualElement escapeActions;
    private DropdownField dropdownMenuMusic;
    private DropdownField dropdownLevelMusic;
    private Label deathReasonLabel;

    private ScreenState state = ScreenState.Playing;
    private Coroutine escapeRoutine;

    public bool CanTogglePause => state == ScreenState.Playing || state == ScreenState.Paused;
    public bool IsSettingsOpen => state == ScreenState.Settings;

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
        settingsRoot = root.Q<VisualElement>("settings-root");
        deathRoot = root.Q<VisualElement>("death-root");
        escapeRoot = root.Q<VisualElement>("escape-root");

        escapeLine1 = root.Q<Label>("escape-line-1");
        escapeLine2 = root.Q<Label>("escape-line-2");
        escapeActions = root.Q<VisualElement>("escape-actions");
        deathReasonLabel = root.Q<Label>("death-reason");

        // Pause buttons
        btnResume = root.Q<Button>("btn-resume");
        if (btnResume != null) btnResume.clicked += () => pauseController.Resume();

        Button btnRestart = root.Q<Button>("btn-restart");
        if (btnRestart != null) btnRestart.clicked += RestartActiveScene;

        Button btnSettings = root.Q<Button>("btn-settings");
        if (btnSettings != null) btnSettings.clicked += OpenSettings;

        Button btnPauseMenu = root.Q<Button>("btn-pause-mainmenu");
        if (btnPauseMenu != null) btnPauseMenu.clicked += () => LoadSceneSafe(mainMenuSceneName);

        Button btnQuit = root.Q<Button>("btn-quit");
        if (btnQuit != null) btnQuit.clicked += QuitGame;

        // Settings controls
        SetupSettingsUI(root);

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
    public void SetBossBarVisible(bool visible, string title = "BOSS") => hudController.SetBossBarVisible(visible, title);
    public void SetBossHealthNormalized(float normalized01) => hudController.SetBossHealthNormalized(normalized01);

    public void ShowDeath()
    {
        if (state == ScreenState.Dead || state == ScreenState.Escaped) return;

        if (deathReasonLabel != null)
            deathReasonLabel.text = !string.IsNullOrEmpty(timeManager?.LoseReason)
                ? timeManager.LoseReason
                : "Retry starts the level over.";

        CombatFeelManager.CancelSlowmo();
        Time.timeScale = 0f;
        timeManager?.StopTimer();
        pauseController.SetGameplayControlActive(false);
        SetState(ScreenState.Dead);
        FocusElement(btnRetry);
    }

    public void ShowEscape()
    {
        if (state == ScreenState.Escaped) return;

        CombatFeelManager.CancelSlowmo();
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

    public void OpenSettings()
    {
        if (state != ScreenState.Paused) return;
        RefreshSettingsSliders();
        SetState(ScreenState.Settings);
        FocusElement(sliderMouseSens != null ? (VisualElement)sliderMouseSens : btnSettingsBack);
    }

    public void CloseSettings()
    {
        if (state != ScreenState.Settings) return;
        GameSettings.Save();
        SetState(ScreenState.Paused);
        FocusElement(btnResume);
    }

    private void SetupSettingsUI(VisualElement root)
    {
        GameSettings.EnsureLoaded();

        sliderMouseSens = root.Q<Slider>("slider-mouse-sens");
        sliderAimSens = root.Q<Slider>("slider-aim-sens");
        sliderFov = root.Q<Slider>("slider-fov");
        sliderVolume = root.Q<Slider>("slider-volume");
        valMouseSens = root.Q<Label>("val-mouse-sens");
        valAimSens = root.Q<Label>("val-aim-sens");
        valFov = root.Q<Label>("val-fov");
        valVolume = root.Q<Label>("val-volume");
        dropdownMenuMusic = root.Q<DropdownField>("dropdown-menu-music");
        dropdownLevelMusic = root.Q<DropdownField>("dropdown-level-music");

        if (sliderMouseSens != null)
            sliderMouseSens.RegisterValueChangedCallback(e =>
            {
                GameSettings.SetMouseSensitivity(e.newValue);
                if (valMouseSens != null) valMouseSens.text = $"{GameSettings.MouseSensitivity:0.00}x";
            });

        if (sliderAimSens != null)
            sliderAimSens.RegisterValueChangedCallback(e =>
            {
                GameSettings.SetAimSensitivityMultiplier(e.newValue);
                if (valAimSens != null) valAimSens.text = $"{GameSettings.AimSensitivityMultiplier:0.00}x";
            });

        if (sliderFov != null)
            sliderFov.RegisterValueChangedCallback(e =>
            {
                GameSettings.SetFov(e.newValue);
                if (valFov != null) valFov.text = $"{Mathf.RoundToInt(GameSettings.Fov)}";
            });

        if (sliderVolume != null)
            sliderVolume.RegisterValueChangedCallback(e =>
            {
                GameSettings.SetMasterVolume(e.newValue);
                if (valVolume != null) valVolume.text = $"{Mathf.RoundToInt(GameSettings.MasterVolume * 100f)}%";
            });

        if (dropdownMenuMusic != null)
            dropdownMenuMusic.RegisterValueChangedCallback(e => MusicManager.EnsureCreated().SetMenuTrackChoice(e.newValue));

        if (dropdownLevelMusic != null)
            dropdownLevelMusic.RegisterValueChangedCallback(e => MusicManager.EnsureCreated().SetLevelTrackChoice(e.newValue));

        btnSettingsBack = root.Q<Button>("btn-settings-back");
        if (btnSettingsBack != null) btnSettingsBack.clicked += CloseSettings;

        RefreshSettingsSliders();
    }

    private void RefreshSettingsSliders()
    {
        GameSettings.EnsureLoaded();
        if (sliderMouseSens != null) sliderMouseSens.SetValueWithoutNotify(GameSettings.MouseSensitivity);
        if (sliderAimSens != null) sliderAimSens.SetValueWithoutNotify(GameSettings.AimSensitivityMultiplier);
        if (sliderFov != null) sliderFov.SetValueWithoutNotify(GameSettings.Fov);
        if (sliderVolume != null) sliderVolume.SetValueWithoutNotify(GameSettings.MasterVolume);

        if (valMouseSens != null) valMouseSens.text = $"{GameSettings.MouseSensitivity:0.00}x";
        if (valAimSens != null) valAimSens.text = $"{GameSettings.AimSensitivityMultiplier:0.00}x";
        if (valFov != null) valFov.text = $"{Mathf.RoundToInt(GameSettings.Fov)}";
        if (valVolume != null) valVolume.text = $"{Mathf.RoundToInt(GameSettings.MasterVolume * 100f)}%";

        MusicManager music = MusicManager.EnsureCreated();
        if (dropdownMenuMusic != null)
        {
            dropdownMenuMusic.choices = music.GetDropdownChoices();
            dropdownMenuMusic.SetValueWithoutNotify(music.SelectedMenuChoice);
        }

        if (dropdownLevelMusic != null)
        {
            dropdownLevelMusic.choices = music.GetDropdownChoices();
            dropdownLevelMusic.SetValueWithoutNotify(music.SelectedLevelChoice);
        }
    }

    private void SetState(ScreenState newState)
    {
        state = newState;

        SetScreenVisible(hudRoot, newState == ScreenState.Playing || newState == ScreenState.Paused || newState == ScreenState.Settings, interactive: false);
        SetScreenVisible(pauseRoot, newState == ScreenState.Paused, interactive: true);
        SetScreenVisible(settingsRoot, newState == ScreenState.Settings, interactive: true);
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
