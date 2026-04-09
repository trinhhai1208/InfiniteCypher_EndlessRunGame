using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý giao diện Menu chính. 
/// Tất cả các hàm đã được chuyển thành Public để gán thủ công trong Unity Inspector.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject _menuPanel;

    [Header("References")]
    [SerializeField] private CharacterSelector _characterSelector;

    [Header("Statistics")]
    [SerializeField] private TextMeshProUGUI _totalGoldText;

    private void Start()
    {
        // Hiển thị tổng số vàng từ PlayerPrefs
        UpdateTotalGoldUI();

        // TỰ ĐỘNG ẨN MENU NẾU ĐANG TRONG TRẠNG THÁI AUTO-START (Dùng khi Restart game)
        if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
        {
            if (_menuPanel != null) _menuPanel.SetActive(false);
        }
        else
        {
            if (_menuPanel != null) _menuPanel.SetActive(true);
        }
    }

    /// <summary>
    /// HÀM CHÍNH: Bắt đầu game. 
    /// Hãy gán hàm này vào nút START DASH.
    /// </summary>
    public void StartGame()
    {
        Debug.Log("[MainMenuUI] Đang chuyển sang GameScene...");
        
        // Đặt flag để GameScene biết là cần vào chơi luôn
        GameManager.SetAutoStart(true);
        
        // Chuyển sang màn chơi chính
        SceneManager.LoadScene("GameScene");
    }

    /// <summary>
    /// Chọn nhân vật kế tiếp.
    /// Hãy gán hàm này vào nút Mũi tên Phải.
    /// </summary>
    public void SelectNextCharacter()
    {
        if (_characterSelector != null)
        {
            _characterSelector.ChangeCharacter(1);
        }
    }

    /// <summary>
    /// Chọn nhân vật trước đó.
    /// Hãy gán hàm này vào nút Mũi tên Trái.
    /// </summary>
    public void SelectPrevCharacter()
    {
        if (_characterSelector != null)
        {
            _characterSelector.ChangeCharacter(-1);
        }
    }

    private void UpdateTotalGoldUI()
    {
        if (_totalGoldText != null)
        {
            // Đọc trực tiếp từ PlayerPrefs để chắc chắn có dữ liệu dù ở Scene nào
            int totalGold = PlayerPrefs.GetInt("TotalGold", 0);
            _totalGoldText.text = totalGold.ToString();
        }
    }
}
