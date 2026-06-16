using System.Collections;
using UnityEngine;

/// <summary>
/// Boss1 - Mãng Xà Tinh.
/// Cơ chế chính:
/// - Khi được ActivateCombat() thì Boss1 mới bắt đầu hoạt động.
/// - Boss1 target Wukong.
/// - Nếu đoàn thỉnh kinh đứng giữa Boss1 và Wukong, Boss1 ưu tiên bắn đoàn thỉnh kinh.
/// - Boss1 giữ khoảng cách với Wukong / đoàn khi đang trong Attack Window.
/// - Boss1 không vừa chạy vừa tấn công.
/// - Khi vào tầm đánh, Boss1 dừng lại, về Idle ổn định rồi mới Attack.
/// - Projectile sinh bằng Animation Event.
/// - Khi projectile trúng Wukong lần đầu, Boss mới bắt đầu đếm Attack Window.
/// - Hết Attack Window, Boss về Idle rồi mới bắt đầu tính giờ nghỉ.
/// - Trong giờ nghỉ, Boss đứng Idle, không bắn, không lùi, Wukong có thể áp sát đánh.
/// - Khi chết: chuyển Die, đợi Die chạy xong rồi biến mất.
/// </summary>
public class Boss1Controller : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Rigidbody2D của Boss1.")]
    public Rigidbody2D rb;

    [Tooltip("Animator của Boss1.")]
    public Animator animator;

    [Tooltip("SpriteRenderer của Boss1.")]
    public SpriteRenderer spriteRenderer;

    [Tooltip("Target chính là Wukong.")]
    public Transform wukongTarget;

    [Tooltip("Điểm sinh projectile.")]
    public Transform projectileSpawnPoint;

    [Tooltip("Prefab projectile của Boss1.")]
    public GameObject projectilePrefab;

    [Header("Tags")]
    [Tooltip("Tag của Wukong.")]
    public string playerTag = "Player";

    [Tooltip("Tag của đoàn thỉnh kinh.")]
    public string partyTag = "Party";

    [Header("Activation")]
    [Tooltip("Boss tự hoạt động khi bắt đầu scene. Map2 chưa có cốt truyện thì bật cái này.")]
    public bool activeOnStart = true;

    [Tooltip("Ép Boss1 tự ActivateCombat ngay khi Play. Dùng tạm cho Map2 khi chưa có StoryManager.")]
    public bool forceCombatOnStart = true;

    [Tooltip("Boss đang được kích hoạt combat.")]
    public bool isActive;

    [Header("Move")]
    [Tooltip("Tốc độ Boss1 di chuyển ngang.")]
    public float moveSpeed = 3f;

    [Tooltip("Khoảng cách ngang tối thiểu Boss1 giữ với Wukong.")]
    public float stopDistanceToWukong = 3.5f;

    [Tooltip("Boss1 cũng giữ khoảng cách với đoàn thỉnh kinh.")]
    public bool keepDistanceFromParty = true;

    [Tooltip("Khoảng cách ngang tối thiểu Boss1 giữ với đoàn thỉnh kinh.")]
    public float stopDistanceToParty = 3f;

    [Tooltip("Khoảng cách ngang tối đa để tìm đoàn thỉnh kinh.")]
    public float partyDetectRange = 10f;

    [Tooltip("Dung sai khoảng cách để Boss không bị giật tới/lùi liên tục quanh điểm dừng.")]
    public float stopDistanceTolerance = 0.15f;

    [Header("Party Blocking Target")]
    [Tooltip("Bật cơ chế Party đứng giữa Boss1 và Wukong thì Boss1 đánh Party.")]
    public bool prioritizePartyBlockingWukong = true;

    [Tooltip("Dung sai theo trục Y khi xét Party có nằm cùng đường với Boss/Wukong không.")]
    public float partyBlockMaxYDifference = 2.5f;

    [Tooltip("Nếu không có Party chắn đường, Boss1 sẽ target lại Wukong.")]
    public bool returnToWukongWhenNoPartyBlocking = true;

    [Header("Facing")]
    [Tooltip("Sprite gốc đang quay sang phải.")]
    public bool spriteFacesRightByDefault = true;

    [Tooltip("Lật bằng SpriteRenderer.flipX.")]
    public bool useSpriteRendererFlip = true;

    [Tooltip("Lật bằng localScale X.")]
    public bool useTransformScaleFlip = false;

    [Header("Animator")]
    [Tooltip("Tên parameter tốc độ trong Animator.")]
    public string speedParameterName = "Speed";

    [Tooltip("Tên trigger attack trong Animator.")]
    public string attackTriggerName = "Attack";

    [Tooltip("Tên trigger die trong Animator.")]
    public string dieTriggerName = "Die";

    [Tooltip("Tên state Idle thật trong Animator Boss1.")]
    public string idleStateName = "Boss1_idle";

    [Tooltip("Tên state Die thật trong Animator Boss1. Phải đúng y hệt tên state trong Animator.")]
    public string dieStateName = "Boss1_die";

    [Header("Combat Cycle")]
    [Tooltip("Bật cơ chế Boss1 có chu kỳ tấn công rồi nghỉ để Wukong có thời gian áp sát.")]
    public bool useCombatCycle = true;

    [Tooltip("Thời gian Boss1 được phép giữ khoảng cách và tấn công sau khi Wukong trúng chiêu lần đầu.")]
    public float attackWindowDuration = 10f;

    [Tooltip("Thời gian Boss1 nghỉ sau khi hết Attack Window. Trong thời gian này Boss đứng Idle, không bắn, không lùi.")]
    public float vulnerableCooldownDuration = 5f;

    [Tooltip("Bắt đầu combat bằng phase tấn công.")]
    public bool startWithAttackWindow = true;

    [Tooltip("Chỉ bắt đầu đếm Attack Window khi projectile của Boss1 trúng Wukong lần đầu.")]
    public bool waitFirstWukongHitToStartAttackTimer = true;

    [Tooltip("Boss1 đang trong phase được phép tấn công.")]
    public bool isInAttackWindow = true;

    [Tooltip("Attack Window đã bắt đầu đếm giờ chưa.")]
    public bool attackWindowTimerStarted;

    [Tooltip("Boss đang chuyển từ phase tấn công sang phase nghỉ. Lúc này chờ Boss về Idle rồi mới đếm giờ nghỉ.")]
    public bool isPreparingVulnerableCooldown;

    [Tooltip("Boss đang chờ attack hiện tại kết thúc rồi mới vào phase nghỉ.")]
    public bool pendingEnterVulnerableCooldown;

    [Tooltip("Thời gian còn lại của phase hiện tại.")]
    public float combatCycleTimer;

    [Tooltip("Boss phải đứng Idle ổn định bao lâu trước khi bắt đầu tính giờ nghỉ.")]
    public float idleStableTimeBeforeVulnerableCooldown = 0.2f;

    [Tooltip("Thời gian chờ tối đa để Boss về Idle trước khi bắt đầu tính giờ nghỉ.")]
    public float maxWaitIdleBeforeVulnerableCooldown = 1.5f;

    [Header("Attack")]
    [Tooltip("Tầm bắn projectile tính theo khoảng cách ngang X.")]
    public float attackRange = 9f;

    [Tooltip("Thời gian hồi chiêu giữa các lần bắn.")]
    public float attackCooldown = 2f;

    [Tooltip("Sát thương projectile gây ra.")]
    public int projectileDamage = 100;

    [Tooltip("Tốc độ projectile.")]
    public float projectileSpeed = 7f;

    [Tooltip("Thời gian tự hủy projectile.")]
    public float projectileLifeTime = 3f;

    [Tooltip("Bắn projectile bằng Animation Event.")]
    public bool useAnimationEventToFireProjectile = true;

    [Tooltip("Thời gian khóa Boss1 trong animation tấn công. Nên gần bằng độ dài animation attack.")]
    public float attackLockDuration = 0.8f;

    [Tooltip("Trước khi attack, bắt Boss1 về Idle rồi mới ra đòn.")]
    public bool waitIdleBeforeAttack = true;

    [Tooltip("Boss phải đứng Idle ổn định bao lâu mới được bắt đầu attack.")]
    public float idleStableTimeBeforeAttack = 0.15f;

    [Tooltip("Thời gian chờ tối đa để Boss về Idle trước attack. Quá thời gian vẫn attack để tránh kẹt.")]
    public float maxWaitIdleBeforeAttack = 1f;

    [Header("Projectile Spawn")]
    [Tooltip("Tự cập nhật điểm spawn projectile theo hướng nhìn.")]
    public bool autoUpdateProjectileSpawnPoint = true;

    [Tooltip("Offset điểm spawn so với tâm Boss1.")]
    public Vector2 projectileSpawnOffset = new Vector2(1.4f, 0.4f);

    [Header("Health")]
    [Tooltip("Máu tối đa của Boss1.")]
    public int maxHealth = 1000;

    [Tooltip("Máu hiện tại của Boss1.")]
    public int currentHealth = 1000;

    [Tooltip("Boss đã chết chưa.")]
    public bool isDead;

    [Header("Death")]
    [Tooltip("Tắt collider ngay khi Boss chết để không còn va chạm.")]
    public bool disableCollidersOnDeath = true;

    [Tooltip("Chờ animation Die chạy xong rồi mới tắt object.")]
    public bool waitDieAnimationBeforeDisappear = true;

    [Tooltip("Thời gian chờ tối đa để Die animation chạy xong. Tránh kẹt nếu Animator lỗi.")]
    public float maxWaitDieAnimationTime = 3f;

    [Tooltip("Sau khi Die chạy xong thì tắt object Boss.")]
    public bool disappearAfterDie = true;

    [Tooltip("Delay nhẹ sau khi Die xong rồi mới biến mất.")]
    public float disappearDelayAfterDie = 0.15f;

    [Header("Stop Combat")]
    [Tooltip("Dừng Boss1 khi Wukong chết.")]
    public bool stopBossWhenWukongDead = true;

    [Tooltip("Dừng Boss1 khi đoàn thỉnh kinh chết.")]
    public bool stopBossWhenPartyDead = true;

    [Tooltip("Boss1 đã dừng combat do death/state story.")]
    public bool combatStoppedByDeath;

    [Header("Debug")]
    public bool enableDebugLog = false;

    private bool isFacingRight = true;
    private bool hasForcedIdleAfterCombatStop;
    private bool isAttacking;
    private float attackTimer;
    private Coroutine attackRoutine;
    private Coroutine deathRoutine;
    private Coroutine vulnerableCooldownRoutine;
    private Transform currentTarget;

    private void Awake()
    {
        AutoBindReferences();

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        isFacingRight = spriteFacesRightByDefault;

        FindWukongIfNeeded();
    }

    private void Start()
    {
        ForceIdleState(true);
        SetupCombatCycle();

        if (activeOnStart || forceCombatOnStart)
        {
            ActivateCombat();
        }
        else
        {
            isActive = false;
        }
    }

    private void Update()
    {
        if (isDead)
        {
            StopMove();
            return;
        }

        if (combatStoppedByDeath)
        {
            StopMove();

            if (!hasForcedIdleAfterCombatStop)
            {
                StopBossCombatAndReturnIdle();
            }
            else
            {
                KeepIdleAfterCombatStopped();
            }

            return;
        }

        FindWukongIfNeeded();
        UpdateAttackTimer();

        if (autoUpdateProjectileSpawnPoint)
        {
            UpdateProjectileSpawnPointPosition();
        }

        if (!isActive)
        {
            StopMove();
            ForceIdleState(false);
            return;
        }

        UpdateCombatCycle();

        if (useCombatCycle && (!isInAttackWindow || isPreparingVulnerableCooldown))
        {
            StopMove();

            if (wukongTarget != null)
            {
                FaceTarget(wukongTarget);
            }

            // Không gọi ForceIdleState liên tục ở đây.
            // Nếu gọi liên tục, animation Idle dễ bị giữ ở frame đầu như ảnh tĩnh.
            return;
        }

        if (isAttacking)
        {
            StopMove();
            return;
        }

        RunBossAI();
    }

    private void AutoBindReferences()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void UpdateAttackTimer()
    {
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;

            if (attackTimer < 0f)
            {
                attackTimer = 0f;
            }
        }
    }

    private void SetupCombatCycle()
    {
        if (!useCombatCycle)
        {
            isInAttackWindow = true;
            attackWindowTimerStarted = true;
            isPreparingVulnerableCooldown = false;
            pendingEnterVulnerableCooldown = false;
            combatCycleTimer = 0f;
            return;
        }

        isInAttackWindow = startWithAttackWindow;
        isPreparingVulnerableCooldown = false;
        pendingEnterVulnerableCooldown = false;

        if (vulnerableCooldownRoutine != null)
        {
            StopCoroutine(vulnerableCooldownRoutine);
            vulnerableCooldownRoutine = null;
        }

        if (isInAttackWindow)
        {
            combatCycleTimer = attackWindowDuration;
            attackWindowTimerStarted = !waitFirstWukongHitToStartAttackTimer;
        }
        else
        {
            combatCycleTimer = vulnerableCooldownDuration;
            attackWindowTimerStarted = false;
        }
    }

    private void UpdateCombatCycle()
    {
        if (!useCombatCycle)
        {
            isInAttackWindow = true;
            attackWindowTimerStarted = true;
            return;
        }

        if (isPreparingVulnerableCooldown)
        {
            return;
        }

        if (isInAttackWindow)
        {
            if (waitFirstWukongHitToStartAttackTimer && !attackWindowTimerStarted)
            {
                // Boss vẫn được đánh, nhưng chưa tính 10 giây cho tới khi Wukong trúng chiêu đầu tiên.
                return;
            }

            combatCycleTimer -= Time.deltaTime;

            if (combatCycleTimer <= 0f)
            {
                RequestEnterVulnerableCooldown();
            }

            return;
        }

        // Đang trong phase nghỉ.
        combatCycleTimer -= Time.deltaTime;

        if (combatCycleTimer <= 0f)
        {
            EnterAttackWindow();
        }
    }

    private void EnterAttackWindow()
    {
        isInAttackWindow = true;
        isPreparingVulnerableCooldown = false;
        pendingEnterVulnerableCooldown = false;

        combatCycleTimer = attackWindowDuration;
        attackWindowTimerStarted = !waitFirstWukongHitToStartAttackTimer;

        if (enableDebugLog)
        {
            Debug.Log("Boss1: Bắt đầu Attack Window. Boss được phép giữ khoảng cách và tấn công.");
        }
    }

    private void RequestEnterVulnerableCooldown()
    {
        if (isPreparingVulnerableCooldown)
        {
            return;
        }

        if (!isInAttackWindow)
        {
            return;
        }

        if (isAttacking)
        {
            pendingEnterVulnerableCooldown = true;

            if (enableDebugLog)
            {
                Debug.Log("Boss1: Hết Attack Window nhưng đang attack. Chờ attack kết thúc rồi mới nghỉ.");
            }

            return;
        }

        StartVulnerableCooldownAfterIdle();
    }

    private void StartVulnerableCooldownAfterIdle()
    {
        isInAttackWindow = false;
        isPreparingVulnerableCooldown = true;
        pendingEnterVulnerableCooldown = false;
        attackWindowTimerStarted = false;

        StopMove();

        // Chỉ ép về Idle một lần khi bắt đầu chuyển phase nghỉ.
        ForceIdleState(false);

        if (vulnerableCooldownRoutine != null)
        {
            StopCoroutine(vulnerableCooldownRoutine);
        }

        vulnerableCooldownRoutine = StartCoroutine(StartVulnerableCooldownWhenIdleRoutine());

        if (enableDebugLog)
        {
            Debug.Log("Boss1: Chuyển về Idle trước khi bắt đầu tính giờ nghỉ.");
        }
    }

    private IEnumerator StartVulnerableCooldownWhenIdleRoutine()
    {
        float timer = 0f;
        float stableTimer = 0f;

        while (timer < maxWaitIdleBeforeVulnerableCooldown)
        {
            timer += Time.deltaTime;

            bool idleReady = true;

            if (animator != null && !string.IsNullOrEmpty(idleStateName))
            {
                if (animator.IsInTransition(0))
                {
                    idleReady = false;
                }
                else
                {
                    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

                    if (!stateInfo.IsName(idleStateName))
                    {
                        idleReady = false;
                    }
                }
            }

            if (idleReady)
            {
                stableTimer += Time.deltaTime;

                if (stableTimer >= idleStableTimeBeforeVulnerableCooldown)
                {
                    break;
                }
            }
            else
            {
                stableTimer = 0f;
            }

            yield return null;
        }

        isPreparingVulnerableCooldown = false;
        combatCycleTimer = vulnerableCooldownDuration;

        if (enableDebugLog)
        {
            Debug.Log("Boss1: Bắt đầu Vulnerable Cooldown. Wukong có thể áp sát đánh Boss.");
        }

        vulnerableCooldownRoutine = null;
    }

    public void NotifyWukongHitByProjectile()
    {
        if (!useCombatCycle)
        {
            return;
        }

        if (!isActive || isDead || combatStoppedByDeath)
        {
            return;
        }

        if (!isInAttackWindow)
        {
            return;
        }

        if (attackWindowTimerStarted)
        {
            return;
        }

        attackWindowTimerStarted = true;
        combatCycleTimer = attackWindowDuration;

        if (enableDebugLog)
        {
            Debug.Log("Boss1: Wukong trúng chiêu lần đầu. Bắt đầu đếm Attack Window " + attackWindowDuration + " giây.");
        }
    }

    private void RunBossAI()
    {
        currentTarget = SelectCurrentTarget();

        if (currentTarget == null)
        {
            StopMove();
            ForceIdleState(false);
            return;
        }

        FaceTarget(currentTarget);

        float horizontalDistanceToTarget = GetHorizontalDistance(transform, currentTarget);
        float stopDistance = GetStopDistanceForTarget(currentTarget);

        bool tooClose = horizontalDistanceToTarget < stopDistance - stopDistanceTolerance;
        bool tooFarToAttack = horizontalDistanceToTarget > attackRange;
        bool inAttackZone = horizontalDistanceToTarget <= attackRange && horizontalDistanceToTarget >= stopDistance - stopDistanceTolerance;

        if (tooClose)
        {
            MoveAwayFromTarget(currentTarget);
            return;
        }

        if (tooFarToAttack)
        {
            MoveToTarget(currentTarget);
            return;
        }

        if (inAttackZone)
        {
            StopMove();
            ForceIdleState(false);

            if (CanAttack())
            {
                StartAttack(currentTarget);

                if (enableDebugLog)
                {
                    Debug.Log("Boss1 bắt đầu attack target: " + currentTarget.name);
                }
            }
        }
    }

    private Transform SelectCurrentTarget()
    {
        Transform blockingParty = FindPartyBlockingWukong();

        if (prioritizePartyBlockingWukong && blockingParty != null)
        {
            return blockingParty;
        }

        if (returnToWukongWhenNoPartyBlocking)
        {
            return wukongTarget;
        }

        return wukongTarget;
    }

    private Transform FindPartyBlockingWukong()
    {
        if (wukongTarget == null)
        {
            return null;
        }

        GameObject[] partyObjects = GameObject.FindGameObjectsWithTag(partyTag);

        Transform bestParty = null;
        float bestDistance = Mathf.Infinity;

        float bossX = transform.position.x;
        float wukongX = wukongTarget.position.x;

        float minX = Mathf.Min(bossX, wukongX);
        float maxX = Mathf.Max(bossX, wukongX);

        for (int i = 0; i < partyObjects.Length; i++)
        {
            GameObject partyObject = partyObjects[i];

            if (partyObject == null)
            {
                continue;
            }

            Transform party = partyObject.transform;

            float partyX = party.position.x;
            float partyYDiff = Mathf.Abs(party.position.y - transform.position.y);

            if (partyYDiff > partyBlockMaxYDifference)
            {
                continue;
            }

            bool partyBetweenBossAndWukong = partyX >= minX && partyX <= maxX;

            if (!partyBetweenBossAndWukong)
            {
                continue;
            }

            float distanceToBoss = GetHorizontalDistance(transform, party);

            if (distanceToBoss > attackRange)
            {
                continue;
            }

            if (distanceToBoss > partyDetectRange)
            {
                continue;
            }

            if (distanceToBoss < bestDistance)
            {
                bestDistance = distanceToBoss;
                bestParty = party;
            }
        }

        return bestParty;
    }

    private float GetStopDistanceForTarget(Transform target)
    {
        if (target == null)
        {
            return stopDistanceToWukong;
        }

        if (target.CompareTag(partyTag) && keepDistanceFromParty)
        {
            return stopDistanceToParty;
        }

        return stopDistanceToWukong;
    }

    private bool CanAttack()
    {
        if (!isActive) return false;
        if (isDead) return false;
        if (combatStoppedByDeath) return false;
        if (isAttacking) return false;
        if (attackTimer > 0f) return false;
        if (useCombatCycle && !isInAttackWindow) return false;
        if (useCombatCycle && isPreparingVulnerableCooldown) return false;
        if (projectilePrefab == null) return false;
        if (projectileSpawnPoint == null) return false;

        return true;
    }

    private void StartAttack(Transform target)
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
        }

        attackRoutine = StartCoroutine(AttackRoutine(target));
    }

    private IEnumerator AttackRoutine(Transform target)
    {
        isAttacking = true;
        attackTimer = attackCooldown;

        StopMove();

        if (target != null)
        {
            FaceTarget(target);
        }

        ForceIdleState(false);

        if (waitIdleBeforeAttack)
        {
            yield return StartCoroutine(WaitUntilIdleBeforeAttackRoutine());
        }

        StopMove();

        if (animator != null)
        {
            if (HasAnimatorParameter(speedParameterName))
            {
                animator.SetFloat(speedParameterName, 0f);
            }

            if (HasAnimatorParameter(attackTriggerName))
            {
                animator.ResetTrigger(attackTriggerName);
            }

            animator.SetTrigger(attackTriggerName);
        }

        if (!useAnimationEventToFireProjectile)
        {
            FireProjectile();
        }

        if (attackLockDuration > 0f)
        {
            yield return new WaitForSeconds(attackLockDuration);
        }

        FinishAttackState();
    }

    public void EndRangedAttackAnimation()
    {
        if (isDead)
        {
            return;
        }

        FinishAttackState();

        if (enableDebugLog)
        {
            Debug.Log("Boss1: Animation Event EndRangedAttackAnimation. Kết thúc attack.");
        }
    }

    private void FinishAttackState()
    {
        if (!isAttacking && attackRoutine == null)
        {
            return;
        }

        StopMove();
        ForceIdleState(false);

        isAttacking = false;
        attackRoutine = null;

        if (pendingEnterVulnerableCooldown)
        {
            StartVulnerableCooldownAfterIdle();
        }

        if (enableDebugLog)
        {
            Debug.Log("Boss1 attack xong, quay về Idle.");
        }
    }

    private IEnumerator WaitUntilIdleBeforeAttackRoutine()
    {
        if (animator == null || string.IsNullOrEmpty(idleStateName))
        {
            yield break;
        }

        float timer = 0f;
        float stableTimer = 0f;

        while (timer < maxWaitIdleBeforeAttack)
        {
            timer += Time.deltaTime;

            if (!animator.IsInTransition(0))
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

                if (stateInfo.IsName(idleStateName))
                {
                    stableTimer += Time.deltaTime;

                    if (stableTimer >= idleStableTimeBeforeAttack)
                    {
                        yield break;
                    }
                }
                else
                {
                    stableTimer = 0f;
                    ForceIdleState(false);
                }
            }

            yield return null;
        }
    }

    public void Boss1_AttackFireEvent()
    {
        if (isDead) return;
        if (combatStoppedByDeath) return;

        FireProjectile();

        if (enableDebugLog)
        {
            Debug.Log("Boss1 Animation Event: sinh projectile.");
        }
    }

    public void FireProjectile()
    {
        if (isDead) return;
        if (combatStoppedByDeath) return;
        if (projectilePrefab == null) return;
        if (projectileSpawnPoint == null) return;

        UpdateProjectileSpawnPointPosition();

        Vector2 shootDirection = GetBossFacingDirection();

        GameObject projectileObject = Instantiate(
            projectilePrefab,
            projectileSpawnPoint.position,
            Quaternion.identity
        );

        Boss1Projectile projectile = projectileObject.GetComponent<Boss1Projectile>();

        if (projectile != null)
        {
            projectile.Init(
                shootDirection,
                projectileDamage,
                projectileSpeed,
                projectileLifeTime,
                transform,
                playerTag,
                partyTag
            );
        }
        else
        {
            Debug.LogWarning("Prefab projectile Boss1 thiếu script Boss1Projectile.");
        }
    }

    private void MoveToTarget(Transform target)
    {
        if (target == null)
        {
            StopMove();
            ForceIdleState(false);
            return;
        }

        float directionX = target.position.x - transform.position.x;

        if (Mathf.Abs(directionX) < 0.01f)
        {
            StopMove();
            ForceIdleState(false);
            return;
        }

        float moveDirectionX = Mathf.Sign(directionX);

        FaceDirection(moveDirectionX);

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(moveDirectionX * moveSpeed, rb.linearVelocity.y);
        }

        SetAnimatorSpeed(Mathf.Abs(moveSpeed));
    }

    private void MoveAwayFromTarget(Transform target)
    {
        if (target == null)
        {
            StopMove();
            ForceIdleState(false);
            return;
        }

        float directionAwayX = transform.position.x - target.position.x;

        if (Mathf.Abs(directionAwayX) < 0.01f)
        {
            StopMove();
            ForceIdleState(false);
            return;
        }

        float moveDirectionX = Mathf.Sign(directionAwayX);

        FaceTarget(target);

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(moveDirectionX * moveSpeed, rb.linearVelocity.y);
        }

        SetAnimatorSpeed(Mathf.Abs(moveSpeed));
    }

    private void StopMove()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        SetAnimatorSpeed(0f);
    }

    private void SetAnimatorSpeed(float speed)
    {
        if (animator == null)
        {
            return;
        }

        if (HasAnimatorParameter(speedParameterName))
        {
            animator.SetFloat(speedParameterName, speed);
        }
    }

    private void FaceTarget(Transform target)
    {
        if (target == null)
        {
            return;
        }

        float directionX = target.position.x - transform.position.x;
        FaceDirection(directionX);
    }

    private void FaceDirection(float directionX)
    {
        if (Mathf.Abs(directionX) < 0.01f)
        {
            return;
        }

        isFacingRight = directionX > 0f;

        if (spriteRenderer != null && useSpriteRendererFlip)
        {
            bool shouldFlip = spriteFacesRightByDefault ? !isFacingRight : isFacingRight;
            spriteRenderer.flipX = shouldFlip;
        }

        if (useTransformScaleFlip)
        {
            Vector3 scale = transform.localScale;
            float absX = Mathf.Abs(scale.x);

            if (spriteFacesRightByDefault)
            {
                scale.x = isFacingRight ? absX : -absX;
            }
            else
            {
                scale.x = isFacingRight ? -absX : absX;
            }

            transform.localScale = scale;
        }
    }

    private Vector2 GetBossFacingDirection()
    {
        return isFacingRight ? Vector2.right : Vector2.left;
    }

    private void UpdateProjectileSpawnPointPosition()
    {
        if (!autoUpdateProjectileSpawnPoint) return;
        if (projectileSpawnPoint == null) return;

        Vector2 facingDirection = GetBossFacingDirection();

        float xOffset = Mathf.Abs(projectileSpawnOffset.x) * facingDirection.x;
        float yOffset = projectileSpawnOffset.y;

        Vector3 spawnPosition = transform.position + new Vector3(xOffset, yOffset, 0f);

        projectileSpawnPoint.position = spawnPosition;
    }

    private float GetHorizontalDistance(Transform a, Transform b)
    {
        if (a == null) return Mathf.Infinity;
        if (b == null) return Mathf.Infinity;

        return Mathf.Abs(a.position.x - b.position.x);
    }

    private void FindWukongIfNeeded()
    {
        if (wukongTarget != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
        {
            wukongTarget = playerObject.transform;
        }
    }

    public void ActivateCombat()
    {
        if (isDead)
        {
            return;
        }

        isActive = true;
        combatStoppedByDeath = false;
        hasForcedIdleAfterCombatStop = false;

        SetupCombatCycle();

        if (enableDebugLog)
        {
            Debug.Log("Boss1: Đã kích hoạt combat.");
        }
    }

    public void StopBossCombat()
    {
        combatStoppedByDeath = true;
        StopBossCombatAndReturnIdle();
    }

    public void StopBossCombatAndReturnIdle()
    {
        combatStoppedByDeath = true;
        isAttacking = false;

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        StopMove();

        if (animator != null)
        {
            if (HasAnimatorParameter(attackTriggerName))
            {
                animator.ResetTrigger(attackTriggerName);
            }

            SetAnimatorSpeed(0f);

            if (!string.IsNullOrEmpty(idleStateName))
            {
                animator.Play(idleStateName, 0, 0f);
                animator.Update(0f);
            }
        }

        hasForcedIdleAfterCombatStop = true;
    }

    private void KeepIdleAfterCombatStopped()
    {
        StopMove();

        if (animator == null) return;
        if (string.IsNullOrEmpty(idleStateName)) return;

        if (HasAnimatorParameter(attackTriggerName))
        {
            animator.ResetTrigger(attackTriggerName);
        }

        SetAnimatorSpeed(0f);

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (!stateInfo.IsName(idleStateName))
        {
            animator.CrossFade(idleStateName, 0.05f, 0, 0f);
        }
    }

    private void ForceIdleState(bool restartIdle)
    {
        StopMove();

        if (animator == null) return;

        if (HasAnimatorParameter(attackTriggerName))
        {
            animator.ResetTrigger(attackTriggerName);
        }

        SetAnimatorSpeed(0f);

        if (string.IsNullOrEmpty(idleStateName)) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsName(idleStateName) && !restartIdle)
        {
            return;
        }

        animator.CrossFade(idleStateName, 0.03f, 0, 0f);
    }

    public void NotifyWukongDead()
    {
        if (!stopBossWhenWukongDead) return;

        combatStoppedByDeath = true;
        StopBossCombatAndReturnIdle();
    }

    public void NotifyPartyDead()
    {
        if (!stopBossWhenPartyDead) return;

        combatStoppedByDeath = true;
        StopBossCombatAndReturnIdle();
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
        {
            return;
        }

        if (damage <= 0)
        {
            return;
        }

        currentHealth -= damage;

        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        if (enableDebugLog)
        {
            Debug.Log("Boss1 nhận damage: " + damage + " | Máu: " + currentHealth + "/" + maxHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void ReceiveDamage(int damage)
    {
        TakeDamage(damage);
    }

    public void ApplyDamage(int damage)
    {
        TakeDamage(damage);
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        isActive = false;
        isAttacking = false;
        combatStoppedByDeath = true;

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        if (vulnerableCooldownRoutine != null)
        {
            StopCoroutine(vulnerableCooldownRoutine);
            vulnerableCooldownRoutine = null;
        }

        StopMove();

        if (disableCollidersOnDeath)
        {
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>();

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = false;
                }
            }
        }

        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
        }

        deathRoutine = StartCoroutine(DeathRoutine());

        if (enableDebugLog)
        {
            Debug.Log("Boss1 đã chết. Chuyển sang animation Die.");
        }
    }

    private IEnumerator DeathRoutine()
    {
        PlayDieAnimation();

        if (waitDieAnimationBeforeDisappear)
        {
            yield return StartCoroutine(WaitDieAnimationFinishedRoutine());
        }

        if (disappearDelayAfterDie > 0f)
        {
            yield return new WaitForSeconds(disappearDelayAfterDie);
        }

        if (disappearAfterDie)
        {
            gameObject.SetActive(false);
        }
    }

    private void PlayDieAnimation()
    {
        if (animator == null)
        {
            return;
        }

        if (HasAnimatorParameter(attackTriggerName))
        {
            animator.ResetTrigger(attackTriggerName);
        }

        SetAnimatorSpeed(0f);

        if (!string.IsNullOrEmpty(dieTriggerName) && HasAnimatorParameter(dieTriggerName))
        {
            animator.SetTrigger(dieTriggerName);
            return;
        }

        if (!string.IsNullOrEmpty(dieStateName))
        {
            animator.CrossFade(dieStateName, 0.05f, 0, 0f);
        }
    }

    private IEnumerator WaitDieAnimationFinishedRoutine()
    {
        if (animator == null)
        {
            yield break;
        }

        if (string.IsNullOrEmpty(dieStateName))
        {
            yield return new WaitForSeconds(maxWaitDieAnimationTime);
            yield break;
        }

        float timer = 0f;

        while (timer < maxWaitDieAnimationTime)
        {
            timer += Time.deltaTime;

            if (!animator.IsInTransition(0))
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

                if (stateInfo.IsName(dieStateName))
                {
                    if (stateInfo.normalizedTime >= 1f)
                    {
                        yield break;
                    }
                }
            }

            yield return null;
        }
    }

    public bool IsDead()
    {
        return isDead;
    }

    public float GetHealthPercent()
    {
        if (maxHealth <= 0)
        {
            return 0f;
        }

        return (float)GetCurrentHealth() / GetMaxHealth();
    }

    public int GetCurrentHealth()
    {
        return Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    public int GetMaxHealth()
    {
        return Mathf.Max(1, maxHealth);
    }

    private bool HasAnimatorParameter(string parameterName)
    {
        if (animator == null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(parameterName))
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName)
            {
                return true;
            }
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistanceToWukong);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, partyDetectRange);

        DrawProjectileSpawnGizmo();
    }

    private void DrawProjectileSpawnGizmo()
    {
        Vector3 basePosition = transform.position;

        Vector2 facingDirection = isFacingRight ? Vector2.right : Vector2.left;

        float xOffset = Mathf.Abs(projectileSpawnOffset.x) * facingDirection.x;
        float yOffset = projectileSpawnOffset.y;

        Vector3 spawnPosition = basePosition + new Vector3(xOffset, yOffset, 0f);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(spawnPosition, 0.12f);

        Gizmos.color = Color.white;
        Gizmos.DrawLine(basePosition, spawnPosition);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            spawnPosition + Vector3.up * 0.25f,
            "Boss1 Projectile Spawn\nX: " + projectileSpawnOffset.x + " | Y: " + projectileSpawnOffset.y
        );
#endif
    }
}