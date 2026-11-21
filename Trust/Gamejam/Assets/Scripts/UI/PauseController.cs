using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Toggles Pause on Esc (New Input System), manages Time.timeScale, TimeManager, cursor,
/// and disables/enables RigidbodyFpsController, WeaponController, and FpsCameraRig.
/// Always resets Time.timeScale = 1 on scene load.
/// </summary>
public class PauseController : MonoBehaviour
{
    private UIManager uiManager;
    private RigidbodyFpsController fpsController;
    private WeaponController weaponController;
    private FpsCameraRig cameraRig;
    private TimeManager timeManager;

    public bool IsPaused { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneReset()
    {
        SceneManager.sceneLoaded -= OnAnySceneLoaded;
        SceneManager.sceneLoaded += OnAnySceneLoaded;
    }

    private static void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;
    }

    public void Initialize(
        UIManager manager,
        RigidbodyFpsController fps,
        WeaponController weapon,
        FpsCameraRig rig,
        TimeManager timer)
    {
        uiManager = manager;
        fpsController = fps;
        weaponController = weapon;
        cameraRig = rig;
        timeManager = timer;
        IsPaused = false;
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (uiManager == null) return;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            if (uiManager.IsSettingsOpen)
                uiManager.CloseSettings();
            else if (uiManager.CanTogglePause)
                TogglePause();
        }
    }

    public void TogglePause()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;

        Time.timeScale = 0f;
        timeManager?.StopTimer();
        SetGameplayControlActive(false);
        uiManager?.SetPauseVisible(true);
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;

        Time.timeScale = 1f;
        timeManager?.StartTimer();
        SetGameplayControlActive(true);
        uiManager?.SetPauseVisible(false);
    }

    public void SetGameplayControlActive(bool active)
    {
        if (fpsController != null) fpsController.enabled = active;
        if (weaponController != null) weaponController.enabled = active;
        if (cameraRig != null) cameraRig.enabled = active;

        UnityEngine.Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
        UnityEngine.Cursor.visible = !active;
    }

    private void OnDestroy()
    {
        if (IsPaused) Time.timeScale = 1f;
    }
}
