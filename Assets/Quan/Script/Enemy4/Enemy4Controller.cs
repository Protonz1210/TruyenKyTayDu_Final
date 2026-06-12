using System.Collections;
using UnityEngine;

public class Enemy4Controller : MonoBehaviour
{
    [Header("Patrol")]
    [Tooltip("Điểm tuần tra bên trái.")]
    public Transform leftPoint;

    [Tooltip("Điểm tuần tra bên phải.")]
    public Transform rightPoint;

    [Tooltip("Tốc độ tuần tra.")]
    public float patrolSpeed = 2f;

    [Tooltip("Bắt đầu di chuyển sang phải.")]
    public bool startMovingRight = true;

    [Header("Detection By Tag")]
    [Tooltip("Tag của Wukong.")]
    public string playerTag = "Player";

    [Tooltip("Tag của đoàn thỉnh kinh.")]
    public string partyTag = "Party";

    [Tooltip("Tầm phát hiện mục tiêu.")]
    public float detectRange = 8f;

    [Tooltip("Tầm tấn công.")]
    public float attackRange = 6f;

    [Header("Attack")]
    [Tooltip("Prefab đạn tấn công.")]
    public GameObject projectilePrefab;

    [Tooltip("Điểm bắn đạn.")]
    public Transform firePoint;

    [Tooltip("Thời gian hồi chiêu tấn công.")]
    public float attackCooldown = 3f;

    [Tooltip("Thời gian giữ trạng thái tấn công.")]
    public float attackActionDuration = 1.2f;

    [Tooltip("Thời điểm bắn nếu không dùng event.")]
    public float attackFireDelay = 0.35f;

    [Tooltip("Sát thương tấn công.")]
    public int attackDamage = 100;

    [Tooltip("Dùng Animation Event để bắn.")]
    public bool useAnimationEventForFire = false;

    [Header("Fire Point Auto Position")]
    [Tooltip("Tự cập nhật Fire Point theo hướng nhìn.")]
    public bool autoUpdateFirePoint = true;

    [Tooltip("Vị trí Fire Point khi nhìn sang phải.")]
    public Vector2 firePointLocalOffset = new Vector2(0.8f, 0.3f);

    [Header("Health")]
    [Tooltip("Máu tối đa.")]
    public int maxHealth = 500;

    [Tooltip("Máu hiện tại.")]
    public int currentHealth;

    [Header("Death")]
    [Tooltip("Tên animation chết.")]
    public string dieStateName = "Enemy4_die";

    [Header("Control")]
    [Tooltip("Cho phép di chuyển.")]
    public bool canMove = true;

    [Tooltip("Cho phép tấn công.")]
    public bool canAttack = true;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private CombatScreenTrigger combatScreenTrigger;

    private Transform currentPatrolTarget;
    private Transform currentAttackTarget;

    private bool isAttacking;
    private bool isDead;
    private bool movingRight;

    private float attackTimer;
    private Coroutine attackCoroutine;


void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        combatScreenTrigger = FindFirstObjectByType<CombatScreenTrigger>();

        currentHealth = maxHealth;

        movingRight = startMovingRight;
        currentPatrolTarget = movingRight ? rightPoint : leftPoint;

        UpdateFirePointPosition();
    }

    void Start()
    {
        if (currentPatrolTarget == null)
        {
            currentPatrolTarget = startMovingRight ? rightPoint : leftPoint;
        }

        UpdateFirePointPosition();
    }

    void Update()
    {
        if (isDead)
            return;

        UpdateAttackTimer();

        currentAttackTarget = FindNearestTargetByTag();

        if (currentAttackTarget != null && canAttack)
        {
            float distanceToTarget = Vector2.Distance(
                transform.position,
                currentAttackTarget.position
            );

            if (distanceToTarget <= attackRange)
            {
                FaceTarget(currentAttackTarget);
                TryAttack();
                StopMove();
                UpdateAnimation(0f);
                return;
            }
        }

        if (canMove && !isAttacking)
        {
            Patrol();
        }
        else
        {
            StopMove();
            UpdateAnimation(0f);
        }
    }

    void UpdateAttackTimer()
    {
        if (attackTimer <= 0f)
            return;

        attackTimer -= Time.deltaTime;

        if (attackTimer < 0f)
            attackTimer = 0f;
    }

    // ================= PATROL =================

    void Patrol()
    {
        if (rb == null)
            return;

        if (leftPoint == null || rightPoint == null || currentPatrolTarget == null)
        {
            StopMove();
            UpdateAnimation(0f);
            return;
        }

        float leftX = Mathf.Min(leftPoint.position.x, rightPoint.position.x);
        float rightX = Mathf.Max(leftPoint.position.x, rightPoint.position.x);

        Vector2 currentPosition = rb.position;

        float targetX = Mathf.Clamp(
            currentPatrolTarget.position.x,
            leftX,
            rightX
        );

        Vector2 targetPosition = new Vector2(
            targetX,
            currentPosition.y
        );

        Vector2 newPosition = Vector2.MoveTowards(
            currentPosition,
            targetPosition,
            patrolSpeed * Time.deltaTime
        );

        newPosition.x = Mathf.Clamp(newPosition.x, leftX, rightX);

        rb.MovePosition(newPosition);

        float moveDirection = targetPosition.x - currentPosition.x;
        FaceDirection(moveDirection);

        UpdateAnimation(1f);

        float distanceToPoint = Mathf.Abs(newPosition.x - targetX);

        if (distanceToPoint <= 0.05f)
        {
            SwitchPatrolTarget();
        }
    }

    void SwitchPatrolTarget()
    {
        if (currentPatrolTarget == rightPoint)
        {
            currentPatrolTarget = leftPoint;
            movingRight = false;
        }
        else
        {
            currentPatrolTarget = rightPoint;
            movingRight = true;
        }
    }

    // ================= DETECTION =================

    Transform FindNearestTargetByTag()
    {
        Transform nearestTarget = null;
        float nearestDistance = Mathf.Infinity;

        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
        GameObject[] partyMembers = GameObject.FindGameObjectsWithTag(partyTag);

        CheckTargets(players, ref nearestTarget, ref nearestDistance);
        CheckTargets(partyMembers, ref nearestTarget, ref nearestDistance);

        return nearestTarget;
    }

    void CheckTargets(
        GameObject[] targetObjects,
        ref Transform nearestTarget,
        ref float nearestDistance
    )
    {
        if (targetObjects == null)
            return;

        foreach (GameObject targetObject in targetObjects)
        {
            if (targetObject == null)
                continue;

            if (!targetObject.activeInHierarchy)
                continue;

            float distance = Vector2.Distance(
                transform.position,
                targetObject.transform.position
            );

            if (distance <= detectRange && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestTarget = targetObject.transform;
            }
        }
    }

    // ================= ATTACK =================

    void TryAttack()
    {
        if (isDead)
            return;

        if (isAttacking)
            return;

        if (attackTimer > 0f)
            return;

        attackCoroutine = StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;

        StopMove();

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.ResetTrigger("Die");
            animator.SetTrigger("Attack");
        }

        if (!useAnimationEventForFire)
        {
            yield return new WaitForSeconds(attackFireDelay);

            if (isDead)
                yield break;

            FireAttack();
        }

        yield return new WaitForSeconds(attackActionDuration);

        if (isDead)
            yield break;

        isAttacking = false;
        attackCoroutine = null;

        attackTimer = attackCooldown;
    }

    public void FireAttack()
    {
        if (isDead)
            return;

        if (projectilePrefab == null)
        {
            Debug.LogWarning("Enemy4 thiếu Projectile Prefab.");
            return;
        }

        if (firePoint == null)
        {
            Debug.LogWarning("Enemy4 thiếu Fire Point.");
            return;
        }

        UpdateFirePointPosition();

        GameObject projectileObject = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.identity
        );

        projectileObject.tag = "Projectile";

        EnemySoundWaveProjectile projectile =
            projectileObject.GetComponent<EnemySoundWaveProjectile>();

        if (projectile != null)
        {
            Vector2 direction = GetFacingDirection();
            projectile.Init(direction, attackDamage);
        }
        else
        {
            Debug.LogWarning("Projectile Prefab chưa có EnemySoundWaveProjectile.");
        }
    }

    // ================= FIRE POINT =================

    void UpdateFirePointPosition()
    {
        if (!autoUpdateFirePoint)
            return;

        if (firePoint == null)
            return;

        Vector3 localPosition = firePoint.localPosition;

        if (GetFacingDirection().x < 0f)
        {
            localPosition.x = -Mathf.Abs(firePointLocalOffset.x);
        }
        else
        {
            localPosition.x = Mathf.Abs(firePointLocalOffset.x);
        }

        localPosition.y = firePointLocalOffset.y;
        localPosition.z = 0f;

        firePoint.localPosition = localPosition;
    }

    // ================= FACING =================

    void FaceTarget(Transform target)
    {
        if (target == null)
            return;

        float direction = target.position.x - transform.position.x;
        FaceDirection(direction);
    }

    void FaceDirection(float direction)
    {
        if (Mathf.Abs(direction) < 0.01f)
            return;

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction < 0f;
        }
        else
        {
            Vector3 scale = transform.localScale;

            if (direction > 0f)
                scale.x = Mathf.Abs(scale.x);
            else
                scale.x = -Mathf.Abs(scale.x);

            transform.localScale = scale;
        }

        UpdateFirePointPosition();
    }

    Vector2 GetFacingDirection()
    {
        if (spriteRenderer != null)
        {
            return spriteRenderer.flipX ? Vector2.left : Vector2.right;
        }

        if (transform.localScale.x < 0f)
        {
            return Vector2.left;
        }

        return Vector2.right;
    }

    // ================= HEALTH =================

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        if (damage <= 0)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log("Enemy4 mất máu: " + damage + " | Máu: " + currentHealth + " / " + maxHealth);

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
        isAttacking = false;
        canMove = false;
        canAttack = false;
        attackTimer = 0f;

        // Hủy ngay attack coroutine nếu enemy đang đánh.
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        StopMove();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
        }

        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Die");
            animator.SetFloat("Speed", 0f);

            // Ép nhảy thẳng vào state chết, không chờ animation Attack chạy hết.
            if (!string.IsNullOrEmpty(dieStateName))
            {
                animator.Play(dieStateName, 0, 0f);
            }
            else
            {
                animator.SetTrigger("Die");
            }

            animator.Update(0f);
        }

        Debug.Log("Enemy4 chết ngay khi máu về 0.");
    }

    public void HideAfterDie()
    {
        if (combatScreenTrigger != null)
        {
            combatScreenTrigger.RemoveEnemy(gameObject);
        }

        gameObject.SetActive(false);
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

    // ================= CONTROL LOCK =================

    public void SetControlLock(bool locked)
    {
        if (isDead)
            return;

        canMove = !locked;
        canAttack = !locked;

        if (locked)
        {
            isAttacking = false;

            if (attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
                attackCoroutine = null;
            }

            StopMove();
            UpdateAnimation(0f);
        }
    }

    // ================= MOVEMENT / ANIMATION =================

    void StopMove()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    void UpdateAnimation(float speedValue)
    {
        if (animator == null)
            return;

        animator.SetFloat("Speed", speedValue);
    }

    // ================= GIZMOS =================

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (leftPoint != null && rightPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(leftPoint.position, rightPoint.position);
            Gizmos.DrawWireSphere(leftPoint.position, 0.2f);
            Gizmos.DrawWireSphere(rightPoint.position, 0.2f);
        }

        if (firePoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(firePoint.position, 0.15f);
        }
    }
}