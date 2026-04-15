using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pool tái sử dụng các PowerUpUI thay vì Instantiate/Destroy mỗi lần pickup/expire.
/// Giải quyết hotspot H1: loại bỏ GC spike khi power-up xuất hiện/biến mất.
/// </summary>
public class PowerUpUIPool : MonoBehaviour
{
    private readonly Stack<PowerUpUI> _pool = new();

    private GameObject _prefab;
    private Transform  _poolRoot;

    /// <summary>
    /// Khởi tạo pool với số lượng prewarm.
    /// Gọi từ PowerUpHUDManager.Awake().
    /// </summary>
    public void Initialize(GameObject prefab, Transform poolRoot, int prewarmCount = 4)
    {
        _prefab   = prefab;
        _poolRoot = poolRoot;

        for (int i = 0; i < prewarmCount; i++)
        {
            var ui = CreateNew();
            ui.gameObject.SetActive(false);
            _pool.Push(ui);
        }
    }

    /// <summary>
    /// Lấy 1 PowerUpUI từ pool (hoặc tạo mới nếu pool rỗng) và gắn vào parent.
    /// </summary>
    public PowerUpUI Get(Transform parent)
    {
        PowerUpUI ui = _pool.Count > 0
            ? _pool.Pop()
            : CreateNew();

        ui.transform.SetParent(parent, false);
        ui.gameObject.SetActive(true);
        return ui;
    }

    /// <summary>
    /// Trả PowerUpUI về pool: reset UI, ẩn đi, chờ dùng lại.
    /// </summary>
    public void Return(PowerUpUI ui)
    {
        if (ui == null) return;
        ui.ResetUI();
        ui.gameObject.SetActive(false);
        ui.transform.SetParent(_poolRoot, false);
        _pool.Push(ui);
    }

    // ──────────────────────────────────────────
    private PowerUpUI CreateNew()
    {
        var go = Instantiate(_prefab, _poolRoot);
        var ui = go.GetComponent<PowerUpUI>();

        if (ui == null)
        {
            Debug.LogError("[PowerUpUIPool] Prefab không có component PowerUpUI!");
        }

        return ui;
    }
}
