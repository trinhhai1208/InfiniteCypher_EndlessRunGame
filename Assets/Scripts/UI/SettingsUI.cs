using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý giao diện cài đặt với logic kéo ngược:
/// 0 bên trái, 1 bên phải nhưng kéo từ phải sang trái để TĂNG âm lượng.
/// </summary>
public class SettingsUI : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private Slider _sfxSlider;

    private void OnEnable()
    {
        if (AudioManager.Instance != null)
        {
            if (_musicSlider != null)
            {
                // Vì ta dùng logic nghịch đảo (1 - x), nên slider.value = 1 - volume
                _musicSlider.value = 1f - AudioManager.Instance.MusicVolume;
                _musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
            }

            if (_sfxSlider != null)
            {
                _sfxSlider.value = 1f - AudioManager.Instance.SFXVolume;
                _sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
            }
        }
    }

    private void OnDisable()
    {
        if (_musicSlider != null) _musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
        if (_sfxSlider != null) _sfxSlider.onValueChanged.RemoveListener(OnSFXSliderChanged);
    }

    public void OnMusicSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            // Nghịch đảo: Slider càng nhỏ (về bên trái) -> Âm lượng càng lớn
            AudioManager.Instance.SetMusicVolume(1f - value);
        }
    }

    public void OnSFXSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(1f - value);
        }
    }

    public void CloseSettings()
    {
        PlayerPrefs.Save();
        gameObject.SetActive(false);
    }
}
