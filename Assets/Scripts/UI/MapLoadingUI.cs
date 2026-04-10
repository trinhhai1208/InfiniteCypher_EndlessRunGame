using UnityEngine;
using System.Collections;

/// <summary>
/// Quản lý màn hình Loading che phủ Map khi đang tải.
/// Chỉ biến mất khi TrackManager báo IsReady.
/// </summary>
public class MapLoadingUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TrackManager _trackManager;
    [SerializeField] private CanvasGroup _canvasGroup; // Dùng CanvasGroup để làm hiệu ứng Fade

    [Header("Settings")]
    [SerializeField] private float _fadeOutDuration = 0.5f;

    private void Start()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.gameObject.SetActive(true);
        }

        StartCoroutine(WaitForMapReadyRoutine());
    }
    
    private IEnumerator WaitForMapReadyRoutine()
    {
        // Chờ TrackManager tìm thấy (nếu chưa gán)
        if (_trackManager == null)
            _trackManager = FindObjectOfType<TrackManager>();

        // 1. Chờ cho đến khi TrackManager báo IsReady
        while (_trackManager == null || !_trackManager.IsReady)
        {
            yield return null;
        }

        // 2. Map đã sẵn sàng, thực hiện hiệu ứng Fade Out
        float timer = 0f;
        while (timer < _fadeOutDuration)
        {
            timer += Time.deltaTime;
            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f - (timer / _fadeOutDuration);
            yield return null;
        }

        // 3. Tắt Panel Loading
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.gameObject.SetActive(false);
        }

        // Debug.Log("[MapLoadingUI] Đã mở màn hình Loading. Chúc bạn chơi game vui vẻ!");
    }
}
