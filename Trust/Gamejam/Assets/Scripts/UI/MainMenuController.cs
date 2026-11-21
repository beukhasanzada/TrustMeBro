using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

[RequireComponent(typeof(UIDocument))]
public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private string firstLevel = "Level1";
    [SerializeField] private string tutorial = "TutorialScene";
    [SerializeField] private string loadingScene = "Loading";

    private Button playButton;
    private Button tutorialButton;
    private Button quitButton;
    private Label status;
    private bool leaving;

    private void OnEnable()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        GameSettings.EnsureLoaded();

        var root = GetComponent<UIDocument>().rootVisualElement;
        playButton = root.Q<Button>("play-button");
        tutorialButton = root.Q<Button>("tutorial-button");
        quitButton = root.Q<Button>("quit-button");
        status = root.Q<Label>("menu-status");
        playButton.clicked += Play;
        tutorialButton.clicked += Train;
        quitButton.clicked += Quit;
        playButton.schedule.Execute(() => playButton.Focus());
    }

    private void OnDisable()
    {
        if (playButton == null) return;
        playButton.clicked -= Play;
        tutorialButton.clicked -= Train;
        quitButton.clicked -= Quit;
    }

    private void Play() => Load(firstLevel);
    private void Train() => Load(tutorial);

    private void Load(string destination)
    {
        if (leaving) return;
        if (!Application.CanStreamedLevelBeLoaded(destination) || !Application.CanStreamedLevelBeLoaded(loadingScene))
        {
            status.text = $"Unable to open {destination}.";
            return;
        }

        leaving = true;
        LoadingData.nextScene = destination;
        SceneManager.LoadScene(loadingScene);
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
