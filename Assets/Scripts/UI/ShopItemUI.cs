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
    [SerializeField] private Image ItemIcon;
    [SerializeField] private TextMeshProUGUI TitleText;
    [SerializeField] private TextMeshProUGUI DescriptionText;      // Có thể để trống nếu UI nhỏ
    [SerializeField] private TextMeshProUGUI AttributeText;        // Vd: "Thời gian: 15s"
    [SerializeField] private TextMeshProUGUI LevelText;            // Vd: "Lvl: 2/5"
    [SerializeField] private TextMeshProUGUI PriceText;            // Vd: "1500"
    
    [Header("Interactive Elements")]
    [SerializeField] private Button BuyButton;
    
    [Tooltip("Drag father object")]
    [SerializeField] private Transform SegmentContainer;
    [SerializeField] private Color ActiveColor = new Color(0.0157f, 1f, 0.6862f); // Mã Hex 04FFAF
    [SerializeField] private Color InactiveColor = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Xám tối trong suốt

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
        int maxLevel = _config.Tiers.Length; // maxLevel bằng độ dài mảng Tiers (vd 5)

        // Cập nhật AttributeText (Hiển thị thời gian/bán kính)
        if (AttributeText != null)
        {
            if (currentLevel == 0)
            {
                AttributeText.text = "Cơ bản (Chưa nâng cấp)";
            }
            else
            {
                // currentLevel từ 1 đến maxLevel sẽ lấy Tiers[0] đến Tiers[maxLevel-1]
                int idx = Mathf.Clamp(currentLevel - 1, 0, maxLevel - 1);
                float dur = _config.Tiers[idx].Duration;
                AttributeText.text = "Thời gian: " + dur + "s";
                
                if (_type == PowerUpType.Magnet)
                {
                    float rad = _config.Tiers[idx].SecondaryValue;
                    AttributeText.text += " | Hút xa: " + rad + "m";
                }
            }
        }

        // Cập nhật Level & Slider tiến trình
        if (LevelText != null) LevelText.text = "Lvl " + currentLevel + "/" + maxLevel;
        if (SegmentContainer)
        {
            for (int i = 0; i < SegmentContainer.childCount; i++)
            {
                Image segImg = SegmentContainer.GetChild(i).GetComponent<Image>();
                if (segImg != null)
                {
                    // Nếu index nhỏ hơn level hiện tại -> Bật sáng
                    segImg.color = (i < currentLevel) ? ActiveColor : InactiveColor;
                }
            }
        }

        // Cập nhật Nút Mua
        if (currentLevel >= maxLevel)
        {
            PriceText.text = "MAX LEVEL";
            BuyButton.interactable = false;
        }
        else
        {
            // Level 0 lấy giá Tiers[0] để lên Lv1. Level 1 lấy giá Tiers[1] để lên Lv2...
            int cost = _config.Tiers[currentLevel].Cost; 
            PriceText.text = cost.ToString("N0") + " G";

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
    }
}
