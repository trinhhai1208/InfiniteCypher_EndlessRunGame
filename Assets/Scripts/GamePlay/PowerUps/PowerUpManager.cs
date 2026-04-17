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

    // ─── P1: Magnet - OverlapSphereNonAlloc ───────────────────
    [Header("Magnet Settings")]
    [SerializeField] private float _magnetRadius = 10f;
    [SerializeField] private LayerMask _coinLayer;

    // Buffer tái sử dụng — không alloc mỗi frame
    private readonly Collider[] _magnetBuffer = new Collider[64];

    private void Awake()
    {
        // Singleton: Dọn dẹp rác từ scene cũ
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        ServiceLocator.Register<PowerUpManager>(this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            ServiceLocator.Unregister<PowerUpManager>();
            Instance = null;
        }
    }

    // ─── P1: Chuyển từ FixedUpdate → Update ───────────────────
    // Lý do: Cập nhật thời gian Power-up và kéo xu thuộc về logic game,
    // không cần đồng bộ chính xác với Physics step.
    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        float dt = Time.deltaTime;

        // Cập nhật thời gian ngược
        for (int i = _activePowerUps.Count - 1; i >= 0; i--)
        {
            var p = _activePowerUps[i];
            p.CurrentTime -= dt;

            if (p.CurrentTime <= 0)
            {
                OnPowerUpRemoved?.Invoke(p);
                _activePowerUps.RemoveAt(i);
            }
        }

        // ─── P1: Logic Magnet Player-driven ───────────────────
        // Thay vì mỗi Coin tự poll khoảng cách (O(n) FixedUpdate),
        // Player chủ động quét 1 lần duy nhất bằng NonAlloc (O(1) alloc).
        if (IsMagnetActive() && PlayerController.Instance != null)
        {
            Vector3 playerPos = PlayerController.Instance.transform.position;
            
            // P1: Bán kính từ tính to ra nếu được nâng cấp
            float currentMagnetRadius = _magnetRadius;
            if (ServiceLocator.TryGet<UpgradeManager>(out var upgradeManager))
            {
                currentMagnetRadius = upgradeManager.GetSecondaryValue(PowerUpType.Magnet);
            }
            else if (UpgradeManager.Instance != null)
            {
                currentMagnetRadius = UpgradeManager.Instance.GetSecondaryValue(PowerUpType.Magnet);
            }

            int count = Physics.OverlapSphereNonAlloc(playerPos, currentMagnetRadius, _magnetBuffer, _coinLayer);

            for (int i = 0; i < count; i++)
            {
                if (_magnetBuffer[i] == null) continue;
                var coin = _magnetBuffer[i].GetComponent<Coin>();
                if (coin != null) coin.AttractTo(playerPos, dt);
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
