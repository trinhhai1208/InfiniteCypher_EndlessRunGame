using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Quản lý màn hình Splash/Loading ban đầu.
/// Tự động chuyển sang cảnh chơi sau một khoảng thời gian.
/// </summary>
public class SplashManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string _nextSceneName = "GameScene"; // Đổi tên này khớp với tên Scene chính của bạn
    [SerializeField] private float _delayTime = 3f;

    private void Start()
    {
        StartCoroutine(LoadNextSceneRoutine());
    }

    private IEnumerator LoadNextSceneRoutine()
    {
        // Đợi một khoảng thời gian (có thể dùng để load assets ngầm nếu cần)
        yield return new WaitForSeconds(_delayTime);

        // Chuyển sang scene chính
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(_nextSceneName);

        // Chờ cho đến khi load xong
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}
