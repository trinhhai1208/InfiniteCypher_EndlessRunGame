using UnityEngine;

public enum CameraMode { PlayerOnly, ChaseMode }

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Header("Target")]
    [SerializeField] private Transform _target;

    [Header("Player Only Offset")]
    [SerializeField] private Vector3 _playerOnlyOffset = new Vector3(0f, 3.5f, -5.5f);

    [Header("Chase Mode Offset (khi Boss xuất hiện)")]
    [Tooltip("Lùi xa và cao hơn để lấy cả Player và Boss")]
    [SerializeField] private Vector3 _chaseModeOffset = new Vector3(0f, 5f, -9f);
    [SerializeField] private Vector3 _chaseLookAtOffset = new Vector3(0f, 1f, 2f); // Nhìn về điểm giữa 2 nhân vật

    [Header("Smoothing")]
    [SerializeField] private float _positionSmoothTime  = 0.15f;
    [SerializeField] private float _rotationSmoothTime  = 0.3f;
    [SerializeField] private float _modeSwitchSmooth    = 0.6f;   // Tốc độ chuyển mode

    [Header("Look At")]
    [SerializeField] private Vector3 _lookAtOffset = new Vector3(0f, 1f, 0f);

    // ── State ──────────────────────────────────────────
    private CameraMode _mode = CameraMode.PlayerOnly;
    private Transform  _boss;

    // ── Dynamics ───────────────────────────────────────
    private Vector3 _positionVelocity;
    private Vector3 _currentOffset;
    private Vector3 _offsetVelocity;

    // ── Cached ─────────────────────────────────────────
    private Rigidbody _targetRigidbody;

    // ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _currentOffset = _playerOnlyOffset;
        CacheTargetRigidbody();
    }

    private void Start() => SnapToTarget();

    private void LateUpdate()
    {
        if (_target == null) return;
        FollowTarget();
    }

    // ── Core ───────────────────────────────────────────

    private void FollowTarget()
    {
        // Tính toán offset mục tiêu dựa trên Mode
        Vector3 targetOffset = (_mode == CameraMode.ChaseMode) ? _chaseModeOffset : _playerOnlyOffset;

        // Lerp offset mượt mà khi chuyển mode
        _currentOffset = Vector3.SmoothDamp(_currentOffset, targetOffset, ref _offsetVelocity, _modeSwitchSmooth);

        // ── Vị trí Camera ──
        Vector3 playerPos = _target.position;
        Vector3 targetCamPos = playerPos + _currentOffset;

        Vector3 cur = transform.position;
        float newX = Mathf.SmoothDamp(cur.x, targetCamPos.x, ref _positionVelocity.x, _positionSmoothTime);
        float newY = Mathf.SmoothDamp(cur.y, targetCamPos.y, ref _positionVelocity.y, _positionSmoothTime);
        float newZ = targetCamPos.z; // Bám sát Z trực tiếp tránh cảm giác lag

        transform.position = new Vector3(newX, newY, newZ);

        // ── LookAt Target ──
        // ── LookAt Target ──
        // P1 SỬA: Để Camera KHÔNG xoay giật sang trái/phải, ta đặt toạ độ X của điểm nhìn 
        // bằng chính toạ độ X của Camera hiện tại (newX).
        // Như vậy Camera luôn nhìn thẳng băng về phía trước theo hướng song song.
        Vector3 lookAtTarget;

        if (_mode == CameraMode.ChaseMode)
        {
            lookAtTarget = new Vector3(newX, playerPos.y + _chaseLookAtOffset.y, playerPos.z + _chaseLookAtOffset.z);
        }
        else
        {
            lookAtTarget = new Vector3(newX, playerPos.y + _lookAtOffset.y, playerPos.z + _lookAtOffset.z);
        }

        // Sau đó Camera sẽ bù trừ góc quay để nhìn về điểm đó. 
        // Nếu bạn muốn nhìn LÀN TRƯỚC mặt (tức là nhìn song song), ta dùng:
        // Vector3 forward = Vector3.forward; 
        
        Vector3 direction = lookAtTarget - transform.position;
        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            float rotLerp = _rotationSmoothTime <= 0.0001f ? 1f : Time.deltaTime / _rotationSmoothTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Mathf.Clamp01(rotLerp));
        }
    }

    // ── Public API ─────────────────────────────────────

    /// <summary>
    /// Chuyển sang chế độ theo dõi Player và Boss cùng lúc.
    /// Gọi bởi BossChaseManager.StartChase().
    /// </summary>
    public void SetChaseMode(Transform boss)
    {
        _boss = boss;
        _mode = CameraMode.ChaseMode;
    }

    /// <summary>
    /// Quay về chế độ chỉ theo dõi Player.
    /// Gọi bởi BossChaseManager.EndChase().
    /// </summary>
    public void SetPlayerOnlyMode()
    {
        _boss = null;
        _mode = CameraMode.PlayerOnly;
    }

    public void SnapToTarget()
    {
        if (_target == null) return;
        Vector3 basePos = _target.position;
        transform.position = basePos + _currentOffset;

        Vector3 dir = basePos + _lookAtOffset - transform.position;
        if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
    }

    public void SetTarget(Transform target)
    {
        _target = target;
        CacheTargetRigidbody();
        SnapToTarget();
    }

    private void CacheTargetRigidbody()
    {
        _targetRigidbody = _target != null ? _target.GetComponent<Rigidbody>() : null;
    }
}
