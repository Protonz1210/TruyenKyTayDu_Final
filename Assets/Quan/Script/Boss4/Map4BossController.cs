using System.Collections;
using UnityEngine;

public class Map4BossController : MonoBehaviour
{
    public enum BossHealthUISlot
    {
        Boss3,
        Boss4
    }

    [Header("Boss Identity")]
    [Tooltip("ID boss trong Map 4.")]
    public int bossId = 4;

    [Tooltip("Tên boss để debug.")]
    public string bossName = "Thanh Sư Tinh";

    [Tooltip("Slot thanh máu boss trên HUD.")]
    public BossHealthUISlot healthUISlot = BossHealthUISlot.Boss4;

    [Header("Activation")]
    [Tooltip("Tag của Wukong.")]
    public string playerTag = "Player";

    [Tooltip("Tầm kích hoạt boss.")]
    public float activationRange = 12f;

    [Tooltip("Boss đã được kích hoạt.")]
    public bool activated = false;

    [Header("Main Target")]
    [Tooltip("Mục tiêu chính là Wukong.")]
    public Transform target;

    [Header("Stop When Wukong Dead")]
    [Tooltip("Dừng boss khi Wukong chết.")]
    public bool stopWhenWukongDead = true;

    [Header("Party Target Priority")]
    [Tooltip("Cho phép boss đánh đoàn nếu đoàn gần hơn.")]
    public bool canAttackPartyIfCloser = true;

    [Tooltip("Tag của đoàn thỉnh kinh.")]
    public string partyTag = "Party";

    [Tooltip("Tầm phát hiện đoàn.")]
    public float partyDetectRange = 10f;

    [Tooltip("Khoảng cách Wukong giành lại mục tiêu.")]
    public float wukongReclaimDistance = 4f;

    [Tooltip("Đuổi theo đoàn nếu đoàn gần hơn.")]
    public bool chasePartyIfCloser = true;

    [Tooltip("Mục tiêu boss đang đánh.")]
    public Transform currentCombatTarget;

    [Header("Move")]
    [Tooltip("Tốc độ di chuyển.")]
    public float moveSpeed = 3f;

    [Tooltip("Khoảng cách dừng khi áp sát mục tiêu.")]
    public float stopDistance = 1.2f;

    [Header("Movement Lock")]
    [Tooltip("Khóa di chuyển khi dùng skill.")]
    public bool lockMovementWhileUsingSkill = true;

    [Tooltip("Giữ nguyên vị trí boss khi dùng skill.")]
    public bool freezeWorldPositionWhileUsingSkill = true;

    [Header("Melee Attack")]
    [Tooltip("Tầm đánh cận chiến.")]
    public float meleeRange = 2f;

    [Tooltip("Thời gian hồi chiêu cận chiến.")]
    public float meleeCooldown = 0f;

    [Tooltip("Thời gian giữ trạng thái đánh cận chiến.")]
    public float meleeActionDuration = 1.2f;

    [Tooltip("Sát thương cận chiến.")]
    public int meleeDamage = 200;

    [Tooltip("Hitbox cận chiến.")]
    public Boss4MeleeHitbox meleeHitbox;

    [Header("Ultimate")]
    [Tooltip("Thời gian hồi chiêu ulti.")]
    public float ultimateCooldown = 5f;

    [Tooltip("Thời gian dự phòng của animation ulti.")]
    public float ultimateActionDuration = 5f;

    [Tooltip("Thời điểm bắn nếu không dùng Animation Event.")]
    public float ultimateFireDelay = 0.8f;

    [Tooltip("Dùng Animation Event để bắn ulti.")]
    public bool useAnimationEventForUltimateFire = true;

    [Header("Ultimate Commit")]
    [Tooltip("Thời gian đứng yên trước khi tung ulti.")]
    public float preUltimateIdleDelay = 0.5f;

    [Tooltip("Đảm bảo ulti luôn bắn nếu Animation Event lỗi.")]
    public bool guaranteeUltimateProjectileSpawn = false;

    [Tooltip("Thời điểm bắn dự phòng của ulti.")]
    public float guaranteedUltimateFireTime = 2f;

    [Tooltip("Thời gian đứng yên sau ulti.")]
    public float postUltimateIdleDelay = 0.2f;

    [Header("Ultimate Distance Condition")]
    [Tooltip("Bật điều kiện khoảng cách dùng ulti.")]
    public bool useUltimateDistanceCondition = true;

    [Tooltip("Khoảng cách tối thiểu để dùng ulti.")]
    public float minUltimateDistanceToWukong = 3f;

    [Tooltip("Khoảng cách tối đa để dùng ulti.")]
    public float maxUltimateDistanceToWukong = 9f;

    [Tooltip("Prefab projectile ulti.")]
    public GameObject ultimateProjectilePrefab;

    [Tooltip("Điểm bắn ulti.")]
    public Transform ultimateFirePoint;

    [Tooltip("Sát thương ulti.")]
    public int ultimateDamage = 180;

    [Header("Ultimate Fire Point")]
    [Tooltip("Tự cập nhật vị trí Fire Point.")]
    public bool autoUpdateUltimateFirePoint = true;

    [Tooltip("Vị trí Fire Point khi nhìn sang phải.")]
    public Vector2 ultimateFirePointLocalOffset = new Vector2(1.2f, 0.5f);

    [Header("Health")]
    [Tooltip("Máu tối đa.")]
    public int maxHealth = 1000;

    [Tooltip("Máu hiện tại.")]
    public int currentHealth;

    [Header("Map 4 Boss HUD")]
    [Tooltip("HUD máu boss Map 4.")]
    public Map4BossHUDController map4BossHUD;

    [Header("Animator")]
    [Tooltip("Tên state idle.")]
    public string idleStateName = "Boss4_idle";

    [Tooltip("Tên parameter tốc độ.")]
    public string speedParameterName = "Speed";

    [Tooltip("Tên trigger đánh cận chiến.")]
    public string meleeTriggerName = "MeleeAttack";

    [Tooltip("Tên trigger ulti.")]
    public string ultimateTriggerName = "Ultimate";

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

        currentHealth = maxHealth;

        SyncHealthSlotByBossId();
        FindBossHUDIfNeeded();

        if (target == null)
        {
            FindTargetIfNeeded();
        }

        CacheTargetHealth();

        if (meleeHitbox == null)
        {
            meleeHitbox = GetComponentInChildren<Boss4MeleeHitbox>();
        }

        if (meleeHitbox != null)
        {
            meleeHitbox.owner = this;
            meleeHitbox.ownerRoot = transform;
            meleeHitbox.damage = meleeDamage;
            meleeHitbox.DeactivateHitbox();
        }
    }

    void Start()
    {
        SyncHealthSlotByBossId();
        FindBossHUDIfNeeded();
        UpdateBossHUD();
    }

    void Update()
    {
        if (isDefeated)
        {
            StopMove();
            return;
        }

        FindTargetIfNeeded();
        CacheTargetHealth();

        if (stopWhenWukongDead && IsWukongDead())
        {
            StopBossBecauseWukongDead();
            return;
        }

        hasStoppedBecauseWukongDead = false;

        if (!activated)
        {
            CheckActivation();
            StopMove();
            return;
        }

        UpdateTimers();
        UpdateUltimateFirePointPosition();

        if (isAttacking || isUsingUltimate)
        {
            StopMove();
            return;
        }

        currentCombatTarget = GetBestCombatTarget();

        if (currentCombatTarget == null)
        {
            HoldIdle();
            return;
        }

        FaceTransform(currentCombatTarget);

        float distanceToCombatTarget = Vector2.Distance(transform.position, currentCombatTarget.position);

        if (distanceToCombatTarget <= meleeRange)
        {
            HandleTargetInsideMeleeRange();
            return;
        }

        if (CanUseUltimate())
        {
            StartUltimateAttack();
            return;
        }

        ChaseTarget(currentCombatTarget);
    }

    void LateUpdate()
    {
        MaintainLockedWorldPosition();
    }

    void FixedUpdate()
    {
        MaintainLockedWorldPosition();
    }

    void SyncHealthSlotByBossId()
    {
        if (bossId == 3)
        {
            healthUISlot = BossHealthUISlot.Boss3;
        }
        else if (bossId == 4)
        {
            healthUISlot = BossHealthUISlot.Boss4;
        }
    }

    void FindBossHUDIfNeeded()
    {
        if (map4BossHUD != null) return;

#if UNITY_2023_1_OR_NEWER
        map4BossHUD = FindFirstObjectByType<Map4BossHUDController>();
#else
        map4BossHUD = FindObjectOfType<Map4BossHUDController>();
#endif
    }

    void HandleTargetInsideMeleeRange()
    {
        StopMove();

        if (CanStartMeleeAttack())
        {
            StartMeleeAttack();
        }
        else
        {
            HoldIdle();
        }
    }

    bool CanStartMeleeAttack()
    {
        if (isDefeated) return false;
        if (!activated) return false;
        if (isAttacking) return false;
        if (isUsingUltimate) return false;
        if (currentCombatTarget == null) return false;
        if (meleeTimer > 0f) return false;

        return true;
    }

    void HoldIdle()
    {
        StopMove();
        UpdateAnimation(0f);
    }

    public Transform GetLockedMeleeTarget()
    {
        return lockedMeleeTarget;
    }

    public Vector2 GetBossFacingDirection()
    {
        if (spriteRenderer == null)
        {
            return transform.localScale.x < 0f ? Vector2.left : Vector2.right;
        }

        return spriteRenderer.flipX ? Vector2.left : Vector2.right;
    }

    void CacheTargetHealth()
    {
        if (targetHealth == null && target != null)
        {
            targetHealth = target.GetComponent<PlayerHealth>();
        }
    }

    bool IsWukongDead()
    {
        if (targetHealth == null) return false;
        return targetHealth.currentHealth <= 0;
    }

    void StopBossBecauseWukongDead()
    {
        StopMove();

        isAttacking = false;
        isUsingUltimate = false;
        lockedMeleeTarget = null;

        SetMovementLock(false);

        if (actionCoroutine != null)
        {
            StopCoroutine(actionCoroutine);
            actionCoroutine = null;
        }

        CloseMeleeHitbox();

        if (!hasStoppedBecauseWukongDead)
        {
            hasStoppedBecauseWukongDead = true;
            ForceIdleState(true);
        }
        else
        {
            MaintainIdleAfterWukongDead();
        }
    }

    void MaintainIdleAfterWukongDead()
    {
        StopMove();

        if (animator != null)
        {
            animator.SetFloat(speedParameterName, 0f);
            animator.ResetTrigger(meleeTriggerName);
            animator.ResetTrigger(ultimateTriggerName);
        }
    }

    void FindTargetIfNeeded()
    {
        if (target != null) return;

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
        {
            target = playerObject.transform;
            targetHealth = playerObject.GetComponent<PlayerHealth>();
        }
    }

    void CheckActivation()
    {
        if (target == null) return;

        float distance = Vector2.Distance(transform.position, target.position);

        if (distance <= activationRange)
        {
            activated = true;
        }
    }

    void UpdateTimers()
    {
        if (meleeTimer > 0f)
        {
            meleeTimer -= Time.deltaTime;
        }

        if (ultimateTimerStarted && ultimateTimer > 0f)
        {
            ultimateTimer -= Time.deltaTime;
        }
    }

    Transform GetBestCombatTarget()
    {
        Transform bestTarget = target;

        if (!canAttackPartyIfCloser)
        {
            return bestTarget;
        }

        Transform closestParty = GetClosestPartyTarget();

        if (closestParty == null)
        {
            return bestTarget;
        }

        if (target == null)
        {
            return closestParty;
        }

        float distanceToWukong = Vector2.Distance(transform.position, target.position);
        float distanceToParty = Vector2.Distance(transform.position, closestParty.position);

        if (distanceToWukong <= wukongReclaimDistance)
        {
            return target;
        }

        if (distanceToParty < distanceToWukong && distanceToParty <= partyDetectRange)
        {
            return closestParty;
        }

        return bestTarget;
    }

    Transform GetClosestPartyTarget()
    {
        GameObject[] partyMembers = GameObject.FindGameObjectsWithTag(partyTag);

        Transform closest = null;
        float closestDistance = Mathf.Infinity;

        for (int i = 0; i < partyMembers.Length; i++)
        {
            if (partyMembers[i] == null) continue;

            float distance = Vector2.Distance(transform.position, partyMembers[i].transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = partyMembers[i].transform;
            }
        }

        return closest;
    }

    bool CanUseUltimate()
    {
        if (isDefeated) return false;
        if (!activated) return false;
        if (isAttacking) return false;
        if (isUsingUltimate) return false;
        if (target == null) return false;
        if (ultimateProjectilePrefab == null) return false;
        if (ultimateFirePoint == null) return false;
        if (!ultimateTimerStarted) return false;
        if (ultimateTimer > 0f) return false;
        if (IsWukongDead()) return false;

        float distanceToWukong = Vector2.Distance(transform.position, target.position);

        if (useUltimateDistanceCondition)
        {
            if (distanceToWukong < minUltimateDistanceToWukong)
            {
                return false;
            }

            if (distanceToWukong > maxUltimateDistanceToWukong)
            {
                return false;
            }
        }

        return true;
    }

    void ChaseTarget(Transform chaseTarget)
    {
        if (chaseTarget == null)
        {
            HoldIdle();
            return;
        }

        if (!chasePartyIfCloser && chaseTarget != target)
        {
            HoldIdle();
            return;
        }

        if (lockMovementWhileUsingSkill && (isAttacking || isUsingUltimate))
        {
            StopMove();
            return;
        }

        Vector2 direction = (chaseTarget.position - transform.position).normalized;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            transform.position += new Vector3(direction.x, 0f, 0f) * moveSpeed * Time.deltaTime;
        }

        UpdateAnimation(Mathf.Abs(direction.x));
    }

    void StartMeleeAttack()
    {
        if (actionCoroutine != null)
        {
            StopCoroutine(actionCoroutine);
        }

        lockedMeleeTarget = currentCombatTarget;
        actionCoroutine = StartCoroutine(MeleeAttackRoutine());
    }

    IEnumerator MeleeAttackRoutine()
    {
        isAttacking = true;
        StopMove();

        SetMovementLock(true);

        if (lockedMeleeTarget != null)
        {
            FaceTransform(lockedMeleeTarget);
        }

        if (animator != null)
        {
            animator.ResetTrigger(ultimateTriggerName);
            animator.SetFloat(speedParameterName, 0f);
            animator.SetTrigger(meleeTriggerName);
        }

        if (!ultimateTimerStarted)
        {
            ultimateTimerStarted = true;
            ultimateTimer = ultimateCooldown;
        }

        float elapsed = 0f;

        while (elapsed < meleeActionDuration)
        {
            StopMove();
            MaintainLockedWorldPosition();

            elapsed += Time.deltaTime;
            yield return null;
        }

        CloseMeleeHitbox();

        meleeTimer = meleeCooldown;

        isAttacking = false;
        lockedMeleeTarget = null;

        SetMovementLock(false);
        ForceIdleState(false);

        actionCoroutine = null;
    }

    void StartUltimateAttack()
    {
        if (actionCoroutine != null)
        {
            StopCoroutine(actionCoroutine);
        }

        actionCoroutine = StartCoroutine(UltimateAttackRoutine());
    }

    IEnumerator UltimateAttackRoutine()
    {
        isUsingUltimate = true;
        ultimateProjectileFired = false;
        ultimateAnimationEnded = false;

        StopMove();
        SetMovementLock(true);

        FaceTransform(target);
        ForceIdleState(false);

        float preElapsed = 0f;

        while (preElapsed < preUltimateIdleDelay)
        {
            StopMove();
            MaintainLockedWorldPosition();
            FaceTransform(target);

            preElapsed += Time.deltaTime;
            yield return null;
        }

        if (animator != null)
        {
            animator.ResetTrigger(meleeTriggerName);
            animator.SetFloat(speedParameterName, 0f);
            animator.SetTrigger(ultimateTriggerName);
        }

        float elapsed = 0f;
        bool fallbackFired = false;

        while (!ultimateAnimationEnded)
        {
            StopMove();
            MaintainLockedWorldPosition();

            elapsed += Time.deltaTime;

            if (!useAnimationEventForUltimateFire && !fallbackFired && elapsed >= ultimateFireDelay)
            {
                fallbackFired = true;
                FireUltimateProjectile();
            }

            if (guaranteeUltimateProjectileSpawn && !ultimateProjectileFired && elapsed >= guaranteedUltimateFireTime)
            {
                FireUltimateProjectile();
            }

            if (ultimateActionDuration > 0f && elapsed >= ultimateActionDuration + 2f)
            {
                ultimateAnimationEnded = true;
            }

            yield return null;
        }

        yield return StartCoroutine(EnterPostUltimateIdle());

        FinishUltimate();

        actionCoroutine = null;
    }

    IEnumerator EnterPostUltimateIdle()
    {
        ForceIdleState(false);

        float elapsed = 0f;

        while (elapsed < postUltimateIdleDelay)
        {
            StopMove();
            MaintainLockedWorldPosition();

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    void FinishUltimate()
    {
        ultimateTimer = ultimateCooldown;
        ultimateTimerStarted = true;

        isUsingUltimate = false;
        ultimateProjectileFired = false;
        ultimateAnimationEnded = false;

        SetMovementLock(false);
        ForceIdleState(false);
    }

    public void EndUltimateAnimation()
    {
        ultimateAnimationEnded = true;
    }

    public void OpenMeleeHitbox()
    {
        if (meleeHitbox == null) return;

        meleeHitbox.damage = meleeDamage;
        meleeHitbox.ActivateHitbox(lockedMeleeTarget);
    }

    public void CloseMeleeHitbox()
    {
        if (meleeHitbox == null) return;

        meleeHitbox.DeactivateHitbox();
    }

    public void FireUltimateProjectile()
    {
        if (isDefeated) return;
        if (!isUsingUltimate) return;
        if (ultimateProjectileFired) return;
        if (ultimateProjectilePrefab == null) return;
        if (ultimateFirePoint == null) return;

        UpdateUltimateFirePointPosition();

        ultimateProjectileFired = true;

        Vector2 shootDirection = GetBossFacingDirection();

        GameObject projectileObject = Instantiate(
            ultimateProjectilePrefab,
            ultimateFirePoint.position,
            Quaternion.identity
        );

        Boss4UltimateProjectile projectile = projectileObject.GetComponent<Boss4UltimateProjectile>();

        if (projectile != null)
        {
            projectile.Init(shootDirection, ultimateDamage, transform, ultimateFirePoint);
        }
    }

    void SetMovementLock(bool locked)
    {
        if (!lockMovementWhileUsingSkill)
        {
            return;
        }

        if (locked)
        {
            LockCurrentWorldPosition();
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

        StopMove();
    }

    void MaintainLockedWorldPosition()
    {
        if (!lockMovementWhileUsingSkill) return;
        if (!freezeWorldPositionWhileUsingSkill) return;
        if (!hasLockedWorldPosition) return;
        if (!isAttacking && !isUsingUltimate) return;

        transform.position = lockedWorldPosition;
        StopMove();
    }

    void UpdateUltimateFirePointPosition()
    {
        if (!autoUpdateUltimateFirePoint) return;
        if (ultimateFirePoint == null) return;

        Vector2 facingDirection = GetBossFacingDirection();

        float xOffset = Mathf.Abs(ultimateFirePointLocalOffset.x) * facingDirection.x;
        float yOffset = ultimateFirePointLocalOffset.y;

        Vector3 basePosition = transform.position;

        if (spriteRenderer != null)
        {
            basePosition = spriteRenderer.bounds.center;
        }

        Vector3 spawnPosition = basePosition + new Vector3(xOffset, yOffset, 0f);

        ultimateFirePoint.position = spawnPosition;
    }
    public void TakeDamage(int damageAmount)
    {
        if (isDefeated) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateBossHUD();

        if (currentHealth <= 0)
        {
            DefeatToIdle();
        }
    }

    public void Heal(int healAmount)
    {
        if (isDefeated) return;

        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateBossHUD();
    }

    void DefeatToIdle()
    {
        isDefeated = true;

        StopMove();

        isAttacking = false;
        isUsingUltimate = false;
        lockedMeleeTarget = null;

        if (actionCoroutine != null)
        {
            StopCoroutine(actionCoroutine);
            actionCoroutine = null;
        }

        CloseMeleeHitbox();
        SetMovementLock(false);
        ForceIdleState(true);

        UpdateBossHUD();
    }

    void ForceIdleState(bool restartIdle)
    {
        StopMove();

        if (animator == null) return;

        animator.ResetTrigger(meleeTriggerName);
        animator.ResetTrigger(ultimateTriggerName);
        animator.SetFloat(speedParameterName, 0f);

        if (string.IsNullOrEmpty(idleStateName)) return;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);

        bool isIdle = currentState.IsName(idleStateName);
        bool isTransitioning = animator.IsInTransition(0);

        if (isIdle || isTransitioning)
        {
            return;
        }

        animator.CrossFade(idleStateName, 0.05f, 0);
    }

    void UpdateBossHUD()
    {
        FindBossHUDIfNeeded();

        if (map4BossHUD == null)
        {
            Debug.LogWarning(bossName + " chưa gán Map4BossHUDController.");
            return;
        }

        if (bossId == 3)
        {
            map4BossHUD.SetBoss3Health(currentHealth, maxHealth);
        }
        else if (bossId == 4)
        {
            map4BossHUD.SetBoss4Health(currentHealth, maxHealth);
        }
        else
        {
            Debug.LogWarning(bossName + " sai bossId. Chỉ dùng bossId = 3 hoặc 4.");
        }
    }

    void FaceTransform(Transform lookTarget)
    {
        if (lookTarget == null) return;

        float directionX = lookTarget.position.x - transform.position.x;

        if (Mathf.Abs(directionX) < 0.05f) return;

        FaceDirection(directionX);
    }

    void FaceDirection(float directionX)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = directionX < 0f;
        }
        else
        {
            Vector3 scale = transform.localScale;

            if (directionX < 0f)
            {
                scale.x = -Mathf.Abs(scale.x);
            }
            else
            {
                scale.x = Mathf.Abs(scale.x);
            }

            transform.localScale = scale;
        }
    }

    void StopMove()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        UpdateAnimation(0f);
    }

    void UpdateAnimation(float moveAmount)
    {
        if (animator == null) return;

        animator.SetFloat(speedParameterName, moveAmount);
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

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, partyDetectRange);

        if (useUltimateDistanceCondition)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, minUltimateDistanceToWukong);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, maxUltimateDistanceToWukong);
        }

        DrawUltimateFirePointGizmo();
    }
    void DrawUltimateFirePointGizmo()
    {
        Vector3 basePosition = transform.position;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            basePosition = sr.bounds.center;
        }

        Vector2 facingDirection = Vector2.right;

        if (Application.isPlaying)
        {
            facingDirection = GetBossFacingDirection();
        }
        else
        {
            if (transform.localScale.x < 0f)
            {
                facingDirection = Vector2.left;
            }
            else
            {
                facingDirection = Vector2.right;
            }
        }

        float xOffset = Mathf.Abs(ultimateFirePointLocalOffset.x) * facingDirection.x;
        float yOffset = ultimateFirePointLocalOffset.y;

        Vector3 spawnPosition = basePosition + new Vector3(xOffset, yOffset, 0f);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(spawnPosition, 0.12f);

        Gizmos.color = Color.white;
        Gizmos.DrawLine(basePosition, spawnPosition);

#if UNITY_EDITOR
    UnityEditor.Handles.Label(
        spawnPosition + Vector3.up * 0.25f,
        "Projectile Spawn\nX: " + ultimateFirePointLocalOffset.x + " | Y: " + ultimateFirePointLocalOffset.y
    );
#endif
    }

}