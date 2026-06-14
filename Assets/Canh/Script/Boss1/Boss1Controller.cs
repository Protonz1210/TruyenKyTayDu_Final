using UnityEngine;

public class Boss1Controller : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Mục tiêu chính là Wukong.")]
    public Transform target;

    [Tooltip("Tag của Wukong.")]
    public string playerTag = "Player";

    [Tooltip("Tag của đoàn thỉnh kinh.")]
    public string partyTag = "Party";

    [Tooltip("Tự tìm Wukong nếu chưa kéo target.")]
    public bool autoFindPlayer = true;

    [Header("Activation")]
    [Tooltip("Khoảng cách để Boss bắt đầu hoạt động.")]
    public float activationRange = 14f;

    [Tooltip("Chỉ hoạt động khi Wukong lại gần.")]
    public bool activateOnlyWhenPlayerNear = true;

    [Header("Target Priority Like Boss2")]
    [Tooltip("Target giống Boss2: cho phép bắn đoàn nếu đoàn gần hơn.")]
    public bool canAttackPartyIfCloser = true;

    [Tooltip("Tầm phát hiện đoàn thỉnh kinh.")]
    public float partyDetectRange = 4f;

    [Tooltip("Khoảng cách để Wukong giành lại target.")]
    public float wukongReclaimDistance = 3f;

    [Tooltip("Cho phép Boss chạy theo đoàn nếu đoàn gần hơn.")]
    public bool chasePartyIfCloser = false;

    [Header("Move")]
    [Tooltip("Tốc độ di chuyển của Boss.")]
    public float moveSpeed = 3.2f;

    [Tooltip("Khoảng cách Boss đứng lại để bắn chiêu xa. Ví dụ 8 ô.")]
    public float rangedAttackDistance = 8f;

    [Tooltip("Boss dừng hẳn khi đang bắn.")]
    public bool freezePositionWhileAttacking = true;

    [Header("Ranged Attack")]
    [Tooltip("Prefab luồng khí Boss bắn ra.")]
    public GameObject projectilePrefab;

    [Tooltip("Vị trí bắn luồng khí.")]
    public Transform projectileFirePoint;

    [Tooltip("Thời gian hồi bắn.")]
    public float rangedAttackCooldown = 2.5f;

    [Tooltip("Thời gian tối đa của animation bắn.")]
    public float rangedAttackMaxDuration = 1.3f;

    [Tooltip("Sát thương luồng khí.")]
    public int projectileDamage = 80;

    [Tooltip("Tốc độ bay của luồng khí.")]
    public float projectileSpeed = 7f;

    [Tooltip("Thời gian tự hủy luồng khí.")]
    public float projectileLifeTime = 3f;

    [Header("Health")]
    [Tooltip("Máu tối đa.")]
    public int maxHealth = 1500;

    [Tooltip("Máu hiện tại.")]
    public int currentHealth;

    [Header("Death")]
    [Tooltip("Tự xóa Boss sau animation chết.")]
    public bool destroyAfterDeath = true;

    [Header("Animator")]
    [Tooltip("Tên parameter tốc độ.")]
    public string speedParameterName = "Speed";

    [Tooltip("Tên trigger animation bắn xa. Vẫn dùng MeleeAttack để khớp Animator cũ.")]
    public string rangedAttackTriggerName = "MeleeAttack";

    [Tooltip("Tên trigger chết.")]
    public string dieTriggerName = "Die";

    [Tooltip("Tên state idle trong Animator.")]
    public string idleStateName = "Boss1Idle";

    [Tooltip("Ép Boss về Idle khi bắt đầu.")]
    public bool forceIdleOnStart = true;

    [Header("Control")]
    [Tooltip("Cho phép Boss di chuyển.")]
    public bool canMove = true;

    [Tooltip("Cho phép Boss bắn.")]
    public bool canAttack = true;

    [Header("Debug")]
    [Tooltip("Bật log debug.")]
    public bool enableDebugLog = true;

    private Rigidbody2D rb;
    private Animator animator;

    private Transform currentCombatTarget;

    private bool isActivated;
    private bool isAttacking;
    private bool isDead;
    private bool facingRight = true;

    private float rangedAttackCooldownTimer;
    private float attackTimer;

    private Vector2 attackLockedPosition;
    private bool hasAttackLockedPosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        currentHealth = maxHealth;
        isDead = false;
        isAttacking = false;

        ResetAnimatorToIdle();
    }

    void Start()
    {
        FindPlayerIfNeeded();

        if (!activateOnlyWhenPlayerNear)
        {
            isActivated = true;
        }

        ResetAnimatorToIdle();
    }

    void Update()
    {
        if (isDead)
        {
            StopMoveHard();
            SetAnimatorSpeed(0f);
            return;
        }

        FindPlayerIfNeeded();

        if (target == null)
        {
            StopMoveHard();
            SetAnimatorSpeed(0f);
            return;
        }

        UpdateActivation();
        UpdateCooldownTimer();
        UpdateAttackTimer();

        if (!isActivated)
        {
            StopMoveHard();
            SetAnimatorSpeed(0f);
            return;
        }

        UpdateCurrentCombatTarget();

        if (isAttacking)
        {
            StopMoveHard();
            SetAnimatorSpeed(0f);

            if (freezePositionWhileAttacking)
            {
                LockBossPosition();
            }

            return;
        }

        if (currentCombatTarget == null)
        {
            StopMoveHard();
            SetAnimatorSpeed(0f);
            return;
        }

        float distanceToTarget = Mathf.Abs(currentCombatTarget.position.x - transform.position.x);

        if (distanceToTarget <= rangedAttackDistance)
        {
            StopMoveHard();
            SetAnimatorSpeed(0f);
            FaceTarget(currentCombatTarget);
            TryStartRangedAttack();
            return;
        }
    }

    void FixedUpdate()
    {
        if (isDead)
        {
            StopMoveHard();
            return;
        }

        if (!isActivated)
            return;

        if (!canMove)
        {
            StopMoveHard();
            SetAnimatorSpeed(0f);
            return;
        }

        if (isAttacking)
        {
            StopMoveHard();
            SetAnimatorSpeed(0f);

            if (freezePositionWhileAttacking)
            {
                LockBossPosition();
            }

            return;
        }

        MoveToTarget();
    }

    void ResetAnimatorToIdle()
    {
        if (animator == null)
            return;

        animator.ResetTrigger(rangedAttackTriggerName);
        animator.ResetTrigger(dieTriggerName);
        animator.SetFloat(speedParameterName, 0f);

        if (forceIdleOnStart && !string.IsNullOrEmpty(idleStateName))
        {
            animator.Play(idleStateName, 0, 0f);
        }
    }

    void FindPlayerIfNeeded()
    {
        if (!autoFindPlayer)
            return;

        if (target != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
        {
            target = playerObject.transform;
        }
    }

    void UpdateActivation()
    {
        if (isActivated)
            return;
        if (target == null)
            return;

        float distanceToPlayerX = Mathf.Abs(target.position.x - transform.position.x);

        if (distanceToPlayerX <= activationRange)
        {
            isActivated = true;

            if (enableDebugLog)
            {
                Debug.Log("Mãng Xà Tinh đã được kích hoạt.");
            }
        }
    }

    void UpdateCooldownTimer()
    {
        if (rangedAttackCooldownTimer > 0f)
        {
            rangedAttackCooldownTimer -= Time.deltaTime;
        }
    }

    void UpdateAttackTimer()
    {
        if (!isAttacking)
            return;

        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            EndRangedAttackAnimation();
        }
    }

    void UpdateCurrentCombatTarget()
    {
        currentCombatTarget = target;

        if (!canAttackPartyIfCloser)
            return;

        Transform nearestParty = FindNearestPartyMember();
        if (nearestParty == null)
            return;

        float distanceToWukong = Mathf.Abs(target.position.x - transform.position.x);
        float distanceToParty = Mathf.Abs(nearestParty.position.x - transform.position.x);

        if (distanceToWukong <= wukongReclaimDistance)
        {
            currentCombatTarget = target;
            return;
        }

        if (distanceToParty < distanceToWukong && distanceToParty <= partyDetectRange)
        {
            currentCombatTarget = nearestParty;
        }
    }

    Transform FindNearestPartyMember()
    {
        GameObject[] partyObjects = GameObject.FindGameObjectsWithTag(partyTag);

        Transform nearestTarget = null;
        float nearestDistance = Mathf.Infinity;

        for (int i = 0; i < partyObjects.Length; i++)
        {
            if (partyObjects[i] == null)
                continue;

            float distance = Mathf.Abs(partyObjects[i].transform.position.x - transform.position.x);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestTarget = partyObjects[i].transform;
            }
        }

        return nearestTarget;
    }

    void TryStartRangedAttack()
    {
        if (!canAttack)
            return;

        if (isAttacking)
            return;

        if (rangedAttackCooldownTimer > 0f)
            return;

        isAttacking = true;
        attackTimer = rangedAttackMaxDuration;

        FaceTarget(currentCombatTarget);
        StopMoveHard();
        SetAnimatorSpeed(0f);

        attackLockedPosition = rb != null ? rb.position : (Vector2)transform.position;
        hasAttackLockedPosition = true;

        if (freezePositionWhileAttacking)
        {
            LockBossPosition();
        }

        if (animator != null)
        {
            animator.ResetTrigger(dieTriggerName);
            animator.ResetTrigger(rangedAttackTriggerName);
            animator.SetTrigger(rangedAttackTriggerName);
        }

        if (enableDebugLog)
        {
            Debug.Log("Mãng Xà Tinh bắt đầu bắn xa.");
        }
    }

    void MoveToTarget()
    {
        Transform moveTarget = GetMoveTarget();

        if (moveTarget == null)
        {
            StopMoveHard();
            SetAnimatorSpeed(0f);
            return;
        }

        float distanceX = Mathf.Abs(moveTarget.position.x - transform.position.x);

        if (distanceX <= rangedAttackDistance)
        {
            StopMoveHard();
            SetAnimatorSpeed(0f);
            FaceTarget(moveTarget);
            return;
        }

        float directionX = moveTarget.position.x - transform.position.x;
        float moveDirection = Mathf.Sign(directionX);

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            transform.position += new Vector3(moveDirection, 0f, 0f) * moveSpeed * Time.fixedDeltaTime;
        }

        FaceDirection(moveDirection);
        SetAnimatorSpeed(1f);
    }

    Transform GetMoveTarget()
    {
        if (currentCombatTarget == null)
            return target;

        if (currentCombatTarget.CompareTag(partyTag) && !chasePartyIfCloser)
        {
            return target;
        }

        return currentCombatTarget;
    }

    void StopMoveHard()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    void LockBossPosition()
    {
        if (!hasAttackLockedPosition)
            return;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = attackLockedPosition;
        }
        else
        {
            transform.position = new Vector3(
                attackLockedPosition.x,
                attackLockedPosition.y,
                transform.position.z
            );
        }
    }

    void FaceTarget(Transform faceTarget)
    {
        if (faceTarget == null)
            return;

        float directionX = faceTarget.position.x - transform.position.x;
        FaceDirection(directionX);
    }

    void FaceDirection(float directionX)
    {
        if (Mathf.Abs(directionX) < 0.05f)
            return;

        bool shouldFaceRight = directionX > 0f;

        if (facingRight == shouldFaceRight)
            return;

        facingRight = shouldFaceRight;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (facingRight ? 1f : -1f);
        transform.localScale = scale;
    }

    void SetAnimatorSpeed(float speed)
    {
        if (animator != null)
        {
            animator.SetFloat(speedParameterName, speed);
        }
    }

    public void FireProjectile()
    {
        if (isDead)
            return;

        if (!isAttacking)
            return;

        if (projectilePrefab == null)
        {
            Debug.LogWarning("Mãng Xà Tinh chưa có Projectile Prefab.");
            return;
        }

        Transform firePoint = projectileFirePoint != null ? projectileFirePoint : transform;

        GameObject projectileObject = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.identity
        );

        Boss1Projectile projectile = projectileObject.GetComponent<Boss1Projectile>();

        if (projectile != null)
        {
            Vector2 direction = facingRight ? Vector2.right : Vector2.left;
            projectile.Init(direction, projectileSpeed, projectileDamage, projectileLifeTime, transform.root);
        }

        if (enableDebugLog)
        {
            Debug.Log("Mãng Xà Tinh bắn luồng khí.");
        }
    }

    public void EndRangedAttackAnimation()
    {
        if (isDead)
            return;

        isAttacking = false;
        hasAttackLockedPosition = false;
        attackTimer = 0f;
        rangedAttackCooldownTimer = rangedAttackCooldown;

        StopMoveHard();
        SetAnimatorSpeed(0f);

        if (enableDebugLog)
        {
            Debug.Log("Mãng Xà Tinh kết thúc animation bắn xa.");
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        if (damage <= 0)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("Mãng Xà Tinh nhận damage <= 0 nên không trừ máu.");
            }

            return;
        }

        currentHealth -= damage;

        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        if (enableDebugLog)
        {
            Debug.Log("Mãng Xà Tinh nhận damage: -" + damage + " | Máu còn: " + currentHealth + "/" + maxHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;
        canMove = false;
        canAttack = false;
        isAttacking = false;

        StopMoveHard();
        SetAnimatorSpeed(0f);

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();

        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (animator != null)
        {
            animator.ResetTrigger(rangedAttackTriggerName);
            animator.ResetTrigger(dieTriggerName);
            animator.SetTrigger(dieTriggerName);
        }

        if (enableDebugLog)
        {
            Debug.Log("Mãng Xà Tinh chết.");
        }
    }

    public void DestroyBoss1AfterDieAnimation()
    {
        if (!isDead)
            return;

        if (destroyAfterDeath)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public bool IsDead()
    {
        return isDead;
    }
}