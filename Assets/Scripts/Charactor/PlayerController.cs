using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("=== Core Settings ===")]
    [SerializeField] private float _baseSpeed = 12f;
    [SerializeField] private float _speedIncreaseRate = 0.1f;
    [SerializeField] private float _maxSpeed = 28f;

    [Header("Lane Settings")]
    [SerializeField] private float _laneDistance = 3.8f;
    [SerializeField] private float _laneChangeSpeed = 15f;

    [Header("Jump & Physics")]
    [SerializeField] private float _jumpForce = 11f;
    [SerializeField] private float _gravity = 28f;
    [Tooltip("Lực đặp xuống khi Dive (nhấn S/↓ khi đang trong không trung)")]
    [SerializeField] private float _diveForce = 25f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask _groundLayer;
    //[SerializeField] private float _groundRayDistance = 0.2f;

    [Header("Slide")]
    [SerializeField] private float _slideDuration = 0.8f;
    [SerializeField] private float _slideColliderHeight = 1.2f;
    [SerializeField] private float _slideColliderCenterZ = 0f; // Để 0 để tránh lệch chân khi slide

    [Header("Mobile Settings")]
    [SerializeField] private float _minSwipeDistance = 75f; // Tăng từ 45 lên 75 để chắc chắn hơn
    private Vector2 _startTouchPosition;
    private bool _isSwiping = false;

    // Cached components
    private Rigidbody _rb;
    private Animator _animator;
    private CapsuleCollider _capsuleCollider;

    // Movement state
    private float _currentSpeed;
    private int _currentLane = 0; // -1: Left, 0: Middle, 1: Right
    private float _targetX;
    private float _verticalVelocity;
    private bool _isGrounded;
    private bool _isSliding;
    private bool _isDead;
    private bool _isDiving;
    private float _groundY;
    private float _lastSafeY;

    // Collider cache
    private float _defaultColliderHeight;
    private Vector3 _defaultColliderCenter;

    // Animator Hashes
    private static readonly int HashIsRunning = Animator.StringToHash("IsRunning");
    private static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int HashJump       = Animator.StringToHash("Jump");
    private static readonly int HashSlide      = Animator.StringToHash("Slide");
    private static readonly int HashDeath      = Animator.StringToHash("Death");

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _capsuleCollider = GetComponent<CapsuleCollider>();

        _rb.useGravity = false;
        _rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (_capsuleCollider != null)
        {
            _defaultColliderHeight   = _capsuleCollider.height;
            _defaultColliderCenter   = _capsuleCollider.center;
        }

        _currentSpeed = _baseSpeed;
        _targetX      = transform.position.x;
    }

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
        if (_isDead) return;

        CheckGroundStatus();
        HandleInput();
        UpdateSpeed();
        ApplyMovement();
        UpdateAnimator();

        if (GameManager.Instance != null && !_isDead)
        {
            GameManager.Instance.UpdateDistance(transform.position.z);
        }
    }

    private void CheckGroundStatus()
    {
        bool wasGrounded = _isGrounded;

        // SphereCast để tránh lọt khe và bắt kịp tốc độ Dive
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

            if (_verticalVelocity < 0)
                _verticalVelocity = 0;
        }

        if (!wasGrounded && _isGrounded && _isDiving)
        {
            _isDiving = false;
            if (!_isSliding)
                StartCoroutine(SlideRoutine());
        }
    }

    private void HandleInput()
    {
        // --- 1. KEYBOARD (Giữ nguyên logic gốc của bạn) ---
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            MoveToLane(_currentLane - 1);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            MoveToLane(_currentLane + 1);

        if (_isGrounded)
        {
            if (_verticalVelocity < 0)
                _verticalVelocity = -0.1f;

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                _verticalVelocity = _jumpForce;
                _animator.SetTrigger(HashJump);
            }
        }
        else
        {
            _verticalVelocity -= _gravity * Time.deltaTime;

            if (!_isDiving && (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)))
            {
                _isDiving = true;
                _verticalVelocity = -_diveForce;
            }
        }

        if (_isGrounded && !_isSliding && (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)))
        {
            StartCoroutine(SlideRoutine());
        }

        // --- 2. MOBILE SWIPE (Vuốt liên tục) ---
        HandleMobileSwipe();
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
                    // VUỐT NGANG: Chỉ thực hiện 1 lần duy nhất cho mỗi cú vuốt
                    if (Mathf.Abs(delta.x) > _minSwipeDistance * 1.5f)
                    {
                        if (delta.x > 0) MoveToLane(_currentLane + 1);
                        else MoveToLane(_currentLane - 1);
                        
                        // KHÔNG reset _startTouchPosition ở đây nữa 
                        // -> Buộc người chơi phải nhấc tay hoặc vuốt rất dài mới sang tiếp được
                        _isSwiping = false; 
                    }
                }
                else
                {
                    // VUỐT DỌC
                    if (delta.y > 0)
                    {
                        if (_isGrounded)
                        {
                            _verticalVelocity = _jumpForce;
                            _animator.SetTrigger(HashJump);
                        }
                    }
                    else
                    {
                        if (_isGrounded)
                        {
                            if (!_isSliding) StartCoroutine(SlideRoutine());
                        }
                        else
                        {
                            if (!_isDiving) { _isDiving = true; _verticalVelocity = -_diveForce; }
                        }
                    }
                    
                    // Với Nhảy/Slide ta cũng tạm dừng nhận diện cú vuốt hiện tại để tránh bị lặp lệnh
                    _isSwiping = false; 
                }
            }
        }

        if (Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
        {
            _isSwiping = false;
        }
    }

    private IEnumerator SlideRoutine()
    {
        _isSliding = true;
        _animator.SetBool(HashSlide, true);

        if (_capsuleCollider != null)
        {
            _capsuleCollider.height   = _slideColliderHeight;
            _capsuleCollider.center   = new Vector3(0f, _slideColliderHeight / 2f, _slideColliderCenterZ);
        }

        yield return new WaitForSeconds(_slideDuration);

        if (_capsuleCollider != null)
        {
            _capsuleCollider.height = _defaultColliderHeight;
            _capsuleCollider.center = _defaultColliderCenter;
        }

        _animator.SetBool(HashSlide, false);
        _isSliding = false;
    }

    private void UpdateSpeed()
    {
        if (_currentSpeed < _maxSpeed)
            _currentSpeed += _speedIncreaseRate * Time.deltaTime;
    }

    private void ApplyMovement()
    {
        float newX = Mathf.Lerp(transform.position.x, _targetX, _laneChangeSpeed * Time.deltaTime);
        float newY = transform.position.y + (_verticalVelocity * Time.deltaTime);

        if (_isGrounded && _verticalVelocity <= 0)
        {
            if (newY - _groundY <= 0.2f)
                newY = _groundY; 
        }

        float newZ = transform.position.z + (_currentSpeed * Time.deltaTime);

        // Safety net
        if (newY < -3f)
        {
            newY = _lastSafeY > -2f ? _lastSafeY : 0f;
            _verticalVelocity = 0f;
        }

        transform.position = new Vector3(newX, newY, newZ);
    }

    private void UpdateAnimator()
    {
        bool isRunning = GameManager.Instance != null && GameManager.Instance.IsPlaying;
        _animator.SetBool(HashIsRunning, isRunning);
        _animator.SetBool(HashIsGrounded, _isGrounded);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_isDead || !collision.gameObject.CompareTag("Obstacle")) return;

        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f) return;
        }

        if (PowerUpManager.Instance != null && PowerUpManager.Instance.HasShield())
        {
            PowerUpManager.Instance.ConsumeShield();
            collision.gameObject.SetActive(false); 
            return;
        }

        Die();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isDead || !other.CompareTag("Obstacle")) return;

        if (PowerUpManager.Instance != null && PowerUpManager.Instance.HasShield())
        {
            PowerUpManager.Instance.ConsumeShield();
            other.gameObject.SetActive(false);
            return;
        }

        Die();
    }

    public void MoveToLane(int laneIndex)
    {
        _currentLane = Mathf.Clamp(laneIndex, -1, 1);
        _targetX     = _currentLane * _laneDistance;
    }

    public void Die()
    {
        if (_isDead) return;
        _isDead = true;
        _currentSpeed = 0f;
        _verticalVelocity = 0f;

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

        _animator.SetBool(HashSlide, false);
        _animator.SetTrigger(HashDeath);
        if (GameManager.Instance != null) GameManager.Instance.TriggerGameOver();
    }

    public void Initialize()
    {
        _isDead = false;
        _isSliding = false;
        _isDiving = false;
        _currentSpeed = _baseSpeed;
        _currentLane = 0;
        _targetX = 0;
        _verticalVelocity = 0;
        transform.position = new Vector3(0, transform.position.y, transform.position.z);

        if (_capsuleCollider != null)
        {
            _capsuleCollider.height = _defaultColliderHeight;
            _capsuleCollider.center = _defaultColliderCenter;
        }
        if (_rb != null) _rb.useGravity = false;
    }
}