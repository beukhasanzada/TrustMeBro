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
        triggered = true;

        LoadingData.nextScene = targetScene;
        SceneManager.LoadScene(loadingSceneName);
    }
}
