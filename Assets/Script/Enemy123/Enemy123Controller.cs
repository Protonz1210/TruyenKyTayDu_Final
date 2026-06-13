using UnityEngine;

public class Enemy123Controller : MonoBehaviour
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
    [Tooltip("Enemy123 vừa sinh ra sẽ target Wukong và hoạt động luôn.")]
    public bool targetWukongOnStart = false;

    [Tooltip("Khoảng cách để Enemy123 bắt đầu hoạt động nếu không bật Target Wukong On Start.")]
    public float activationRange = 12f;

    [Tooltip("Chỉ hoạt động khi Wukong lại gần.")]
    public bool activateOnlyWhenPlayerNear = true;

    [Header("Target Priority")]
    [Tooltip("Nếu Party đứng giữa Enemy123 và Wukong, đồng thời Party trong tầm cận chiến, Enemy123 sẽ đánh Party.")]
    public bool attackBlockingParty = true;

    [Tooltip("Tầm phát hiện đoàn thỉnh kinh.")]
    public float partyDetectRange = 4f;

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
    public Enemy123MeleeHitbox meleeHitbox;

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

    [Header("Death")]
    [Tooltip("Tự xóa sau animation chết.")]
    public bool destroyAfterDeath = true;

    [Header("Facing")]
    [Tooltip("Bật nếu sprite gốc của Enemy123 đang quay sang phải. Tắt nếu sprite gốc đang quay sang trái.")]
    public bool spriteFacesRightByDefault = true;

    [Tooltip("Dùng localScale X để lật hướng.")]
    public bool useTransformScaleFlip = true;

    [Header("Animator")]
    [Tooltip("Tên parameter tốc độ.")]
    public string speedParameterName = "Speed";

    [Tooltip("Tên trigger đánh.")]
    public string meleeTriggerName = "MeleeAttack";

    [Tooltip("Tên trigger chết.")]
    public string dieTriggerName = "Die";

    [Tooltip("Tên state idle trong Animator.")]
    public string idleStateName = "Enemy1Idle";

    [Tooltip("Ép Enemy123 về idle khi bắt đầu.")]
    public bool forceIdleOnStart = true;

    [Header("Control")]
    [Tooltip("Cho phép di chuyển.")]
    public bool canMove = true;

    [Tooltip("Cho phép tấn công.")]
    public bool canAttack = true;

    [Header("Stop Combat")]
    [Tooltip("Dừng Enemy123 khi Wukong hoặc đoàn thỉnh kinh hết máu.")]
    public bool combatStoppedByDeath = false;

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
    private float originalScaleX = 1f;
    private bool hasForcedIdleAfterCombatStop;

    private float meleeCooldownTimer;
    private float attackTimer;

    private Vector2 attackLockedPosition;
    private bool hasAttackLockedPosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        originalScaleX = Mathf.Abs(transform.localScale.x);
        facingRight = spriteFacesRightByDefault;
        currentHealth = maxHealth;
        isDead = false;
        isAttacking = false;

        FindPlayerIfNeeded();

        if (meleeHitbox == null)
        {
            meleeHitbox = GetComponentInChildren<Enemy123MeleeHitbox>(true);
        }

        if (meleeHitbox != null)
        {
            meleeHitbox.owner = this;
            meleeHitbox.ownerRoot = transform.root;
            meleeHitbox.damage = meleeDamage;
            meleeHitbox.DeactivateHitbox();
        }

        ResetAnimatorToIdle();
    }

    void Start()
    {
        FindPlayerIfNeeded();

        if (targetWukongOnStart)
        {
            isActivated = true;
            currentCombatTarget = target;
        }
        else if (!activateOnlyWhenPlayerNear)
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

        if (combatStoppedByDeath)
        {
            StopMoveHard();

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

        if (combatStoppedByDeath)
        {
            StopMoveHard();
            return;
        }

        if (!isActivated) return;

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
        if (animator == null) return;

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
        if (!autoFindPlayer) return;
        if (target != null) return;

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
        {
            target = playerObject.transform;
        }
    }

    void UpdateActivation()
    {
        if (targetWukongOnStart)
        {
            isActivated = true;
            return;
        }

        if (isActivated) return;
        if (target == null) return;

        float distanceToPlayerX = Mathf.Abs(target.position.x - transform.position.x);

        if (distanceToPlayerX <= activationRange)
        {
            isActivated = true;

            if (enableDebugLog)
            {
                Debug.Log("Enemy123 đã được kích hoạt.");
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
        if (!isAttacking) return;

        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            EndMeleeAttackAnimation();
        }
    }

    void UpdateCurrentCombatTarget()
    {
        currentCombatTarget = target;

        if (target == null) return;

        if (IsTargetInMeleeRange(target))
        {
            currentCombatTarget = target;
            return;
        }

        if (!attackBlockingParty) return;

        Transform blockingParty = FindBlockingPartyInMeleeRange();

        if (blockingParty != null)
        {
            currentCombatTarget = blockingParty;
        }
    }

    Transform FindBlockingPartyInMeleeRange()
    {
        if (target == null) return null;

        GameObject[] partyObjects = GameObject.FindGameObjectsWithTag(partyTag);

        Transform nearestBlockingParty = null;
        float nearestDistance = Mathf.Infinity;

        for (int i = 0; i < partyObjects.Length; i++)
        {
            GameObject partyObject = partyObjects[i];

            if (partyObject == null) continue;

            Transform partyTransform = partyObject.transform;

            float distanceToParty = Mathf.Abs(partyTransform.position.x - transform.position.x);

            if (distanceToParty > partyDetectRange) continue;
            if (!IsTargetInMeleeRange(partyTransform)) continue;
            if (!IsPartyBetweenEnemyAndWukong(partyTransform)) continue;

            if (distanceToParty < nearestDistance)
            {
                nearestDistance = distanceToParty;
                nearestBlockingParty = partyTransform;
            }
        }

        return nearestBlockingParty;
    }

    bool IsPartyBetweenEnemyAndWukong(Transform partyTarget)
    {
        if (partyTarget == null) return false;
        if (target == null) return false;

        float enemyX = transform.position.x;
        float wukongX = target.position.x;
        float partyX = partyTarget.position.x;

        float minX = Mathf.Min(enemyX, wukongX);
        float maxX = Mathf.Max(enemyX, wukongX);

        return partyX > minX && partyX < maxX;
    }

    bool IsTargetInMeleeRange(Transform checkTarget)
    {
        if (checkTarget == null) return false;

        float distanceX = Mathf.Abs(checkTarget.position.x - transform.position.x);
        float distanceY = Mathf.Abs(checkTarget.position.y - transform.position.y);

        return distanceX <= meleeRange && distanceY <= verticalAttackTolerance;
    }

    void TryStartMeleeAttack(Transform attackTarget)
    {
        if (!canAttack) return;
        if (isAttacking) return;
        if (meleeCooldownTimer > 0f) return;
        if (attackTarget == null) return;
        if (combatStoppedByDeath) return;

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
            Debug.Log("Enemy123 bắt đầu đánh: " + attackTarget.name);
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
        return target;
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
        if (!hasAttackLockedPosition) return;

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
        if (faceTarget == null) return;

        float directionX = faceTarget.position.x - transform.position.x;
        FaceDirection(directionX);
    }

    void FaceDirection(float directionX)
    {
        if (Mathf.Abs(directionX) < 0.05f) return;

        bool shouldFaceRight = directionX > 0f;

        facingRight = shouldFaceRight;

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
        if (isDead) return;
        if (combatStoppedByDeath) return;

        if (damage <= 0)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("Enemy123 nhận damage <= 0 nên không trừ máu.");
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
            Debug.Log("Enemy123 nhận damage: -" + damage + " | Máu còn: " + currentHealth + "/" + maxHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;

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
            Debug.Log("Enemy123 chết.");
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

        canMove = false;
        canAttack = false;
        isAttacking = false;
        lockedMeleeTarget = null;
        hasAttackLockedPosition = false;

        StopMoveHard();

        if (meleeHitbox != null)
        {
            meleeHitbox.DeactivateHitbox();
        }

        if (animator != null)
        {
            animator.ResetTrigger(meleeTriggerName);
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
            Debug.Log("Enemy123 dừng combat và về idle vì Wukong hoặc đoàn đã hết máu.");
        }
    }

    void KeepIdleAfterCombatStopped()
    {
        StopMoveHard();

        if (animator == null) return;
        if (string.IsNullOrEmpty(idleStateName)) return;

        animator.ResetTrigger(meleeTriggerName);
        animator.ResetTrigger(dieTriggerName);
        animator.SetFloat(speedParameterName, 0f);

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (!stateInfo.IsName(idleStateName))
        {
            animator.CrossFade(idleStateName, 0.05f, 0, 0f);
        }
    }

    public void OpenMeleeHitbox()
    {
        if (isDead) return;
        if (combatStoppedByDeath) return;
        if (!isAttacking) return;

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
        if (isDead) return;
        if (combatStoppedByDeath) return;

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

    public void DestroyEnemy123AfterDieAnimation()
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

    public void DestroyTieuYeuAfterDieAnimation()
    {
        DestroyEnemy123AfterDieAnimation();
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

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }
}