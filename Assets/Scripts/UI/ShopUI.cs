using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Quản lý Bảng Giao Diện Shop.
/// Đảm nhiệm việc "chẻ" dữ liệu từ UpgradeManager ra thành các nút trên màn hình.
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Vùng chứa (Content) bên dưới ScrollView")]
    public Transform ContentContainer;
    
    [Tooltip("Prefab của Shop Item dể tự động nhân bản")]
    public GameObject ShopItemPrefab;

    [Tooltip("Text hiển thị tiền hiện có trên góc màn hình Shop")]
    public TextMeshProUGUI TotalGoldText;

    private List<ShopItemUI> _spawnedItems = new();
    private bool _isInitialized = false;

    private void OnEnable()
    {
        // Mỗi lần bật bảng Shop lên, ta làm mới/tạo mới dữ liệu
        InitializeShop();
        RefreshAll();
    }

    private void InitializeShop()
    {
        if (_isInitialized) return;

        // Xóa sạch placeholder rác nếu có trong scene (trong quá trình thiết kế)
        foreach (Transform child in ContentContainer)
        {
            Destroy(child.gameObject);
        }

        // Đọc Config từ UpgradeManager và sinh ra UI
        if (UpgradeManager.Instance != null && UpgradeManager.Instance.Configs != null)
        {
            foreach (var config in UpgradeManager.Instance.Configs)
            {
                if (config == null) continue;

                // Tạo mới 1 dòng UI
                GameObject itemGo = Instantiate(ShopItemPrefab, ContentContainer);
                ShopItemUI itemUI = itemGo.GetComponent<ShopItemUI>();
                
                if (itemUI != null)
                {
                    itemUI.Setup(config, this);
                    _spawnedItems.Add(itemUI);
                }
            }
        }

        _isInitialized = true;
    }

    /// <summary>
    /// Được gọi bởi một ShopItemUI khi người chơi mua đồ thành công.
    /// Giúp update lại hiển thị vàng trên góc và làm "mờ" nút những món không đủ khả năng mua.
    /// </summary>
    public void RefreshAll()
    {
        if (TotalGoldText != null)
        {
            TotalGoldText.text = PlayerPrefs.GetInt("TotalGold", 0).ToString("N0");
        }

        foreach (var item in _spawnedItems)
        {
            if (item != null)
            {
                item.RefreshData();
            }
        }
    }

    /// <summary>
    /// Gán vào Nút "X" góc màn hình Shop
    /// </summary>
    public void CloseShop()
    {
        // Có thể Thêm hiệu ứng Tween ở đây nếu muốn v2
        gameObject.SetActive(false);
    }
}
