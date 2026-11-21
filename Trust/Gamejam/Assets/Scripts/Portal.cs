using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    public string targetScene = "Level1";
    bool used;

    void OnTriggerEnter(Collider other)
    {
        if (used || !other.CompareTag("Player")) return;
        used = true;
        SceneManager.LoadScene(targetScene);
    }
}