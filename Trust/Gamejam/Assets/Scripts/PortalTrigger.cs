using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalTrigger : MonoBehaviour
{
    [SerializeField] private string targetScene = "Level1";
    [SerializeField] private string loadingSceneName = "Loading";
    [SerializeField] private string playerTag = "Player";

    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;

        TriggerPortal();
    }

    public void TriggerPortal()
    {
        if (triggered) return;

        if (string.IsNullOrWhiteSpace(targetScene) || string.IsNullOrWhiteSpace(loadingSceneName)
            || targetScene == loadingSceneName || !Application.CanStreamedLevelBeLoaded(targetScene)
            || !Application.CanStreamedLevelBeLoaded(loadingSceneName))
        {
            Debug.LogWarning($"Portal requires a valid destination and loading scene. Destination '{targetScene}', loading scene '{loadingSceneName}'.", this);
            return;
        }

        triggered = true;

        LoadingData.nextScene = targetScene;
        SceneManager.LoadScene(loadingSceneName);
    }
}
