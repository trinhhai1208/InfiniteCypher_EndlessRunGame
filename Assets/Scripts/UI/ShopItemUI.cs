using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn vào Prefab giao diện 1 dòng nâng cấp trong Shop.
/// Tự động cập nhật thông tin dựa trên dữ liệu Config.
/// </summary>
public class ShopItemUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image ItemIcon;
    public TextMeshProUGUI TitleText;
    public TextMeshProUGUI DescriptionText;      // Có thể để trống nếu UI nhỏ
    public TextMeshProUGUI AttributeText;        // Vd: "Thời gian: 15s"
    public TextMeshProUGUI LevelText;            // Vd: "Lvl: 2/5"
    public TextMeshProUGUI PriceText;            // Vd: "1500"
    
    [Header("Interactive Elements")]
    public Button BuyButton;
    public Slider UpgradeProgressBar;            // Thanh chia 5 nấc

    private UpgradeConfigSO _config;
    private PowerUpType _type;
    private ShopUI _parentShop;

    public void Setup(UpgradeConfigSO config, ShopUI parent)
    {
        _config = config;
        _type = config.Type;
        _parentShop = parent;

        if (ItemIcon != null && config.Icon != null) ItemIcon.sprite = config.Icon;
        if (TitleText != null) TitleText.text = config.DisplayName;
        if (DescriptionText != null) DescriptionText.text = config.Description;

        // Xóa listener cũ (nếu có do pooling hoặc tái sử dụng)
        BuyButton.onClick.RemoveAllListeners();
        BuyButton.onClick.AddListener(OnBuyClicked);

        RefreshData();
    }

    /// <summary>
    /// Làm mới giao diện (Gọi khi khởi tạo và sau mỗi lần mua thành công)
    /// </summary>
    public void RefreshData()
    {
        if (UpgradeManager.Instance == null) return;

        int currentLevel = UpgradeManager.Instance.GetLevel(_type);
        
        // Cập nhật AttributeText (Hiển thị thời gian/bán kính)
        if (AttributeText != null)
        {
            float dur = _config.GetTier(currentLevel).Duration;
            AttributeText.text = "Thời gian: " + dur + "s";
            
            // Nếu là Magnet, hiển thị thêm Bán kính hút
            if (_type == PowerUpType.Magnet)
            {
                float rad = _config.GetTier(currentLevel).SecondaryValue;
                AttributeText.text += " | Hút xa: " + rad + "m";
            }
        }

        // Cập nhật Level & Slider tiến trình (Max level = 5)
        if (LevelText != null) LevelText.text = "Lvl " + currentLevel + "/5";
        if (UpgradeProgressBar != null)
        {
            UpgradeProgressBar.maxValue = 5;
            UpgradeProgressBar.value = currentLevel;
        }

        // Cập nhật Nút Mua
        if (currentLevel >= 5)
        {
            PriceText.text = "MAX LEVEL";
            BuyButton.interactable = false;
        }
        else
        {
            int cost = _config.GetTier(currentLevel).Cost; // Lưu ý: Level 1 dùng config[1] để nhảy lên Level 2
            PriceText.text = cost.ToString("N0") + " G";

            // Có thể mờ nút nếu không đủ vàng
            int totalGold = PlayerPrefs.GetInt("TotalGold", 0);
            BuyButton.interactable = totalGold >= cost;
        }
    }

    private void OnBuyClicked()
    {
        if (UpgradeManager.Instance != null && UpgradeManager.Instance.TryUpgrade(_type))
        {
            // Nếu mua thành công
            // TODO: Bạn có thể play Audio "Kaching" ở đây
            // AudioManager.Instance.PlaySFX("BuySuccess");
            
            RefreshData(); // Làm mới chính nó
            _parentShop.RefreshAll(); // Báo cho Shop biết để Cập nhật tiền (Góc trên) & Làm mới các nút khác
        }
        else
        {
            // Mua thất bại (Hết tiền hoặc lỗi)
            Debug.Log("[Shop] Not enough gold or max level reached.");
        }
    }
}
