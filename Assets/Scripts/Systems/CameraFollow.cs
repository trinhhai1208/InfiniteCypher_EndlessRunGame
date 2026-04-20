using UnityEngine;

/// <summary>
/// Camera follow chuyen dung cho Endless Runner.
/// Camera luon follow player, boss tu xu ly di chuyen cua minh.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Header("Target")]
    [SerializeField] private Transform _target;

    [Header("Offset")]
    [SerializeField] private Vector3 _offset = new Vector3(0f, 3.5f, -5.5f);

    [Header("Smoothing")]
    [Tooltip("Smoothing cho truc X (chuyen lan). Nho = ban chat, lon = mem mai.")]
    [SerializeField] private float _xSmoothTime = 0.12f;
    [Tooltip("Smoothing cho truc Y (nhan vat nhay). Nho = bat keo, lon = mem mai.")]
    [SerializeField] private float _ySmoothTime = 0.15f;
    [SerializeField] private float _rotationSmoothTime = 0.25f;

    [Header("Look At")]
    [SerializeField] private Vector3 _lookAtOffset = new Vector3(0f, 1f, 0f);

    // Dynamics
    private float _xVelocity;
    private float _yVelocity;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start() => SnapToTarget();

    private void LateUpdate()
    {
        if (_target == null) return;
        FollowTarget();
    }

    private void FollowTarget()
    {
        Vector3 playerPos  = _target.position;
        Vector3 targetCamPos = playerPos + _offset;
        Vector3 curPos     = transform.position;

        // X: smooth chuyen lan
        float newX = Mathf.SmoothDamp(curPos.x, targetCamPos.x, ref _xVelocity, _xSmoothTime);
        // Y: smooth nhan vat nhay/ha
        float newY = Mathf.SmoothDamp(curPos.y, targetCamPos.y, ref _yVelocity, _ySmoothTime);
        // Z: bam sat truc tien tranh cam giac lag (khong dung SmoothDamp)
        float newZ = targetCamPos.z;

        transform.position = new Vector3(newX, newY, newZ);

        // LookAt Player
        Vector3 lookAt    = new Vector3(newX, playerPos.y + _lookAtOffset.y, playerPos.z + _lookAtOffset.z);
        Vector3 direction = lookAt - transform.position;
        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation   = Quaternion.Slerp(transform.rotation, targetRot,
                                        Time.deltaTime / _rotationSmoothTime);
        }
    }

    // Public API
    public void SnapToTarget()
    {
        if (_target == null) return;
        transform.position = _target.position + _offset;
        Vector3 dir = (_target.position + _lookAtOffset) - transform.position;
        if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
    }

    public void SetTarget(Transform target)
    {
        _target = target;
        SnapToTarget();
    }

    // Giu lai empty de tranh loi build neu con script tham chieu
    public void SetChaseMode(Transform boss) { }
    public void SetPlayerOnlyMode() { }
}
