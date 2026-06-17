using UnityEngine;

public class Map1StoryTrigger : MonoBehaviour
{
    public enum Map1TriggerType
    {
        EnemyWave,
        SupplyPoint,
        EndMap,
        LocationTitle
    }

    [Header("Manager")]
    [Tooltip("Map1StoryManager điều phối toàn bộ map.")]
    public Map1StoryManager storyManager;

    [Header("Trigger")]
    [Tooltip("Loại trigger của map.")]
    public Map1TriggerType triggerType = Map1TriggerType.EnemyWave;

    [Tooltip("Tag của Wukong.")]
    public string playerTag = "Player";

    [Tooltip("Trigger chỉ chạy một lần.")]
    public bool triggerOnlyOnce = true;

    [Header("Location Title Trigger")]
    [Tooltip("Chỉ dùng khi Trigger Type = LocationTitle. Bật lên để chỉ hiện UI địa điểm sau khi EnemyWave đã clear.")]
    public bool locationTitleOnlyAfterEnemyWaveCleared = true;

    [Header("Jump / Fall Handling")]
    [Tooltip("Nếu Wukong nhảy/rơi vào trigger thì chờ Wukong về Idle rồi mới kích hoạt.")]
    public bool waitIdleIfPlayerEnterWhileAirborne = true;

    [Tooltip("Tên state Idle thật trong Animator của Wukong.")]
    public string wukongIdleStateName = "Wukong Idle";

    [Tooltip("Tốc độ rơi/nhảy theo trục Y lớn hơn số này thì coi là đang ở trên không.")]
    public float airborneVelocityYThreshold = 0.08f;

    [Tooltip("Sau khi về Idle, chờ ổn định thêm một chút rồi mới kích hoạt.")]
    public float idleStableTime = 0.15f;

    [Tooltip("Thời gian chờ tối đa. Nếu quá thời gian này vẫn chưa Idle thì vẫn kích hoạt để tránh kẹt.")]
    public float maxWaitIdleTime = 4f;

    [Header("Ground Check")]
    [Tooltip("Bật nếu muốn kiểm tra Wukong đã chạm đất thật chưa.")]
    public bool useGroundCheck = false;

    [Tooltip("Layer mặt đất.")]
    public LayerMask groundLayer;

    [Tooltip("Khoảng raycast kiểm tra đất dưới chân Wukong.")]
    public float groundCheckDistance = 0.3f;

    [Header("Debug")]
    public bool enableDebugLog = false;

    private bool hasTriggered;
    private bool isWaitingForIdle;
    private float waitTimer;
    private float idleTimer;

    private Collider2D waitingPlayerCollider;
    private Rigidbody2D waitingPlayerRigidbody;
    private Animator waitingPlayerAnimator;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
        {
            col.isTrigger = true;
        }

        if (storyManager == null)
        {
            storyManager = FindFirstObjectByType<Map1StoryManager>();
        }
    }

    private void Update()
    {
        if (!isWaitingForIdle)
        {
            return;
        }

        WaitUntilPlayerIdleThenTrigger();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryTrigger(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryTrigger(other);
    }

    private void TryTrigger(Collider2D other)
    {
        if (hasTriggered && triggerOnlyOnce)
        {
            return;
        }

        if (isWaitingForIdle)
        {
            return;
        }

        if (other == null)
        {
            return;
        }

        if (!other.CompareTag(playerTag))
        {
            return;
        }

        Rigidbody2D playerRb = other.attachedRigidbody;
        Animator playerAnimator = other.GetComponent<Animator>();

        if (playerAnimator == null)
        {
            playerAnimator = other.GetComponentInChildren<Animator>();
        }

        bool isAirborne = IsPlayerAirborne(other, playerRb);

        if (waitIdleIfPlayerEnterWhileAirborne && isAirborne)
        {
            StartWaitingForPlayerIdle(other, playerRb, playerAnimator);
            return;
        }

        ActivateTrigger();
    }

    private void StartWaitingForPlayerIdle(Collider2D playerCollider, Rigidbody2D playerRb, Animator playerAnimator)
    {
        isWaitingForIdle = true;
        waitTimer = 0f;
        idleTimer = 0f;

        waitingPlayerCollider = playerCollider;
        waitingPlayerRigidbody = playerRb;
        waitingPlayerAnimator = playerAnimator;

        if (enableDebugLog)
        {
            Debug.Log(gameObject.name + ": Wukong đang nhảy/rơi vào trigger, chờ về Idle rồi mới kích hoạt.");
        }
    }

    private void WaitUntilPlayerIdleThenTrigger()
    {
        waitTimer += Time.deltaTime;

        if (waitingPlayerCollider == null)
        {
            ResetWaitingState();
            return;
        }

        bool isReady = IsPlayerReadyAfterJump();

        if (isReady)
        {
            idleTimer += Time.deltaTime;

            if (idleTimer >= idleStableTime)
            {
                ResetWaitingState();
                ActivateTrigger();
                return;
            }
        }
        else
        {
            idleTimer = 0f;
        }

        if (waitTimer >= maxWaitIdleTime)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning(gameObject.name + ": chờ Wukong về Idle quá lâu, tự kích hoạt trigger để tránh kẹt.");
            }

            ResetWaitingState();
            ActivateTrigger();
        }
    }

    private bool IsPlayerAirborne(Collider2D playerCollider, Rigidbody2D playerRb)
    {
        if (useGroundCheck && !IsPlayerGrounded(playerCollider))
        {
            return true;
        }

        if (playerRb != null)
        {
            float velocityY = Mathf.Abs(playerRb.linearVelocity.y);

            if (velocityY > airborneVelocityYThreshold)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsPlayerReadyAfterJump()
    {
        if (waitingPlayerCollider == null)
        {
            return false;
        }

        if (useGroundCheck && !IsPlayerGrounded(waitingPlayerCollider))
        {
            return false;
        }

        if (waitingPlayerRigidbody != null)
        {
            float velocityY = Mathf.Abs(waitingPlayerRigidbody.linearVelocity.y);

            if (velocityY > airborneVelocityYThreshold)
            {
                return false;
            }
        }

        if (waitingPlayerAnimator != null)
        {
            if (waitingPlayerAnimator.IsInTransition(0))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(wukongIdleStateName))
            {
                AnimatorStateInfo stateInfo = waitingPlayerAnimator.GetCurrentAnimatorStateInfo(0);

                if (!stateInfo.IsName(wukongIdleStateName))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool IsPlayerGrounded(Collider2D playerCollider)
    {
        if (playerCollider == null)
        {
            return false;
        }

        Bounds bounds = playerCollider.bounds;
        Vector2 rayOrigin = new Vector2(bounds.center.x, bounds.min.y + 0.03f);

        RaycastHit2D hit = Physics2D.Raycast(
            rayOrigin,
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );

        Debug.DrawRay(
            rayOrigin,
            Vector2.down * groundCheckDistance,
            hit.collider != null ? Color.green : Color.red
        );

        return hit.collider != null;
    }

    private void ActivateTrigger()
    {
        if (hasTriggered && triggerOnlyOnce)
        {
            return;
        }

        if (storyManager == null)
        {
            Debug.LogWarning(gameObject.name + " chưa gán Map1StoryManager.");
            return;
        }

        // Với LocationTitle, nếu yêu cầu sau EnemyWave thì kiểm tra phase trước.
        // Nếu chưa đúng phase thì không đánh dấu hasTriggered, để Wukong đi qua lại sau vẫn kích hoạt được.
        if (triggerType == Map1TriggerType.LocationTitle)
        {
            if (locationTitleOnlyAfterEnemyWaveCleared && !CanPlayLocationTitleNow())
            {
                if (enableDebugLog)
                {
                    Debug.Log(
                        gameObject.name
                        + ": Chưa hiện UI địa điểm vì EnemyWave chưa clear. Phase hiện tại: "
                        + storyManager.currentPhase
                    );
                }

                return;
            }
        }

        hasTriggered = true;

        if (enableDebugLog)
        {
            Debug.Log(gameObject.name + " kích hoạt trigger Map1: " + triggerType);
        }

        switch (triggerType)
        {
            case Map1TriggerType.EnemyWave:
                storyManager.StartEnemyWaveByTrigger();
                break;

            case Map1TriggerType.SupplyPoint:
                storyManager.StartSupplyPointByTrigger();
                break;

            case Map1TriggerType.EndMap:
                storyManager.StartEndMapByTrigger();
                break;

            case Map1TriggerType.LocationTitle:
                storyManager.PlayLocationTitleFromBoxTrigger();
                break;
        }
    }

    private bool CanPlayLocationTitleNow()
    {
        if (storyManager == null)
        {
            return false;
        }

        return storyManager.currentPhase == Map1StoryManager.Map1Phase.EnemyWaveCleared
            || storyManager.currentPhase == Map1StoryManager.Map1Phase.SupplyDialogue
            || storyManager.currentPhase == Map1StoryManager.Map1Phase.SupplyItemWait
            || storyManager.currentPhase == Map1StoryManager.Map1Phase.HealFullParty
            || storyManager.currentPhase == Map1StoryManager.Map1Phase.WaitWukongIdleBeforeChangeMap
            || storyManager.currentPhase == Map1StoryManager.Map1Phase.ChangeMap;
    }

    private void ResetWaitingState()
    {
        isWaitingForIdle = false;
        waitTimer = 0f;
        idleTimer = 0f;

        waitingPlayerCollider = null;
        waitingPlayerRigidbody = null;
        waitingPlayerAnimator = null;
    }
}