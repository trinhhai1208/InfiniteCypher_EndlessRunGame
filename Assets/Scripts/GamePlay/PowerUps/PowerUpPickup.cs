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
            PowerUpManager.Instance.ActivatePowerUp(Type, Duration);
        }

        // Tạm thời Disable, vì thường Powerup sẽ load qua Pooler hoặc Object Instantiate
        gameObject.SetActive(false);
    }
}
