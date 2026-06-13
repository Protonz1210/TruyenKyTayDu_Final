using UnityEngine;
using UnityEngine.InputSystem;

public class Enemy4Controller : MonoBehaviour
{
    [Header("References")]
    public Rigidbody2D rb;
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    [Tooltip("Target chính là Wukong.")]
    public Transform wukongTarget;

    [Tooltip("Điểm tuần tra bên trái.")]
    public Transform leftPoint;

    [Tooltip("Điểm tuần tra bên phải.")]
    public Transform rightPoint;

    [Tooltip("Điểm sinh projectile.")]
    public Transform projectileSpawnPoint;

    [Tooltip("Prefab projectile của Enemy4.")]
    public GameObject projectilePrefab;

    [Header("Tags")]
    public string playerTag = "Player";
    public string partyTag = "Party";

    [Header("Patrol")]
    [Tooltip("Enemy4 chưa kích hoạt combat thì đi tuần giữa Left Point và Right Point.")]
    public bool patrolBeforeActivated = true;

    [Tooltip("Tốc độ đi tuần.")]
    public float patrolSpeed = 2f;

    [Tooltip("Khoảng cách coi như đã tới điểm tuần.")]
    public float patrolPointReachDistance = 0.15f;

    [Tooltip("Bắt đầu đi về Right Point.")]
    public bool startPatrolToRight = true;

    [Header("Activation")]
    [Tooltip("Combat đã được kích hoạt chưa.")]
    public bool combatActivated = false;

    [Tooltip("Bật phím test để kích hoạt combat.")]
    public bool enableTestActivateKey = true;

    [Tooltip("Bấm phím 9 để kích hoạt combat.")]
    public bool useDigit9ToActivate = true;

    [Header("Combat Move")]
    [Tooltip("Tốc độ di chuyển khi combat.")]
    public float moveSpeed = 2.8f;

    [Tooltip("Khoảng cách ngang tối thiểu Enemy4 giữ với Wukong.")]
    public float stopDistanceToWukong = 3f;

    [Tooltip("Enemy4 bị chặn bởi đoàn nếu đoàn đứng giữa Enemy4 và Wukong.")]
    public bool stopForBlockingParty = true;

    [Tooltip("Khoảng cách ngang tối thiểu Enemy4 giữ với đoàn khi đoàn chắn giữa.")]
    public float stopDistanceToParty = 2.5f;

    [Tooltip("Tầm phát hiện đoàn theo trục X.")]
    public float partyDetectRange = 8f;

    [Header("Attack Range")]
    [Tooltip("Tầm bắn projectile tính theo trục X.")]
    public float attackRange = 10f;

    [Header("Attack Flow")]
    [Tooltip("Thời gian Enemy4 đứng lại chuẩn bị trước khi đánh.")]
    public float attackPrepareTime = 0.4f;

    [Tooltip("Thời gian Enemy4 đứng yên sau khi đánh xong rồi mới di chuyển tiếp.")]
    public float attackRecoveryTime = 0.3f;

    [Tooltip("Thời gian hồi chiêu giữa các lần đánh.")]
    public float attackCooldown = 1.5f;

    [Tooltip("Thời gian dự phòng nếu Animation Event kết thúc attack không chạy.")]
    public float attackMaxDuration = 1.2f;

    [Tooltip("Dùng Animation Event để sinh projectile.")]
    public bool useAnimationEventToFireProjectile = true;

    [Header("Projectile")]
    public int projectileDamage = 100;
    public float projectileSpeed = 7f;
    public float projectileLifeTime = 3f;

    [Tooltip("Tự cập nhật điểm spawn projectile theo hướng nhìn.")]
    public bool autoUpdateProjectileSpawnPoint = true;

    [Tooltip("Khoảng cách spawn từ gốc Enemy4, không lấy theo SpriteRenderer để tránh nhảy theo animation.")]
    public Vector2 projectileSpawnOffset = new Vector2(1.3f, 0.4f);

    [Header("Health")]
    public int maxHealth = 500;
    public int currentHealth;

    [Header("Death")]
    public bool destroyAfterDeath = true;

    [Header("Facing")]
    [Tooltip("Bật nếu sprite gốc quay sang phải. Tắt nếu sprite gốc quay sang trái.")]
    public bool spriteFacesRightByDefault = true;

    [Tooltip("Dùng localScale X để lật hướng.")]
    public bool useTransformScaleFlip = true;

    [Header("Animator")]
    public string speedParameterName = "Speed";
    public string attackTriggerName = "Attack";
    public string dieTriggerName = "Die";
    public string idleStateName = "Enemy4_idle";
    public string dieStateName = "Enemy4_die";

    [Header("Stop Combat")]
    [Tooltip("Wukong hoặc đoàn chết thì Enemy4 dừng và về idle, không chạy die.")]
    public bool combatStoppedByDeath = false;

    [Header("Debug")]
    public bool enableDebugLog = true;

    bool isFacingRight = true;
    float originalScaleX = 1f;

    Transform currentPatrolPoint;

    bool isPreparingAttack;
    bool isAttacking;
    bool isRecoveringAfterAttack;

    float attackPrepareTimer;
    float attackRecoveryTimer;
    float attackCooldownTimer;
    float attackTimer;

    bool isDead;
    bool hasForcedIdleAfterCombatStop;

    void Awake()
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

        originalScaleX = Mathf.Abs(transform.localScale.x);
        isFacingRight = spriteFacesRightByDefault;

        currentHealth = maxHealth;

        FindWukongIfNeeded();

        currentPatrolPoint = startPatrolToRight ? rightPoint : leftPoint;

        ForceIdleState(true);
    }

    void Start()
    {
        FindWukongIfNeeded();
        ForceIdleState(true);
    }

    void Update()
    {
        if (isDead)
        {
            StopMove();
            return;
        }

        CheckTestActivateKey();

        if (combatStoppedByDeath)
        {
            StopMove();

            if (!hasForcedIdleAfterCombatStop)
            {
                StopCombatAndReturnIdle();
            }
            else
            {
                KeepIdleAfterCombatStopped();
            }

            return;
        }

        FindWukongIfNeeded();
        UpdateCooldownTimer();

        if (autoUpdateProjectileSpawnPoint)
        {
            UpdateProjectileSpawnPointPosition();
        }

        if (IsInAttackLockState())
        {
            UpdateAttackFlow();
            StopMove();
            return;
        }

        if (!combatActivated)
        {
            RunPatrol();
            return;
        }

        RunCombatAI();
    }

    void CheckTestActivateKey()
    {
        if (!enableTestActivateKey) return;
        if (!useDigit9ToActivate) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.digit9Key.wasPressedThisFrame)
        {
            ActivateCombat();
        }
    }

    public void ActivateCombat()
    {
        if (isDead) return;
        if (combatStoppedByDeath) return;

        combatActivated = true;
        FindWukongIfNeeded();

        if (enableDebugLog)
        {
            Debug.Log("Enemy4 combat đã được kích hoạt.");
        }
    }

    public void DeactivateCombat()
    {
        combatActivated = false;
        ClearAttackState();
        ForceIdleState(false);
    }

    void RunPatrol()
    {
        if (!patrolBeforeActivated)
        {
            StopMove();
            ForceIdleState(false);
            return;
        }

        if (leftPoint == null || rightPoint == null)
        {
            StopMove();
            ForceIdleState(false);
            return;
        }

        if (currentPatrolPoint == null)
        {
            currentPatrolPoint = startPatrolToRight ? rightPoint : leftPoint;
        }

        float distanceX = Mathf.Abs(currentPatrolPoint.position.x - transform.position.x);

        if (distanceX <= patrolPointReachDistance)
        {
            currentPatrolPoint = currentPatrolPoint == rightPoint ? leftPoint : rightPoint;
        }

        MoveHorizontalTo(currentPatrolPoint, patrolSpeed);
    }

    void RunCombatAI()
    {
        if (wukongTarget == null)
        {
            StopMove();
            ForceIdleState(false);
            return;
        }

        FaceTarget(wukongTarget);

        float distanceToWukongX = GetHorizontalDistance(transform, wukongTarget);

        Transform blockingParty = FindBlockingParty();
        bool hasBlockingParty = blockingParty != null;

        bool shouldStopForWukong = distanceToWukongX <= stopDistanceToWukong;
        bool shouldStopForParty = hasBlockingParty && GetHorizontalDistance(transform, blockingParty) <= stopDistanceToParty;

        bool wukongInAttackRange = distanceToWukongX <= attackRange;
        bool partyInAttackRange = hasBlockingParty && GetHorizontalDistance(transform, blockingParty) <= attackRange;

        if (shouldStopForWukong || shouldStopForParty)
        {
            StopMove();
            ForceIdleState(false);
        }
        else
        {
            MoveHorizontalTo(wukongTarget, moveSpeed);
        }

        if ((wukongInAttackRange || partyInAttackRange) && CanStartAttackFlow())
        {
            StartAttackPrepare();
        }
    }

    Transform FindBlockingParty()
    {
        if (!stopForBlockingParty) return null;
        if (wukongTarget == null) return null;

        GameObject[] partyObjects = GameObject.FindGameObjectsWithTag(partyTag);

        Transform nearestBlockingParty = null;
        float nearestDistance = Mathf.Infinity;

        for (int i = 0; i < partyObjects.Length; i++)
        {
            GameObject partyObject = partyObjects[i];

            if (partyObject == null) continue;

            Transform partyTransform = partyObject.transform;

            float distanceX = GetHorizontalDistance(transform, partyTransform);

            if (distanceX > partyDetectRange) continue;
            if (!IsPartyBetweenEnemyAndWukong(partyTransform)) continue;

            if (distanceX < nearestDistance)
            {
                nearestDistance = distanceX;
                nearestBlockingParty = partyTransform;
            }
        }

        return nearestBlockingParty;
    }

    bool IsPartyBetweenEnemyAndWukong(Transform partyTarget)
    {
        if (partyTarget == null) return false;
        if (wukongTarget == null) return false;

        float enemyX = transform.position.x;
        float wukongX = wukongTarget.position.x;
        float partyX = partyTarget.position.x;

        float minX = Mathf.Min(enemyX, wukongX);
        float maxX = Mathf.Max(enemyX, wukongX);

        return partyX > minX && partyX < maxX;
    }

    bool CanStartAttackFlow()
    {
        if (isDead) return false;
        if (combatStoppedByDeath) return false;
        if (!combatActivated) return false;
        if (attackCooldownTimer > 0f) return false;
        if (IsInAttackLockState()) return false;
        if (projectilePrefab == null) return false;
        if (projectileSpawnPoint == null) return false;

        return true;
    }

    void StartAttackPrepare()
    {
        StopMove();

        isPreparingAttack = true;
        isAttacking = false;
        isRecoveringAfterAttack = false;

        attackPrepareTimer = attackPrepareTime;

        ForceIdleState(false);

        if (enableDebugLog)
        {
            Debug.Log("Enemy4 bắt đầu đứng chuẩn bị chiêu.");
        }
    }

    void UpdateAttackFlow()
    {
        if (isPreparingAttack)
        {
            attackPrepareTimer -= Time.deltaTime;

            if (attackPrepareTimer <= 0f)
            {
                StartAttackAnimation();
            }

            return;
        }

        if (isAttacking)
        {
            attackTimer -= Time.deltaTime;

            if (attackTimer <= 0f)
            {
                Enemy4_AttackEndEvent();
            }

            return;
        }

        if (isRecoveringAfterAttack)
        {
            attackRecoveryTimer -= Time.deltaTime;

            if (attackRecoveryTimer <= 0f)
            {
                EndAttackRecovery();
            }
        }
    }

    void StartAttackAnimation()
    {
        isPreparingAttack = false;
        isAttacking = true;
        isRecoveringAfterAttack = false;

        attackTimer = attackMaxDuration;
        attackCooldownTimer = attackCooldown;

        StopMove();

        if (wukongTarget != null)
        {
            FaceTarget(wukongTarget);
        }

        if (animator != null)
        {
            animator.ResetTrigger(attackTriggerName);
            animator.ResetTrigger(dieTriggerName);
            animator.SetFloat(speedParameterName, 0f);
            animator.SetTrigger(attackTriggerName);
        }

        if (!useAnimationEventToFireProjectile)
        {
            FireProjectile();
        }

        if (enableDebugLog)
        {
            Debug.Log("Enemy4 chạy animation attack.");
        }
    }

    public void Enemy4_AttackFireEvent()
    {
        if (isDead) return;
        if (combatStoppedByDeath) return;
        if (!isAttacking) return;

        FireProjectile();

        if (enableDebugLog)
        {
            Debug.Log("Enemy4 Animation Event: sinh projectile.");
        }
    }

    public void Enemy4_AttackEndEvent()
    {
        if (isDead) return;
        if (combatStoppedByDeath) return;

        if (!isAttacking) return;

        isAttacking = false;
        isPreparingAttack = false;
        isRecoveringAfterAttack = true;

        attackRecoveryTimer = attackRecoveryTime;

        StopMove();
        ForceIdleState(false);

        if (enableDebugLog)
        {
            Debug.Log("Enemy4 kết thúc attack, chuyển sang recovery.");
        }
    }

    void EndAttackRecovery()
    {
        isRecoveringAfterAttack = false;
        attackRecoveryTimer = 0f;

        StopMove();
        ForceIdleState(false);

        if (enableDebugLog)
        {
            Debug.Log("Enemy4 hết recovery, được phép di chuyển tiếp.");
        }
    }

    bool IsInAttackLockState()
    {
        return isPreparingAttack || isAttacking || isRecoveringAfterAttack;
    }

    void ClearAttackState()
    {
        isPreparingAttack = false;
        isAttacking = false;
        isRecoveringAfterAttack = false;

        attackPrepareTimer = 0f;
        attackRecoveryTimer = 0f;
        attackTimer = 0f;
    }

    void UpdateCooldownTimer()
    {
        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }
    }

    public void FireProjectile()
    {
        if (isDead) return;
        if (combatStoppedByDeath) return;
        if (projectilePrefab == null) return;
        if (projectileSpawnPoint == null) return;

        UpdateProjectileSpawnPointPosition();

        Vector2 shootDirection = GetFacingDirection();

        GameObject projectileObject = Instantiate(
            projectilePrefab,
            projectileSpawnPoint.position,
            Quaternion.identity
        );

        Enemy4Projectile projectile = projectileObject.GetComponent<Enemy4Projectile>();

        if (projectile != null)
        {
            projectile.Init(
                shootDirection,
                projectileDamage,
                projectileSpeed,
                projectileLifeTime,
                transform
            );
        }
        else
        {
            Debug.LogWarning("Projectile prefab thiếu Enemy4Projectile.");
        }
    }

    void MoveHorizontalTo(Transform target, float speed)
    {
        if (target == null)
        {
            StopMove();
            ForceIdleState(false);
            return;
        }

        float directionX = target.position.x - transform.position.x;

        if (Mathf.Abs(directionX) < 0.03f)
        {
            StopMove();
            ForceIdleState(false);
            return;
        }

        float moveDirection = Mathf.Sign(directionX);

        FaceDirection(moveDirection);

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(moveDirection * speed, rb.linearVelocity.y);
        }
        else
        {
            transform.position += new Vector3(moveDirection, 0f, 0f) * speed * Time.deltaTime;
        }

        SetAnimatorSpeed(1f);
    }

    void StopMove()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            rb.angularVelocity = 0f;
        }

        SetAnimatorSpeed(0f);
    }

    void FaceTarget(Transform target)
    {
        if (target == null) return;

        float directionX = target.position.x - transform.position.x;
        FaceDirection(directionX);
    }

    void FaceDirection(float directionX)
    {
        if (Mathf.Abs(directionX) < 0.05f) return;

        bool shouldFaceRight = directionX > 0f;

        isFacingRight = shouldFaceRight;

        if (useTransformScaleFlip)
        {
            Vector3 scale = transform.localScale;

            if (spriteFacesRightByDefault)
            {
                scale.x = shouldFaceRight ? originalScaleX : -originalScaleX;
            }
            else
            {
                scale.x = shouldFaceRight ? -originalScaleX : originalScaleX;
            }

            transform.localScale = scale;
        }
    }

    Vector2 GetFacingDirection()
    {
        return isFacingRight ? Vector2.right : Vector2.left;
    }

    void UpdateProjectileSpawnPointPosition()
    {
        if (!autoUpdateProjectileSpawnPoint) return;
        if (projectileSpawnPoint == null) return;

        Vector2 facingDirection = GetFacingDirection();

        float xOffset = Mathf.Abs(projectileSpawnOffset.x) * facingDirection.x;
        float yOffset = projectileSpawnOffset.y;

        Vector3 basePosition = transform.position;

        Vector3 spawnPosition = basePosition + new Vector3(xOffset, yOffset, 0f);

        projectileSpawnPoint.position = spawnPosition;
    }

    float GetHorizontalDistance(Transform a, Transform b)
    {
        if (a == null) return Mathf.Infinity;
        if (b == null) return Mathf.Infinity;

        return Mathf.Abs(a.position.x - b.position.x);
    }

    void SetAnimatorSpeed(float speed)
    {
        if (animator != null)
        {
            animator.SetFloat(speedParameterName, speed);
        }
    }

    void ForceIdleState(bool restartIdle)
    {
        StopMove();

        if (animator == null) return;

        animator.ResetTrigger(attackTriggerName);
        animator.SetFloat(speedParameterName, 0f);

        if (string.IsNullOrEmpty(idleStateName)) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsName(idleStateName) && !restartIdle)
        {
            return;
        }

        animator.CrossFade(idleStateName, 0.03f, 0, 0f);
    }

    void KeepIdleAfterCombatStopped()
    {
        StopMove();

        if (animator == null) return;
        if (string.IsNullOrEmpty(idleStateName)) return;

        animator.ResetTrigger(attackTriggerName);
        animator.ResetTrigger(dieTriggerName);
        animator.SetFloat(speedParameterName, 0f);

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (!stateInfo.IsName(idleStateName))
        {
            animator.CrossFade(idleStateName, 0.05f, 0, 0f);
        }
    }

    void FindWukongIfNeeded()
    {
        if (wukongTarget != null) return;

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
        {
            wukongTarget = playerObject.transform;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        if (damage <= 0) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (enableDebugLog)
        {
            Debug.Log("Enemy4 nhận damage: -" + damage + " | Máu: " + currentHealth + "/" + maxHealth);
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

    void Die()
    {
        if (isDead) return;

        isDead = true;
        combatActivated = false;
        combatStoppedByDeath = false;

        ClearAttackState();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }

        if (animator != null)
        {
            animator.ResetTrigger(attackTriggerName);
            animator.ResetTrigger(dieTriggerName);

            animator.SetFloat(speedParameterName, 0f);

            if (!string.IsNullOrEmpty(dieStateName))
            {
                animator.Play(dieStateName, 0, 0f);
                animator.Update(0f);
            }
            else
            {
                animator.SetTrigger(dieTriggerName);
            }
        }

        if (enableDebugLog)
        {
            Debug.Log("Enemy4 chết, chuyển Die ngay.");
        }
    }

    public void DestroyEnemy4AfterDieAnimation()
    {
        if (!isDead) return;

        if (destroyAfterDeath)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void NotifyWukongDead()
    {
        StopCombatAndReturnIdle();
    }

    public void NotifyPartyDead()
    {
        StopCombatAndReturnIdle();
    }

    public void StopCombatAndReturnIdle()
    {
        if (isDead) return;

        combatStoppedByDeath = true;
        combatActivated = false;

        ClearAttackState();
        StopMove();

        if (animator != null)
        {
            animator.ResetTrigger(attackTriggerName);
            animator.ResetTrigger(dieTriggerName);
            animator.SetFloat(speedParameterName, 0f);

            if (!string.IsNullOrEmpty(idleStateName))
            {
                animator.Play(idleStateName, 0, 0f);
                animator.Update(0f);
            }
        }

        hasForcedIdleAfterCombatStop = true;

        if (enableDebugLog)
        {
            Debug.Log("Enemy4 dừng combat và về idle.");
        }
    }

    public bool IsDead()
    {
        return isDead;
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    void OnDrawGizmosSelected()
    {
        if (leftPoint != null && rightPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(leftPoint.position, rightPoint.position);
            Gizmos.DrawSphere(leftPoint.position, 0.12f);
            Gizmos.DrawSphere(rightPoint.position, 0.12f);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistanceToWukong);

        Gizmos.color = Color.green;
        DrawProjectileSpawnGizmo();
    }

    void DrawProjectileSpawnGizmo()
    {
        Vector3 basePosition = transform.position;

        Vector2 facingDirection = isFacingRight ? Vector2.right : Vector2.left;

        float xOffset = Mathf.Abs(projectileSpawnOffset.x) * facingDirection.x;
        float yOffset = projectileSpawnOffset.y;

        Vector3 spawnPosition = basePosition + new Vector3(xOffset, yOffset, 0f);

        Gizmos.DrawSphere(spawnPosition, 0.12f);
    }
    public void HideAfterDie()
    {
        DestroyEnemy4AfterDieAnimation();
    }
}