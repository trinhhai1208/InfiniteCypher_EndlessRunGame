using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Quản lý giao diện Game Over: Hiện điểm, Kỷ lục, và Nút Chơi Lại / Thoát.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Panel Giao Diện (Kéo UI Panel vào đây)")]
    [Tooltip("Panel chứa toàn bộ màn hình Game Over, mặc định phải TẮT (ẩn đi)")]
    [SerializeField] private GameObject _gameOverPanel;

    [Header("Các Text Hiển Thị")]
    [SerializeField] private TextMeshProUGUI _distanceText;
    [SerializeField] private TextMeshProUGUI _coinText;
    [SerializeField] private TextMeshProUGUI _bestDistanceText;

    [Header("Các Nút Bấm")]
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _homeButton;

    [Header("Panels")]
    [SerializeField] private GameObject _distancePanel;
    [SerializeField] private GameObject _coinPanel;

    private void Start()
    {
        if(_distancePanel != null && _coinPanel != null) {
            _distancePanel.SetActive(true);
            _coinPanel.SetActive(true);
        }
        
        // Ẩn UI lúc ban đầu
        if (_gameOverPanel != null)
            _gameOverPanel.SetActive(false);

        // Đăng ký sự kiện nút bấm
        if (_restartButton != null)
            _restartButton.onClick.AddListener(RestartGame);
        
        if (_homeButton != null)
            _homeButton.onClick.AddListener(GoToMainMenu);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver += ShowGameOverScreen;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= ShowGameOverScreen;
        }
    }

    /// <summary>
    /// Hàm này tự động được GameManager gọi khi nhân vật chết.
    /// </summary>
    private void ShowGameOverScreen()
    {
        if (_gameOverPanel == null) return;

        // Bật panel lên
        _gameOverPanel.SetActive(true);
        if (_distancePanel != null) _distancePanel.SetActive(false);
        if (_coinPanel != null) _coinPanel.SetActive(false);

        // Lấy số liệu cuối cùng từ GameManager
        int currentDistance = GameManager.Instance.Distance;
        int currentCoins = GameManager.Instance.CoinCount;
        int bestDistance = GameManager.Instance.BestDistance;

        // Hiển thị ra màn hình
        if (_distanceText != null) _distanceText.text ="Distance: " + currentDistance.ToString() + "m";
        if (_coinText != null) _coinText.text ="Coin: " + currentCoins.ToString();
        if (_bestDistanceText != null) _bestDistanceText.text = "Best: " + bestDistance.ToString() + "m";
    }

    /// <summary>Chơi Lại</summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>Về Màn Hình Chính</summary>
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
    }
}
