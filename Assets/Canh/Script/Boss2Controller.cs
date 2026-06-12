using System.Collections;
using UnityEngine;

public class Boss2Controller : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Mục tiêu chính là Wukong.")]
    public Transform target;

    [Tooltip("Tag của Wukong.")]
    public string playerTag = "Player";

    [Tooltip("Tự tìm Wukong theo tag nếu chưa kéo target.")]
    public bool autoFindPlayer = true;

    [Header("Activation")]
    [Tooltip("Khoảng cách để Boss2 bắt đầu hoạt động.")]
    public float activationRange = 8f;

    [Tooltip("Boss2 chỉ hoạt động khi Wukong lại gần.")]
    public bool activateOnlyWhenPlayerNear = true;

    [Header("Move")]
    [Tooltip("Tốc độ di chuyển của Boss2.")]
    public float moveSpeed = 3.5f;

    [Tooltip("Khoảng cách dừng trước Wukong.")]
    public float stopDistance = 1.2f;

    [Tooltip("Khóa di chuyển khi đang đánh.")]
    public bool lockMovementWhileAttacking = true;

    [Header("Melee Attack")]
    [Tooltip("Hitbox đánh cận chiến của Boss2.")]
    public Boss2MeleeHitbox meleeHitbox;

    [Tooltip("Tầm đánh cận chiến.")]
    public float meleeRange = 1.6f;

    [Tooltip("Thời gian hồi đánh cận chiến.")]
    public float meleeCooldown = 1.2f;

    [Tooltip("Thời gian giữ trạng thái đánh.")]
    public float meleeActionDuration = 0.9f;

    [Tooltip("Sát thương đánh cận chiến.")]
    public int meleeDamage = 120;

    [Header("Health")]
    [Tooltip("Máu tối đa của Boss2.")]
    public int maxHealth = 1200;

    [Tooltip("Máu hiện tại của Boss2.")]
    public int currentHealth;

    [Header("Death")]
    [Tooltip("Tên state chết trong Animator.")]
    public string dieStateName = "Boss2_die";

    [Tooltip("Thời gian chờ trước khi Boss2 biến mất.")]
    public float deathDestroyDelay = 1.2f;

    [Tooltip("Tự xóa Boss2 sau khi chết.")]
    public bool destroyAfterDeath = true;

    [Header("Animator")]
    [Tooltip("Tên parameter tốc độ.")]
    public string speedParameterName = "Speed";

    [Tooltip("Tên trigger đánh cận chiến.")]
    public string meleeTriggerName = "MeleeAttack";

    [Tooltip("Tên state idle.")]
    public string idleStateName = "Boss2_idle";

    [Header("Control")]
    [Tooltip("Cho phép Boss2 di chuyển.")]
    public bool canMove = true;

    [Tooltip("Cho phép Boss2 tấn công.")]
    public bool canAttack = true;

    [Header("Debug")]
    [Tooltip("Bật log debug.")]
    public bool enableDebugLog = true;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private Transform lockedMeleeTarget;

    private bool isActivated;
    private bool isAttacking;
    private bool isDead;
    private bool facingRight = true;

    private float meleeTimer;
    private Coroutine meleeCoroutine;
    private Coroutine deathCoroutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        currentHealth = maxHealth;

        if (meleeHitbox == null)
        {
            meleeHitbox = GetComponentInChildren<Boss2MeleeHitbox>();
        }

        if (meleeHitbox != null)
        {
            meleeHitbox.owner = this;
            meleeHitbox.ownerRoot = transform.root;
            meleeHitbox.damage = meleeDamage;
            meleeHitbox.DeactivateHitbox();
        }
    }

    void Start()
    {
        FindPlayerIfNeeded();

        if (!activateOnlyWhenPlayerNear)
        {
            isActivated = true;
        }
    }

    void Update()
    {
        if (isDead)
            return;

        FindPlayerIfNeeded();

        if (target == null)
        {
            SetAnimatorSpeed(0f);
            return;
        }

        UpdateActivation();
        UpdateTimers();

        if (!isActivated)
        {
            SetAnimatorSpeed(0f);
            StopMove();
            return;
        }

        HandleAttack();
    }

    void FixedUpdate()
    {
        if (isDead)
            return;

        if (!isActivated)
            return;

        if (!canMove)
            return;

        if (lockMovementWhileAttacking && isAttacking)
        {
            StopMove();
            return;
        }

        MoveToTarget();
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

        float distanceToPlayer = Vector2.Distance(transform.position, target.position);

        if (distanceToPlayer <= activationRange)
        {
            isActivated = true;

            if (enableDebugLog)
            {
                Debug.Log("Boss2 đã được kích hoạt.");
            }
        }
    }

    void UpdateTimers()
    {
        if (meleeTimer > 0f)
        {
            meleeTimer -= Time.deltaTime;
        }
    }

    void HandleAttack()
    {
        if (!canAttack)
            return;

        if (target == null)
            return;

        float distanceToTarget = Vector2.Distance(transform.position, target.position);

        if (distanceToTarget <= meleeRange)
        {
            TryMeleeAttack(target);
        }
    }

    void TryMeleeAttack(Transform attackTarget)
    {
        if (isAttacking)
            return;

        if (meleeTimer > 0f)
            return;

        lockedMeleeTarget = attackTarget;

        FaceTarget(attackTarget);

        meleeTimer = meleeCooldown;

        if (meleeCoroutine != null)
        {
            StopCoroutine(meleeCoroutine);
        }

        meleeCoroutine = StartCoroutine(MeleeAttackRoutine());
    }

    IEnumerator MeleeAttackRoutine()
    {
        isAttacking = true;
        StopMove();

        if (animator != null)
        {
            animator.ResetTrigger(meleeTriggerName);
            animator.SetTrigger(meleeTriggerName);
        }

        if (enableDebugLog)
        {
            Debug.Log("Boss2 bắt đầu đánh cận chiến.");
        }

        yield return new WaitForSeconds(meleeActionDuration);

        isAttacking = false;
        lockedMeleeTarget = null;

        if (meleeHitbox != null)
        {
            meleeHitbox.DeactivateHitbox();
        }

        if (enableDebugLog)
        {
            Debug.Log("Boss2 kết thúc đánh cận chiến.");
        }
    }

    void MoveToTarget()
    {
        if (target == null)
        {
            StopMove();
            SetAnimatorSpeed(0f);
            return;
        }

        float distance = Vector2.Distance(transform.position, target.position);

        if (distance <= stopDistance)
        {
            StopMove();
            SetAnimatorSpeed(0f);
            FaceTarget(target);
            return;
        }

        Vector2 direction = (target.position - transform.position).normalized;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            transform.position += new Vector3(direction.x, 0f, 0f) * moveSpeed * Time.fixedDeltaTime;
        }

        FaceDirection(direction.x);
        SetAnimatorSpeed(Mathf.Abs(direction.x));
    }

    void StopMove()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
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
        if (Mathf.Abs(directionX) < 0.01f)
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

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;

        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        if (enableDebugLog)
        {
            Debug.Log("Boss2 nhận damage: -" + damage + " | Máu còn: " + currentHealth + "/" + maxHealth);
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

        StopMove();

        if (meleeCoroutine != null)
        {
            StopCoroutine(meleeCoroutine);
            meleeCoroutine = null;
        }

        if (meleeHitbox != null)
        {
            meleeHitbox.DeactivateHitbox();
        }

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();

        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (animator != null)
        {
            animator.ResetTrigger(meleeTriggerName);
            animator.Play(dieStateName, 0, 0f);
        }

        if (deathCoroutine != null)
        {
            StopCoroutine(deathCoroutine);
        }

        deathCoroutine = StartCoroutine(DeathRoutine());

        if (enableDebugLog)
        {
            Debug.Log("Boss2 chết, chạy animation Die.");
        }
    }

    IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(deathDestroyDelay);

        if (destroyAfterDeath)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void OpenMeleeHitbox()
    {
        if (isDead)
            return;

        if (meleeHitbox != null)
        {
            meleeHitbox.ActivateHitbox(lockedMeleeTarget);
        }
    }

    public void CloseMeleeHitbox()
    {
        if (meleeHitbox != null)
        {
            meleeHitbox.DeactivateHitbox();
        }
    }

    public Transform GetLockedMeleeTarget()
    {
        return lockedMeleeTarget;
    }

    public Vector2 GetBossFacingDirection()
    {
        return facingRight ? Vector2.right : Vector2.left;
    }

    public bool IsDead()
    {
        return isDead;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeRange);
    }
}