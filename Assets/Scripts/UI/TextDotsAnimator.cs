using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Tạo hiệu ứng nhấp nháy dấu chấm cho văn bản (ví dụ: LOADING... -> LOADING . -> LOADING ..)
/// </summary>
public class TextDotsAnimator : MonoBehaviour
{
    [SerializeField] private string _baseText = "LOADING";
    [SerializeField] private float _delay = 0.5f;
    
    private TextMeshProUGUI _tmpText;
    private int _dotCount = 0;

    private void Awake()
    {
        _tmpText = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        if (_tmpText != null)
        {
            StartCoroutine(AnimateDotsRoutine());
        }
    }

    private IEnumerator AnimateDotsRoutine()
    {
        while (true)
        {
            _dotCount = (_dotCount + 1) % 4; // Chạy từ 0, 1, 2, 3
            
            string dots = new string('.', _dotCount);
            _tmpText.text = _baseText + " " + dots;
            
            yield return new WaitForSeconds(_delay);
        }
    }
}
