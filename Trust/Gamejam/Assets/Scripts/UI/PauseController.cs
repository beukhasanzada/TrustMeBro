using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Toggles Pause on Esc (New Input System), manages Time.timeScale, TimeManager, cursor,
/// and disables/enables PlayerController. Always resets Time.timeScale = 1 on scene load.
/// </summary>
public class PauseController : MonoBehaviour
{
    private UIManager uiManager;
    private PlayerController playerController;
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

    public void Initialize(UIManager manager, PlayerController controller, TimeManager timer)
    {
        uiManager = manager;
        playerController = controller;
        timeManager = timer;
        IsPaused = false;
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (uiManager == null || !uiManager.CanTogglePause) return;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
            TogglePause();
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
        if (playerController != null)
            playerController.enabled = active;

        UnityEngine.Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
        UnityEngine.Cursor.visible = !active;
    }

    private void OnDestroy()
    {
        if (IsPaused) Time.timeScale = 1f;
    }
}
