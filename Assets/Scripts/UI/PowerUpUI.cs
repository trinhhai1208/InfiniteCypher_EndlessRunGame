using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý 1 thanh tiến trình của 1 Power-up trên HUD.
/// 
/// P1 Optimization: Đã xóa Update() riêng.
/// Việc cập nhật Slider giờ do PowerUpHUDManager điều phối tập trung (1 Update duy nhất)
/// thay vì N Update() riêng lẻ (N = số power-up đang active).
/// </summary>
public class PowerUpUI : MonoBehaviour
{
    [SerializeField] private Image  _iconImage;
    [SerializeField] private Slider _timeSlider;

    private ActivePowerUp _powerUpRef;

    // ── Setup / Reset ─────────────────────────────────────────────────────────

    /// <summary>Được gọi bởi PowerUpHUDManager khi lấy từ Pool.</summary>
    public void Setup(ActivePowerUp p, Sprite iconSprite)
    {
        _powerUpRef = p;

        if (_iconImage != null && iconSprite != null)
            _iconImage.sprite = iconSprite;

        if (_timeSlider != null)
        {
            _timeSlider.maxValue = p.MaxDuration;
            _timeSlider.value    = p.CurrentTime;
        }
    }

    /// <summary>Được gọi bởi PowerUpUIPool khi Return về pool. Xóa state cũ.</summary>
    public void ResetUI()
    {
        _powerUpRef = null;

        if (_iconImage != null)  _iconImage.sprite = null;
        if (_timeSlider != null) _timeSlider.value  = 0f;
    }

    // ── Centralized Tick (gọi bởi PowerUpHUDManager.Update) ──────────────────

    /// <summary>
    /// Cập nhật Slider. KHÔNG gọi mỗi Update() riêng — chỉ gọi từ Manager.
    /// </summary>
    public void Tick()
    {
        if (_powerUpRef == null || _timeSlider == null) return;
        _timeSlider.value = _powerUpRef.CurrentTime;
    }

    // ── Getter ────────────────────────────────────────────────────────────────

    public ActivePowerUp GetPowerUpReference() => _powerUpRef;
}
