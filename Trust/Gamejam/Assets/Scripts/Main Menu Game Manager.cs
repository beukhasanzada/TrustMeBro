using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuGameManager : MonoBehaviour
{
    public void Play()
    {
        SceneManager.LoadScene("Game");
    }

    public void OnQuitButtonClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }
}
