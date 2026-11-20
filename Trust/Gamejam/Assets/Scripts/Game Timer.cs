using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.ComponentModel.Design.Serialization;

public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance { get; private set; }

    [Header("Time Settings")]
    [SerializeField] private float startTime = 30f;   // стартовое время, секунды
    [SerializeField] private float maxTime = 60f;    // больше этого не накопить (0 = без лимита)

    [Header("UI")]
    [SerializeField] private TMP_Text timerText;      // текст с таймером
    [SerializeField] private GameObject gameOverPanel; // панель "Время вышло" (можно не назначать)
    [SerializeField] private Color textColor;

    public float TimeLeft { get; private set; }
    public bool isOver { get; private set; }

    private void Awake()
    {
        if(Instance == null) Instance = this;
        Time.timeScale = 1f;   // на случай рестарта после паузы
        TimeLeft = startTime;
        isOver = false;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        timerText.color = textColor;
    }

    private void Update()
    {
        if (isOver) return;
        
        TimeLeft -= Time.deltaTime;

        if (TimeLeft <= 0f)
        {
            TimeLeft = 0f;
            GameOver();
        }

        UpdateText();
    }

    // Вызывается, когда убили врага
    public void AddTime(float seconds)
    {
        if (isOver) return;

        TimeLeft += seconds;
        if (maxTime > 0f && TimeLeft > maxTime)
            TimeLeft = maxTime;
    }

    private void UpdateText()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(TimeLeft / 60f);
        int seconds = Mathf.FloorToInt(TimeLeft % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";

        // Последние 10 секунд красным
        timerText.color = TimeLeft <= 10f ? Color.red : Color.white;
    }

    private void GameOver()
    {
        isOver = true;

        // Звук "смерти" игрока, когда время вышло
        if (SoundManager.Instance != null && Camera.main != null)
            SoundManager.Instance.PlayDeath(Camera.main.transform.position);

        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        timerText.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;   // останавливаем игру
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main Menu");
    }
}