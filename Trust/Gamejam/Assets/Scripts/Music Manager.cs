using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameMusic;

    [Header("Settings")]
    [SerializeField] private string menuSceneName = "MainMenu";
    [SerializeField, Range(0f, 1f)] private float volume = 0.5f;
    [SerializeField] private float fadeTime = 1f;

    private AudioSource source;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        // Singleton: музыка не прерывается при смене сцены
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        source = GetComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // Первая сцена загружена до подписки на событие
        PlayForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayForScene(scene.name);
    }

    private void PlayForScene(string sceneName)
    {
        AudioClip target = sceneName == menuSceneName ? menuMusic : gameMusic;
        Play(target);
    }

    public void Play(AudioClip clip)
    {
        // Уже играет этот трек — ничего не делаем
        if (clip == null || (source.clip == clip && source.isPlaying)) return;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(SwitchTrack(clip));
    }

    private IEnumerator SwitchTrack(AudioClip newClip)
    {
        // Плавно убираем текущий трек
        float start = source.volume;
        for (float t = 0; t < fadeTime && source.isPlaying; t += Time.unscaledDeltaTime)
        {
            source.volume = Mathf.Lerp(start, 0f, t / fadeTime);
            yield return null;
        }

        source.clip = newClip;
        source.Play();

        // Плавно включаем новый
        for (float t = 0; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            source.volume = Mathf.Lerp(0f, volume, t / fadeTime);
            yield return null;
        }
        source.volume = volume;
    }

    public void SetVolume(float value)
    {
        volume = Mathf.Clamp01(value);
        source.volume = volume;
    }
}