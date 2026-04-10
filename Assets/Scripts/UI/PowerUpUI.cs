using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý 1 thanh tiến trình của 1 Power-up trên HUD.
/// </summary>
public class PowerUpUI : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private Slider _timeSlider;
    
    private ActivePowerUp _powerUpRef;

    public void Setup(ActivePowerUp p, Sprite iconSprite)
    {
        _powerUpRef = p;
        if (_iconImage != null && iconSprite != null) 
        {
            _iconImage.sprite = iconSprite;
        }
        if (_timeSlider != null) _timeSlider.maxValue = p.MaxDuration;
    }

    private void FixedUpdate()
    {
        if (_powerUpRef != null && _timeSlider != null)
        {
            _timeSlider.value = _powerUpRef.CurrentTime;
        }
    }

    public ActivePowerUp GetPowerUpReference()
    {
        return _powerUpRef;
    }
}
