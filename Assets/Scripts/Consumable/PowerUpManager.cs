using System;
using System.Collections.Generic;
using UnityEngine;

public enum PowerUpType
{
    Magnet,
    Shield,
    Multiplier
}

public class ActivePowerUp
{
    public PowerUpType Type;
    public float MaxDuration;
    public float CurrentTime;
}

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    private readonly List<ActivePowerUp> _activePowerUps = new();

    // Sự kiện để HUD cập nhật
    public event Action<ActivePowerUp> OnPowerUpAdded;
    public event Action<ActivePowerUp> OnPowerUpRemoved;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        // Cập nhật thời gian ngược
        for (int i = _activePowerUps.Count - 1; i >= 0; i--)
        {
            var p = _activePowerUps[i];
            p.CurrentTime -= Time.fixedDeltaTime;

            if (p.CurrentTime <= 0)
            {
                OnPowerUpRemoved?.Invoke(p);
                _activePowerUps.RemoveAt(i);
            }
        }
    }

    public void ActivatePowerUp(PowerUpType type, float duration)
    {
        // Kiểm tra xem đã có chưa
        var existing = _activePowerUps.Find(x => x.Type == type);
        if (existing != null)
        {
            // Có rồi -> Reset time
            existing.MaxDuration = duration;
            existing.CurrentTime = duration;
            // UI sẽ tự cập nhật vì nó lấy thông số trực tiếp thông qua class tham chiếu
        }
        else
        {
            // Báo chưa có -> Thêm mới
            var newPowerUp = new ActivePowerUp
            {
                Type = type,
                MaxDuration = duration,
                CurrentTime = duration
            };
            _activePowerUps.Add(newPowerUp);
            OnPowerUpAdded?.Invoke(newPowerUp);
        }
    }

    // --- CÁC HÀM TIỆN ÍCH KIỂM TRA TRẠNG THÁI ---
    
    public bool IsMagnetActive()
    {
        return _activePowerUps.Exists(x => x.Type == PowerUpType.Magnet);
    }

    public bool IsMultiplierActive()
    {
        return _activePowerUps.Exists(x => x.Type == PowerUpType.Multiplier);
    }

    public bool HasShield()
    {
        return _activePowerUps.Exists(x => x.Type == PowerUpType.Shield);
    }

    public void ConsumeShield()
    {
        var shield = _activePowerUps.Find(x => x.Type == PowerUpType.Shield);
        if (shield != null)
        {
            shield.CurrentTime = 0; // Ép hủy ở frame sau
            OnPowerUpRemoved?.Invoke(shield);
            _activePowerUps.Remove(shield);
        }
    }

    public void ClearAll()
    {
        foreach (var p in _activePowerUps)
        {
            OnPowerUpRemoved?.Invoke(p);
        }
        _activePowerUps.Clear();
    }
}
