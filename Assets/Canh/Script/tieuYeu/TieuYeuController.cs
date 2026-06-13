using System;
using UnityEngine;

public class TieuYeuController : MonoBehaviour
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
    [Tooltip("Khoảng cách để Tiểu yêu bắt đầu hoạt động.")]
    public float activationRange = 12f;

    [Tooltip("Chỉ hoạt động khi Wukong lại gần.")]
    public bool activateOnlyWhenPlayerNear = true;

    [Header("Target Priority")]
    [Tooltip("Cho phép đánh đoàn nếu đoàn gần hơn.")]
    public bool canAttackPartyIfCloser = true;

    [Tooltip("Tầm phát hiện đoàn thỉnh kinh.")]
    public float partyDetectRange = 4f;

    [Tooltip("Khoảng cách để Wukong giành lại mục tiêu.")]
    public float wukongReclaimDistance = 3f;

    [Tooltip("Cho phép chạy theo đoàn nếu đoàn gần hơn.")]
    public bool chasePartyIfCloser = false;

    [Header("Move")]
    [Tooltip("Tốc độ di chuyển.")]
    public float moveSpeed = 2.8f;

    [Tooltip("Khoảng cách dừng trước mục tiêu.")]
    public float stopDistance = 1.2f;

    [Tooltip("Khi đã vào tầm đánh thì đứng yên.")]
    public bool stopCompletelyWhenInMeleeRange = true;

    [Tooltip("Khóa vị trí khi đang đánh.")]
    public bool freezePositionWhileAttacking = true;

    [Header("Melee Attack")]
    [Tooltip("Hitbox đánh cận chiến.")]
    public TieuYeuMeleeHitbox meleeHitbox;

    [Tooltip("Tầm đánh ngang.")]
    public float meleeRange = 1.6f;

    [Tooltip("Độ lệch cao thấp cho phép khi đánh.")]
    public float verticalAttackTolerance = 2.5f;

    [Tooltip("Thời gian hồi đánh.")]
    public float meleeCooldown = 1.1f;

    [Tooltip("Thời gian tối đa của animation đánh.")]
    public float attackMaxDuration = 1.2f;

    [Tooltip("Sát thương cận chiến.")]
    public int meleeDamage = 50;

    [Header("Health")]
    [Tooltip("Máu tối đa.")]
    public int maxHealth = 500;

    [Tooltip("Máu hiện tại.")]
    public int currentHealth;

    [Tooltip("UI máu trên đầu.")]
    public TieuYeuHealthTextUI healthTextUI;

    [Header("Death")]
    [Tooltip("Tự xóa sau animation chết.")]
    public bool destroyAfterDeath = true;

    [Header("Animator")]
    [Tooltip("Tên parameter tốc độ.")]
    public string speedParameterName = "Speed";

    [Tooltip("Tên trigger đánh.")]
    public string meleeTriggerName = "MeleeAttack";

    [Tooltip("Tên trigger chết.")]
    public string dieTriggerName = "Die";

    [Tooltip("Tên state idle trong Animator.")]
    public string idleStateName = "TieuYeu_idle";

    [Tooltip("Ép Tiểu yêu về idle khi bắt đầu.")]
    public bool forceIdleOnStart = true;

    [Header("Control")]
    [Tooltip("Cho phép di chuyển.")]
    public bool canMove = true;

    [Tooltip("Cho phép tấn công.")]
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
        isDead = false;
        isAttacking = false;

        if (meleeHitbox == null)
        {
            meleeHitbox = GetComponentInChildren<TieuYeuMeleeHitbox>();
        }

        if (meleeHitbox != null)
        {
            meleeHitbox.owner = this;
            meleeHitbox.ownerRoot = transform.root;
            meleeHitbox.damage = meleeDamage;
            meleeHitbox.DeactivateHitbox();
        }

        if (healthTextUI == null)
        {
            healthTextUI = GetComponentInChildren<TieuYeuHealthTextUI>();
        }

        ResetAnimatorToIdle();
        UpdateHealthUI();
    }

    void Start()
    {
        FindPlayerIfNeeded();

        if (!activateOnlyWhenPlayerNear)
        {
            isActivated = true;
        }

        ResetAnimatorToIdle();
        UpdateHealthUI();
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
                LockEnemyPosition();
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
                LockEnemyPosition();
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

    void ResetAnimatorToIdle()
    {
        if (animator == null)
            return;

        animator.ResetTrigger(meleeTriggerName);
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
                Debug.Log("Tiểu yêu đã được kích hoạt.");
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
            LockEnemyPosition();
        }

        if (meleeHitbox != null)
        {
            meleeHitbox.DeactivateHitbox();
        }

        if (animator != null)
        {
            animator.ResetTrigger(dieTriggerName);
            animator.ResetTrigger(meleeTriggerName);
            animator.SetTrigger(meleeTriggerName);
        }

        if (enableDebugLog)
        {
            Debug.Log("Tiểu yêu bắt đầu đánh.");
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

    void LockEnemyPosition()
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

    void UpdateHealthUI()
    {
        if (healthTextUI != null)
        {
            healthTextUI.UpdateHealthText(currentHealth, maxHealth);
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

        if (damage <= 0)
            return;

        currentHealth -= damage;

        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        UpdateHealthUI();

        if (enableDebugLog)
        {
            Debug.Log("Tiểu yêu nhận damage: -" + damage + " | Máu còn: " + currentHealth + "/" + maxHealth);
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
        UpdateHealthUI();

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
            Debug.Log("Tiểu yêu chết.");
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
            LockEnemyPosition();
        }

        if (meleeHitbox != null)
        {
            meleeHitbox.ActivateHitbox(lockedMeleeTarget);
            meleeHitbox.ForceHitTarget(lockedMeleeTarget);
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
            LockEnemyPosition();
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
    }

    public void DestroyTieuYeuAfterDieAnimation()
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

    public Transform GetLockedMeleeTarget()
    {
        return lockedMeleeTarget;
    }

    public Vector2 GetEnemyFacingDirection()
    {
        return facingRight ? Vector2.right : Vector2.left;
    }

    public bool IsDead()
    {
        return isDead;
    }
}