using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform _target;

    [Header("Offset")]
    [SerializeField] private Vector3 _offset = new Vector3(0f, 4f, -5f);

    [Header("Smoothing")]
    [SerializeField] private float _positionSmoothTime = 0.15f;
    [SerializeField] private float _rotationSmoothTime = 0.3f;
    [SerializeField] private Vector3 _lookAtOffset = new Vector3(0f, 1f, 0f);

    // Cached
    private Vector3 _positionVelocity;
    private float _rotationVelocityY;

    private void LateUpdate()
    {
        if (_target == null) return;

        FollowTarget();
    }

    private void FollowTarget()
    {
        // Calculate world-space desired position based on target's Z rotation only
        Vector3 desiredPosition = _target.position + _target.rotation * _offset;

        // Smooth the position
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _positionVelocity, _positionSmoothTime);

        // Smooth the look-at rotation
        Vector3 lookAtTarget = _target.position + _lookAtOffset;
        Vector3 direction = lookAtTarget - transform.position;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime / _rotationSmoothTime);
        }
    }

    /// <summary>
    /// Call this to instantly teleport the camera to the correct position (e.g. on game start/restart).
    /// </summary>
    public void SnapToTarget()
    {
        if (_target == null) return;

        transform.position = _target.position + _target.rotation * _offset;

        Vector3 lookAtTarget = _target.position + _lookAtOffset;
        Vector3 direction = lookAtTarget - transform.position;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    public void SetTarget(Transform target)
    {
        _target = target;
    }
}
