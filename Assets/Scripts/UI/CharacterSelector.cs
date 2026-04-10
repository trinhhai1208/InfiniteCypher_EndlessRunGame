using UnityEngine;

/// <summary>
/// Quản lý việc chọn nhân vật (Skin) ngay trên Object Player.
/// </summary>
public class CharacterSelector : MonoBehaviour
{
    [Header("Skin Settings")]
    [SerializeField] private GameObject[] _skins; // Kéo các model nhân vật con vào đây
    
    private int _currentSkinIndex = 0;
    private const string SKIN_PREFAB_KEY = "SelectedSkinID";

    private void Awake()
    {
        // Tải skin đã chọn từ trước
        if (PlayerPrefs.HasKey(SKIN_PREFAB_KEY))
        {
            _currentSkinIndex = PlayerPrefs.GetInt(SKIN_PREFAB_KEY);
        }

        // Đảm bảo chỉ có skin đã chọn được hiển thị
        ApplySkin();
    }

    /// <summary>
    /// Chuyển sang nhân vật tiếp theo hoặc trước đó.
    /// direction: 1 (Kế), -1 (Trước)
    /// </summary>
    public void ChangeCharacter(int direction)
    {
        _currentSkinIndex += direction;

        // Vòng lặp nếu vượt quá danh sách
        if (_currentSkinIndex >= _skins.Length)
            _currentSkinIndex = 0;
        else if (_currentSkinIndex < 0)
            _currentSkinIndex = _skins.Length - 1;

        // Áp dụng và lưu
        ApplySkin();
        PlayerPrefs.SetInt(SKIN_PREFAB_KEY, _currentSkinIndex);
        PlayerPrefs.Save();
        
        // Debug.Log("[CharacterSelector] Switched to Skin Index: " + _currentSkinIndex);
    }

    private void ApplySkin()
    {
        if (_skins == null || _skins.Length == 0) return;

        for (int i = 0; i < _skins.Length; i++)
        {
            if (_skins[i] != null)
            {
                _skins[i].SetActive(i == _currentSkinIndex);
            }
        }
    }

    public int GetSelectedSkinIndex() => _currentSkinIndex;
}
