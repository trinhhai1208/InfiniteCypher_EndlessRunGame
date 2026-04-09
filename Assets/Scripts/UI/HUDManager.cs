using UnityEngine;
using TMPro;

/// <summary>
/// Quản lý giao diện hiển thị trong khi chơi.
/// Lắng nghe các sự kiện từ GameManager để cập nhật văn bản.
/// </summary>
public class HUDManager : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Text hiển thị số xu thu thập được (Kéo UI Text vào đây!)")]
    [SerializeField] private TextMeshProUGUI _coinText;
    
    [Tooltip("Text hiển thị quãng đường chạy được (Kéo UI Text vào đây!)")]
    [SerializeField] private TextMeshProUGUI _distanceText;

    private bool _isSubscribed = false;

    private void Start()
    {
        // Auto-connect to GameManager if Instance is somehow not set yet
        if (GameManager.Instance == null)
        {
            GameManager foundManager = FindObjectOfType<GameManager>();
            if (foundManager != null)
            {
                foundManager.SendMessage("Awake"); 
            }
        }

        if (GameManager.Instance != null && !_isSubscribed)
        {
            GameManager.Instance.OnCoinChanged += UpdateCoinUI;
            GameManager.Instance.OnDistanceChanged += UpdateDistanceUI;
            GameManager.Instance.OnGameStart += ShowHUD; // Hiện HUD khi bắt đầu chạy
            _isSubscribed = true;

            // Mặc định ẩn HUD lúc đầu
            gameObject.SetActive(false);

            // Hiển thị giá trị ngay từ khung hình đầu tiên
            UpdateCoinUI(GameManager.Instance.CoinCount);
            UpdateDistanceUI(GameManager.Instance.Distance);
        }
    }

    private void ShowHUD()
    {
        gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null && _isSubscribed)
        {
            GameManager.Instance.OnCoinChanged -= UpdateCoinUI;
            GameManager.Instance.OnDistanceChanged -= UpdateDistanceUI;
            GameManager.Instance.OnGameStart -= ShowHUD;
            _isSubscribed = false;
        }
    }

    // ─────────────────────────────────────────
    // UI Update Methods
    // ─────────────────────────────────────────

    private void UpdateCoinUI(int count)
    {
        if (_coinText != null)
            _coinText.text = count.ToString();
    }

    private void UpdateDistanceUI(int distance)
    {
        if (_distanceText != null)
            _distanceText.text = distance.ToString() + "m";
    }
}
