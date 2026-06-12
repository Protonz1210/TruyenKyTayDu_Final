using System;
using System.Collections;
using UnityEngine;

public class Boss2Controller : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Mục tiêu chính là Wukong.")]
    public Transform target;

    [Tooltip("Tag của Wukong.")]
    public string playerTag = "Player";

    [Tooltip("Tag của đoàn thỉnh kinh.")]
    public string partyTag = "Party";

    [Tooltip("Tự tìm Wukong theo tag nếu chưa kéo target.")]
    public bool autoFindPlayer = true;

    [Header("Activation")]
    [Tooltip("Khoảng cách để Boss2 bắt đầu hoạt động.")]
    public float activationRange = 8f;

    [Tooltip("Boss2 chỉ hoạt động khi Wukong lại gần.")]
    public bool activateOnlyWhenPlayerNear = true;

    [Header("Target Priority")]
    [Tooltip("Cho phép Boss2 đánh đoàn nếu đoàn gần hơn Wukong.")]
    public bool canAttackPartyIfCloser = true;

    [Tooltip("Tầm phát hiện đoàn thỉnh kinh.")]
    public float partyDetectRange = 4f;

    [Tooltip("Khoảng cách để Wukong giành lại mục tiêu.")]
    public float wukongReclaimDistance = 3f;

    [Tooltip("Cho phép Boss2 chạy theo đoàn nếu đoàn gần hơn.")]
    public bool chasePartyIfCloser = false;

    [Header("Move")]
    [Tooltip("Tốc độ di chuyển của Boss2.")]
    public float moveSpeed = 3.5f;

    [Tooltip("Khoảng cách dừng trước mục tiêu.")]
    public float stopDistance = 1.2f;

    [Tooltip("Khóa di chuyển khi đang đánh.")]
    public bool lockMovementWhileAttacking = true;

    [Header("Melee Attack")]
    [Tooltip("Hitbox đánh cận chiến của Boss2.")]
    public Boss2MeleeHitbox meleeHitbox;

    [Tooltip("Tầm đánh cận chiến theo trục ngang.")]
    public float meleeRange = 1.8f;

    [Tooltip("Độ lệch cao thấp cho phép khi đánh.")]
    public float verticalAttackTolerance = 2.5f;

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
    [Tooltip("Tự xóa Boss2 sau khi animation chết kết thúc.")]
    public bool destroyAfterDeath = true;

    [Header("Animator")]
    [Tooltip("Tên parameter tốc độ.")]
    public string speedParameterName = "Speed";

    [Tooltip("Tên trigger đánh cận chiến.")]
    public string meleeTriggerName = "MeleeAttack";

    [Tooltip("Tên trigger animation chết.")]
    public string dieTriggerName = "Die";

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

    private Transform currentCombatTarget;
    private Transform lockedMeleeTarget;

    private bool isActivated;
    private bool isAttacking;
    private bool isDead;
    private bool facingRight = true;

    private float meleeTimer;
    private Coroutine meleeCoroutine;

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

        UpdateCurrentCombatTarget();
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
            SetAnimatorSpeed(0f);
            return;
        }

        if (currentCombatTarget != null && IsTargetInMeleeRange(currentCombatTarget))
        {
            StopMove();
            SetAnimatorSpeed(0f);
            FaceTarget(currentCombatTarget);
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

        float distanceToPlayer = Mathf.Abs(target.position.x - transform.position.x);

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

    void HandleAttack()
    {
        if (!canAttack)
            return;

        if (currentCombatTarget == null)
            return;

        if (IsTargetInMeleeRange(currentCombatTarget))
        {
            TryMeleeAttack(currentCombatTarget);
        }
    }

    bool IsTargetInMeleeRange(Transform checkTarget)
    {
        if (checkTarget == null)
            return false;

        float horizontalDistance = Mathf.Abs(checkTarget.position.x - transform.position.x);
        float verticalDistance = Mathf.Abs(checkTarget.position.y - transform.position.y);

        return horizontalDistance <= meleeRange && verticalDistance <= verticalAttackTolerance;
    }

    void TryMeleeAttack(Transform attackTarget)
    {
        if (isAttacking)
            return;

        if (meleeTimer > 0f)
            return;

        lockedMeleeTarget = attackTarget;

        FaceTarget(attackTarget);
        StopMove();
        SetAnimatorSpeed(0f);

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
        SetAnimatorSpeed(0f);

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
        Transform moveTarget = GetMoveTarget();

        if (moveTarget == null)
        {
            StopMove();
            SetAnimatorSpeed(0f);
            return;
        }

        float horizontalDistance = Mathf.Abs(moveTarget.position.x - transform.position.x);

        if (horizontalDistance <= stopDistance)
        {
            StopMove();
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
        SetAnimatorSpeed(0f);

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
            animator.ResetTrigger(dieTriggerName);
            animator.SetTrigger(dieTriggerName);
        }

        if (enableDebugLog)
        {
            Debug.Log("Boss2 chết, Animator chuyển sang animation Die.");
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

    public void DestroyBoss2AfterDieAnimation()
    {
        if (!isDead)
            return;

        if (enableDebugLog)
        {
            Debug.Log("Boss2 hết animation chết, biến mất.");
        }

        if (destroyAfterDeath)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
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

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, partyDetectRange);
    }
}