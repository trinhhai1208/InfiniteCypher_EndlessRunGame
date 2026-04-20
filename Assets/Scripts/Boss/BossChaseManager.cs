using System.Collections;
using UnityEngine;

/// <summary>
/// Điều phối flow boss truy đuổi: nghe sự kiện stumble/jump từ player,
/// điều khiển boss và camera.
/// </summary>
public class BossChaseManager : MonoBehaviour
{
    public static BossChaseManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private BossController _bossController;

    [Header("Chase Settings")]
    [SerializeField] private float _safeEscapeDuration = 5f;

    public bool IsChasing { get; private set; }

    private Coroutine _escapeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

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

    private void HandlePlayerStumble(PlayerStumbleEvent e)
    {
        if (IsChasing)
            CatchPlayer();
        else
            StartChase();
    }

    private void HandlePlayerJump(PlayerJumpEvent e)
    {
        BossController boss = ResolveBossController();
        if (IsChasing && boss != null)
            boss.PlayJump();
    }

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

        if (CameraFollow.Instance != null && boss != null)
            CameraFollow.Instance.SetChaseMode(boss.transform);
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

        if (CameraFollow.Instance != null)
            CameraFollow.Instance.SetPlayerOnlyMode();
    }

    public void CatchPlayer()
    {
        BossController boss = ResolveBossController();
        IsChasing = false;

        if (_escapeCoroutine != null)
        {
            StopCoroutine(_escapeCoroutine);
            _escapeCoroutine = null;
        }

        if (boss != null)
            boss.PlayVictory();

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

        IsChasing = false;

        if (boss != null)
            boss.ForceHide();

        if (CameraFollow.Instance != null)
            CameraFollow.Instance.SetPlayerOnlyMode();
    }

    private BossController ResolveBossController()
    {
        if (_bossController != null)
            return _bossController;

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
