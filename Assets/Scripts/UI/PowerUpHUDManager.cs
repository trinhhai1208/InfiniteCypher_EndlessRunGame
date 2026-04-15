using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct PowerUpIconMapping
{
    public PowerUpType Type;
    public Sprite      Icon;
}

/// <summary>
/// Quản lý danh sách các thanh hiển thị Power-up trên màn hình.
///
/// P1 Optimization:
/// - Dùng PowerUpUIPool thay cho Instantiate/Destroy → loại bỏ GC spike (H1).
/// - Centralized Tick: 1 Update() ở đây gọi Tick() cho tất cả UIs active,
///   thay vì mỗi PowerUpUI tự có Update() riêng (H2).
/// </summary>
public class PowerUpHUDManager : MonoBehaviour
{
    [SerializeField] private Transform _layoutContainer;  // Vertical Layout Group
    [SerializeField] private GameObject _powerUpUIPrefab; // Kéo Prefab PowerUpUI vào

    [Header("Pool Settings")]
    [SerializeField] private int _prewarmCount = 4;

    [Header("Icons")]
    [SerializeField] private List<PowerUpIconMapping> _icons = new();

    // ── Private State ─────────────────────────────────────────────────────────
    private readonly List<PowerUpUI> _activeUIs = new();
    private PowerUpUIPool _pool;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Tạo Pool root ẩn để chứa các UI đã Return
        var poolRoot = new GameObject("PowerUpUI_PoolRoot").transform;
        poolRoot.SetParent(transform, false);
        poolRoot.gameObject.SetActive(false);

        // Khởi tạo Pool với prewarm
        _pool = gameObject.AddComponent<PowerUpUIPool>();
        _pool.Initialize(_powerUpUIPrefab, poolRoot, _prewarmCount);
    }

    private void Start()
    {
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.OnPowerUpAdded   += HandlePowerUpAdded;
            PowerUpManager.Instance.OnPowerUpRemoved += HandlePowerUpRemoved;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.OnGameOver += HandleGameOver;
    }

    private void OnDestroy()
    {
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.OnPowerUpAdded   -= HandlePowerUpAdded;
            PowerUpManager.Instance.OnPowerUpRemoved -= HandlePowerUpRemoved;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.OnGameOver -= HandleGameOver;
    }

    // ── Centralized Tick ──────────────────────────────────────────────────────

    /// <summary>
    /// 1 Update() duy nhất cho toàn bộ active UIs.
    /// Tốt hơn N Update() (N = số power-up active).
    /// </summary>
    private void Update()
    {
        for (int i = 0; i < _activeUIs.Count; i++)
            _activeUIs[i].Tick();
    }

    // ── Event Handlers ────────────────────────────────────────────────────────

    private void HandlePowerUpAdded(ActivePowerUp p)
    {
        if (_powerUpUIPrefab == null || _layoutContainer == null) return;

        // Lấy từ Pool thay vì Instantiate → GC = 0
        var ui = _pool.Get(_layoutContainer);
        ui.transform.SetAsFirstSibling();

        // Tìm icon theo loại power-up
        Sprite icon = null;
        var mapping = _icons.Find(x => x.Type == p.Type);
        if (mapping.Icon != null) icon = mapping.Icon;

        ui.Setup(p, icon);
        _activeUIs.Add(ui);
    }

    private void HandlePowerUpRemoved(ActivePowerUp p)
    {
        var targetUi = _activeUIs.Find(x => x.GetPowerUpReference() == p);
        if (targetUi == null) return;

        _activeUIs.Remove(targetUi);

        // Trả về Pool thay vì Destroy → GC = 0
        _pool.Return(targetUi);
    }

    private void HandleGameOver()
    {
        if (_layoutContainer != null)
            _layoutContainer.gameObject.SetActive(false);
    }
}
