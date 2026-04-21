using System.Collections;
using UnityEngine;

/// <summary>
/// Điều phối flow boss truy đuổi:
/// - Nghe sự kiện stumble/jump từ player.
/// - PlayIntro() khi game bắt đầu: Boss xuất hiện giới thiệu rồi tự lùi.
/// - StartChase() khi player vấp: Boss tấn công từ phía sau.
/// - CatchPlayer() chỉ gây Victory animation, không gây chết.
/// </summary>
public class BossChaseManager : MonoBehaviour
{
    public static BossChaseManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private BossController _bossController;

    [Header("Chase Settings")]
    [Tooltip("Thời gian Boss rượt trước khi tự rút lui.")]
    [SerializeField] private float _safeEscapeDuration = 5f;

    [Header("Intro Settings")]
    [Tooltip("Thời gian chờ sau khi game bắt đầu trước khi Boss xuất hiện màn Intro.")]
    [SerializeField] private float _introDelay = 0f;
    [Tooltip("Cho phép Boss chạy màn Intro đầu game.")]
    [SerializeField] private bool _playIntroOnStart = true;

    public bool IsChasing { get; private set; }

    private Coroutine _escapeCoroutine;
    private Coroutine _introCoroutine;

    // ─────────────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        ServiceLocator.Register<BossChaseManager>(this);
        ResolveBossController();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            ServiceLocator.Unregister<BossChaseManager>();
            Instance = null;
        }
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerStumbleEvent>(HandlePlayerStumble);
        EventBus.Subscribe<PlayerJumpEvent>(HandlePlayerJump);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerStumbleEvent>(HandlePlayerStumble);
        EventBus.Unsubscribe<PlayerJumpEvent>(HandlePlayerJump);
    }

    // ─────────────────────────────────────────────────────────────
    // Event Handlers
    // ─────────────────────────────────────────────────────────────

    private void HandlePlayerStumble(PlayerStumbleEvent e)
    {
        BossController boss = ResolveBossController();
        bool bossIsVisible = boss != null && (boss.State == BossState.Chasing || boss.State == BossState.Intro);

        if (IsChasing || bossIsVisible)
        {
            CatchPlayer();
        }
        else
        {
            StartChase();
        }
    }

    private void HandlePlayerJump(PlayerJumpEvent e)
    {
        BossController boss = ResolveBossController();
        if (boss != null)
            boss.PlayJump();
    }

    // ─────────────────────────────────────────────────────────────
    // Intro
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi khi game bắt đầu để thực hiện màn giới thiệu Boss.
    /// Boss xuất hiện gần player → giữ vài giây → tự lùi ra.
    /// </summary>
    public void PlayIntroSequence()
    {
        if (!_playIntroOnStart) return;
        if (_introCoroutine != null) StopCoroutine(_introCoroutine);
        _introCoroutine = StartCoroutine(IntroRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        yield return new WaitForSeconds(_introDelay);

        BossController boss = ResolveBossController();
        if (boss == null) yield break;

        boss.PlayIntro();
        // Boss tự lùi sau _introHoldDuration giây (xử lý bên trong BossController)
    }

    // ─────────────────────────────────────────────────────────────
    // Chase
    // ─────────────────────────────────────────────────────────────

    public void StartChase()
    {
        if (IsChasing) return;

        BossController boss = ResolveBossController();
        IsChasing = true;

        if (_escapeCoroutine != null)
            StopCoroutine(_escapeCoroutine);

        _escapeCoroutine = StartCoroutine(EscapeCountdown());

        if (boss != null)
            boss.Appear();
    }

    private IEnumerator EscapeCountdown()
    {
        float timer = _safeEscapeDuration;
        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            yield return null;
        }
        EndChase();
    }

    public void EndChase()
    {
        if (!IsChasing) return;

        BossController boss = ResolveBossController();
        IsChasing = false;

        if (_escapeCoroutine != null)
        {
            StopCoroutine(_escapeCoroutine);
            _escapeCoroutine = null;
        }

        if (boss != null)
            boss.Disappear();
    }

    /// <summary>
    /// Boss không gây chết. Hàm này chỉ phát animation Victory
    /// nhưng KHÔNG gọi PlayerController.Die().
    /// </summary>
    public void CatchPlayer()
    {
        BossController boss = ResolveBossController();
        IsChasing = false;

        if (_escapeCoroutine != null)
        {
            StopCoroutine(_escapeCoroutine);
            _escapeCoroutine = null;
        }
        // Thực sự gây chết khi bị bắt lần 2
        if (PlayerController.Instance != null)
            PlayerController.Instance.Die();
    }

    public void ResetChase()
    {
        BossController boss = ResolveBossController();

        if (_escapeCoroutine != null)
        {
            StopCoroutine(_escapeCoroutine);
            _escapeCoroutine = null;
        }

        if (_introCoroutine != null)
        {
            StopCoroutine(_introCoroutine);
            _introCoroutine = null;
        }

        IsChasing = false;

        if (boss != null)
            boss.ForceHide();
    }

    // ─────────────────────────────────────────────────────────────
    // Private
    // ─────────────────────────────────────────────────────────────

    private BossController ResolveBossController()
    {
        if (_bossController != null) return _bossController;

        if (BossController.Instance != null)
        {
            _bossController = BossController.Instance;
            return _bossController;
        }

        BossController[] bosses = Resources.FindObjectsOfTypeAll<BossController>();
        foreach (BossController boss in bosses)
        {
            if (boss == null) continue;
            if (!boss.gameObject.scene.IsValid()) continue;
            _bossController = boss;
            return _bossController;
        }

        return null;
    }
}