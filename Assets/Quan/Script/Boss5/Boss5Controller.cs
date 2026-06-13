using UnityEngine;

public class Boss5Controller : MonoBehaviour
{
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

    [Tooltip("Tag của đoàn thỉnh kinh.")]
    public string partyTag = "Party";

    [Header("Move")]
    [Tooltip("Boss tự hoạt động khi sinh ra.")]
    public bool activeOnStart = true;

    [Tooltip("Tốc độ Boss5 di chuyển ngang.")]
    public float moveSpeed = 3f;

    [Tooltip("Khoảng cách ngang tối thiểu Boss5 giữ với Wukong.")]
    public float stopDistanceToWukong = 3f;

    [Tooltip("Boss5 cũng giữ khoảng cách với đoàn thỉnh kinh.")]
    public bool keepDistanceFromParty = true;

    [Tooltip("Khoảng cách ngang tối thiểu Boss5 giữ với đoàn thỉnh kinh.")]
    public float stopDistanceToParty = 2.5f;

    [Tooltip("Khoảng cách ngang tối đa để tìm đoàn thỉnh kinh gần Boss5.")]
    public float partyDetectRange = 8f;

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

    [Tooltip("Tên state idle trong Animator.")]
    public string idleStateName = "Boss5_idle";

    [Header("Attack")]
    [Tooltip("Tầm bắn projectile tính theo khoảng cách ngang X.")]
    public float attackRange = 10f;

    [Tooltip("Thời gian hồi chiêu bắn.")]
    public float attackCooldown = 2f;

    [Tooltip("Sát thương projectile.")]
    public int projectileDamage = 100;

    [Tooltip("Tốc độ bay projectile.")]
    public float projectileSpeed = 7f;

    [Tooltip("Thời gian tự hủy projectile.")]
    public float projectileLifeTime = 3f;

    [Tooltip("Bắn projectile bằng Animation Event thay vì bắn ngay khi bắt đầu attack.")]
    public bool useAnimationEventToFireProjectile = true;

    [Header("Projectile Spawn")]
    [Tooltip("Tự cập nhật điểm spawn theo hướng nhìn.")]
    public bool autoUpdateProjectileSpawnPoint = true;

    [Tooltip("Khoảng cách spawn từ tâm sprite Boss5.")]
    public Vector2 projectileSpawnOffset = new Vector2(1.4f, 0.4f);

    [Header("Stop Combat")]
    [Tooltip("Dừng Boss5 khi Wukong chết.")]
    public bool stopBossWhenWukongDead = true;

    [Tooltip("Dừng Boss5 khi đoàn thỉnh kinh chết.")]
    public bool stopBossWhenPartyDead = true;

    [Tooltip("Boss5 đã dừng combat.")]
    public bool combatStoppedByDeath = false;

    [Header("Debug")]
    [Tooltip("Bật log debug.")]
    public bool enableDebugLog = false;

    bool isActive;
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

        float horizontalDistanceToWukong = GetHorizontalDistance(transform, wukongTarget);

        bool shouldStopForWukong = horizontalDistanceToWukong <= stopDistanceToWukong;
        bool shouldStopForParty = IsPartyTooClose();

        if (shouldStopForWukong || shouldStopForParty)
        {
            StopMove();
            ForceIdleState(false);

            if (enableDebugLog)
            {
                Debug.Log(
                    "Boss5 dừng lại | Gần Wukong: " + shouldStopForWukong +
                    " | Gần Party: " + shouldStopForParty +
                    " | Distance Wukong X: " + horizontalDistanceToWukong
                );
            }
        }
        else
        {
            MoveToTarget(wukongTarget);
        }

        bool canAttackWukong = horizontalDistanceToWukong <= attackRange;
        bool canAttackParty = IsPartyInAttackRange();

        if ((canAttackWukong || canAttackParty) && CanAttack())
        {
            StartAttack();

            if (enableDebugLog)
            {
                Debug.Log(
                    "Boss5 ra chiêu | Wukong trong vùng đánh: " + canAttackWukong +
                    " | Party trong vùng đánh: " + canAttackParty
                );
            }
        }
    }
    bool IsPartyInAttackRange()
    {
        Transform nearestParty = FindNearestParty();

        if (nearestParty == null) return false;

        float horizontalDistanceToParty = GetHorizontalDistance(transform, nearestParty);

        return horizontalDistanceToParty <= attackRange;
    }
    bool IsPartyTooClose()
    {
        if (!keepDistanceFromParty) return false;

        Transform nearestParty = FindNearestParty();

        if (nearestParty == null) return false;

        float horizontalDistanceToParty = GetHorizontalDistance(transform, nearestParty);

        if (enableDebugLog)
        {
            Debug.Log("Boss5 check Party X distance: " + horizontalDistanceToParty);
        }

        return horizontalDistanceToParty <= stopDistanceToParty;
    }

    Transform FindNearestParty()
    {
        GameObject[] partyObjects = GameObject.FindGameObjectsWithTag(partyTag);

        Transform nearestParty = null;
        float nearestHorizontalDistance = Mathf.Infinity;

        for (int i = 0; i < partyObjects.Length; i++)
        {
            GameObject partyObject = partyObjects[i];

            if (partyObject == null) continue;

            float horizontalDistance = Mathf.Abs(transform.position.x - partyObject.transform.position.x);

            if (horizontalDistance > partyDetectRange) continue;

            if (horizontalDistance < nearestHorizontalDistance)
            {
                nearestHorizontalDistance = horizontalDistance;
                nearestParty = partyObject.transform;
            }
        }

        return nearestParty;
    }

    float GetHorizontalDistance(Transform a, Transform b)
    {
        if (a == null) return Mathf.Infinity;
        if (b == null) return Mathf.Infinity;

        return Mathf.Abs(a.position.x - b.position.x);
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

        // Nếu không dùng Animation Event thì mới bắn ngay.
        // Còn nếu dùng Animation Event, projectile sẽ được sinh ở đúng frame trong animation.
        if (!useAnimationEventToFireProjectile)
        {
            FireProjectile();
        }

        if (enableDebugLog)
        {
            Debug.Log("Boss5 bắt đầu animation attack.");
        }
    }
    public void Boss5_AttackFireEvent()
    {
        if (combatStoppedByDeath) return;

        FireProjectile();

        if (enableDebugLog)
        {
            Debug.Log("Boss5 Animation Event: sinh projectile.");
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
            Debug.LogWarning("Prefab projectile Boss5 thiếu script Boss5Projectile.");
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
            rb.linearVelocity = new Vector2(moveDirectionX * moveSpeed, 0f);
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
            rb.linearVelocity = Vector2.zero;
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
        if (enableDebugLog)
        {
            Debug.Log("Boss5 không nhận sát thương.");
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistanceToWukong);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, partyDetectRange);

        DrawProjectileSpawnGizmo();
    }

    void DrawProjectileSpawnGizmo()
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
        "Boss5 Projectile Spawn\nX: " + projectileSpawnOffset.x + " | Y: " + projectileSpawnOffset.y
    );
#endif
    }
}