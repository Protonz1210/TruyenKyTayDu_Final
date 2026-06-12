using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Boss4Controller : MonoBehaviour
{
    [Header("Activation")]
    [Tooltip("Tag của Wukong.")]
    public string playerTag = "Player";

    [Tooltip("Khoảng cách kích hoạt Boss4.")]
    public float activationRange = 7f;

    [Tooltip("Boss4 đã được kích hoạt.")]
    public bool activated;

    [Header("Main Target")]
    [Tooltip("Mục tiêu chính là Wukong.")]
    public Transform target;

    [Header("Stop When Wukong Dead")]
    [Tooltip("Dừng Boss4 khi Wukong chết.")]
    public bool stopWhenWukongDead = true;

    [Header("Party Target Priority")]
    [Tooltip("Cho phép Boss4 đánh đoàn nếu đoàn gần hơn.")]
    public bool canAttackPartyIfCloser = true;

    [Tooltip("Tag của đoàn thỉnh kinh.")]
    public string partyTag = "Party";

    [Tooltip("Tầm phát hiện đoàn thỉnh kinh.")]
    public float partyDetectRange = 4f;

    [Tooltip("Khoảng cách Wukong giành lại ưu tiên target.")]
    public float wukongReclaimDistance = 3f;

    [Tooltip("Cho phép Boss4 đuổi theo đoàn.")]
    public bool chasePartyIfCloser = false;

    [Tooltip("Mục tiêu hiện tại Boss4 đang đánh.")]
    public Transform currentCombatTarget;

    [Header("Move")]
    [Tooltip("Tốc độ di chuyển.")]
    public float moveSpeed = 7f;

    [Tooltip("Khoảng cách dừng trước mục tiêu.")]
    public float stopDistance = 1f;

    [Header("Movement Lock")]
    [Tooltip("Khóa di chuyển khi dùng skill.")]
    public bool lockMovementWhileUsingSkill = true;

    [Tooltip("Đóng băng vị trí khi dùng skill.")]
    public bool freezeWorldPositionWhileUsingSkill = true;

    [Tooltip("Trạng thái đang bị khóa di chuyển.")]
    public bool isMovementLocked;

    [Header("Melee Attack")]
    [Tooltip("Tầm đánh cận chiến.")]
    public float meleeRange = 1.6f;

    [Tooltip("Thời gian hồi chiêu cận chiến.")]
    public float meleeCooldown = 0f;

    [Tooltip("Thời gian giữ trạng thái đánh cận chiến.")]
    public float meleeActionDuration = 5f;

    [Tooltip("Sát thương cận chiến.")]
    public int meleeDamage = 200;

    [Tooltip("Hitbox cận chiến.")]
    public Boss4MeleeHitbox meleeHitbox;

    [Header("Ultimate")]
    [Tooltip("Thời gian hồi chiêu ulti.")]
    public float ultimateCooldown = 5f;

    [Tooltip("Thời gian giữ trạng thái ulti.")]
    public float ultimateActionDuration = 1.6f;

    [Tooltip("Thời điểm bắn projectile nếu không dùng event.")]
    public float ultimateFireDelay = 0.6f;

    [Tooltip("Dùng Animation Event để bắn projectile.")]
    public bool useAnimationEventForUltimateFire = true;

    [Header("Ultimate Commit")]
    [Tooltip("Thời gian đứng idle trước khi dùng ulti.")]
    public float preUltimateIdleDelay = 0.5f;

    [Tooltip("Tự sinh projectile nếu event bị miss.")]
    public bool guaranteeUltimateProjectileSpawn = false;

    [Tooltip("Thời điểm tự sinh projectile dự phòng.")]
    public float guaranteedUltimateFireTime = 2f;

    [Tooltip("Thời gian đứng idle sau khi dùng ulti.")]
    public float postUltimateIdleDelay = 0.35f;

    [Header("Ultimate Distance Condition")]
    [Tooltip("Bật điều kiện khoảng cách để dùng ulti.")]
    public bool useUltimateDistanceCondition = true;

    [Tooltip("Khoảng cách tối thiểu để dùng ulti.")]
    public float minUltimateDistanceToWukong = 4f;

    [Tooltip("Khoảng cách tối đa để dùng ulti.")]
    public float maxUltimateDistanceToWukong = 9f;

    [Tooltip("Prefab projectile ulti.")]
    public GameObject ultimateProjectilePrefab;

    [Tooltip("Điểm sinh projectile ulti.")]
    public Transform ultimateFirePoint;

    [Tooltip("Sát thương ulti.")]
    public int ultimateDamage = 180;

    [Header("Ultimate Fire Point")]
    [Tooltip("Tự cập nhật Fire Point theo hướng nhìn.")]
    public bool autoUpdateUltimateFirePoint = true;

    [Tooltip("Vị trí Fire Point khi nhìn sang phải.")]
    public Vector2 ultimateFirePointLocalOffset = new Vector2(1.3f, 0.95f);

    [Header("Health")]
    [Tooltip("Máu tối đa.")]
    public int maxHealth = 1000;

    [Tooltip("Máu hiện tại.")]
    public int currentHealth;

    [Header("Map 4 Boss HUD")]
    [Tooltip("UI máu Boss4.")]
    public Map4BossHUDController map4BossHUD;

    [Header("Animator")]
    [Tooltip("Tên animation idle.")]
    public string idleStateName = "Boss4_idle";

    [Tooltip("Tên parameter tốc độ.")]
    public string speedParameterName = "Speed";

    [Tooltip("Tên trigger đánh cận chiến.")]
    public string meleeTriggerName = "MeleeAttack";

    [Tooltip("Tên trigger ulti.")]
    public string ultimateTriggerName = "Ultimate";

    [Header("Debug")]
    [Tooltip("Bật phím debug.")]
    public bool enableDebugKeys = true;

    [Tooltip("Sát thương test.")]
    public int testDamageAmount = 100;

    [Tooltip("Hồi máu test.")]
    public int testHealAmount = 100;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerHealth targetHealth;

    private bool isDefeated;
    private bool isAttacking;
    private bool isUsingUltimate;
    private bool ultimateProjectileFired;
    private bool ultimateAnimationEnded;
    private bool hasStoppedBecauseWukongDead;

    private float meleeTimer;
    private float ultimateTimer;
    private bool ultimateTimerStarted;

    private Coroutine actionCoroutine;

    private Vector3 lockedWorldPosition;
    private bool hasLockedWorldPosition;

    private Transform lockedMeleeTarget;

  


void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (target == null)
        {
            FindTargetIfNeeded();
        }

        CacheTargetHealth();

        if (map4BossHUD == null)
        {
            map4BossHUD = FindFirstObjectByType<Map4BossHUDController>();
        }

        currentHealth = maxHealth;

        if (meleeHitbox != null)
        {
            meleeHitbox.owner = this;
            meleeHitbox.damage = meleeDamage;
            meleeHitbox.DeactivateHitbox();
        }

        SetMovementLock(false);
        UpdateUltimateFirePointPosition();
        UpdateBossHUD();

        Debug.Log("Boss4 khởi tạo máu: " + currentHealth + " / " + maxHealth);
    }

    void Start()
    {
        UpdateBossHUD();
    }

    void Update()
    {
        HandleDebugKeys();

        if (isDefeated)
        {
            ForceIdleState();
            return;
        }

        FindTargetIfNeeded();
        CacheTargetHealth();

        if (IsWukongDead())
        {
            if (!hasStoppedBecauseWukongDead)
            {
                StopBossBecauseWukongDead();
            }
            else
            {
                MaintainIdleAfterWukongDead();
            }

            return;
        }
        else
        {
            hasStoppedBecauseWukongDead = false;
        }

        if (!activated)
        {
            CheckActivation();
        }

        if (!activated)
        {
            HoldIdle();
            return;
        }

        UpdateTimers();

        if (target == null)
        {
            HoldIdle();
            return;
        }

        if (isMovementLocked || isAttacking || isUsingUltimate)
        {
            StopMove();

            if (freezeWorldPositionWhileUsingSkill)
            {
                MaintainLockedWorldPosition();
            }

            UpdateAnimation(0f);
            return;
        }

        currentCombatTarget = GetBestCombatTarget();

        if (currentCombatTarget == null)
        {
            currentCombatTarget = target;
        }

        FaceTransform(currentCombatTarget);

        if (CanUseUltimate())
        {
            StartUltimateAttack();
            return;
        }

        float distanceToCombatTarget = Vector2.Distance(transform.position, currentCombatTarget.position);

        if (distanceToCombatTarget <= meleeRange)
        {
            HandleTargetInsideMeleeRange(currentCombatTarget);
            return;
        }

        ChaseTarget(currentCombatTarget);
    }

    void LateUpdate()
    {
        if ((isMovementLocked || isAttacking || isUsingUltimate) && freezeWorldPositionWhileUsingSkill)
        {
            MaintainLockedWorldPosition();
        }
    }

    void FixedUpdate()
    {
        if ((isMovementLocked || isAttacking || isUsingUltimate) && freezeWorldPositionWhileUsingSkill)
        {
            MaintainLockedWorldPosition();
        }
    }

    void HandleTargetInsideMeleeRange(Transform attackTarget)
    {
        StopMove();
        FaceTransform(attackTarget);
        UpdateAnimation(0f);

        if (CanStartMeleeAttack())
        {
            StartMeleeAttack(attackTarget);
            return;
        }

        HoldIdle();
    }

    bool CanStartMeleeAttack()
    {
        if (isDefeated)
            return false;

        if (IsWukongDead())
            return false;

        if (isUsingUltimate)
            return false;

        if (isAttacking)
            return false;

        if (isMovementLocked)
            return false;

        if (actionCoroutine != null)
            return false;

        if (meleeTimer > 0f)
            return false;

        return true;
    }

    void HoldIdle()
    {
        StopMove();
        CloseMeleeHitbox();
        UpdateAnimation(0f);
    }

    public Transform GetLockedMeleeTarget()
    {
        return lockedMeleeTarget;
    }

    public Vector2 GetBossFacingDirection()
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

    void CacheTargetHealth()
    {
        if (target == null)
            return;

        if (targetHealth != null)
            return;

        targetHealth = target.GetComponentInParent<PlayerHealth>();
    }

    bool IsWukongDead()
    {
        if (!stopWhenWukongDead)
            return false;

        if (target == null)
            return true;

        if (!target.gameObject.activeInHierarchy)
            return true;

        if (targetHealth == null)
        {
            CacheTargetHealth();
        }

        if (targetHealth == null)
            return false;

        return targetHealth.IsDead();
    }

    void StopBossBecauseWukongDead()
    {
        activated = false;

        StopMove();
        CloseMeleeHitbox();

        lockedMeleeTarget = null;
        currentCombatTarget = null;

        isAttacking = false;
        isUsingUltimate = false;
        isMovementLocked = false;
        hasLockedWorldPosition = false;

        meleeTimer = 0f;
        ultimateTimer = 0f;
        ultimateProjectileFired = false;

        if (actionCoroutine != null)
        {
            StopCoroutine(actionCoroutine);
            actionCoroutine = null;
        }

        if (animator != null)
        {
            animator.ResetTrigger(meleeTriggerName);
            animator.ResetTrigger(ultimateTriggerName);
            animator.SetFloat(speedParameterName, 0f);

            if (!string.IsNullOrEmpty(idleStateName))
            {
                animator.Play(idleStateName, 0, 0f);
            }
        }

        hasStoppedBecauseWukongDead = true;

        Debug.Log("Wukong đã chết, Boss4 dừng toàn bộ hành động và chuyển về idle.");
    }

    void MaintainIdleAfterWukongDead()
    {
        StopMove();
        CloseMeleeHitbox();

        isAttacking = false;
        isUsingUltimate = false;
        isMovementLocked = false;
        hasLockedWorldPosition = false;

        if (animator != null)
        {
            animator.ResetTrigger(meleeTriggerName);
            animator.ResetTrigger(ultimateTriggerName);
            animator.SetFloat(speedParameterName, 0f);
        }
    }

    void FindTargetIfNeeded()
    {
        if (target != null && target.gameObject.activeInHierarchy)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
        {
            target = playerObject.transform;
            targetHealth = null;
            CacheTargetHealth();
        }
    }

    void CheckActivation()
    {
        if (activated)
            return;

        if (target == null)
            return;

        float distance = Vector2.Distance(transform.position, target.position);

        if (distance <= activationRange)
        {
            activated = true;
            Debug.Log("Boss4 đã kích hoạt.");
        }
    }

    void UpdateTimers()
    {
        if (meleeTimer > 0f)
        {
            meleeTimer -= Time.deltaTime;

            if (meleeTimer < 0f)
            {
                meleeTimer = 0f;
            }
        }

        if (ultimateTimerStarted)
        {
            ultimateTimer += Time.deltaTime;
        }
    }

    Transform GetBestCombatTarget()
    {
        if (target == null)
            return null;

        Transform bestTarget = target;

        if (!canAttackPartyIfCloser)
            return bestTarget;

        float wukongDistance = Vector2.Distance(transform.position, target.position);

        if (wukongDistance <= wukongReclaimDistance)
        {
            return target;
        }

        GameObject[] partyObjects = GameObject.FindGameObjectsWithTag(partyTag);

        Transform nearestParty = null;
        float nearestPartyDistance = Mathf.Infinity;

        for (int i = 0; i < partyObjects.Length; i++)
        {
            GameObject partyObject = partyObjects[i];

            if (partyObject == null)
                continue;

            if (!partyObject.activeInHierarchy)
                continue;

            float partyDistance = Vector2.Distance(transform.position, partyObject.transform.position);

            if (partyDistance > partyDetectRange)
                continue;

            if (partyDistance < nearestPartyDistance)
            {
                nearestPartyDistance = partyDistance;
                nearestParty = partyObject.transform;
            }
        }

        if (nearestParty == null)
            return target;

        bool partyCloserThanWukong = nearestPartyDistance < wukongDistance;

        if (!partyCloserThanWukong)
            return target;

        if (!chasePartyIfCloser)
        {
            if (nearestPartyDistance <= meleeRange)
            {
                return nearestParty;
            }

            return target;
        }

        return nearestParty;
    }

    bool CanUseUltimate()
    {
        if (isDefeated)
            return false;

        if (IsWukongDead())
            return false;

        if (isAttacking || isUsingUltimate || isMovementLocked)
            return false;

        if (!ultimateTimerStarted)
            return false;

        if (ultimateTimer < ultimateCooldown)
            return false;

        if (ultimateProjectilePrefab == null)
            return false;

        if (ultimateFirePoint == null)
            return false;

        if (target == null)
            return false;

        if (useUltimateDistanceCondition)
        {
            float wukongDistance = Vector2.Distance(transform.position, target.position);

            if (wukongDistance < minUltimateDistanceToWukong)
            {
                return false;
            }

            if (wukongDistance > maxUltimateDistanceToWukong)
            {
                return false;
            }
        }

        return true;
    }

    void ChaseTarget(Transform chaseTarget)
    {
        if (rb == null || chaseTarget == null)
            return;

        Vector2 currentPosition = rb.position;
        Vector2 targetPosition = chaseTarget.position;

        targetPosition.y = currentPosition.y;

        float distanceX = Mathf.Abs(targetPosition.x - currentPosition.x);

        if (distanceX <= stopDistance)
        {
            HoldIdle();
            return;
        }

        float directionX = targetPosition.x - currentPosition.x;
        FaceDirection(directionX);

        Vector2 newPosition = Vector2.MoveTowards(
            currentPosition,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        rb.MovePosition(newPosition);
        UpdateAnimation(1f);
    }

    void StartMeleeAttack(Transform attackTarget)
    {
        if (!CanStartMeleeAttack())
            return;

        actionCoroutine = StartCoroutine(MeleeAttackRoutine(attackTarget));
    }

    IEnumerator MeleeAttackRoutine(Transform attackTarget)
    {
        isAttacking = true;
        lockedMeleeTarget = attackTarget;

        meleeTimer = Mathf.Max(0f, meleeCooldown);

        StopMove();
        CloseMeleeHitbox();
        FaceTransform(lockedMeleeTarget);
        UpdateAnimation(0f);

        if (lockMovementWhileUsingSkill)
        {
            SetMovementLock(true);
        }

        if (!ultimateTimerStarted)
        {
            ultimateTimerStarted = true;
            ultimateTimer = 0f;
            Debug.Log("Boss4 bắt đầu đếm Ulti sau lần đánh cận chiến đầu tiên.");
        }

        if (animator != null)
        {
            animator.ResetTrigger(ultimateTriggerName);
            animator.ResetTrigger(meleeTriggerName);
            animator.SetFloat(speedParameterName, 0f);
            animator.SetTrigger(meleeTriggerName);
        }

        yield return new WaitForSeconds(meleeActionDuration);

        if (isDefeated || IsWukongDead())
        {
            CloseMeleeHitbox();
            yield break;
        }

        CloseMeleeHitbox();

        lockedMeleeTarget = null;
        isAttacking = false;
        SetMovementLock(false);

        actionCoroutine = null;

        if (meleeTimer > 0f)
        {
            HoldIdle();
        }

        Debug.Log("Boss4 kết thúc cận chiến.");
    }

    void StartUltimateAttack()
    {
        if (actionCoroutine != null)
            return;

        actionCoroutine = StartCoroutine(UltimateAttackRoutine());
    }

    IEnumerator UltimateAttackRoutine()
    {
        isUsingUltimate = true;
        isAttacking = true;
        ultimateProjectileFired = false;

        StopMove();
        CloseMeleeHitbox();

        Transform ultimateTarget = target;

        if (ultimateTarget == null)
        {
            ultimateTarget = currentCombatTarget;
        }

        FaceTransform(ultimateTarget);
        UpdateAnimation(0f);

        if (lockMovementWhileUsingSkill)
        {
            SetMovementLock(true);
        }

        if (animator != null)
        {
            animator.ResetTrigger(meleeTriggerName);
            animator.ResetTrigger(ultimateTriggerName);
            animator.SetFloat(speedParameterName, 0f);

            if (!string.IsNullOrEmpty(idleStateName))
            {
                animator.Play(idleStateName, 0, 0f);
            }
        }

        yield return new WaitForSeconds(preUltimateIdleDelay);

        if (isDefeated || IsWukongDead())
        {
            CloseMeleeHitbox();
            FinishUltimateForceIdle();
            yield break;
        }

        StopMove();

        if (freezeWorldPositionWhileUsingSkill)
        {
            MaintainLockedWorldPosition();
        }

        FaceTransform(ultimateTarget);
        UpdateUltimateFirePointPosition();

        if (animator != null)
        {
            animator.ResetTrigger(meleeTriggerName);
            animator.ResetTrigger(ultimateTriggerName);
            animator.SetFloat(speedParameterName, 0f);
            animator.SetTrigger(ultimateTriggerName);
        }

        float elapsed = 0f;
        bool fallbackFireChecked = false;

        while (elapsed < ultimateActionDuration)
        {
            if (isDefeated || IsWukongDead())
            {
                CloseMeleeHitbox();
                FinishUltimateForceIdle();
                yield break;
            }

            StopMove();

            if (freezeWorldPositionWhileUsingSkill)
            {
                MaintainLockedWorldPosition();
            }

            elapsed += Time.deltaTime;

            if (!useAnimationEventForUltimateFire &&
                !ultimateProjectileFired &&
                elapsed >= ultimateFireDelay)
            {
                FireUltimateProjectile();
            }

            if (useAnimationEventForUltimateFire &&
                guaranteeUltimateProjectileSpawn &&
                !fallbackFireChecked &&
                !ultimateProjectileFired &&
                elapsed >= guaranteedUltimateFireTime)
            {
                fallbackFireChecked = true;
                FireUltimateProjectile();
            }

            yield return null;
        }

        FinishUltimate();
    }

    void FinishUltimate()
    {
        if (!isUsingUltimate)
            return;

        ultimateTimer = 0f;

        isUsingUltimate = false;
        isAttacking = false;

        CloseMeleeHitbox();
        SetMovementLock(false);

        actionCoroutine = null;

        StopMove();
        UpdateAnimation(0f);

        if (animator != null)
        {
            animator.ResetTrigger(meleeTriggerName);
            animator.ResetTrigger(ultimateTriggerName);
            animator.SetFloat(speedParameterName, 0f);

            if (!string.IsNullOrEmpty(idleStateName))
            {
                animator.Play(idleStateName, 0, 0f);
            }
        }

        Debug.Log("Boss4 kết thúc Ulti và chuyển về idle.");
    }

    void FinishUltimateForceIdle()
    {
        isUsingUltimate = false;
        isAttacking = false;
        ultimateProjectileFired = false;

        CloseMeleeHitbox();
        SetMovementLock(false);

        actionCoroutine = null;

        StopMove();
        UpdateAnimation(0f);

        if (animator != null)
        {
            animator.ResetTrigger(meleeTriggerName);
            animator.ResetTrigger(ultimateTriggerName);
            animator.SetFloat(speedParameterName, 0f);

            if (!string.IsNullOrEmpty(idleStateName))
            {
                animator.Play(idleStateName, 0, 0f);
            }
        }
    }

    public void EndUltimateAnimation()
    {
        Debug.Log("EndUltimateAnimation đang được bỏ qua. Ulti kết thúc bằng Ultimate Action Duration.");
    }

    public void OpenMeleeHitbox()
    {
        if (isDefeated)
            return;

        if (IsWukongDead())
            return;

        if (isUsingUltimate)
            return;

        if (!isAttacking)
            return;

        if (meleeHitbox != null)
        {
            meleeHitbox.damage = meleeDamage;
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

    public void FireUltimateProjectile()
    {
        if (isDefeated)
            return;

        if (IsWukongDead())
            return;

        if (!isUsingUltimate)
            return;

        if (ultimateProjectileFired)
            return;

        if (ultimateProjectilePrefab == null || ultimateFirePoint == null)
        {
            Debug.LogWarning("Boss4 không thể tạo projectile ulti vì thiếu Prefab hoặc Fire Point.");
            return;
        }

        ultimateProjectileFired = true;

        StopMove();

        if (freezeWorldPositionWhileUsingSkill)
        {
            MaintainLockedWorldPosition();
        }

        FaceTransform(target);
        UpdateUltimateFirePointPosition();

        Vector3 spawnPosition = ultimateFirePoint.position;
        Vector2 facingDirection = GetBossFacingDirection();

        GameObject projectileObject = Instantiate(
            ultimateProjectilePrefab,
            spawnPosition,
            Quaternion.identity
        );

        Boss4UltimateProjectile projectile =
            projectileObject.GetComponent<Boss4UltimateProjectile>();

        if (projectile != null)
        {
            projectile.Init(
                facingDirection,
                ultimateDamage,
                transform.root,
                ultimateFirePoint
            );
        }

        Debug.Log("Boss4 tạo projectile Ulti.");
    }

    void SetMovementLock(bool locked)
    {
        isMovementLocked = locked;

        if (locked)
        {
            StopMove();

            if (freezeWorldPositionWhileUsingSkill)
            {
                LockCurrentWorldPosition();
            }
        }
        else
        {
            hasLockedWorldPosition = false;
        }
    }

    void LockCurrentWorldPosition()
    {
        lockedWorldPosition = transform.position;
        hasLockedWorldPosition = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = lockedWorldPosition;
        }

        transform.position = lockedWorldPosition;
    }

    void MaintainLockedWorldPosition()
    {
        if (!hasLockedWorldPosition)
            return;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = lockedWorldPosition;
        }

        transform.position = lockedWorldPosition;
    }

    void UpdateUltimateFirePointPosition()
    {
        if (!autoUpdateUltimateFirePoint)
            return;

        if (ultimateFirePoint == null)
            return;

        Vector3 localPosition = ultimateFirePoint.localPosition;

        if (GetBossFacingDirection().x < 0f)
        {
            localPosition.x = -Mathf.Abs(ultimateFirePointLocalOffset.x);
        }
        else
        {
            localPosition.x = Mathf.Abs(ultimateFirePointLocalOffset.x);
        }

        localPosition.y = ultimateFirePointLocalOffset.y;
        localPosition.z = 0f;

        ultimateFirePoint.localPosition = localPosition;
    }

    public void TakeDamage(int damage)
    {
        if (isDefeated)
            return;

        if (damage <= 0)
            return;

        activated = true;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateBossHUD();

        Debug.Log("Boss4 mất máu: " + damage + " | Máu: " + currentHealth + " / " + maxHealth);

        if (currentHealth <= 0)
        {
            DefeatToIdle();
        }
    }

    public void Heal(int healAmount)
    {
        if (healAmount <= 0)
            return;

        if (isDefeated)
        {
            isDefeated = false;
            activated = false;
            isAttacking = false;
            isUsingUltimate = false;
            hasStoppedBecauseWukongDead = false;
            SetMovementLock(false);

            if (animator != null)
            {
                animator.SetFloat(speedParameterName, 0f);

                if (!string.IsNullOrEmpty(idleStateName))
                {
                    animator.Play(idleStateName, 0, 0f);
                }
            }
        }

        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateBossHUD();
    }

    void DefeatToIdle()
    {
        isDefeated = true;
        activated = false;
        isAttacking = false;
        isUsingUltimate = false;

        meleeTimer = 0f;
        ultimateTimer = 0f;
        ultimateTimerStarted = false;
        ultimateProjectileFired = false;
        lockedMeleeTarget = null;

        CloseMeleeHitbox();

        if (actionCoroutine != null)
        {
            StopCoroutine(actionCoroutine);
            actionCoroutine = null;
        }

        SetMovementLock(false);
        StopMove();

        if (animator != null)
        {
            animator.ResetTrigger(meleeTriggerName);
            animator.ResetTrigger(ultimateTriggerName);
            animator.SetFloat(speedParameterName, 0f);

            if (!string.IsNullOrEmpty(idleStateName))
            {
                animator.Play(idleStateName, 0, 0f);
            }
        }

        UpdateBossHUD();

        Debug.Log("Boss4 hết máu, dừng hoạt động.");
    }

    void ForceIdleState()
    {
        StopMove();
        CloseMeleeHitbox();

        isAttacking = false;
        isUsingUltimate = false;
        isMovementLocked = false;
        hasLockedWorldPosition = false;

        if (animator != null)
        {
            animator.ResetTrigger(meleeTriggerName);
            animator.ResetTrigger(ultimateTriggerName);
            animator.SetFloat(speedParameterName, 0f);

            if (!string.IsNullOrEmpty(idleStateName))
            {
                animator.Play(idleStateName, 0, 0f);
            }
        }
    }

    void UpdateBossHUD()
    {
        if (map4BossHUD == null)
        {
            map4BossHUD = FindFirstObjectByType<Map4BossHUDController>();
        }

        if (map4BossHUD != null)
        {
            map4BossHUD.SetBoss4Health(currentHealth, maxHealth);
        }
    }

    void FaceTransform(Transform faceTarget)
    {
        if (faceTarget == null)
            return;

        float direction = faceTarget.position.x - transform.position.x;
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
            {
                scale.x = Mathf.Abs(scale.x);
            }
            else
            {
                scale.x = -Mathf.Abs(scale.x);
            }

            transform.localScale = scale;
        }

        UpdateUltimateFirePointPosition();
    }

    void StopMove()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    void UpdateAnimation(float speedValue)
    {
        if (animator != null)
        {
            animator.SetFloat(speedParameterName, speedValue);
        }
    }

    void HandleDebugKeys()
    {
        if (!enableDebugKeys)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            TakeDamage(testDamageAmount);
        }

        if (Keyboard.current.yKey.wasPressedThisFrame)
        {
            Heal(testHealAmount);
        }

        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            TakeDamage(maxHealth);
        }

        if (Keyboard.current.uKey.wasPressedThisFrame)
        {
            activated = true;
            ultimateTimerStarted = true;
            ultimateTimer = ultimateCooldown;
            Debug.Log("TEST: Ép Boss4 dùng Ulti.");
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

    public bool IsDefeated()
    {
        return isDefeated;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, partyDetectRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, wukongReclaimDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, minUltimateDistanceToWukong);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxUltimateDistanceToWukong);

        if (ultimateFirePoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(ultimateFirePoint.position, 0.15f);
        }
    }
}