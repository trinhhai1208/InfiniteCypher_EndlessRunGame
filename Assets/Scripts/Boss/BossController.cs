using UnityEngine;

public enum BossState { Hidden, Intro, Chasing, Recovering }

/// <summary>
/// Điều khiển transform và animator của Boss.
/// State machine:
///   Hidden     → Boss không xuất hiện (inactive).
///   Intro      → Xuất hiện gần player, giữ nguyên khoảng cách ngắn rồi tự lùi.
///   Chasing    → Bám sát sau player một khoảng cố định (suspense only, không gây chết).
///   Recovering → Lùi dần theo tốc độ cố định để tạo cảm giác rình rập.
/// </summary>
public class BossController : MonoBehaviour
{
    public static BossController Instance { get; private set; }

    [Header("Chase Movement")]
    [Tooltip("Khoảng cách Boss giữ sau Player khi đang rượt (suspense only).")]
    [SerializeField] private float _chaseDistance = 2f;
    [Tooltip("Khoảng cách Boss sinh ra từ xa để phóng tới.")]
    [SerializeField] private float _spawnBehindDistance = 15f;

    [Header("Intro Sequence")]
    [Tooltip("Khoảng cách Boss xuất hiện sau Player trong màn Intro (nhìn thấy rõ).")]
    [SerializeField] private float _introDistance = 1f;
    [Tooltip("Thời gian Boss đứng gần (Intro) trước khi bắt đầu lùi.")]
    [SerializeField] private float _introHoldDuration = 5.0f;

    [Header("Recovering (Retreat)")]
    [Tooltip("Tốc độ Boss lùi về phía sau (m/s). Nhỏ = lùi chậm, tạo cảm giác rình rập.")]
    [SerializeField] private float _retreatSpeed = 4f;
    [Tooltip("Khoảng cách tối đa từ player để Boss ẩn đi.")]
    [SerializeField] private float _retreatHideDistance = 30f;
    [Tooltip("Thời gian SmoothDamp trục Z khi bám sát.")]
    [SerializeField] private float _zSmoothTime = 0.10f;

    [Header("Lane Follow (Smooth Lag)")]
    [Tooltip("Thời gian Boss lag lại so với Player khi đổi lane.")]
    [SerializeField] private float _laneFollowDelay = 0.35f;
    [SerializeField] private float _xSmoothTime = 0.12f;

    public BossState State { get; private set; } = BossState.Hidden;

    private Animator  _animator;
    private Transform _player;

    private float _currentZ;
    private float _targetZ;
    private float _zVelocity;

    private float _currentX;
    private float _delayedPlayerX;
    private float _xLagVelocity;
    private float _xVelocity;

    private float _currentY;
    private float _yVelocity;
    private const float _ySmoothTime = 0.08f;

    private float _introTimer;

    private static readonly int HashIsRunning = Animator.StringToHash("IsRunning");
    private static readonly int HashJump      = Animator.StringToHash("Jump");

    // ─────────────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        ServiceLocator.Register<BossController>(this);
        EnsureInitialized();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            ServiceLocator.Unregister<BossController>();
            Instance = null;
        }
    }

    private void Start()
    {
        EnsureInitialized();
        if (State != BossState.Chasing && State != BossState.Intro)
            ForceHide();
    }

    private void LateUpdate()
    {
        EnsureInitialized();
        if (_player == null) return;
        if (State == BossState.Hidden) return;

        float playerZ = _player.position.z;
        float playerY = _player.position.y;

        switch (State)
        {
            case BossState.Intro:
                _targetZ        = playerZ - _introDistance;
                _delayedPlayerX = Mathf.SmoothDamp(_delayedPlayerX, _player.position.x,
                                                    ref _xLagVelocity, _laneFollowDelay);
                _introTimer -= Time.deltaTime;
                if (_introTimer <= 0f)
                    State = BossState.Recovering;
                break;

            case BossState.Chasing:
                _targetZ        = playerZ - _chaseDistance;
                _delayedPlayerX = Mathf.SmoothDamp(_delayedPlayerX, _player.position.x,
                                                    ref _xLagVelocity, _laneFollowDelay);
                break;

            case BossState.Recovering:
                _currentZ      -= _retreatSpeed * Time.deltaTime;
                _delayedPlayerX = _player.position.x;

                if (playerZ - _currentZ >= _retreatHideDistance)
                {
                    ForceHide();
                    return;
                }

                _currentX = Mathf.SmoothDamp(_currentX, _delayedPlayerX, ref _xVelocity, _xSmoothTime);
                _currentY = Mathf.SmoothDamp(_currentY, playerY,          ref _yVelocity, _ySmoothTime);
                transform.position = new Vector3(_currentX, _currentY, _currentZ);
                return;
        }

        // Cập nhật Z cho Intro & Chasing
        float distZ = Mathf.Abs(_currentZ - _targetZ);
        if (distZ < 0.15f)
        {
            _currentZ  = _targetZ;
            _zVelocity = 0f;
        }
        else
        {
            _currentZ = Mathf.SmoothDamp(_currentZ, _targetZ, ref _zVelocity, _zSmoothTime);
        }

        _currentX = Mathf.SmoothDamp(_currentX, _delayedPlayerX, ref _xVelocity, _xSmoothTime);
        _currentY = Mathf.SmoothDamp(_currentY, playerY,          ref _yVelocity, _ySmoothTime);
        transform.position = new Vector3(_currentX, _currentY, _currentZ);
    }

    // ─────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Màn Intro: Boss xuất hiện rất gần player trong vài giây rồi tự lùi ra.
    /// </summary>
    public void PlayIntro()
    {
        EnsureInitialized();
        if (_player == null || _animator == null) return;

        _currentZ       = _player.position.z - _introDistance;
        _targetZ        = _currentZ;
        _currentX       = _player.position.x;
        _currentY       = _player.position.y;
        _delayedPlayerX = _player.position.x;
        _zVelocity = _xVelocity = _yVelocity = _xLagVelocity = 0f;

        _introTimer        = _introHoldDuration;
        transform.position = new Vector3(_currentX, _currentY, _currentZ);

        gameObject.SetActive(true);
        State = BossState.Intro;
        _animator.SetBool(HashIsRunning, true);
    }

    /// <summary>
    /// Chase thông thường: Boss chạy ra từ xa và bám sát sau player.
    /// </summary>
    public void Appear()
    {
        EnsureInitialized();
        if (_player == null || _animator == null) return;

        _currentZ       = _player.position.z - _spawnBehindDistance;
        _targetZ        = _player.position.z - _chaseDistance;
        _currentX       = _player.position.x;
        _currentY       = _player.position.y;
        _delayedPlayerX = _player.position.x;
        _zVelocity = _xVelocity = _yVelocity = _xLagVelocity = 0f;

        transform.position = new Vector3(_currentX, _currentY, _currentZ);
        gameObject.SetActive(true);
        State = BossState.Chasing;
        _animator.SetBool(HashIsRunning, true);
    }

    /// <summary>
    /// Bắt đầu lùi dần — trạng thái rình rập.
    /// </summary>
    public void Disappear()
    {
        EnsureInitialized();
        if (State == BossState.Hidden) return;
        State = BossState.Recovering;
    }

    public void ForceHide()
    {
        EnsureInitialized();
        State = BossState.Hidden;
        if (_animator != null)
            _animator.SetBool(HashIsRunning, false);
        gameObject.SetActive(false);
    }

    public void PlayJump()
    {
        EnsureInitialized();
        if ((State == BossState.Chasing || State == BossState.Intro) && _animator != null)
            _animator.SetTrigger(HashJump);
    }


    // ─────────────────────────────────────────────────────────────
    // Private
    // ─────────────────────────────────────────────────────────────

    private void EnsureInitialized()
    {
        if (Instance == null) Instance = this;
        if (_animator == null) _animator = GetComponent<Animator>();
        if (_player == null && PlayerController.Instance != null)
            _player = PlayerController.Instance.transform;
    }
}