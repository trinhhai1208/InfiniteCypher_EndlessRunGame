using UnityEngine;

public enum BossState { Hidden, Chasing, Recovering, Victory }

/// <summary>
/// Điều khiển transform và animator của Boss.
/// Có thể hoạt động ngay cả khi object Boss bị inactive từ đầu scene.
/// </summary>
public class BossController : MonoBehaviour
{
    public static BossController Instance { get; private set; }

    [Header("Chase Movement")]
    [Header("Chase Movement")]
    [Tooltip("Kho\u1ea3ng c\u00e1ch Boss s\u1ebd d\u1eebng l\u1ea1i ngay sau Player (hi\u1ec7n trong frame).")]
    [SerializeField] private float _chaseDistance = 2f;
    [Tooltip("Kho\u1ea3ng c\u00e1ch Boss sinh ra t\u1eeb xa \u0111\u1ec3 ph\u00f3ng t\u1edbi.")]
    [SerializeField] private float _spawnBehindDistance = 15f;
    [SerializeField] private float _retreatDistance = 25f;
    [SerializeField] private float _zSmoothTime = 0.12f; // Giảm xuống để phản ứng nhanh hơn
    //[SerializeField] private float _xFollowSpeed = 5f;

    [Header("Lane Follow (Smooth Lag)")]
    [Tooltip("Thời gian Boss lag lại so với Player khi đổi lane. Tăng lên để Boss đổi lane chậm hơn.")]
    [SerializeField] private float _laneFollowDelay = 0.35f;   // s dùng làm smoothTime cho X
    [SerializeField] private float _xSmoothTime = 0.12f;        // Fine-tune nếu cần SmoothDamp thêm

    public BossState State { get; private set; } = BossState.Hidden;

    private Animator _animator;
    private Transform _player;

    private float _currentZ;
    private float _targetZ;
    private float _zVelocity;

    private float _currentX;
    private float _delayedPlayerX;   // Lagged follower: chase player X chậm lại tự nhiên
    private float _xLagVelocity;     // Velocity cho SmoothDamp của lagged follower
    private float _xVelocity;        // Velocity cho _currentX -> _delayedPlayerX

    private float _currentY;         // Trục Y mượt để boss không bị giật mồng trên mặt đất
    private float _yVelocity;
    private const float _ySmoothTime = 0.08f;

    private static readonly int HashIsRunning = Animator.StringToHash("IsRunning");
    private static readonly int HashJump = Animator.StringToHash("Jump");
    private static readonly int HashVictory = Animator.StringToHash("Victory");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
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
        // Cực kì quan trọng: Nếu Boss đang được lệnh Chasing (do gọi Appear() từ frame trước) 
        // thì không được đè lệnh ForceHide() lên!
        if (State != BossState.Chasing)
        {
            ForceHide();
        }
    }

    private void LateUpdate()
    {
        EnsureInitialized();

        if (_player == null) return;
        if (State == BossState.Hidden || State == BossState.Victory) return;

        float playerZ = _player.position.z;
        float playerY = _player.position.y;

        if (State == BossState.Chasing)
        {
            // Lu\u00f4n b\u00e1m s\u00e1t sau player m\u1ed9t kho\u1ea3ng c\u1ed1 \u0111\u1ecbnh
            _targetZ = playerZ - _chaseDistance;

            // Lagged follower: _delayedPlayerX s\u1ebd t\u1ef1 \u0111\u1ed9ng \u0111u\u1edbi player X ch\u1eadm l\u1ea1i t\u1ef1 nhi\u00ean
            _delayedPlayerX = Mathf.SmoothDamp(
                _delayedPlayerX,
                _player.position.x,
                ref _xLagVelocity,
                _laneFollowDelay
            );
        }
        else if (State == BossState.Recovering)
        {
            _targetZ = playerZ - _retreatDistance;

            // Khi lui, theo thẳng player X (không lag)
            _delayedPlayerX = _player.position.x;

            float gapZ = playerZ - _currentZ;
            if (gapZ >= _retreatDistance - 2f)
            {
                ForceHide();
                return;
            }
        }

        // ── Trục Z (Tiến/Lùi) ────────────────────
        if (State == BossState.Chasing)
        {
            // Nếu đã tiến sát mục tiêu (trong tầm 0.1m), khóa chặt Z để tránh trễ theo vận tốc
            float distZ = Mathf.Abs(_currentZ - _targetZ);
            if (distZ < 0.15f)
            {
                _currentZ = _targetZ;
                _zVelocity = 0f;
            }
            else
            {
                _currentZ = Mathf.SmoothDamp(_currentZ, _targetZ, ref _zVelocity, _zSmoothTime);
            }
        }
        else
        {
            // Khi Recovery (lùi đi), dùng SmoothDamp cho mượt mà
            _currentZ = Mathf.SmoothDamp(_currentZ, _targetZ, ref _zVelocity, _zSmoothTime);
        }
        // _currentX bám theo _delayedPlayerX (vốn đã lag sẵn), thêm smooth nhẹ để tránh jitter
        _currentX = Mathf.SmoothDamp(_currentX, _delayedPlayerX, ref _xVelocity, _xSmoothTime);
        
        // Hấp thụ rung giật trục Y từ Animator/Physics của Player bằng SmoothDamp ngắn
        _currentY = Mathf.SmoothDamp(_currentY, playerY, ref _yVelocity, _ySmoothTime);

        transform.position = new Vector3(_currentX, _currentY, _currentZ);
    }

    public void Appear()
    {
        EnsureInitialized();
        if (_player == null || _animator == null) return;

        // Sinh ra t\u1eeb xa (ngo\u00e0i camera)
        _currentZ     = _player.position.z - _spawnBehindDistance;
        // M\u1ee5c ti\u00eau l\u00e0 ti\u1ebfn s\u00e1t d\u01b0\u1edbi l\u01b0ng player
        _targetZ      = _player.position.z - _chaseDistance;
        
        _currentX     = _player.position.x;
        _currentY     = _player.position.y;
        _delayedPlayerX = _player.position.x;
        _zVelocity    = 0f;
        _xVelocity    = 0f;
        _yVelocity    = 0f;
        _xLagVelocity = 0f;

        transform.position = new Vector3(_currentX, _currentY, _currentZ);

        gameObject.SetActive(true);
        State = BossState.Chasing;
        _animator.SetBool(HashIsRunning, true);
    }

    public void Disappear()
    {
        EnsureInitialized();
        if (State == BossState.Hidden || State == BossState.Victory) return;
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
        if (State == BossState.Chasing && _animator != null)
            _animator.SetTrigger(HashJump);
    }

    public void PlayVictory()
    {
        EnsureInitialized();
        if (State == BossState.Hidden || _animator == null) return;

        State = BossState.Victory;
        _animator.SetBool(HashIsRunning, false);
        _animator.SetTrigger(HashVictory);
    }

    private void EnsureInitialized()
    {
        if (Instance == null)
            Instance = this;

        if (_animator == null)
            _animator = GetComponent<Animator>();

        if (_player == null && PlayerController.Instance != null)
            _player = PlayerController.Instance.transform;
    }
}
