using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct PowerUpIconMapping
{
    public PowerUpType Type;
    public Sprite Icon;
}

/// <summary>
/// Quản lý danh sách các thanh hiển thị Power-up trên màn hình
/// </summary>
public class PowerUpHUDManager : MonoBehaviour
{
    [SerializeField] private Transform _layoutContainer; // Gắn GameObject chứa Component Vertical Layout Group vào đây
    [SerializeField] private GameObject _powerUpUIPrefab; // Kéo Prefab PowerUpUI vào đây

    [Header("Icons")]
    [SerializeField] private List<PowerUpIconMapping> _icons = new();

    private readonly List<PowerUpUI> _activeUIs = new();

    private void Start()
    {
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.OnPowerUpAdded += HandlePowerUpAdded;
            PowerUpManager.Instance.OnPowerUpRemoved += HandlePowerUpRemoved;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver += HandleGameOver;
        }
    }

    private void HandleGameOver()
    {
        if (_layoutContainer != null)
            _layoutContainer.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.OnPowerUpAdded -= HandlePowerUpAdded;
            PowerUpManager.Instance.OnPowerUpRemoved -= HandlePowerUpRemoved;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= HandleGameOver;
        }
    }

    private void HandlePowerUpAdded(ActivePowerUp p)
    {
        if (_powerUpUIPrefab == null || _layoutContainer == null) return;

        var go = Instantiate(_powerUpUIPrefab, _layoutContainer);
        // Đưa cái mới nhất lên trên cùng
        go.transform.SetAsFirstSibling();
        
        var ui = go.GetComponent<PowerUpUI>();
        if (ui != null)
        {
            // Tìm icon map theo type
            Sprite icon = null;
            var mapping = _icons.Find(x => x.Type == p.Type);
            if (mapping.Icon != null) icon = mapping.Icon;

            ui.Setup(p, icon);
            _activeUIs.Add(ui);
        }
    }

    private void HandlePowerUpRemoved(ActivePowerUp p)
    {
        var targetUi = _activeUIs.Find(x => x.GetPowerUpReference() == p);
        if (targetUi != null)
        {
            _activeUIs.Remove(targetUi);
            Destroy(targetUi.gameObject);
        }
    }
}
