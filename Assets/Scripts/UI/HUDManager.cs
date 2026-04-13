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
        // Kiểm tra GameManager.Instance, nếu không có thì thử tìm trong scene
        if (GameManager.Instance == null)
        {
            GameManager foundManager = FindObjectOfType<GameManager>();
            // Không gọi Awake() bằng SendMessage vì có thể làm reset state
            if (foundManager == null)
            {
                Debug.LogWarning("[HUDManager] Không tìm thấy GameManager trong scene. Script này sẽ tự tắt.");
                enabled = false;
                return;
            }
        }

        if (GameManager.Instance != null && !_isSubscribed)
        {
            GameManager.Instance.OnCoinChanged += UpdateCoinUI;
            GameManager.Instance.OnDistanceChanged += UpdateDistanceUI;
            GameManager.Instance.OnGameStart += ShowHUD;
            _isSubscribed = true;

            // Mặc định ẩn HUD lúc đầu (GameManager sẽ trigger OnGameStart khi vào chơi)
            gameObject.SetActive(false);

            // Hiển thị giá trị ban đầu
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
