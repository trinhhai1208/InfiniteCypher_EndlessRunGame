using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform _target;

    [Header("Offset")]
    [SerializeField] private Vector3 _offset = new Vector3(0f, 3.5f, -5.5f);

    [Header("Smoothing")]
    [SerializeField] private float _positionSmoothTime = 0.15f;
    [SerializeField] private float _rotationSmoothTime = 0.3f;
    [SerializeField] private Vector3 _lookAtOffset = new Vector3(0f, 1f, 0f);

    // Cached
    private Vector3 _positionVelocity;
    private Rigidbody _targetRigidbody;

    private void Awake()
    {
        CacheTargetRigidbody();
    }

    private void Start()
    {
        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (_target == null) return;

        FollowTarget();
    }

    private void FollowTarget()
    {
        Vector3 targetBasePosition = _targetRigidbody != null ? _targetRigidbody.position : _target.position;

        // VỊ TRÍ MỤC TIÊU:
        // Chúng ta muốn bám sát Tuyệt đối theo trục Z (chiều chạy) để không bị trễ
        // Nhưng vẫn làm mượt trục X (chuyển làn) và Y (nhảy)
        Vector3 targetPos = targetBasePosition + _offset;

        Vector3 currentPos = transform.position;
        
        // Làm mượt X và Y
        float newX = Mathf.SmoothDamp(currentPos.x, targetPos.x, ref _positionVelocity.x, _positionSmoothTime);
        float newY = Mathf.SmoothDamp(currentPos.y, targetPos.y, ref _positionVelocity.y, _positionSmoothTime);
        
        // Trục Z: endless runner cần bám gần như trực tiếp để tránh cảm giác camera bị kéo giật phía sau.
        float newZ = targetPos.z;

        transform.position = new Vector3(newX, newY, newZ);

        // QUAY CAMERA:
        // Nhắm vào nhân vật với một khoảng Offset (ví dụ nhắm vào ngực thay vì chân)
        Vector3 lookAtTarget = targetBasePosition + _lookAtOffset;
        Vector3 direction = lookAtTarget - transform.position;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            float rotationLerp = _rotationSmoothTime <= 0.0001f ? 1f : Time.deltaTime / _rotationSmoothTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Mathf.Clamp01(rotationLerp));
        }
    }

    /// <summary>
    /// Call this to instantly teleport the camera to the correct position (e.g. on game start/restart).
    /// </summary>
    public void SnapToTarget()
    {
        if (_target == null) return;

        Vector3 targetBasePosition = _targetRigidbody != null ? _targetRigidbody.position : _target.position;
        transform.position = targetBasePosition + _offset;

        Vector3 lookAtTarget = targetBasePosition + _lookAtOffset;
        Vector3 direction = lookAtTarget - transform.position;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
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
