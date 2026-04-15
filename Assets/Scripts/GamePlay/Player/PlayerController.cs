using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Config (Data-Driven)")]
    [Tooltip("Gắn PlayerConfigSO vào đây để điều chỉnh thông số không cần sửa code.")]
    [SerializeField] private PlayerConfigSO _config;

    [Header("Core Settings")]
    [SerializeField] private float _baseSpeed = 12f;
    [SerializeField] private float _speedIncreaseRate = 0.1f;
    [SerializeField] private float _maxSpeed = 28f;

    [Header("Lane Settings")]
    [SerializeField] private float _laneDistance = 3.8f;
    [SerializeField] private float _laneChangeSpeed = 15f;

    [Header("Jump And Physics")]
    [SerializeField] private float _jumpForce = 11f;
    [SerializeField] private float _gravity = 28f;
    [SerializeField] private float _diveForce = 25f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask _groundLayer;

    [Header("Roll")]
    [FormerlySerializedAs("_slideDuration")]
    [SerializeField] private float _rollDuration = 0.8f;
    [FormerlySerializedAs("_slideColliderHeight")]
    [SerializeField] private float _rollColliderHeight = 1.2f;
    [FormerlySerializedAs("_slideColliderCenterZ")]
    [SerializeField] private float _rollColliderCenterZ = 0f;

    [Header("Mobile Settings")]
    [SerializeField] private float _minSwipeDistance = 45f;

    [Header("Stumble Settings")]
    [SerializeField] private float _stumbleSpeedPenalty = 0.4f;
    [SerializeField] private float _stumbleDuration = 0.5f;
    [SerializeField] private float _stumbleForwardFreezeTime = 0.2f;
    [SerializeField] private float _stumbleBackwardPush = 0.35f;
    [SerializeField] private float _stumbleSidePush = 0.45f;

    // Legacy events — giữ để không break BossChaseManager hiện tại
    public static event System.Action OnPlayerStumble;
    public static event System.Action OnPlayerJump;

    public static PlayerController Instance { get; private set; }

    private Rigidbody _rb;
    private Animator _animator;
    private CapsuleCollider _capsuleCollider;
    private CharacterSelector _characterSelector;

    private float _currentSpeed;
    private int _currentLane;
    private float _targetX;
    private float _verticalVelocity;
    private bool _isGrounded;
    private bool _isRolling;
    private bool _isDead;
    private bool _isDiving;
    private bool _isStumbling;
    private bool _freezeForwardMovement;
    private float _groundY;
    private float _lastSafeY;
    private Coroutine _rollCoroutine;

    private bool _jumpRequested;
    private bool _diveRequested;
    private bool _rollRequested;

    private Vector2 _startTouchPosition;
    private bool _isSwiping;

    private float _defaultColliderHeight;
    private Vector3 _defaultColliderCenter;

    private static readonly int HashIsRunning = Animator.StringToHash("IsRunning");
    private static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int HashJump = Animator.StringToHash("Jump");
    private static readonly int HashRoll = Animator.StringToHash("Roll");
    private static readonly int HashDeath = Animator.StringToHash("Death");

    private void Awake()
    {
        Instance = this;
        ServiceLocator.Register<PlayerController>(this);
        _rb = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _capsuleCollider = GetComponent<CapsuleCollider>();
        _characterSelector = GetComponent<CharacterSelector>();

        _rb.useGravity = false;
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (_capsuleCollider != null)
        {
            _defaultColliderHeight = _capsuleCollider.height;
            _defaultColliderCenter = _capsuleCollider.center;
        }

        _currentSpeed = _baseSpeed;
        _targetX = _rb.position.x;

        // Áp dụng config từ SO nếu được gán (Data-Driven)
        ApplyConfig();
    }

    /// <summary>
    /// Nếu có PlayerConfigSO gán vào Inspector, ghi đè SerializeField bằng giá trị từ SO.
    /// Không ảnh hưởng gì nếu SO không được gán.
    /// </summary>
    private void ApplyConfig()
    {
        if (_config == null) return;

        _baseSpeed                = _config.baseSpeed;
        _speedIncreaseRate        = _config.speedIncreaseRate;
        _maxSpeed                 = _config.maxSpeed;
        _laneDistance             = _config.laneDistance;
        _laneChangeSpeed          = _config.laneChangeSpeed;
        _jumpForce                = _config.jumpForce;
        _gravity                  = _config.gravity;
        _diveForce                = _config.diveForce;
        _rollDuration             = _config.rollDuration;
        _rollColliderHeight       = _config.rollColliderHeight;
        _rollColliderCenterZ      = _config.rollColliderCenterZ;
        _minSwipeDistance         = _config.minSwipeDistance;
        _stumbleSpeedPenalty      = _config.stumbleSpeedPenalty;
        _stumbleDuration          = _config.stumbleDuration;
        _stumbleForwardFreezeTime = _config.stumbleForwardFreezeTime;
        _stumbleBackwardPush      = _config.stumbleBackwardPush;
        _stumbleSidePush          = _config.stumbleBackwardSidePush;
    }

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
        if (_isDead) return;

        HandleInput();
        UpdateAnimator();

        if (GameManager.Instance != null)
            GameManager.Instance.UpdateDistance(transform.position.z);
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
        if (_isDead) return;

        CheckGroundStatus();
        UpdateSpeed();
        HandleJumpAndDive();
        ApplyMovementFixed();
    }

    private void CheckGroundStatus()
    {
        bool wasGrounded = _isGrounded;

        _isGrounded = Physics.SphereCast(
            transform.position + Vector3.up * 0.5f,
            0.2f,
            Vector3.down,
            out RaycastHit hit,
            0.6f,
            _groundLayer);

        if (_isGrounded)
        {
            _groundY = hit.point.y;
            _lastSafeY = _groundY;

            if (_verticalVelocity < 0f)
                _verticalVelocity = 0f;
        }

        if (!wasGrounded && _isGrounded && _isDiving)
            _isDiving = false;
    }

    private void HandleInput()
    {
        if (_isStumbling) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            MoveToLane(_currentLane - 1);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            MoveToLane(_currentLane + 1);

        if (_isGrounded)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                _jumpRequested = true;
        }
        else
        {
            if (!_isDiving && (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)))
                _diveRequested = true;
        }

        if (_isGrounded && !_isRolling && (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)))
            _rollRequested = true;

        HandleMobileSwipe();
    }

    private void HandleJumpAndDive()
    {
        if (_isGrounded)
        {
            if (_verticalVelocity < 0f)
                _verticalVelocity = -0.1f;

            if (_jumpRequested)
            {
                if (_isRolling) StopRoll();

                _verticalVelocity = _jumpForce;
                _isGrounded = false;

                _animator.SetTrigger(HashJump);
                _jumpRequested = false;
                _rollRequested = false;

                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayJump();

                // Publish qua cả 2 kênh: EventBus (mới) và static event (legacy)
                EventBus.Publish(new PlayerJumpEvent());
                OnPlayerJump?.Invoke();
            }
            else if (_rollRequested)
            {
                if (!_isRolling)
                    _rollCoroutine = StartCoroutine(RollRoutine());

                _rollRequested = false;
            }
        }
        else
        {
            _verticalVelocity -= _gravity * Time.fixedDeltaTime;

            if (_diveRequested)
            {
                _isDiving = true;
                _verticalVelocity = -_diveForce;
                _diveRequested = false;
            }
        }

        if (!_isGrounded)
        {
            _jumpRequested = false;
            _rollRequested = false;
        }
        else
        {
            _diveRequested = false;
        }
    }

    private void HandleMobileSwipe()
    {
        Vector2 currentPos = Vector2.zero;
        bool inputDown = false;
        bool inputHeld = false;

        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            currentPos = t.position;
            if (t.phase == TouchPhase.Began) inputDown = true;
            if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) inputHeld = true;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            inputDown = true;
            currentPos = Input.mousePosition;
        }
        else if (Input.GetMouseButton(0))
        {
            inputHeld = true;
            currentPos = Input.mousePosition;
        }

        if (inputDown)
        {
            _startTouchPosition = currentPos;
            _isSwiping = true;
        }

        if (_isSwiping && inputHeld)
        {
            Vector2 delta = currentPos - _startTouchPosition;
            if (delta.magnitude > _minSwipeDistance)
            {
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                {
                    if (Mathf.Abs(delta.x) > _minSwipeDistance * 1.5f)
                    {
                        if (delta.x > 0) MoveToLane(_currentLane + 1);
                        else MoveToLane(_currentLane - 1);

                        _isSwiping = false;
                    }
                }
                else
                {
                    if (delta.y > 0)
                    {
                        if (_isGrounded)
                        {
                            if (_isRolling) StopRoll();
                            _jumpRequested = true;
                        }
                    }
                    else
                    {
                        if (_isGrounded)
                            _rollRequested = true;
                        else if (!_isDiving)
                            _diveRequested = true;
                    }

                    _isSwiping = false;
                }
            }
        }

        if (Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
            _isSwiping = false;
    }

    private IEnumerator RollRoutine()
    {
        _isRolling = true;
        _animator.SetBool(HashRoll, true);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySlide();

        if (_capsuleCollider != null)
        {
            _capsuleCollider.height = _rollColliderHeight;
            _capsuleCollider.center = new Vector3(0f, _rollColliderHeight / 2f, _rollColliderCenterZ);
        }

        yield return new WaitForSeconds(_rollDuration);
        StopRoll();
    }

    private void StopRoll()
    {
        if (!_isRolling) return;

        if (_rollCoroutine != null)
        {
            StopCoroutine(_rollCoroutine);
            _rollCoroutine = null;
        }

        if (_capsuleCollider != null)
        {
            _capsuleCollider.height = _defaultColliderHeight;
            _capsuleCollider.center = _defaultColliderCenter;
        }

        _animator.SetBool(HashRoll, false);
        _isRolling = false;
    }

    private void UpdateSpeed()
    {
        if (_isDead || _isStumbling) return;

        if (_currentSpeed < _maxSpeed)
            _currentSpeed += _speedIncreaseRate * Time.fixedDeltaTime;
    }

    private void ApplyMovementFixed()
    {
        float dt = Time.fixedDeltaTime;
        Vector3 currentPosition = _rb.position;

        float newZ = _freezeForwardMovement ? currentPosition.z : currentPosition.z + (_currentSpeed * dt);
        float newX = Mathf.MoveTowards(currentPosition.x, _targetX, _laneChangeSpeed * dt);
        float newY = currentPosition.y + (_verticalVelocity * dt);

        if (_isGrounded && _verticalVelocity <= 0f)
        {
            if (Mathf.Abs(newY - _groundY) < 0.5f)
                newY = _groundY;
        }

        if (newY < -3f)
        {
            newY = _lastSafeY > -2f ? _lastSafeY : 0f;
            _verticalVelocity = 0f;
        }

        if (Mathf.Abs(newX - _targetX) < 0.01f)
            newX = _targetX;

        _rb.MovePosition(new Vector3(newX, newY, newZ));
    }

    private void UpdateAnimator()
    {
        bool isRunning = GameManager.Instance != null && GameManager.Instance.IsPlaying;
        _animator.SetBool(HashIsRunning, isRunning);
        _animator.SetBool(HashIsGrounded, _isGrounded);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_isDead || _isStumbling) return;

        ObstacleIdentity identity = ResolveObstacleIdentity(collision.collider);
        if (identity == null && !collision.gameObject.CompareTag("Obstacle")) return;

        bool isTopHit = false;
        bool isSideHit = false;
        foreach (ContactPoint c in collision.contacts)
        {
            if (c.normal.y > 0.5f) isTopHit = true;
            if (Mathf.Abs(c.normal.x) > 0.4f) isSideHit = true;
        }

        if (isTopHit)
        {
            bool allowTop = identity != null
                ? (identity.AllowTopLanding || identity.CollisionType == ObstacleCollisionType.JumpableTop)
                : true;

            if (allowTop) return;
        }

        if (PowerUpManager.Instance != null && PowerUpManager.Instance.HasShield())
        {
            PowerUpManager.Instance.ConsumeShield();
            collision.gameObject.SetActive(false);
            return;
        }

        if (identity != null && identity.CollisionType == ObstacleCollisionType.VehicleStumble)
        {
            float obstacleX = identity.transform.position.x;
            float deltaX = Mathf.Abs(transform.position.x - obstacleX);
            bool isPositionSideHit = deltaX > 0.6f;

            if (isSideHit || isPositionSideHit)
            {
                TriggerStumble(identity.transform.position);
                return;
            }
        }

        Die();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isDead || _isStumbling) return;

        ObstacleIdentity identity = ResolveObstacleIdentity(other);
        if (identity == null && !other.CompareTag("Obstacle")) return;

        if (identity != null && identity.CollisionType == ObstacleCollisionType.JumpableTop)
            return;

        if (PowerUpManager.Instance != null && PowerUpManager.Instance.HasShield())
        {
            PowerUpManager.Instance.ConsumeShield();
            other.gameObject.SetActive(false);
            return;
        }

        if (identity != null && identity.CollisionType == ObstacleCollisionType.VehicleStumble)
        {
            float obstacleX = identity.transform.position.x;
            float deltaX = Mathf.Abs(transform.position.x - obstacleX);
            if (deltaX > 0.6f)
            {
                TriggerStumble(identity.transform.position);
                return;
            }
        }

        Die();
    }

    private ObstacleIdentity ResolveObstacleIdentity(Component hitComponent)
    {
        if (hitComponent == null) return null;

        ObstacleIdentity identity = hitComponent.GetComponent<ObstacleIdentity>();
        if (identity != null) return identity;

        return hitComponent.GetComponentInParent<ObstacleIdentity>();
    }

    public void TriggerStumble()
    {
        TriggerStumble(transform.position);
    }

    public void TriggerStumble(Vector3 obstaclePosition)
    {
        if (_isDead || _isStumbling) return;
        StartCoroutine(StumbleRoutine(obstaclePosition));
    }

    private IEnumerator StumbleRoutine(Vector3 obstaclePosition)
    {
        _isStumbling = true;
        _freezeForwardMovement = true;

        float previousSpeed = _currentSpeed;
        _currentSpeed *= (1f - _stumbleSpeedPenalty);

        if (_rb != null)
        {
            Vector3 stumblePosition = _rb.position;
            float pushDirection = stumblePosition.x >= obstaclePosition.x ? 1f : -1f;
            stumblePosition.x += pushDirection * _stumbleSidePush;
            stumblePosition.z -= _stumbleBackwardPush;
            stumblePosition.x = Mathf.Clamp(stumblePosition.x, -_laneDistance, _laneDistance);

            _rb.position = stumblePosition;

            int snappedLane = Mathf.RoundToInt(stumblePosition.x / _laneDistance);
            _currentLane = Mathf.Clamp(snappedLane, -1, 1);
            _targetX = _currentLane * _laneDistance;
        }

        // Publish qua cả 2 kênh: EventBus (mới) và static event (legacy)
        EventBus.Publish(new PlayerStumbleEvent());
        OnPlayerStumble?.Invoke();

        yield return new WaitForSeconds(_stumbleForwardFreezeTime);
        _freezeForwardMovement = false;

        float remainingStumble = Mathf.Max(0f, _stumbleDuration - _stumbleForwardFreezeTime);
        if (remainingStumble > 0f)
            yield return new WaitForSeconds(remainingStumble);

        if (!_isDead)
            _currentSpeed = previousSpeed;

        _isStumbling = false;
        _freezeForwardMovement = false;
    }

    public void MoveToLane(int laneIndex)
    {
        _currentLane = Mathf.Clamp(laneIndex, -1, 1);
        _targetX = _currentLane * _laneDistance;
    }

    public void Die()
    {
        if (_isDead) return;

        _isDead = true;
        _currentSpeed = 0f;
        _verticalVelocity = 0f;
        _freezeForwardMovement = false;

        if (_rb != null)
        {
            _rb.velocity = Vector3.zero;
            _rb.useGravity = true;
        }

        StopAllCoroutines();
        if (_capsuleCollider != null)
        {
            _capsuleCollider.height = _defaultColliderHeight;
            _capsuleCollider.center = _defaultColliderCenter;
        }

        _animator.SetBool(HashRoll, false);
        _animator.SetTrigger(HashDeath);

        if (BossChaseManager.Instance != null)
            BossChaseManager.Instance.ResetChase();

        if (AudioManager.Instance != null)
        {
            bool isFemale = _characterSelector != null && _characterSelector.GetSelectedSkinIndex() == 1;
            AudioManager.Instance.PlayDeath(isFemale);
        }

        if (GameManager.Instance != null)
            GameManager.Instance.TriggerGameOver();
    }

    public void Initialize()
    {
        _isDead = false;
        _isRolling = false;
        _isDiving = false;
        _isStumbling = false;
        _freezeForwardMovement = false;
        _currentSpeed = _baseSpeed;
        _currentLane = 0;
        _targetX = 0f;
        _verticalVelocity = 0f;

        if (BossChaseManager.Instance != null)
            BossChaseManager.Instance.ResetChase();

        Vector3 resetPosition = _rb != null ? _rb.position : transform.position;
        resetPosition.x = 0f;

        if (_rb != null)
        {
            _rb.position = resetPosition;
            _rb.velocity = Vector3.zero;
            _rb.useGravity = false;
        }
        else
        {
            transform.position = resetPosition;
        }

        if (_capsuleCollider != null)
        {
            _capsuleCollider.height = _defaultColliderHeight;
            _capsuleCollider.center = _defaultColliderCenter;
        }
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<PlayerController>();
        OnPlayerStumble = null;
        OnPlayerJump    = null;
    }
}
