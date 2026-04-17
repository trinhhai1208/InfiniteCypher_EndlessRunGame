using UnityEngine;

public enum MissionSensorAction 
{ 
    Roll, 
    Jump 
}

/// <summary>
/// Gắn vào một GameObject chứa BoxCollider (IsTrigger = true) đặt xung quanh vật cản.
/// - Để bắt Roll: Đặt Collider dưới gầm rào chắn (nơi Player chui qua).
/// - Để bắt Jump: Đặt Collider lơ lửng trên không, ngay trên vật cản.
/// </summary>
public class MissionSensor : MonoBehaviour
{
    [SerializeField] private MissionSensorAction _actionToDetect;
    private bool _triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        
        if (other.CompareTag("Player"))
        {
            // Tránh phát event nếu Player đã chết
            if (PlayerController.Instance != null && PlayerController.Instance.gameObject == other.gameObject)
            {
                // Gọi EventBus
                if (_actionToDetect == MissionSensorAction.Roll)
                {
                    EventBus.Publish(new PlayerBarrierRollEvent());
                }
                else if (_actionToDetect == MissionSensorAction.Jump)
                {
                    EventBus.Publish(new PlayerObstacleJumpEvent());
                }
                
                _triggered = true;
            }
        }
    }

    private void OnDisable()
    {
        // Reset khi Obstacle bị đưa về Pool
        _triggered = false;
    }
}
