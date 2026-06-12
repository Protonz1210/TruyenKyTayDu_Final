using System;
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
    public float stopDistance = 1.5f;

    [Tooltip("Khi đã vào tầm đánh thì đứng yên.")]
    public bool stopCompletelyWhenInMeleeRange = true;

    [Tooltip("Khóa cứng vị trí Boss2 trong lúc đánh.")]
    public bool freezePositionWhileAttacking = true;

    [Header("Melee Attack")]
    [Tooltip("Hitbox đánh cận chiến của Boss2.")]
    public Boss2MeleeHitbox meleeHitbox;

    [Tooltip("Tầm đánh cận chiến theo trục ngang.")]
    public float meleeRange = 2.2f;

    [Tooltip("Độ lệch cao thấp cho phép khi đánh.")]
    public float verticalAttackTolerance = 3f;

    [Tooltip("Thời gian hồi đánh sau khi animation đánh kết thúc.")]
    public float meleeCooldown = 1.2f;

    [Tooltip("Thời gian tối đa của animation đánh. Nếu event cuối không chạy, Boss sẽ tự thoát trạng thái đánh sau thời gian này.")]
    public float attackMaxDuration = 4.6f;

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

    private Transform currentCombatTarget;
    private Transform lockedMeleeTarget;

    private bool isActivated;
    private bool isAttacking;
    private bool isDead;
    private bool facingRight = true;

    private float meleeCooldownTimer;
    private float attackTimer;

    private Vector2 attackLockedPosition;
    private bool hasAttackLockedPosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

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

        if (currentCombatTarget != null && IsTargetInMeleeRange(currentCombatTarget))
        {
            StopMoveHard();
            SetAnimatorSpeed(0f);
            FaceTarget(currentCombatTarget);
            TryStartMeleeAttack(currentCombatTarget);
            return;
        }
    }

    void FixedUpdate()
    {
        if (isDead)
            return;

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

        if (stopCompletelyWhenInMeleeRange && currentCombatTarget != null && IsTargetInMeleeRange(currentCombatTarget))
        {
            StopMoveHard();
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

        float distanceToPlayerX = Mathf.Abs(target.position.x - transform.position.x);

        if (distanceToPlayerX <= activationRange)
        {
            isActivated = true;

            if (enableDebugLog)
            {
                Debug.Log("Boss2 đã được kích hoạt.");
            }
        }
    }

    void UpdateCooldownTimer()
    {
        if (meleeCooldownTimer > 0f)
        {
            meleeCooldownTimer -= Time.deltaTime;
        }
    }

    void UpdateAttackTimer()
    {
        if (!isAttacking)
            return;

        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("Boss2 tự kết thúc đánh bằng Attack Max Duration.");
            }

            EndMeleeAttackAnimation();
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

    bool IsTargetInMeleeRange(Transform checkTarget)
    {
        if (checkTarget == null)
            return false;

        float distanceX = Mathf.Abs(checkTarget.position.x - transform.position.x);
        float distanceY = Mathf.Abs(checkTarget.position.y - transform.position.y);

        return distanceX <= meleeRange && distanceY <= verticalAttackTolerance;
    }

    void TryStartMeleeAttack(Transform attackTarget)
    {
        if (!canAttack)
            return;

        if (isAttacking)
            return;

        if (meleeCooldownTimer > 0f)
            return;

        if (attackTarget == null)
            return;

        isAttacking = true;
        lockedMeleeTarget = attackTarget;
        attackTimer = attackMaxDuration;

        FaceTarget(attackTarget);
        StopMoveHard();
        SetAnimatorSpeed(0f);

        attackLockedPosition = rb != null ? rb.position : (Vector2)transform.position;
        hasAttackLockedPosition = true;

        if (freezePositionWhileAttacking)
        {
            LockBossPosition();
        }

        if (meleeHitbox != null)
        {
            meleeHitbox.DeactivateHitbox();
        }

        if (animator != null)
        {
            animator.ResetTrigger(meleeTriggerName);
            animator.SetTrigger(meleeTriggerName);
        }

        if (enableDebugLog)
        {
            Debug.Log("Boss2 bắt đầu animation đánh.");
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

        if (distanceX <= stopDistance)
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

    public bool IsTargetStillInMeleeRange(Transform checkTarget)
    {
        return IsTargetInMeleeRange(checkTarget);
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

        StopMoveHard();
        SetAnimatorSpeed(0f);

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
            rb.angularVelocity = 0f;
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

        if (!isAttacking)
            return;

        StopMoveHard();

        if (freezePositionWhileAttacking)
        {
            LockBossPosition();
        }

        if (meleeHitbox != null)
        {
            meleeHitbox.ActivateHitbox(lockedMeleeTarget);
            meleeHitbox.ForceHitTarget(lockedMeleeTarget);
        }

        if (enableDebugLog)
        {
            Debug.Log("Boss2 mở hitbox đánh.");
        }
    }

    public void CloseMeleeHitbox()
    {
        if (meleeHitbox != null)
        {
            meleeHitbox.DeactivateHitbox();
        }

        StopMoveHard();

        if (freezePositionWhileAttacking)
        {
            LockBossPosition();
        }

        if (enableDebugLog)
        {
            Debug.Log("Boss2 đóng hitbox đánh.");
        }
    }

    public void EndMeleeAttackAnimation()
    {
        if (isDead)
            return;

        isAttacking = false;
        lockedMeleeTarget = null;
        hasAttackLockedPosition = false;
        attackTimer = 0f;
        meleeCooldownTimer = meleeCooldown;

        StopMoveHard();
        SetAnimatorSpeed(0f);

        if (meleeHitbox != null)
        {
            meleeHitbox.DeactivateHitbox();
        }

        if (enableDebugLog)
        {
            Debug.Log("Boss2 kết thúc animation đánh, được phép đuổi và đánh tiếp.");
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