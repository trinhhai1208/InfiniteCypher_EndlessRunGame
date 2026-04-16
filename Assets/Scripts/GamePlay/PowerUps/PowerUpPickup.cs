using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PowerUpPickup : MonoBehaviour
{
    [Header("Settings")]
    public PowerUpType Type;
    public float Duration = 10f;


    private bool _collected;

    private void OnEnable()
    {
        _collected = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_collected) return;
        if (!other.CompareTag("Player")) return;

        _collected = true;

        if (PowerUpManager.Instance != null)
        {
            // P1: Lấy thời gian đã được Nâng cấp thay vì dùng thông số của Prefab
            float actualDuration = Duration; 
            if (ServiceLocator.TryGet<UpgradeManager>(out var upgradeManager))
            {
                actualDuration = upgradeManager.GetDuration(Type);
            }
            else if (UpgradeManager.Instance != null)
            {
                actualDuration = UpgradeManager.Instance.GetDuration(Type);
            }
            
            PowerUpManager.Instance.ActivatePowerUp(Type, actualDuration);
        }

        // Tạm thời Disable, vì thường Powerup sẽ load qua Pooler hoặc Object Instantiate
        gameObject.SetActive(false);
    }
}
