using UnityEngine;

public class SoundManager : MonoBehaviour
{
    // Через это поле другие скрипты обращаются к менеджеру
    public static SoundManager Instance { get; private set; }

    [SerializeField] private Enemy enemyScript;
    [SerializeField] private FallSound fallSoundScript;

    [Header("Audio")]
    public AudioClip[] shootClips;
    public AudioClip[] reloadClips;
    public AudioClip[] fallClips;
    public AudioClip[] deathClips;

    [Header("General volume")]
    [Range(0f, 1f)] public float volume = 1f;

    private AudioClip clip;

    private void Awake()
    {
        // Менеджер должен быть один на сцене
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ===== Эти методы ты будешь вызывать из других скриптов =====

    public void PlayShoot(Vector3 position) { Play(shootClips, position); }
    public void PlayReload(Vector3 position) { Play(reloadClips, position); }
    public void PlayFall(Vector3 position) { Play(fallClips, position); }
    public void PlayDeath(Vector3 position) { Play(deathClips, position); }

    // ===== Внутренняя логика =====

    private void Play(AudioClip[] clips, Vector3 position)
    {
        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning("No audio in Sound Manager!");
            return;
        }

        if (clips == deathClips)
        {   
            if (GameTimer.Instance.isOver) clip = clips[1];
            if (enemyScript.isDead) clip = clips[0];
        }

        if (clips == fallClips)
        {
            clip = clips[0];
        }

        // Проигрывает звук в точке мира и сам удаляет временный объект
        AudioSource.PlayClipAtPoint(clip, position, volume);
    }
}