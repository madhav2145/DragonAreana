using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private Health enemyHealth;

    [Header("Winner Screen")]
    [SerializeField] private GameObject winnerScreen;
    [SerializeField] private TextMeshProUGUI winnerLabel;

    private bool _gameOver;

    private void OnEnable()
    {
        playerHealth.OnDeath += HandlePlayerDeath;
        enemyHealth.OnDeath += HandleEnemyDeath;
    }

    private void OnDisable()
    {
        playerHealth.OnDeath -= HandlePlayerDeath;
        enemyHealth.OnDeath -= HandleEnemyDeath;
    }

    private void HandlePlayerDeath() => HandleGameOver("Enemy");
    private void HandleEnemyDeath() => HandleGameOver("Player");

    private void HandleGameOver(string winnerName)
    {
        if (_gameOver) return;
        _gameOver = true;

        Time.timeScale = 0f;
        winnerScreen.SetActive(true);
        winnerLabel.text = $"{winnerName} Wins!";
    }

    // Function for Restart button's OnClick() in the Inspector
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
