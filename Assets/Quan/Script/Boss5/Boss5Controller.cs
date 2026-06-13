using UnityEngine;

public class Boss5Controller : MonoBehaviour
{
    [Header("Health")]
    [Tooltip("Máu tối đa.")]
    public int maxHealth = 1000;

    [Tooltip("Máu hiện tại.")]
    public int currentHealth;

    [Tooltip("Boss chết thì ẩn object.")]
    public bool hideWhenDefeated = false;

    [Header("References")]
    [Tooltip("Rigidbody2D của Boss5.")]
    public Rigidbody2D rb;

    [Tooltip("Animator của Boss5.")]
    public Animator animator;

    [Tooltip("SpriteRenderer của Boss5.")]
    public SpriteRenderer spriteRenderer;

    [Tooltip("Target chính là Wukong.")]
    public Transform wukongTarget;

    [Tooltip("Điểm sinh projectile.")]
    public Transform projectileSpawnPoint;

    [Tooltip("Prefab projectile của Boss5.")]
    public GameObject projectilePrefab;

    [Header("Tags")]
    [Tooltip("Tag của Wukong.")]
    public string playerTag = "Player";

    [Header("Move")]
    [Tooltip("Boss tự hoạt động khi sinh ra.")]
    public bool activeOnStart = true;

    [Tooltip("Tốc độ di chuyển.")]
    public float moveSpeed = 2.5f;

    [Tooltip("Khoảng cách dừng lại gần Wukong.")]
    public float stopDistanceToWukong = 2f;

    [Tooltip("Boss chỉ di chuyển theo trục X.")]
    public bool moveOnlyX = true;

    [Header("Facing")]
    [Tooltip("Sprite gốc của Boss5 đang quay sang phải.")]
    public bool spriteFacesRightByDefault = true;

    [Tooltip("Lật hướng bằng SpriteRenderer.flipX.")]
    public bool useSpriteRendererFlip = true;

    [Tooltip("Lật hướng bằng localScale X.")]
    public bool useTransformScaleFlip = false;

    [Header("Animator")]
    [Tooltip("Tên parameter tốc độ trong Animator.")]
    public string speedParameterName = "Speed";

    [Tooltip("Tên trigger attack trong Animator.")]
    public string attackTriggerName = "Attack";

    [Tooltip("Tên state idle trong Animator.")]
    public string idleStateName = "Boss5_idle";

    [Header("Attack")]
    [Tooltip("Boss có thể vừa di chuyển vừa bắn.")]
    public bool canAttackWhileMoving = true;

    [Tooltip("Khoảng cách tối đa để bắn projectile.")]
    public float attackRange = 10f;

    [Tooltip("Thời gian hồi chiêu bắn.")]
    public float attackCooldown = 2f;

    [Tooltip("Sát thương projectile.")]
    public int projectileDamage = 100;

    [Tooltip("Tốc độ bay projectile.")]
    public float projectileSpeed = 7f;

    [Tooltip("Thời gian tự hủy projectile.")]
    public float projectileLifeTime = 3f;

    [Header("Projectile Spawn")]
    [Tooltip("Tự cập nhật vị trí spawn projectile theo hướng nhìn.")]
    public bool autoUpdateProjectileSpawnPoint = true;

    [Tooltip("Khoảng cách spawn projectile tính từ tâm sprite Boss5.")]
    public Vector2 projectileSpawnOffset = new Vector2(1.2f, 0.5f);

    [Header("Stop Combat")]
    [Tooltip("Boss dừng hoạt động khi Wukong chết.")]
    public bool stopBossWhenWukongDead = true;

    [Tooltip("Boss dừng hoạt động khi đoàn thỉnh kinh chết.")]
    public bool stopBossWhenPartyDead = true;

    [Tooltip("Boss đã dừng combat.")]
    public bool combatStoppedByDeath = false;

    [Header("Debug")]
    [Tooltip("Bật log debug.")]
    public bool enableDebugLog = false;

    bool isActive;
    bool isDefeated;
    bool isFacingRight = true;
    bool hasForcedIdleAfterCombatStop;

    float attackTimer;

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

        currentHealth = maxHealth;
        isFacingRight = spriteFacesRightByDefault;

        FindWukongIfNeeded();
    }

    void Start()
    {
        isActive = activeOnStart;
        ForceIdleState(true);
    }

    void Update()
    {
        if (isDefeated) return;

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
            return;
        }

        RunBossAI();
    }

    void UpdateAttackTimer()
    {
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }
    }

    void RunBossAI()
    {
        if (wukongTarget == null)
        {
            StopMove();
            ForceIdleState(false);
            return;
        }

        FaceTarget(wukongTarget);

        float distanceToWukong = Vector2.Distance(transform.position, wukongTarget.position);

        if (distanceToWukong > stopDistanceToWukong)
        {
            MoveToTarget(wukongTarget);
        }
        else
        {
            StopMove();
        }

        if (distanceToWukong <= attackRange && CanAttack())
        {
            StartAttack();
        }
    }

    bool CanAttack()
    {
        if (combatStoppedByDeath) return false;
        if (attackTimer > 0f) return false;
        if (projectilePrefab == null) return false;
        if (projectileSpawnPoint == null) return false;

        return true;
    }

    void StartAttack()
    {
        attackTimer = attackCooldown;

        if (animator != null)
        {
            animator.ResetTrigger(attackTriggerName);
            animator.SetTrigger(attackTriggerName);
        }

        FireProjectile();

        if (enableDebugLog)
        {
            Debug.Log("Boss5 bắn projectile.");
        }
    }

    public void FireProjectile()
    {
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

        Boss5Projectile projectile = projectileObject.GetComponent<Boss5Projectile>();

        if (projectile != null)
        {
            projectile.Init(shootDirection, projectileDamage, projectileSpeed, projectileLifeTime, transform);
        }
    }

    void MoveToTarget(Transform target)
    {
        if (target == null)
        {
            StopMove();
            ForceIdleState(false);
            return;
        }

        Vector2 direction = target.position - transform.position;

        if (moveOnlyX)
        {
            direction.y = 0f;
        }

        float moveDirectionX = Mathf.Sign(direction.x);

        FaceDirection(moveDirectionX);

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(moveDirectionX * moveSpeed, rb.linearVelocity.y);
        }

        if (animator != null)
        {
            animator.SetFloat(speedParameterName, Mathf.Abs(moveSpeed));
        }
    }

    void StopMove()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        if (animator != null)
        {
            animator.SetFloat(speedParameterName, 0f);
        }
    }

    void FaceTarget(Transform target)
    {
        if (target == null) return;

        float directionX = target.position.x - transform.position.x;

        FaceDirection(directionX);
    }

    void FaceDirection(float directionX)
    {
        if (Mathf.Abs(directionX) < 0.01f) return;

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

    Vector2 GetBossFacingDirection()
    {
        return isFacingRight ? Vector2.right : Vector2.left;
    }

    void UpdateProjectileSpawnPointPosition()
    {
        if (!autoUpdateProjectileSpawnPoint) return;
        if (projectileSpawnPoint == null) return;

        Vector2 facingDirection = GetBossFacingDirection();

        float xOffset = Mathf.Abs(projectileSpawnOffset.x) * facingDirection.x;
        float yOffset = projectileSpawnOffset.y;

        Vector3 basePosition = transform.position;

        if (spriteRenderer != null)
        {
            basePosition = spriteRenderer.bounds.center;
        }

        Vector3 spawnPosition = basePosition + new Vector3(xOffset, yOffset, 0f);

        projectileSpawnPoint.position = spawnPosition;
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

    public void StopBossCombat()
    {
        combatStoppedByDeath = true;
        StopBossCombatAndReturnIdle();
    }

    public void StopBossCombatAndReturnIdle()
    {
        combatStoppedByDeath = true;

        StopMove();

        if (animator != null)
        {
            animator.ResetTrigger(attackTriggerName);
            animator.SetFloat(speedParameterName, 0f);

            if (!string.IsNullOrEmpty(idleStateName))
            {
                animator.Play(idleStateName, 0, 0f);
                animator.Update(0f);
            }
        }

        hasForcedIdleAfterCombatStop = true;
    }

    void KeepIdleAfterCombatStopped()
    {
        StopMove();

        if (animator == null) return;
        if (string.IsNullOrEmpty(idleStateName)) return;

        animator.ResetTrigger(attackTriggerName);
        animator.SetFloat(speedParameterName, 0f);

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (!stateInfo.IsName(idleStateName))
        {
            animator.CrossFade(idleStateName, 0.05f, 0, 0f);
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
        if (isDefeated) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

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
        if (isDefeated) return;

        isDefeated = true;
        StopMove();

        if (hideWhenDefeated)
        {
            gameObject.SetActive(false);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistanceToWukong);

        DrawProjectileSpawnGizmo();
    }

    void DrawProjectileSpawnGizmo()
    {
        Vector3 basePosition = transform.position;

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();

        if (sr != null)
        {
            basePosition = sr.bounds.center;
        }

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
            "Boss5 Projectile Spawn\nX: " + projectileSpawnOffset.x + " | Y: " + projectileSpawnOffset.y
        );
#endif
    }
}